using System.Text.Json;
using Imcodec.ObjectProperty.TypeCache;
using Imlight.Common;
using Imlight.CoreLib.Game.Requirements.Contexts;
using Imlight.CoreLib.Game.Requirements;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.WizardData;
using Imlight.CoreLib.WizardData.Models.Player;
using Xunit;

namespace Imlight.CoreLib.Tests.Mods.ShadowSchool;

[Collection("SharedConfiguration")]
public sealed class ShadowQuestContractTests {

    private static readonly string[] OriginalSchools = ["FIRE", "ICE", "LIFE", "MYTH", "DEATH", "BALANCE", "STORM"];
    private static readonly (string Street, string FinalQuest)[] Streets = [
        ("Triton", "WC-TRITON-MAIN-009"),
        ("Firecat", "WC-FIRECAT-MAIN-007"),
        ("Cyclops", "WC-CYCLOPS-MAIN-005"),
    ];

    [Fact]
    public void SevenVanillaEnrollmentPathsStayUnshadowedAndShadowAddsAnEighthBranch() {
        var modFolder = ModFolder();
        foreach (var school in OriginalSchools) {
            Assert.False(File.Exists(QuestPath(modFolder, $"WC-COMMONS-MAIN-002-{school}")));
        }

        var enrollment = ReadQuest(modFolder, "WC-COMMONS-MAIN-002-SHADOW");
        Assert.Equal("WC-COMMONS-MAIN-002-SHADOW", enrollment.RootElement.GetProperty("m_questName").GetString());
        Assert.Equal("QuestTitle_00001718", enrollment.RootElement.GetProperty("m_questTitle").GetString());

        var prereqs = enrollment.RootElement.GetProperty("m_requirements").GetProperty("m_requirements").EnumerateArray().ToArray();
        Assert.Contains(prereqs, r => r.TryGetProperty("m_questName", out var name)
            && name.GetString() == "WC-UNICORN-MAIN-008"
            && r.GetProperty("m_entryName").GetString() == "Complete");
        Assert.Contains(prereqs, r => r.TryGetProperty("m_magicSchoolName", out var school)
            && school.GetString() == "Shadow");

        var goals = enrollment.RootElement.GetProperty("m_goals").EnumerateArray().ToArray();
        Assert.Equal(["Goal 1", "Goal 2", "Goal 3", "Goal 4"], goals.Select(g => g.GetProperty("m_goalName").GetString()));
        Assert.Equal("WC-HUB-NPC08-B", goals[0].GetProperty("m_personaName").GetString());
        var ambroseTeleport = goals[0].GetProperty("m_completeResults").GetProperty("m_results")[0];
        Assert.Equal("WizardCity/Interiors/WC_LibraryBookStacks", ambroseTeleport.GetProperty("m_destinationZone").GetString());
        Assert.Equal("6400,-1104,0,1.65", ambroseTeleport.GetProperty("m_destinationLoc").GetString());
        Assert.Equal("ShadowSchool-Tutor", goals[1].GetProperty("m_personaName").GetString());
        Assert.Equal("WizardCity/Interiors/WC_LibraryBookStacks", goals[1].GetProperty("m_destinationZone").GetString());
        Assert.Equal("ShadowSchool_EnrollmentLocation", goals[1].GetProperty("m_locationName").GetString());
        Assert.Empty(goals[1].GetProperty("m_clientTags").EnumerateArray());
        Assert.Equal("GUI/NpcPortraits/Portrait_VampireHunterF_A_NPC.dds", goals[1].GetProperty("m_displayImage1").GetString());
        var tutorDialog = goals[1].GetProperty("m_dialogList").GetProperty("m_dialogs")[0].GetProperty("m_dialogEntries")[0];
        Assert.Equal("GUI/NpcPortraits/Portrait_VampireHunterF_A_NPC.dds", tutorDialog.GetProperty("m_picture").GetString());
        Assert.Equal(1880000100, tutorDialog.GetProperty("m_actorTemplateID").GetInt32());
        Assert.Equal(string.Empty, tutorDialog.GetProperty("m_cameraName").GetString());
        Assert.Equal("WizardCity/Interiors/WC_LibraryBookStacks", tutorDialog.GetProperty("m_cameraZoneName").GetString());
        Assert.Equal("WC-GTW-Registrar", goals[2].GetProperty("m_personaName").GetString());
        Assert.Equal("WC-HUB-NPC01", goals[3].GetProperty("m_personaName").GetString());
        Assert.Equal(4, enrollment.RootElement.GetProperty("m_goalLogic").GetArrayLength());

        // The installed paths use this shared reward table; the Shadow clone retains it.
        Assert.Equal("WC-COMMONS-MAIN-002-STORM", enrollment.RootElement.GetProperty("m_endResults")
            .GetProperty("m_results")[0].GetProperty("m_tableName").GetString());
        Assert.Equal("ShadowSchool_EnrollmentTutorIntro", goals[1].GetProperty("m_dialogList")
            .GetProperty("m_dialogs")[0].GetProperty("m_dialogEntries")[0].GetProperty("m_dialog").GetString());
        var schoolStrings = ReadShadowStrings(modFolder);
        var registrationEntries = goals[0].GetProperty("m_dialogList").GetProperty("m_dialogs")[0]
            .GetProperty("m_dialogEntries").EnumerateArray().ToArray();
        var stormEntries = registrationEntries.Where(e => e.GetProperty("m_personaName").GetString() == "WC-RAV-NPC05-B_Persona").ToArray();
        Assert.Single(stormEntries);
        Assert.Equal("WizQst1628CB_00000008", stormEntries[0].GetProperty("m_dialog").GetString());
        var referral = Assert.Single(registrationEntries.Where(e => e.GetProperty("m_dialog").GetString() == "ShadowSchool_EnrollmentLincolnReferral"));
        Assert.Equal("WC-HUB-NPC08-B_Persona", referral.GetProperty("m_personaName").GetString());
        Assert.Equal("GUI/NpcPortraits/Art_Portrait_Registrar.dds", referral.GetProperty("m_picture").GetString());
        Assert.Contains("Vespera Quillmoor", schoolStrings["EnrollmentLincolnReferral"], StringComparison.Ordinal);
        Assert.Contains("Library Book Stacks", schoolStrings["EnrollmentLincolnReferral"], StringComparison.Ordinal);
        Assert.Equal("Wizard City|Library Book Stacks", schoolStrings["EnrollmentLocation"]);
    }

    [Fact]
    public void ThreeStreetsJoinRetainsAllSevenOriginalPredecessorsAndAddsShadow() {
        using var quest = ReadQuest(ModFolder(), "WC-COMMONS-MAIN-003");
        Assert.Equal("WC-COMMONS-MAIN-003", quest.RootElement.GetProperty("m_questName").GetString());
        Assert.Equal("QuestTitle_1625D8", quest.RootElement.GetProperty("m_questTitle").GetString());
        Assert.Equal(1, quest.RootElement.GetProperty("m_goals").GetArrayLength());
        Assert.Equal("WC-ST02-NPC01", quest.RootElement.GetProperty("m_goals")[0].GetProperty("m_personaName").GetString());

        var predecessors = quest.RootElement.GetProperty("m_requirements").GetProperty("m_requirements").EnumerateArray().ToArray();
        var expected = OriginalSchools.Select(s => $"WC-COMMONS-MAIN-002-{s}")
            .Append("WC-COMMONS-MAIN-002-SHADOW").ToHashSet(StringComparer.Ordinal);
        Assert.Equal(expected, predecessors.Select(r => r.GetProperty("m_questName").GetString()!).ToHashSet(StringComparer.Ordinal));
        Assert.All(predecessors, r => Assert.Equal("ROP_OR", r.GetProperty("m_operator").GetString()));
    }

    [Fact]
    public void StreetCheckInsAreIndependentShadowOnlyAndMentionOnlyTheirCompletedStreet() {
        var modFolder = ModFolder();
        var strings = ReadShadowStrings(modFolder);
        foreach (var (street, finalQuest) in Streets) {
            using var quest = ReadQuest(modFolder, $"ShadowSchool-CheckIn-{street}");
            Assert.Equal($"ShadowSchool-CheckIn-{street}", quest.RootElement.GetProperty("m_questName").GetString());
            Assert.False(quest.RootElement.GetProperty("m_mainline").GetBoolean());
            Assert.Empty(quest.RootElement.GetProperty("m_endResults").GetProperty("m_results").EnumerateArray());

            var requirements = quest.RootElement.GetProperty("m_requirements").GetProperty("m_requirements").EnumerateArray().ToArray();
            Assert.Contains(requirements, r => r.TryGetProperty("m_magicSchoolName", out var school) && school.GetString() == "Shadow");
            Assert.Contains(requirements, r => r.TryGetProperty("m_questName", out var name) && name.GetString() == finalQuest
                && r.GetProperty("m_entryName").GetString() == "Complete");
            Assert.Equal(1, quest.RootElement.GetProperty("m_goals").GetArrayLength());
            var goal = quest.RootElement.GetProperty("m_goals")[0];
            Assert.Equal("ShadowSchool-Tutor", goal.GetProperty("m_personaName").GetString());
            Assert.Equal("WizardCity/Interiors/WC_LibraryBookStacks", goal.GetProperty("m_destinationZone").GetString());
            Assert.Equal("GUI/NpcPortraits/Portrait_VampireHunterF_A_NPC.dds", goal.GetProperty("m_displayImage1").GetString());
            var dialog = goal.GetProperty("m_dialogList").GetProperty("m_dialogs")[0].GetProperty("m_dialogEntries")[0];
            Assert.Equal("ShadowSchool-Tutor_Persona", dialog.GetProperty("m_personaName").GetString());
            Assert.Equal("GUI/NpcPortraits/Portrait_VampireHunterF_A_NPC.dds", dialog.GetProperty("m_picture").GetString());
            Assert.Equal(1880000100, dialog.GetProperty("m_actorTemplateID").GetInt32());
            Assert.Equal(string.Empty, dialog.GetProperty("m_cameraName").GetString());
            Assert.Equal("WizardCity/Interiors/WC_LibraryBookStacks", dialog.GetProperty("m_cameraZoneName").GetString());
            Assert.Equal(string.Empty, dialog.GetProperty("m_soundFile").GetString());

            var reactionKey = $"{street}CheckInReaction";
            Assert.True(strings.TryGetValue(reactionKey, out var reaction));
            foreach (var other in Streets.Select(s => s.Street).Where(s => s != street)) {
                Assert.DoesNotContain(other + " Avenue", reaction!, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(other + " Lane", reaction!, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(other + " Alley", reaction!, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void EveryAddedQuestDeserializesWithTheRuntimeSpiralDbSerializer() {
        var modFolder = ModFolder();
        var names = new[] {
            "WC-COMMONS-MAIN-002-SHADOW",
            "WC-COMMONS-MAIN-003",
            "ShadowSchool-CheckIn-Triton",
            "ShadowSchool-CheckIn-Firecat",
            "ShadowSchool-CheckIn-Cyclops",
        };

        foreach (var name in names) {
            var path = QuestPath(modFolder, name);
            var template = JsonSerializer.Deserialize<QuestTemplate>(File.ReadAllText(path), SpiralJsonOptions.Options);
            Assert.NotNull(template);
            Assert.Equal(name, template.m_questName);
        }
    }

    [Fact]
    public void StreetPresentationRequirementsReadOnlyTheInteractingCharactersState() {
        InitializeIsolatedTestConfiguration();
        var modFolder = ModFolder();
        foreach (var (street, finalQuest) in Streets) {
            var shadowWithStreetComplete = NewWizard(MagicSchool.Shadow);
            var shadowWithoutStreetComplete = NewWizard(MagicSchool.Shadow);
            var otherSchoolWithSameFlag = NewWizard(MagicSchool.Fire);
            shadowWithStreetComplete.QuestBehavior.SetQuestRegistryValue(finalQuest, "Complete", 1);
            otherSchoolWithSameFlag.QuestBehavior.SetQuestRegistryValue(finalQuest, "Complete", 1);

            using var checkIn = ReadQuest(modFolder, $"ShadowSchool-CheckIn-{street}");
            Assert.True(CheckInAvailable(checkIn, street, shadowWithStreetComplete));
            Assert.False(CheckInAvailable(checkIn, street, shadowWithoutStreetComplete));
            Assert.False(CheckInAvailable(checkIn, street, otherSchoolWithSameFlag));
        }
    }

    [Fact]
    public void AllSixStreetCompletionOrdersUnlockOnlyTheMatchingCheckIn() {
        InitializeIsolatedTestConfiguration();
        var modFolder = ModFolder();
        var checkIns = Streets.ToDictionary(s => s.Street, s => ReadQuest(modFolder, $"ShadowSchool-CheckIn-{s.Street}"));
        foreach (var order in Permutations(Streets.Select(s => s.Street).ToArray())) {
            var wizard = NewWizard(MagicSchool.Shadow);
            var completed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var street in order) {
                foreach (var (candidate, _) in Streets) {
                    var candidateQuest = Streets.Single(s => s.Street == candidate).FinalQuest;
                    Assert.Equal(completed.Contains(candidateQuest), CheckInAvailable(checkIns[candidate], candidate, wizard));
                }

                var finalQuest = Streets.Single(s => s.Street == street).FinalQuest;
                wizard.QuestBehavior.SetQuestRegistryValue(finalQuest, "Complete", 1);
                completed.Add(finalQuest);

                foreach (var (candidate, _) in Streets) {
                    var candidateFinalQuest = Streets.Single(s => s.Street == candidate).FinalQuest;
                    Assert.Equal(completed.Contains(candidateFinalQuest), CheckInAvailable(checkIns[candidate], candidate, wizard));
                }
            }
        }

        foreach (var quest in checkIns.Values) quest.Dispose();
    }

    private static bool CheckInAvailable(JsonDocument quest, string name, Wizard wizard) {
        var requirements = JsonSerializer.Deserialize<RequirementList>(
            quest.RootElement.GetProperty("m_requirements").GetRawText(), SpiralJsonOptions.Options)!;
        return RequirementDispatcher.EvaluateRequirements(
            requirements,
            new QuestRequirementContext(requirements, null!, null!, wizard, $"ShadowSchool-CheckIn-{name}"));
    }

    private static IEnumerable<string[]> Permutations(string[] values) {
        if (values.Length == 1) {
            yield return [values[0]];
            yield break;
        }

        for (var i = 0; i < values.Length; i++) {
            var head = values[i];
            var rest = values.Where((_, index) => index != i).ToArray();
            foreach (var tail in Permutations(rest)) {
                yield return [head, .. tail];
            }
        }
    }

    private static Dictionary<string, string> ReadShadowStrings(string modFolder) {
        var path = Path.Combine(modFolder, "text", "en-US.txt");
        var inSection = false;
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadLines(path)) {
            if (line.Trim() is "[ShadowSchool]") { inSection = true; continue; }
            if (line.TrimStart().StartsWith('[')) { inSection = false; continue; }
            if (!inSection || line.TrimStart().StartsWith('#')) continue;
            var split = line.IndexOf(" = ", StringComparison.Ordinal);
            if (split > 0) values[line[..split].Trim()] = line[(split + 3)..].Trim();
        }

        return values;
    }

    private static JsonDocument ReadQuest(string modFolder, string questName)
        => JsonDocument.Parse(File.ReadAllText(QuestPath(modFolder, questName)));

    private static Wizard NewWizard(MagicSchool school) => new() {
        MagicSchoolBehavior = new ServerMagicSchoolBehavior { MagicSchool = school, Level = 1 },
        QuestBehavior = new ServerQuestBehavior(),
    };

    private static void InitializeIsolatedTestConfiguration() {
        var path = Path.Combine(Path.GetTempPath(), $"imlight-shadow-quest-tests-{Environment.ProcessId}.ini");
        File.WriteAllText(path, $"[Logging]{Environment.NewLine}LogPath = {Path.Combine(Path.GetTempPath(), "imlight-shadow-quest-tests.log")}{Environment.NewLine}LogLevel = ERROR{Environment.NewLine}SeqSinkUrl = http://127.0.0.1:1{Environment.NewLine}");
        ConfigurationManager.Initialize(path);
        File.Delete(path);
    }

    private static string QuestPath(string modFolder, string questName)
        => Path.Combine(modFolder, "spiraldb", "QuestTemplates", $"questtemplates_{questName}.json");

    private static string ModFolder() {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Imlight.Director", "mods", "ShadowSchool"))) {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new InvalidOperationException("Could not find src/Imlight.Director/mods/ShadowSchool above the test folder.")
            : Path.Combine(dir.FullName, "src", "Imlight.Director", "mods", "ShadowSchool");
    }
}
