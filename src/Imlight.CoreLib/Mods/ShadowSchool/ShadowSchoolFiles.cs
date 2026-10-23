/*
 * Reading and writing the game's file formats for the Shadow School content.
 *
 * BiND files ("BINd", a uint of serializer flags, then the object): Imcodec's BindSerializer reads them, but its
 * Serialize returns nothing, so they are written here with an ObjectSerializer and the magic put in front. Imcodec
 * writes properties the game's own files leave out at their default values; the format names every property, so the
 * game reads both the same way.
 *
 * .lang files (Locale/<locale>/<Name>.lang): UTF-16 with a BOM; the first line is "1:<Name>", then for each string
 * three lines: the key, an empty line, the text. The game finds the text of "<Name>_<key>" in <Name>.lang.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Imcodec.ObjectProperty;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal static class ShadowSchoolFiles {

    private const PropertyFlags FILE_PROPERTIES = (PropertyFlags) 7;
    private static readonly byte[] s_bindMagic = "BINd"u8.ToArray();

    internal static T ReadBind<T>(byte[] file, string path) where T : PropertyClass
        => new BindSerializer().Deserialize<T>(file, FILE_PROPERTIES, out var obj) && obj is not null
            ? obj
            : throw new InvalidOperationException($"Could not read {path} as {typeof(T).Name}.");

    /// <summary>Writes a BiND file with the serializer flags of the file it was made from (7, or 15 for compressed).</summary>
    internal static byte[] WriteBind(PropertyClass obj, uint serializerFlags) {
        var serializer = new ObjectSerializer(true, (SerializerFlags) serializerFlags, null);
        if (!serializer.Serialize(obj, FILE_PROPERTIES, out var body)) {
            throw new InvalidOperationException($"Could not write {obj.GetType().Name}.");
        }

        return [.. s_bindMagic, .. (byte[]) body];
    }

    internal static uint BindFlags(byte[] file) => BitConverter.ToUInt32(file, 4);

    /// <summary>A deep copy, made by writing and reading the object.</summary>
    internal static T Clone<T>(T obj) where T : PropertyClass {
        var bytes = WriteBind(obj, 7);

        return new BindSerializer().Deserialize<PropertyClass>(bytes, FILE_PROPERTIES, out var copy) && copy is T typed
            ? typed
            : throw new InvalidOperationException($"Could not copy {obj.GetType().Name}.");
    }

    internal static List<(string Key, string Text)> ReadLang(byte[] file, out string name) {
        var text = Encoding.Unicode.GetString(file);
        if (text.Length > 0 && text[0] == '﻿') {
            text = text[1..];
        }

        var lines = text.Split("\r\n");
        name = lines[0].Contains(':') ? lines[0][(lines[0].IndexOf(':') + 1)..] : lines[0];
        var entries = new List<(string, string)>();
        for (var i = 1; i + 2 < lines.Length; i += 3) {
            entries.Add((lines[i], lines[i + 2]));
        }

        return entries;
    }

    internal static byte[] WriteLang(string name, IEnumerable<(string Key, string Text)> entries) {
        var sb = new StringBuilder();
        sb.Append('﻿').Append("1:").Append(name).Append("\r\n");
        foreach (var (key, text) in entries) {
            sb.Append(key).Append("\r\n\r\n").Append(text).Append("\r\n");
        }

        return Encoding.Unicode.GetBytes(sb.ToString());
    }

    /// <summary>A lang file with <paramref name="added"/> put after its own strings (a key it already has is replaced).</summary>
    internal static byte[] ExtendLang(byte[] file, IReadOnlyList<(string Key, string Text)> added) {
        var entries = ReadLang(file, out var name);
        var keys = added.Select(a => a.Key).ToHashSet(StringComparer.Ordinal);
        entries.RemoveAll(e => keys.Contains(e.Key));
        entries.AddRange(added);

        return WriteLang(name, entries);
    }

}
