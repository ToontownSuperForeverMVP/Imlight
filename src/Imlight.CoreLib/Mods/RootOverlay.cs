/*
 * Lays the enabled mods' Root.wad files over the game's Root.wad when the server loads it (RootArchiveLoader).
 *
 * Every mod builds its files from the game's own Root.wad, so the result only depends on the game revision and the
 * mod. They are also written to <mod>/generated/root/ with generated/manifest.json (path, size, SHA-256, and the
 * SHA-256 of the game's file it replaces), which the mod's client installer uses to put the very same files into the
 * game client's Root.wad. The game client and the server must agree on these files (new templates, GUI, text).
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Imcodec.Wad;
using Imlight.Common;
using Imlight.CoreLib.Shared.Resources;

namespace Imlight.CoreLib.Mods;

internal static class RootOverlay {

    internal const string GENERATED_FOLDER = "generated";
    internal const string MANIFEST_FILE = "manifest.json";

    private static readonly JsonSerializerOptions s_json = new() { WriteIndented = true };

    /// <summary>The paths each enabled mod put in Root.wad, by mod name.</summary>
    internal static IReadOnlyDictionary<string, IReadOnlyCollection<string>> FilesByMod { get; private set; }
        = new Dictionary<string, IReadOnlyCollection<string>>();

    internal sealed record ManifestFile(string Path, long Size, string Sha256, string ReplacesSha256);

    internal sealed record Manifest(string Mod, string GameRevision, string RootWadSha256Note, List<ManifestFile> Files);

    /// <summary>The game's Root.wad with the enabled mods' files laid over it, or the archive itself without mods.</summary>
    internal static Archive Apply(Archive root) {
        if (root is null || ModRegistry.Enabled.Count == 0) {
            return root;
        }

        var all = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        var byMod = new Dictionary<string, IReadOnlyCollection<string>>();
        foreach (var mod in ModRegistry.Enabled) {
            var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
            try {
                mod.BuildRootFiles(root, files);
            }
            catch (Exception ex) {
                // A half-built mod would leave the server and the client disagreeing; leave the mod's files out.
                Logger.Error("Mod {0} could not build its Root.wad files, so they are left out: {1}",
                    Logger.Args(mod.Name, ex));

                continue;
            }

            foreach (var (path, data) in files) {
                if (all.ContainsKey(path)) {
                    Logger.Warning("Mods change the same Root.wad file {0}; {1}'s version is used.", Logger.Args(path, mod.Name));
                }

                all[path] = data;
            }

            byMod[mod.Name] = files.Keys.ToList();
            WriteGenerated(mod, root, files);
        }

        FilesByMod = byMod;
        if (all.Count == 0) {
            return root;
        }

        var merged = Merge(root, all);
        Logger.Information("Mods put {0} files in Root.wad.", Logger.Args(all.Count));

        return merged;
    }

    private static Archive Merge(Archive root, IDictionary<string, byte[]> overlay) {
        var stream = root.GetData();
        var header = new byte[9];
        stream.ReadExactly(header);
        var version = BitConverter.ToUInt32(header, 5);

        var entries = root.Files.ToDictionary(f => f.Key, f => f.Value.Value, StringComparer.Ordinal);
        stream.Seek(0, SeekOrigin.End);
        foreach (var (path, data) in overlay) {
            var offset = stream.Position;
            if (offset + data.Length > uint.MaxValue) {
                throw new InvalidOperationException("Root.wad with the mods' files is too large.");
            }

            stream.Write(data);
            entries[path] = new FileEntry {
                FileName = path,
                Offset = (uint) offset,
                UncompressedSize = (uint) data.Length,
                CompressedSize = 0,
                IsCompressed = false,
                Crc32 = WadCrc(data),
            };
        }

        stream.Seek(0, SeekOrigin.Begin);

        return new Archive(entries, stream, version);
    }

    /// <summary>The CRC a KIWAD entry stores: CRC-32 (reflected, polynomial EDB88320) with no initial or final XOR.</summary>
    internal static uint WadCrc(ReadOnlySpan<byte> data) {
        uint crc = 0;
        foreach (var b in data) {
            crc ^= b;
            for (var i = 0; i < 8; i++) {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return crc;
    }

    private static void WriteGenerated(IMod mod, Archive root, IDictionary<string, byte[]> files) {
        var folder = Path.Combine(ModRegistry.FolderOf(mod), GENERATED_FOLDER);
        var rootFolder = Path.Combine(folder, "root");
        try {
            if (Directory.Exists(rootFolder)) {
                Directory.Delete(rootFolder, recursive: true);
            }

            var manifest = new Manifest(mod.Name, ConfigurationManager.GetValue("Global Settings.GameRevision", ""),
                "ReplacesSha256 is the SHA-256 of the game's own file at that path (empty for a new file).", []);
            foreach (var (path, data) in files) {
                var target = Path.Combine(rootFolder, path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllBytes(target, data);

                var original = root.ContainsFile(path) ? root.OpenFile(path)?.ToArray() : null;
                manifest.Files.Add(new ManifestFile(path, data.Length, Sha(data), original is null ? "" : Sha(original)));
            }

            File.WriteAllText(Path.Combine(folder, MANIFEST_FILE), JsonSerializer.Serialize(manifest, s_json));
        }
        catch (Exception ex) {
            Logger.Error("Could not write the generated files of mod {0} to {1}: {2}", Logger.Args(mod.Name, folder, ex.Message));
        }
    }

    private static string Sha(byte[] data) => Convert.ToHexStringLower(SHA256.HashData(data));

}
