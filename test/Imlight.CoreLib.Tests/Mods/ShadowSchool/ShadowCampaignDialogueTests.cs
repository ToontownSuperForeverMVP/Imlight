using System.Text.Json;
using Imcodec.ObjectProperty.TypeCache;
using Imlight.Common;
using Imlight.CoreLib.Game.Quests;
using Imlight.CoreLib.Game.Requirements;
using Imlight.CoreLib.Game.Requirements.Contexts;
using Imlight.CoreLib.Mods.ShadowSchool;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.WizardData;
using Imlight.CoreLib.WizardData.Models.Player;
using Imlight.CoreLib.WizardData.Models.World;
using Xunit;

namespace Imlight.CoreLib.Tests.Mods.ShadowSchool;

/// <summary>
/// Stage C2.2: the Shadow versions of the Foulgaze investigation (rest of WC-OLDE-MAIN-003A), Throwing Nightshade
/// (WC-HAUNTED-MAIN-001) and Things and Stuff (WC-HAUNTED-MAIN-002), and the Shadow-only reward rows. The canonical
/// quests and drop tables are read from the installed SpiralDB cache.
/// </summary>
[Collection("SharedConfiguration")]
public sealed class ShadowCampaignDialogueTests {

    private const string Reunion = "WC-OLDE-MAIN-003A";
    private const string Nightshade = "WC-HAUNTED-MAIN-001";
    private const string Things = "WC-HAUNTED-MAIN-002";

    private static readonly MagicSchool[] OriginalSchools = [
        MagicSchool.Fire, MagicSchool.Ice, MagicSchool.Storm, MagicSchool.Myth,
        MagicSchool.Life, MagicSchool.Death, MagicSchool.Balance,
    ];
    private static readonly IQuestDialogueProvider[] Providers = [new ShadowSchoolMod()];

    public enum Stage { Reunion, Nightshade, Things }

    /// <summary>Quest, goal (null = the quest's own), tag, stage the wizard is in, and the keys a Shadow wizard hears.</summary>
    public static IEnumerable<object?[]> ExpectedShadowDialogues() {
        static string[] Ab(params int[] n) => [.. n.Select(i => $"WizQst1731AB_{i:D8}")];
        static string[] Ad(params int[] n) => [.. n.Select(i => $"WizQst1731AD_{i:D8}")];
        static string[] Ae(params int[] n) => [.. n.Select(i => $"WizQst1731AE_{i:D8}")];
        const string S = "ShadowSchool_";

        yield return Case(Reunion, "Goal 1", "Completion", Stage.Reunion,
            [.. Ab(9, 10, 11, 12), S + "BartlebyEye", .. Ab(13, 14, 15, 0)]);
        yield return Case(Reunion, "Goal 2", "Completion", Stage.Reunion, [.. Ab(18, 19, 20), S + "FoulgazeTaunt"]);
        yield return Case(Reunion, "Goal 3", "Completion", Stage.Reunion, [.. Ab(22, 23, 24), S + "FoulgazeBooks"]);
        yield return Case(Reunion, "Goal 5", "Completion", Stage.Reunion, [.. Ab(27), S + "DaisyCave"]);
        yield return Case(Nightshade, null, "Prep", Stage.Nightshade, [.. Ad(1, 2, 3, 4), S + "NightshadeOffer"]);
        yield return Case(Nightshade, "Goal", "Completion", Stage.Nightshade, [.. Ad(6, 7), S + "NightshadeTower"]);
        yield return Case(Nightshade, "Goal 1", "Completion", Stage.Nightshade, [.. Ad(8, 9), S + "NightshadeDefeat"]);
        yield return Case(Nightshade, "Goal 2", "Completion", Stage.Nightshade, [.. Ad(10), S + "DaisyNightshade"]);
        yield return Case(Things, null, "Prep", Stage.Things, [.. Ae(1, 2, 3), S + "PumpkinOffer", .. Ae(4)]);
        yield return Case(Things, "Goal 1", "Completion", Stage.Things, [.. Ae(5, 6), S + "PumpkinReturn", .. Ae(7)]);

        static object?[] Case(string quest, string? goal, string tag, Stage stage, string[] keys) => [quest, goal, tag, stage, keys];
    }

    [Theory]
    [MemberData(nameof(ExpectedShadowDialogues))]
    public void AShadowWizardInTheRightStateHearsTheShadowDialogue(string questName, string? goalName, string tag, Stage stage, string[] expected) {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(questName);
        var wizard = WizardAt(MagicSchool.Shadow, stage);
        var canonical = Dialogue(quest, goalName, tag);

        var shown = QuestDialogueResolver.Resolve(wizard, quest, Goal(quest, goalName), canonical, Providers);

        Assert.NotSame(canonical, shown);
        Assert.Equal(expected, Keys(shown));
        Assert.Equal(tag, (string) shown.m_dialogTag);

        // Every canonical line is still there, in order, as the canonical object (voice, camera and all).
        var kept = shown.m_dialogEntries.Where(e => !((string) e.m_dialog).StartsWith("ShadowSchool_")).ToArray();
        Assert.Equal(canonical.m_dialogEntries.Count, kept.Length);
        for (var i = 0; i < kept.Length; i++) {
            Assert.Same(canonical.m_dialogEntries[i], kept[i]);
            Assert.False(string.IsNullOrEmpty(kept[i].m_soundFile));
        }

        // Each added line is text-only and keeps the presentation of the line it borrows.
        foreach (var line in shown.m_dialogEntries.Where(e => ((string) e.m_dialog).StartsWith("ShadowSchool_"))) {
            var scripted = ShadowMainlineDialogue.Adaptations.SelectMany(a => a.Script)
                .Single(l => l.Text is not null && ShadowMainlineDialogue.TextKey(l.Text) == (string) line.m_dialog);
            var source = canonical.m_dialogEntries[scripted.Canonical];
            Assert.Equal(string.Empty, (string) line.m_soundFile);
            Assert.Equal(string.Empty, (string) line.m_soundEffectFile);
            Assert.Equal(string.Empty, (string) line.m_musicFile);
            Assert.Equal((string) source.m_picture, (string) line.m_picture);
            Assert.Equal(source.m_actorTemplateID, line.m_actorTemplateID);
            Assert.Equal((string) source.m_cameraName, (string) line.m_cameraName);
            Assert.Equal(source.GetType(), line.GetType());
        }
    }

    [Theory]
    [MemberData(nameof(ExpectedShadowDialogues))]
    public void AnOriginalSchoolWizardInTheSameStateHearsTheCanonicalDialogue(string questName, string? goalName, string tag, Stage stage, string[] _) {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(questName);
        var canonical = Dialogue(quest, goalName, tag);

        foreach (var school in OriginalSchools) {
            var wizard = WizardAt(school, stage);
            Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, Goal(quest, goalName), canonical, Providers));
            Assert.Same(quest, QuestDialogueResolver.ResolveTemplate(wizard, quest, Providers));
        }
    }

    [Theory]
    [MemberData(nameof(ExpectedShadowDialogues))]
    public void TheDialogueWaitsForItsQuestState(string questName, string? goalName, string tag, Stage stage, string[] _) {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(questName);
        var canonical = Dialogue(quest, goalName, tag);
        var goal = Goal(quest, goalName);

        foreach (var wrong in Enum.GetValues<Stage>().Where(s => s != stage)) {
            // Too early (the quest is not reachable) or too late (the next quest's state): the game's own dialogue.
            Assert.Same(canonical, QuestDialogueResolver.Resolve(WizardAt(MagicSchool.Shadow, wrong), quest, goal, canonical, Providers));
        }

        // Not enrolled, a missing street, nothing done at all.
        Assert.Same(canonical, QuestDialogueResolver.Resolve(WizardAt(MagicSchool.Shadow, stage, enrolled: false), quest, goal, canonical, Providers));
        foreach (var street in ShadowMainlineDialogue.StreetFinals) {
            Assert.Same(canonical, QuestDialogueResolver.Resolve(WizardAt(MagicSchool.Shadow, stage, skipStreet: street), quest, goal, canonical, Providers));
        }

        Assert.Same(canonical, QuestDialogueResolver.Resolve(NewWizard(MagicSchool.Shadow), quest, goal, canonical, Providers));

        // The quest itself finished: its dialogue is the game's again.
        var done = WizardAt(MagicSchool.Shadow, stage);
        Assert.True(done.QuestBehavior.CompleteQuest(questName));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(done, quest, goal, canonical, Providers));
    }

    [Fact]
    public void TwoPlayersInTheSameQuestNeverSeeEachOthersDialogue() {
        InitializeIsolatedTestConfiguration();
        foreach (var (questName, goalName, tag, stage) in new[] {
                     (Reunion, "Goal 2", "Completion", Stage.Reunion),
                     (Nightshade, "Goal", "Completion", Stage.Nightshade),
                     (Things, "Goal 1", "Completion", Stage.Things) }) {
            var quest = LoadQuest(questName);
            var before = Snapshot(quest);
            var canonical = Dialogue(quest, goalName, tag);
            var goal = Goal(quest, goalName);
            var shadowA = WizardAt(MagicSchool.Shadow, stage);
            var shadowB = WizardAt(MagicSchool.Shadow, stage);
            var fire = WizardAt(MagicSchool.Fire, stage);
            var death = WizardAt(MagicSchool.Death, stage);

            var a = QuestDialogueResolver.Resolve(shadowA, quest, goal, canonical, Providers);
            var f = QuestDialogueResolver.Resolve(fire, quest, goal, canonical, Providers);
            var b = QuestDialogueResolver.Resolve(shadowB, quest, goal, canonical, Providers);
            var d = QuestDialogueResolver.Resolve(death, quest, goal, canonical, Providers);
            var a2 = QuestDialogueResolver.Resolve(shadowA, quest, goal, canonical, Providers);

            Assert.Same(canonical, f);
            Assert.Same(canonical, d);
            Assert.NotSame(canonical, a);
            Assert.NotSame(a, b);
            Assert.NotSame(a.m_dialogEntries, b.m_dialogEntries);
            Assert.Equal(Keys(a), Keys(b));
            Assert.Equal(Keys(a), Keys(a2));
            Assert.DoesNotContain(Keys(f), k => k.StartsWith("ShadowSchool_"));

            // Whole-template delivery (the quest offer cache) is per wizard as well.
            var shadowCopy = QuestDialogueResolver.ResolveTemplate(shadowA, quest, Providers);
            Assert.NotSame(quest, shadowCopy);
            Assert.Same(quest, QuestDialogueResolver.ResolveTemplate(fire, quest, Providers));
            Assert.Equal(before, Snapshot(quest));
        }
    }

    [Fact]
    public void TheCanonicalQuestTemplatesAreNeverChanged() {
        InitializeIsolatedTestConfiguration();
        foreach (var name in new[] { Reunion, Nightshade, Things }) {
            var quest = LoadQuest(name);
            var before = Snapshot(quest);
            var dialogs = AllDialogs(quest).ToArray();
            var entryObjects = dialogs.Select(d => d.Dialog.m_dialogEntries.ToArray()).ToArray();

            foreach (var stage in Enum.GetValues<Stage>()) {
                foreach (var school in OriginalSchools.Append(MagicSchool.Shadow)) {
                    var wizard = WizardAt(school, stage);
                    QuestDialogueResolver.ResolveTemplate(wizard, quest, Providers);
                    foreach (var (goal, dialog) in dialogs) {
                        QuestDialogueResolver.Resolve(wizard, quest, goal, dialog, Providers);
                    }
                }
            }

            Assert.Equal(before, Snapshot(quest));
            for (var i = 0; i < dialogs.Length; i++) {
                Assert.Equal(entryObjects[i].Length, dialogs[i].Dialog.m_dialogEntries.Count);
                for (var j = 0; j < entryObjects[i].Length; j++) {
                    Assert.Same(entryObjects[i][j], dialogs[i].Dialog.m_dialogEntries[j]);
                }
            }
        }
    }

    [Fact]
    public void TheShadowCopyKeepsEveryGoalRequirementResultAndFlagOfTheQuest() {
        InitializeIsolatedTestConfiguration();
        var expected = new Dictionary<string, (string Table, string[] Goals, GOAL_TYPE[] Types, string[] Requires, Stage Stage)> {
            [Reunion] = ("WC-OLDE-MAIN-003A", ["Goal", "Goal 1", "Goal 2", "Goal 3", "Goal 4", "Goal 5"],
                [GOAL_TYPE.GOAL_TYPE_PERSONA, GOAL_TYPE.GOAL_TYPE_WAYPOINT, GOAL_TYPE.GOAL_TYPE_WAYPOINT,
                 GOAL_TYPE.GOAL_TYPE_BOUNTY, GOAL_TYPE.GOAL_TYPE_WAYPOINT, GOAL_TYPE.GOAL_TYPE_PERSONA],
                ["WC-TRITON-MAIN-009", "WC-CYCLOPS-MAIN-005", "WC-FIRECAT-MAIN-007"], Stage.Reunion),
            [Nightshade] = ("WC-HAUNTED-MAIN-001", ["Goal", "Goal 1", "Goal 2"],
                [GOAL_TYPE.GOAL_TYPE_WAYPOINT, GOAL_TYPE.GOAL_TYPE_BOUNTY, GOAL_TYPE.GOAL_TYPE_PERSONA],
                [Reunion], Stage.Nightshade),
            [Things] = ("WC-HAUNTED-MAIN-002", ["Goal", "Goal 1", "Goal 2"],
                [GOAL_TYPE.GOAL_TYPE_BOUNTYCOLLECT, GOAL_TYPE.GOAL_TYPE_PERSONA, GOAL_TYPE.GOAL_TYPE_PERSONA],
                [Nightshade], Stage.Things),
        };

        foreach (var (name, (table, goals, types, requires, stage)) in expected) {
            var quest = LoadQuest(name);
            var wizard = WizardAt(MagicSchool.Shadow, stage);
            var copy = QuestDialogueResolver.ResolveTemplate(wizard, quest, Providers);

            Assert.NotSame(quest, copy);
            Assert.Same(quest.m_requirements, copy.m_requirements);
            Assert.Same(quest.m_endResults, copy.m_endResults);
            Assert.Same(quest.m_startResults, copy.m_startResults);
            Assert.Same(quest.m_startGoals, copy.m_startGoals);
            Assert.Same(quest.m_goalLogic, copy.m_goalLogic);
            Assert.Equal(quest.m_mainline, copy.m_mainline);
            Assert.Equal(quest.m_questLevel, copy.m_questLevel);
            Assert.Equal((string) quest.m_questName, (string) copy.m_questName);
            Assert.Equal(goals, copy.m_goals.Select(g => (string) g.m_goalName));
            Assert.Equal(types, copy.m_goals.Select(g => g.m_goalType));
            Assert.Equal(requires, quest.m_requirements.m_requirements.Cast<ReqHasEntry>().Select(r => (string) r.m_questName));
            Assert.Equal(table, (string) Assert.IsType<ResDropTable>(Assert.Single(copy.m_endResults.m_results)).m_tableName);
            for (var i = 0; i < quest.m_goals.Count; i++) {
                // A goal differs from the canonical only in its dialogue, and then by nothing else.
                Assert.Equal(GoalJsonWithoutDialogue(quest.m_goals[i]), GoalJsonWithoutDialogue(copy.m_goals[i]));
            }

            // Completing the quest writes the quest's own flag, whatever was shown.
            Assert.True(wizard.QuestBehavior.CompleteQuest(name));
            Assert.True(wizard.HasCompletedQuest(name));
        }

        // The two collection and kill objectives keep their descriptors.
        var nightshade = LoadQuest(Nightshade);
        Assert.Contains("Lord_Nightshade", Snapshot(nightshade));
        var things = LoadQuest(Things);
        Assert.Contains("Harvest_Guard", Snapshot(things));
        Assert.Contains("WizQst1731AE_00000010", Snapshot(things));
    }

    [Fact]
    public void TheQuestsRunInOrderForAShadowWizardWhateverIsShown() {
        InitializeIsolatedTestConfiguration();
        var wizard = WizardAt(MagicSchool.Shadow, Stage.Reunion);
        Assert.True(RequirementsMet(LoadQuest(Reunion), wizard));
        Assert.False(RequirementsMet(LoadQuest(Nightshade), wizard));
        wizard.QuestBehavior.SetQuestRegistryValue(Reunion, "Complete", 1);
        Assert.True(RequirementsMet(LoadQuest(Nightshade), wizard));
        Assert.False(RequirementsMet(LoadQuest(Things), wizard));
        wizard.QuestBehavior.SetQuestRegistryValue(Nightshade, "Complete", 1);
        Assert.True(RequirementsMet(LoadQuest(Things), wizard));
    }

    [Fact]
    public void TheNewLinesAreInTheLocalizationAndKeepTheStoryEmbargo() {
        var text = ShadowText.Load(ModFolder());
        var strings = text.Section(ShadowSchoolContent.LANG_NAME).ToDictionary(e => e.Key, e => e.Text);
        var adapted = ShadowMainlineDialogue.Adaptations.Where(a => a.Id != "AmbroseReunion" && !a.Id.StartsWith("Nightside") && !a.Id.StartsWith("UmbralSight")).ToArray();
        var added = adapted.SelectMany(a => a.Script.Where(l => l.Text is not null).Select(l => l.Text!)).ToArray();

        Assert.Equal(10, adapted.Length);
        Assert.Equal(10, added.Length);
        Assert.Equal(added.Length, added.Distinct().Count());
        Assert.Equal(adapted.Select(a => a.Id).Order(), added.Order());

        var lang = ShadowSchoolFiles.ReadLang(
            ShadowSchoolFiles.WriteLang(ShadowSchoolContent.LANG_NAME, text.Section(ShadowSchoolContent.LANG_NAME)), out var name);
        foreach (var key in added) {
            Assert.True(strings.ContainsKey(key), key);
            var line = strings[key];
            Assert.InRange(line.Length, 40, 240);
            Assert.Equal(line, lang.Single(e => e.Key == key).Text);
            Assert.StartsWith(name + "_", ShadowMainlineDialogue.TextKey(key));
            Assert.DoesNotContain("\r", line);
            foreach (var banned in new[] { "Morganthe", "Umbral", "pact", "binding", "Nightside", "Krokotopia", "Dworgyn", "Mortis" }) {
                Assert.DoesNotContain(banned, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Nightshade stays an enemy: his added lines threaten or boast; none welcomes, offers an alliance or teaches.
        foreach (var key in new[] { "NightshadeTower", "NightshadeDefeat" }) {
            Assert.DoesNotContain("welcome", strings[key], StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ally", strings[key], StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("join", strings[key], StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("Kneel", strings["NightshadeTower"]);
        // Bartleby's Eye: Ambrose says nothing about who took it (he learns that in the same scene).
        Assert.DoesNotContain("Malistaire", strings["BartlebyEye"]);
        // Things and Stuff: the Pumpkin Heads notice the wizard; nothing gives or names a vision.
        Assert.Contains("Pumpkin Heads", strings["PumpkinReturn"]);
    }

    [Theory]
    [InlineData("WC-OLDE-MAIN-003A", "Ring", 10, "droptables_wc-olde-main-003a.json", 250)]
    [InlineData("WC-HAUNTED-MAIN-001", "Athame", 5, "droptables_wc-haunted-main-001.json", 275)]
    public void TheShadowRewardIsAnExtraRowAndTheOriginalRewardsAreUntouched(string tableName, string slot, int level, string fixture, int xp) {
        InitializeIsolatedTestConfiguration();
        var original = ReadTable(Path.Combine(RepoRoot(), "spiraldb-cache", "DropTables", fixture));
        var shadowFile = Path.Combine(ModFolder(), "spiraldb", "DropTables", $"droptables_{tableName}_Shadow.json");
        var modded = ReadTable(shadowFile);

        // Same table, same scalar rewards, same rows in the same order, plus exactly one.
        Assert.Equal(tableName, modded.Name);
        Assert.Equal(original.Name, modded.Name);
        Assert.Equal(xp, modded.ExperienceAmount);
        Assert.Equal(original.ExperienceAmount, modded.ExperienceAmount);
        Assert.Equal(original.MinGold, modded.MinGold);
        Assert.Equal(original.MaxGold, modded.MaxGold);
        Assert.Equal(original.TrainingPoints, modded.TrainingPoints);
        Assert.Equal(original.RollChance, modded.RollChance);
        Assert.Equal(original.NoneChance, modded.NoneChance);
        Assert.Equal(original.EachItem, modded.EachItem);
        Assert.Equal(original.GrantsPotionSlot, modded.GrantsPotionSlot);
        Assert.Equal(original.Items.Count + 1, modded.Items.Count);
        Assert.Equal(JsonSerializer.Serialize(original.Items), JsonSerializer.Serialize(modded.Items.Take(original.Items.Count)));

        // The extra row is the mod's own Shadow piece of that slot (the ring is the very Death ring the quest already grants, level 10; the athame stays the weaker level-5 piece, since no Shadow athame matches the canonical one), for Shadow wizards only.
        var piece = ShadowGear.Pieces.Single(p => p.Slot == slot && p.Level == level);
        var row = modded.Items.Last();
        Assert.Equal(piece.TemplateId.ToString(), row.ItemId);
        var requirement = Assert.IsType<ReqSchoolOfFocus>(Assert.Single(row.Requirements!.m_requirements));
        Assert.Equal(MagicSchool.Shadow.ToString(), requirement.m_magicSchool.ToString());

        // Which rows each school may roll: the original schools exactly as before, Shadow gains the one row.
        foreach (var school in OriginalSchools) {
            Assert.Equal(Eligible(original, school), Eligible(modded, school));
        }

        // Guard against a vacuous comparison: an original school can roll the generic row and its own, Shadow only the generic.
        Assert.Equal(2, Eligible(original, MagicSchool.Fire).Count);
        Assert.Single(Eligible(original, MagicSchool.Shadow));

        var canonicalShadow = Eligible(original, MagicSchool.Shadow);
        Assert.Equal([.. canonicalShadow, row.ItemId], Eligible(modded, MagicSchool.Shadow));
        Assert.Equal(piece.TemplateId.ToString(), Eligible(modded, MagicSchool.Shadow).Last());

        // The quest still points at that table by name, with its one roll.
        var quest = LoadQuest(tableName == "WC-OLDE-MAIN-003A" ? Reunion : Nightshade);
        var result = Assert.IsType<ResDropTable>(Assert.Single(quest.m_endResults.m_results));
        Assert.Equal(tableName, (string) result.m_tableName);
    }

    [Fact]
    public void WithoutTheDropTableOverridesTheCanonicalTablesStillApplyAndNothingIsRequiredOfTheMod() {
        // The override files sit in the mod's own spiraldb folder, which only an enabled mod's loader reads, so a
        // server without the mod (or with the files removed) rolls SpiralDB's own tables for everyone.
        var files = Directory.GetFiles(Path.Combine(ModFolder(), "spiraldb", "DropTables"), "*.json");
        Assert.Equal(2, files.Length);
        Assert.All(files, f => Assert.Contains("_Shadow", Path.GetFileName(f)));
        Assert.False(Directory.Exists(Path.Combine(RepoRoot(), "spiraldb-cache", "DropTables", "ShadowSchool")));
    }

    [Fact]
    public void WhenTheGamesDialogueDiffersOrAProviderFailsTheCanonicalDialogueIsShown() {
        InitializeIsolatedTestConfiguration();
        foreach (var (questName, goalName, tag, stage, _) in ExpectedShadowDialogues().Select(a => ((string) a[0]!, (string?) a[1], (string) a[2]!, (Stage) a[3]!, 0))) {
            var quest = LoadQuest(questName);
            var wizard = WizardAt(MagicSchool.Shadow, stage);
            var canonical = Dialogue(quest, goalName, tag);
            var goal = Goal(quest, goalName);

            // A game revision that renumbers a line, drops one or adds one.
            var renumbered = canonical with {
                m_dialogEntries = [.. canonical.m_dialogEntries.Select((e, i) => i == 0 ? e with { m_dialog = "WizQst1731AB_00000099" } : e)],
            };
            Assert.Same(renumbered, QuestDialogueResolver.Resolve(wizard, quest, goal, renumbered, Providers));
            var shorter = canonical with { m_dialogEntries = [.. canonical.m_dialogEntries.Skip(1)] };
            Assert.Same(shorter, QuestDialogueResolver.Resolve(wizard, quest, goal, shorter, Providers));
            var longer = canonical with { m_dialogEntries = [.. canonical.m_dialogEntries, canonical.m_dialogEntries[0]] };
            Assert.Same(longer, QuestDialogueResolver.Resolve(wizard, quest, goal, longer, Providers));

            // No providers, or one that throws or answers with something unusable.
            Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, goal, canonical, []));
            Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, goal, canonical, [new Throwing()]));
            Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, goal, canonical, [new Answering(canonical with { m_dialogEntries = [] })]));
            Assert.Same(quest, QuestDialogueResolver.ResolveTemplate(wizard, quest, []));
        }
    }

    [Fact]
    public void TheSameKeysUnderAnotherQuestOrGoalAreNeverAdapted() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(Nightshade);
        var wizard = WizardAt(MagicSchool.Shadow, Stage.Nightshade);
        var canonical = Dialogue(quest, "Goal", "Completion");

        // The same keys under another quest name or another goal are not the adapted dialogue.
        Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest with { m_questName = "WC-HAUNTED-MAIN-003" }, quest.m_goals[0], canonical, Providers));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, quest.m_goals[1], canonical, Providers));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, null, canonical, Providers));
    }

    [Fact]
    public void StateAndDialogueAreTheSameAfterAReconnect() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(Things);
        var canonical = Dialogue(quest, "Goal 1", "Completion");
        foreach (var school in new[] { MagicSchool.Shadow, MagicSchool.Fire }) {
            var wizard = WizardAt(school, Stage.Things);
            var before = Keys(QuestDialogueResolver.Resolve(wizard, quest, quest.m_goals[1], canonical, Providers));
            var reloaded = Reload(wizard);
            Assert.True(reloaded.HasCompletedQuest(Nightshade));
            Assert.Equal(before, Keys(QuestDialogueResolver.Resolve(reloaded, quest, quest.m_goals[1], canonical, Providers)));
        }
    }

    // --- helpers ---

    private static List<string> Eligible(DropTable table, MagicSchool school) {
        var wizard = NewWizard(school);

        return [.. table.Items.Where(i => i.Requirements is null
                || RequirementDispatcher.EvaluateRequirements(i.Requirements,
                    new GenericRequirementContext(i.Requirements, null!, null!, wizard))).Select(i => i.ItemId)];
    }

    private static DropTable ReadTable(string path)
        => JsonSerializer.Deserialize<DropTable>(File.ReadAllText(path), SpiralJsonOptions.Options)!;

    private static ActorDialog Dialogue(QuestTemplate quest, string? goalName, string tag) {
        var list = goalName is null ? quest.m_dialogList : quest.m_goals.Single(g => g.m_goalName == goalName).m_dialogList;

        return ((ActorDialogList) list).m_dialogs.Single(d => d.m_dialogTag == tag);
    }

    private static GoalTemplate? Goal(QuestTemplate quest, string? goalName)
        => goalName is null ? null : quest.m_goals.Single(g => g.m_goalName == goalName);

    private static IEnumerable<(GoalTemplate Goal, ActorDialog Dialog)> AllDialogs(QuestTemplate quest) {
        foreach (var dialog in (quest.m_dialogList as ActorDialogList)?.m_dialogs ?? []) {
            yield return (null!, dialog);
        }

        foreach (var goal in quest.m_goals) {
            foreach (var dialog in (goal.m_dialogList as ActorDialogList)?.m_dialogs ?? []) {
                yield return (goal, dialog);
            }
        }
    }

    private static string[] Keys(ActorDialog dialog) => [.. dialog.m_dialogEntries.Select(e => (string) e.m_dialog)];

    private static string Snapshot(QuestTemplate quest) => JsonSerializer.Serialize(quest, SpiralJsonOptions.Options);

    private static string GoalJsonWithoutDialogue(GoalTemplate goal)
        => JsonSerializer.Serialize(goal with { m_dialogList = null }, SpiralJsonOptions.Options);

    private static bool RequirementsMet(QuestTemplate quest, Wizard wizard)
        => RequirementDispatcher.EvaluateRequirements(quest.m_requirements,
            new QuestRequirementContext(quest.m_requirements, null!, null!, wizard, quest.m_questName));

    /// <summary>A wizard who has everything the stage's quest needs, holds it, and has not finished it.</summary>
    private static Wizard WizardAt(MagicSchool school, Stage stage, string? skipStreet = null, bool enrolled = true) {
        var wizard = NewWizard(school);
        if (enrolled) {
            var enrollment = school == MagicSchool.Shadow
                ? ShadowMainlineDialogue.SHADOW_ENROLLMENT
                : $"WC-COMMONS-MAIN-002-{school.ToString().ToUpperInvariant()}";
            wizard.QuestBehavior.SetQuestRegistryValue(enrollment, "Complete", 1);
        }

        foreach (var street in ShadowMainlineDialogue.StreetFinals.Where(s => s != skipStreet)) {
            wizard.QuestBehavior.SetQuestRegistryValue(street, "Complete", 1);
        }

        if (stage >= Stage.Nightshade) {
            wizard.QuestBehavior.SetQuestRegistryValue(Reunion, "Complete", 1);
        }

        if (stage >= Stage.Things) {
            wizard.QuestBehavior.SetQuestRegistryValue(Nightshade, "Complete", 1);
        }

        var current = stage switch { Stage.Reunion => Reunion, Stage.Nightshade => Nightshade, _ => Things };
        wizard.QuestBehavior.AddQuest(new QuestInstance(LoadQuest(current), wizard.CharId));

        return wizard;
    }

    private static Wizard NewWizard(MagicSchool school) => new() {
        CharId = (ulong) Random.Shared.NextInt64(1, long.MaxValue),
        MagicSchoolBehavior = new ServerMagicSchoolBehavior { MagicSchool = school, Level = 10 },
        QuestBehavior = new ServerQuestBehavior(),
    };

    private static Wizard Reload(Wizard wizard) {
        var behavior = new ServerQuestBehavior();
        var registry = JsonSerializer.Deserialize<Dictionary<string, ulong>>(JsonSerializer.Serialize(wizard.QuestBehavior.Registry))!;
        foreach (var (key, value) in registry) {
            behavior.Registry[key] = value;
        }

        behavior.CurrentQuestIDs.AddRange(wizard.QuestBehavior.CurrentQuestIDs);
        behavior.CurrentQuestInstances = [.. wizard.QuestBehavior.CurrentQuestInstances
            .Select(q => JsonSerializer.Deserialize<QuestInstance>(JsonSerializer.Serialize(q))!)];

        return new Wizard {
            CharId = wizard.CharId,
            MagicSchoolBehavior = new ServerMagicSchoolBehavior {
                MagicSchool = wizard.MagicSchoolBehavior.MagicSchool, Level = wizard.MagicSchoolBehavior.Level,
            },
            QuestBehavior = behavior,
        };
    }

    private static QuestTemplate LoadQuest(string name)
        => JsonSerializer.Deserialize<QuestTemplate>(File.ReadAllText(Path.Combine(RepoRoot(), "spiraldb-cache", "QuestTemplates", $"questtemplates_{name}.json")), SpiralJsonOptions.Options)!;

    private static string ModFolder() => Path.Combine(RepoRoot(), "src", "Imlight.Director", "mods", "ShadowSchool");

    private static string RepoRoot() {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Imlight.Director", "mods", "ShadowSchool"))) {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not find the repository above the test folder.");
    }

    private static void InitializeIsolatedTestConfiguration() {
        var path = Path.Combine(Path.GetTempPath(), $"imlight-shadow-campaign-tests-{Environment.ProcessId}-{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, $"[Logging]{Environment.NewLine}LogPath = {Path.Combine(Path.GetTempPath(), "imlight-shadow-campaign-tests.log")}{Environment.NewLine}LogLevel = ERROR{Environment.NewLine}SeqSinkUrl = http://127.0.0.1:1{Environment.NewLine}");
        ConfigurationManager.Initialize(path);
        File.Delete(path);
    }

    private sealed class Throwing : IQuestDialogueProvider {
        public ActorDialog Resolve(QuestDialogueRequest request) => throw new InvalidOperationException("provider failure");
    }

    private sealed class Answering(ActorDialog answer) : IQuestDialogueProvider {
        public ActorDialog Resolve(QuestDialogueRequest request) => answer;
    }

}
