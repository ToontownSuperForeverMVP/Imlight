/*
 * The mod's strings, from <mod>/text/en-US.txt:
 *
 *   # a comment
 *   [ShadowSchool]          the lang file the keys below go in (ShadowSchool.lang is the mod's own)
 *   Spell_DarkSprite = Dark Sprite
 *
 * The text is kept as written, so the game's own markup works in it: \n for a new line, $...$ tokens in spell
 * descriptions, <string;Key> and so on. The server also reads these strings (Get), for text it sends itself.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal sealed class ShadowText {

    private readonly Dictionary<string, List<(string Key, string Text)>> _sections = new(StringComparer.Ordinal);

    internal IEnumerable<string> Sections => _sections.Keys;

    internal IReadOnlyList<(string Key, string Text)> Section(string name)
        => _sections.TryGetValue(name, out var list) ? list : [];

    internal static ShadowText Load(string modFolder) {
        var path = Path.Combine(modFolder, "text", "en-US.txt");
        var text = new ShadowText();
        var section = ShadowSchoolContent.LANG_NAME;
        var lineNumber = 0;
        foreach (var raw in File.ReadLines(path)) {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']')) {
                section = line[1..^1].Trim();
                continue;
            }

            var eq = line.IndexOf(" = ", StringComparison.Ordinal);
            if (eq <= 0) {
                throw new FormatException($"{path} line {lineNumber}: expected 'Key = Text'.");
            }

            var key = line[..eq].Trim();
            var value = line[(eq + 3)..].Trim();
            if (!text._sections.TryGetValue(section, out var list)) {
                text._sections[section] = list = [];
            }

            if (list.Any(e => e.Key == key)) {
                throw new FormatException($"{path} line {lineNumber}: {section} {key} is written twice.");
            }

            list.Add((key, value));
        }

        return text;
    }

}
