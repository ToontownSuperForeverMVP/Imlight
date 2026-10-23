/*
 * Mods: content packages that live in their own folder and change the game without touching SpiralDB or the game files.
 *
 * [Mods] in Imlight.ini:
 *   Enabled   the mod names to load, comma-separated (ShadowSchool). Empty: no mods.
 *   Path      the folder holding one folder per mod (default: the mods folder next to the server).
 *
 * A mod folder can hold:
 *   spiraldb/          files in the spiraldb-extra layout (QuestTemplates, NpcSpellInventory, ...), loaded after
 *                      spiraldb-extra, so a mod's quest replaces the quest of the same name.
 *   generated/root/    written by the server: the files the mod adds to or changes in Root.wad (RootOverlay.cs).
 *                      The game client needs the same files; the mod's client installer copies them into the
 *                      client's Root.wad.
 * A mod's code is an IMod registered in s_known below; it gives the Root.wad files it changes.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Imcodec.Wad;
using Imlight.Common;

namespace Imlight.CoreLib.Mods;

internal interface IMod {

    /// <summary>The mod's folder name and the name used in [Mods] Enabled.</summary>
    string Name { get; }

    /// <summary>
    /// Adds the Root.wad files the mod changes or adds to <paramref name="files"/> (path in the archive, contents).
    /// <paramref name="root"/> is the game's own Root.wad, before any mod.
    /// </summary>
    void BuildRootFiles(Archive root, IDictionary<string, byte[]> files);

}

internal static class ModRegistry {

    private static readonly IMod[] s_known = [new ShadowSchool.ShadowSchoolMod()];

    private static readonly Lazy<IReadOnlyList<IMod>> s_enabled = new(LoadEnabled);

    /// <summary>The folder that holds one folder per mod.</summary>
    internal static string ModsPath => Path.GetFullPath(
        ConfigurationManager.GetValue("Mods.Path", "") is { Length: > 0 } configured
            ? configured
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods"));

    internal static IReadOnlyList<IMod> Enabled => s_enabled.Value;

    internal static bool IsEnabled(string name) => Enabled.Any(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    internal static string FolderOf(IMod mod) => Path.Combine(ModsPath, mod.Name);

    /// <summary>The spiraldb folders of the enabled mods that have one, in [Mods] Enabled order.</summary>
    internal static IEnumerable<string> SpiralDbFolders
        => Enabled.Select(m => Path.Combine(FolderOf(m), "spiraldb")).Where(Directory.Exists);

    private static IReadOnlyList<IMod> LoadEnabled() {
        var names = ConfigurationManager.GetValue("Mods.Enabled", "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var enabled = new List<IMod>();
        foreach (var name in names) {
            var mod = s_known.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (mod is null) {
                Logger.Error("[Mods] Enabled names {0}, which is not a known mod. Known: {1}.",
                    Logger.Args(name, string.Join(", ", s_known.Select(m => m.Name))));

                continue;
            }

            if (!Directory.Exists(FolderOf(mod))) {
                Logger.Error("The mod {0} is enabled but its folder {1} does not exist, so it stays off.",
                    Logger.Args(mod.Name, FolderOf(mod)));

                continue;
            }

            enabled.Add(mod);
            Logger.Information("Mod {0} enabled from {1}.", Logger.Args(mod.Name, FolderOf(mod)));
        }

        return enabled;
    }

}
