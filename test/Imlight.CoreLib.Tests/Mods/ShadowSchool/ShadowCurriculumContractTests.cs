using System.Text.Json;
using Imcodec.Cryptography;
using Imlight.CoreLib.Mods.ShadowSchool;
using Xunit;

namespace Imlight.CoreLib.Tests.Mods.ShadowSchool;

/// <summary>
/// The three places that have to agree before a Shadow wizard can train a spell: the curriculum
/// (ShadowCurriculum.cs) that makes the spell template, the tutor's spell list
/// (mods/ShadowSchool/spiraldb/NpcSpellInventory) that offers it at a level, and the tutorial quest that
/// grants the two starting spells. A drift between them is silent on the server and looks like "training
/// does nothing" in the client, so it is pinned here.
/// </summary>
public sealed class ShadowCurriculumContractTests {

    // The agreed level 1-50 curriculum: spell key, level, ordinary pips, shadow pips, accuracy.
    private static readonly (string Key, int Level, byte Pips, byte ShadowPips, int Accuracy)[] Plan = [
        ("DarkSprite", 1, 1, 0, 85),
        ("DarkEmpower", 1, 0, 1, 100),
        ("ShadowTroll", 5, 2, 0, 85),
        ("TowerShield", 8, 0, 0, 100),
        ("ShadowBanshee", 10, 3, 0, 85),
        ("ShadowTrap", 16, 0, 0, 100),
        ("Shadowblade", 18, 0, 0, 100),
        ("Humongofrog", 22, 4, 0, 85),
        ("Helephant", 26, 4, 0, 85),
        ("Shadowspear", 30, 0, 1, 100),
        ("Minotaur", 33, 5, 0, 85),
        ("StormShark", 38, 3, 1, 85),
        ("Triton", 42, 6, 0, 80),
        ("FrostGiant", 50, 7, 1, 80),
    ];

    [Fact]
    public void CurriculumMatchesTheAgreedLevelAndCostTable() {
        var spells = ShadowCurriculum.Spells;

        Assert.Equal(Plan.Length, spells.Length);
        for (var i = 0; i < Plan.Length; i++) {
            var (key, level, pips, shadowPips, accuracy) = Plan[i];
            var spell = spells[i];

            Assert.Equal(key, spell.Key);
            Assert.Equal(level, spell.Level);
            Assert.Equal(pips, spell.Pips);
            Assert.Equal(shadowPips, spell.ShadowPips);
            Assert.Equal(accuracy, spell.Accuracy);
            // A spell's id is the hash of its template name; the tutor's list and the client use the same id.
            Assert.Equal(StringHash.Compute("ShadowSchool " + key), spell.TemplateId);
        }

        Assert.Equal(Plan.Length, spells.Select(s => s.TemplateId).Distinct().Count());
    }

    /// <summary>
    /// The spell each entry of the tutor's list needs first, as an index into the curriculum, or -1 for none.
    /// The attack spells form one chain from Dark Sprite up; the level-1 utility Dark Empower is not a gate,
    /// so Shadow Troll follows Dark Sprite rather than Dark Empower.
    /// </summary>
    private static readonly int[] GateOf = [-1, -1, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

    [Fact]
    public void TutorOffersTheWholeCurriculumInOrder() {
        var entries = ReadTutorInventory();
        var spells = ShadowCurriculum.Spells;

        Assert.Equal(spells.Length, entries.Count);
        Assert.Equal(spells.Length, GateOf.Length);

        for (var i = 0; i < spells.Length; i++) {
            var spell = spells[i];
            var entry = entries[i];

            Assert.Equal(spell.Key, entry.Key);
            Assert.Equal(spell.TemplateId, entry.TemplateId);
            Assert.Equal(spell.Level, entry.Level);
            Assert.Equal(GateOf[i] < 0 ? 0u : spells[GateOf[i]].TemplateId, entry.RequiredSpell);
        }
    }

    [Fact]
    public void TutorialGrantsBothStartingSpellsAndLeavesTheOtherSchoolsAlone() {
        var grants = ReadTutorialSpellGrants();

        var shadow = grants.Where(g => g.School == "Shadow").Select(g => g.TemplateId).OrderBy(id => id).ToArray();
        var expected = ShadowCurriculum.StartingSpells.Select(s => s.TemplateId).OrderBy(id => id).ToArray();

        Assert.Equal(expected, shadow);
        // The tutorial still hands every one of the seven original schools its own spell.
        Assert.Equal(7, grants.Select(g => g.School).Distinct().Count(g => g != "Shadow"));
        Assert.Equal(9, grants.Count);
    }

    // ---------------------------------------------------------------- reading the mod's files

    private static string ModFolder() {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Imlight.Director", "mods", "ShadowSchool"))) {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new InvalidOperationException("Could not find src/Imlight.Director/mods/ShadowSchool above the test folder.")
            : Path.Combine(dir.FullName, "src", "Imlight.Director", "mods", "ShadowSchool");
    }

    private static List<(string Key, uint TemplateId, int Level, uint RequiredSpell)> ReadTutorInventory() {
        var path = Path.Combine(ModFolder(), "spiraldb", "NpcSpellInventory", "NPCSpellInventories_ShadowSchool-Tutor.json");
        using var json = JsonDocument.Parse(File.ReadAllText(path));

        return [.. json.RootElement.GetProperty("Spells").EnumerateArray().Select(e => (
            Key: e.GetProperty("Key").GetString()!,
            TemplateId: e.GetProperty("TemplateID").GetUInt32(),
            Level: e.GetProperty("Level").GetInt32(),
            RequiredSpell: e.GetProperty("RequiredSpellID").GetUInt32()))];
    }

    /// <summary>Every <c>ResLearnSpell</c> the tutorial grants, with the school of focus it is granted to.</summary>
    private static List<(string School, uint TemplateId)> ReadTutorialSpellGrants() {
        var path = Path.Combine(ModFolder(), "spiraldb", "QuestTemplates", "questtemplates_Tutorial_Intro.json");
        using var json = JsonDocument.Parse(File.ReadAllText(path));

        var grants = new List<(string, uint)>();
        Walk(json.RootElement, grants);

        return grants;
    }

    private static void Walk(JsonElement element, List<(string, uint)> grants) {
        switch (element.ValueKind) {
            case JsonValueKind.Object:
                if (element.TryGetProperty("$type", out var type) && type.GetString()?.EndsWith(".ResLearnSpell, Imcodec.ObjectProperty") == true) {
                    var school = element.GetProperty("m_requirements").GetProperty("m_requirements")[0]
                        .GetProperty("m_magicSchool").GetString();
                    if (school is not null) {
                        grants.Add((school, element.GetProperty("m_templateID").GetUInt32()));
                    }
                }

                foreach (var property in element.EnumerateObject()) {
                    Walk(property.Value, grants);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) {
                    Walk(item, grants);
                }

                break;
        }
    }

}
