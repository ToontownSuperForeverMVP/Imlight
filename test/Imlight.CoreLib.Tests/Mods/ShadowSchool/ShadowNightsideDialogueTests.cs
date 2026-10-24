using System.Text.Json;
using Imcodec.ObjectProperty.TypeCache;
using Imlight.Common;
using Imlight.CoreLib.Game.Quests;
using Imlight.CoreLib.Mods.ShadowSchool;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.WizardData;
using Imlight.CoreLib.WizardData.Models.Player;
using Xunit;

namespace Imlight.CoreLib.Tests.Mods.ShadowSchool;

/// <summary>
/// Stage C2.3: the Shadow version of Take a Walk on the Nightside (WC-COMMONS-MAIN-004): Ambrose's temporary
/// enchantment, Mortis, Dworgyn and the return. Presentation only: the canonical quest comes from the installed
/// SpiralDB cache, and nothing here grants or records a power.
/// </summary>
[Collection("SharedConfiguration")]
public sealed class ShadowNightsideDialogueTests {

    private const string Nightside = "WC-COMMONS-MAIN-004";
    private const string FixtureFile = "questtemplates_WC-COMMONS-MAIN-004.json";

    private static readonly MagicSchool[] OriginalSchools = [
        MagicSchool.Fire, MagicSchool.Ice, MagicSchool.Storm, MagicSchool.Myth,
        MagicSchool.Life, MagicSchool.Death, MagicSchool.Balance,
    ];
    private static readonly IQuestDialogueProvider[] Providers = [new ShadowSchoolMod()];

    // Which stage of the story a wizard is in; the Nightside quest is held (not finished) from Nightside on.
    public enum Stage { Reunion, Nightshade, Things, Nightside }

    /// <summary>Goal (null = the quest's own), tag, and the keys a Shadow wizard hears, with the added line's key.</summary>
    public static IEnumerable<object?[]> ExpectedShadowDialogues() {
        static string[] Af(params int[] n) => [.. n.Select(i => $"WizQst1731AF_{i:D8}")];
        const string S = "ShadowSchool_";

        yield return [null, "Prep", Af(1, 2, 3, 4).Append(S + "NightsideEnchantReaction").Append(S + "NightsideEnchantCaution").ToArray()];
        yield return ["Goal 1", "Completion", Af(7, 8, 9, 10).Append(S + "NightsideMortis").ToArray()];
        yield return ["Goal 3", "Completion", Af(20, 21, 22, 23, 24).Append(S + "NightsideDworgyn").ToArray()];
        yield return ["Goal 4", "Completion", Af(25).Append(S + "NightsideAfterimage").Append(S + "NightsideAfterimageHold").ToArray()];
    }

    [Theory]
    [MemberData(nameof(ExpectedShadowDialogues))]
    public void AShadowWizardAtTheNightsideHearsTheShadowDialogue(string? goalName, string tag, string[] expected) {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest();
        var wizard = WizardAt(MagicSchool.Shadow, Stage.Nightside);
        var canonical = Dialogue(quest, goalName, tag);

        var shown = QuestDialogueResolver.Resolve(wizard, quest, Goal(quest, goalName), canonical, Providers);

        Assert.NotSame(canonical, shown);
        Assert.Equal(expected, Keys(shown));
        Assert.Equal(tag, (string) shown.m_dialogTag);

        // Every canonical line is still there, in order, as the canonical object (recorded voice, camera and all).
        var kept = shown.m_dialogEntries.Where(e => !((string) e.m_dialog).StartsWith("ShadowSchool_")).ToArray();
        Assert.Equal(canonical.m_dialogEntries.Count, kept.Length);
        for (var i = 0; i < kept.Length; i++) {
            Assert.Same(canonical.m_dialogEntries[i], kept[i]);
            Assert.False(string.IsNullOrEmpty(kept[i].m_soundFile));
        }

        // Each added line is text-only, keeps the presentation of the line it borrows, and comes after the line it borrows.
        var index = 0;
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
            Assert.True(Array.IndexOf(Keys(shown), (string) line.m_dialog) > Array.IndexOf(Keys(shown), (string) source.m_dialog), "added line " + index);
            index++;
        }
    }

    [Theory]
    [MemberData(nameof(ExpectedShadowDialogues))]
    public void AnOriginalSchoolWizardInTheSameStateHearsTheCanonicalDialogue(string? goalName, string tag, string[] _) {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest();
        var canonical = Dialogue(quest, goalName, tag);

        foreach (var school in OriginalSchools) {
            var wizard = WizardAt(school, Stage.Nightside);
            Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, Goal(quest, goalName), canonical, Providers));
            Assert.Same(quest, QuestDialogueResolver.ResolveTemplate(wizard, quest, Providers));
        }
    }

    [Theory]
    [MemberData(nameof(ExpectedShadowDialogues))]
    public void NothingShadowAppearsBeforeTheStoryEventOrAfterTheQuest(string? goalName, string tag, string[] _) {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest();
        var canonical = Dialogue(quest, goalName, tag);
        var goal = Goal(quest, goalName);

        // Every earlier stage: the reunion, Nightshade and Things and Stuff are not yet done.
        foreach (var early in new[] { Stage.Reunion, Stage.Nightshade, Stage.Things }) {
            Assert.Same(canonical, QuestDialogueResolver.Resolve(WizardAt(MagicSchool.Shadow, early), quest, goal, canonical, Providers));
        }

        // Not enrolled, a missing street, a fresh wizard, each missing predecessor on its own.
        Assert.Same(canonical, QuestDialogueResolver.Resolve(WizardAt(MagicSchool.Shadow, Stage.Nightside, enrolled: false), quest, goal, canonical, Providers));
        foreach (var street in ShadowMainlineDialogue.StreetFinals) {
            Assert.Same(canonical, QuestDialogueResolver.Resolve(WizardAt(MagicSchool.Shadow, Stage.Nightside, skip: street), quest, goal, canonical, Providers));
        }

        foreach (var predecessor in new[] { "WC-OLDE-MAIN-003A", "WC-HAUNTED-MAIN-001", "WC-HAUNTED-MAIN-002" }) {
            Assert.Same(canonical, QuestDialogueResolver.Resolve(WizardAt(MagicSchool.Shadow, Stage.Nightside, skip: predecessor), quest, goal, canonical, Providers));
        }

        Assert.Same(canonical, QuestDialogueResolver.Resolve(NewWizard(MagicSchool.Shadow), quest, goal, canonical, Providers));

        // The quest finished: its dialogue is the game's again.
        var done = WizardAt(MagicSchool.Shadow, Stage.Nightside);
        Assert.True(done.QuestBehavior.CompleteQuest(Nightside));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(done, quest, goal, canonical, Providers));
    }

    [Fact]
    public void TheEarlierStagesDialogueIsUnchangedByTheNightsideRows() {
        InitializeIsolatedTestConfiguration();

        // A Shadow wizard at Things and Stuff still hears the C2.2 dialogue and never a Nightside row.
        var nightsideRows = ShadowMainlineDialogue.Adaptations.Where(a => a.QuestName == Nightside).Select(a => a.Id).ToArray();
        Assert.Equal(["NightsideEnchantment", "NightsideMortis", "NightsideDworgyn", "NightsideReturn"], nightsideRows);
        Assert.All(ShadowMainlineDialogue.Adaptations.Where(a => a.QuestName != Nightside),
            a => Assert.DoesNotContain(a.Script, l => l.Text is not null && l.Text.StartsWith("Nightside")));
    }

    [Fact]
    public void TwoPlayersInTheSameQuestNeverSeeEachOthersDialogue() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest();
        var canonical = Dialogue(quest, "Goal 4", "Completion");
        var goal = Goal(quest, "Goal 4");

        var shadowA = WizardAt(MagicSchool.Shadow, Stage.Nightside);
        var fire = WizardAt(MagicSchool.Fire, Stage.Nightside);
        var shadowB = WizardAt(MagicSchool.Shadow, Stage.Nightside);
        var death = WizardAt(MagicSchool.Death, Stage.Nightside);
        var early = WizardAt(MagicSchool.Shadow, Stage.Things);

        var results = new[] { shadowA, fire, shadowB, death, early, shadowA, fire }
            .Select(w => QuestDialogueResolver.Resolve(w, quest, goal, canonical, Providers)).ToArray();

        Assert.NotSame(canonical, results[0]);
        Assert.Same(canonical, results[1]);
        Assert.NotSame(canonical, results[2]);
        Assert.NotSame(results[0], results[2]);
        Assert.Same(canonical, results[3]);
        Assert.Same(canonical, results[4]);
        Assert.Equal(Keys(results[0]), Keys(results[5]));
        Assert.Same(canonical, results[6]);
        Assert.Single(canonical.m_dialogEntries);

        // Whole-template delivery (the quest offer cache) is per wizard too.
        var before = Snapshot(quest);
        Assert.NotSame(quest, QuestDialogueResolver.ResolveTemplate(shadowA, quest, Providers));
        Assert.Same(quest, QuestDialogueResolver.ResolveTemplate(fire, quest, Providers));
        Assert.Equal(before, Snapshot(quest));
    }

    [Fact]
    public void TheCanonicalQuestTemplateIsNeverChanged() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest();
        var before = Snapshot(quest);
        var dialogs = AllDialogs(quest).ToArray();
        var entryObjects = dialogs.Select(d => d.Dialog.m_dialogEntries.ToArray()).ToArray();

        foreach (var school in OriginalSchools.Append(MagicSchool.Shadow)) {
            foreach (var stage in Enum.GetValues<Stage>()) {
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

    [Fact]
    public void TheShadowCopyKeepsEveryGoalRequirementAndResultOfTheQuest() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest();
        var wizard = WizardAt(MagicSchool.Shadow, Stage.Nightside);
        var copy = QuestDialogueResolver.ResolveTemplate(wizard, quest, Providers);

        Assert.NotSame(quest, copy);
        Assert.Same(quest.m_requirements, copy.m_requirements);
        Assert.Same(quest.m_endResults, copy.m_endResults);
        Assert.Same(quest.m_startResults, copy.m_startResults);
        Assert.Same(quest.m_startGoals, copy.m_startGoals);
        Assert.Same(quest.m_goalLogic, copy.m_goalLogic);
        Assert.Equal(quest.m_mainline, copy.m_mainline);
        Assert.Equal(quest.m_questLevel, copy.m_questLevel);

        // The five Nightside goals, in order, with the same targets.
        Assert.Equal(["Goal", "Goal 1", "Goal 2", "Goal 3", "Goal 4"], copy.m_goals.Select(g => (string) g.m_goalName));
        Assert.Equal([GOAL_TYPE.GOAL_TYPE_WAYPOINT, GOAL_TYPE.GOAL_TYPE_PERSONA, GOAL_TYPE.GOAL_TYPE_WAYPOINT,
            GOAL_TYPE.GOAL_TYPE_PERSONA, GOAL_TYPE.GOAL_TYPE_PERSONA], copy.m_goals.Select(g => g.m_goalType));
        Assert.Equal(["WizardCity/WC_NightSide", "", "WizardCity/Interiors/WC_SchoolDeath", "", "WizardCity/Interiors/WC_Headmistress_House"],
            copy.m_goals.Select(g => (string) g.m_destinationZone));
        Assert.Equal(["WC-RVW-DeathTree", "WC-NIS-NPC01", "WC-HUB-NPC01"],
            copy.m_goals.Where(g => g.m_goalType == GOAL_TYPE.GOAL_TYPE_PERSONA).Select(g => Snapshot(g)).Select(PersonaOf));
        for (var i = 0; i < quest.m_goals.Count; i++) {
            // A goal differs from the canonical only in its dialogue, and then by nothing else.
            Assert.Equal(GoalJsonWithoutDialogue(quest.m_goals[i]), GoalJsonWithoutDialogue(copy.m_goals[i]));
        }

        // The prerequisite is Things and Stuff alone.
        Assert.Equal("WC-HAUNTED-MAIN-002", (string) Assert.IsType<ReqHasEntry>(Assert.Single(quest.m_requirements.m_requirements)).m_questName);

        // The drop table and the six school-specific learn-spell results (none for Shadow, as in the game) are untouched.
        var results = copy.m_endResults.m_results;
        Assert.Equal("WC-COMMONS-MAIN-004", (string) Assert.IsType<ResDropTable>(results[0]).m_tableName);
        var spells = results.OfType<ResLearnSpell>().ToArray();
        Assert.Equal(
            [1730822561u, 927448295u, 975200773u, 534093744u, 533943727u, 1627273980u],
            spells.Select(s => (uint) s.m_templateID));
        Assert.Equal(7, results.Count);

        // Completing the quest writes the quest's own flag, whatever was shown.
        Assert.True(wizard.QuestBehavior.CompleteQuest(Nightside));
        Assert.True(wizard.HasCompletedQuest(Nightside));
    }

    [Fact]
    public void NoPermanentStateIsGrantedByResolvingTheDialogue() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest();
        var wizard = WizardAt(MagicSchool.Shadow, Stage.Nightside);
        var registryBefore = JsonSerializer.Serialize(wizard.QuestBehavior.Registry.OrderBy(e => e.Key));
        var questsBefore = wizard.QuestBehavior.CurrentQuestInstances.Count;

        QuestDialogueResolver.ResolveTemplate(wizard, quest, Providers);
        foreach (var (goal, dialog) in AllDialogs(quest)) {
            QuestDialogueResolver.Resolve(wizard, quest, goal, dialog, Providers);
        }

        Assert.Equal(registryBefore, JsonSerializer.Serialize(wizard.QuestBehavior.Registry.OrderBy(e => e.Key)));
        Assert.Equal(questsBefore, wizard.QuestBehavior.CurrentQuestInstances.Count);
        // The walked trail is the test's setup (Stage C3); resolving never unlocks Umbral Sight.
        Assert.DoesNotContain(UmbralSight.STATE_KEY, wizard.QuestBehavior.Registry.Keys);
        Assert.False(wizard.HasCompletedQuest(Nightside));

        // And no Nightside adaptation names any registry key, effect or result to grant.
        Assert.DoesNotContain("Umbral", JsonSerializer.Serialize(quest, SpiralJsonOptions.Options), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheResolverFallsBackWhenTheDialogueOrItsKeysAreNotAsExpected() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest();
        var wizard = WizardAt(MagicSchool.Shadow, Stage.Nightside);
        var canonical = Dialogue(quest, "Goal 3", "Completion");
        var goal = Goal(quest, "Goal 3");

        // A game revision changed the text keys (one key, one fewer line, one more line).
        var renamed = canonical with { m_dialogEntries = [.. canonical.m_dialogEntries.Select((e, i) => i == 2 ? e with { m_dialog = "WizQst1731AF_99999999" } : e)] };
        var shorter = canonical with { m_dialogEntries = [.. canonical.m_dialogEntries.Take(4)] };
        var longer = canonical with { m_dialogEntries = [.. canonical.m_dialogEntries, canonical.m_dialogEntries[0]] };
        foreach (var changed in new[] { renamed, shorter, longer }) {
            Assert.Same(changed, QuestDialogueResolver.Resolve(wizard, quest, goal, changed, Providers));
        }

        // Another goal or tag of the same quest, no providers, a provider that throws, one that answers with nothing.
        Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, Goal(quest, "Goal 2"), canonical, Providers));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, goal, canonical, []));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, goal, canonical, [new Throwing()]));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, goal, canonical, [new Answering(canonical with { m_dialogEntries = [] })]));

        // A wizard whose school or quest data is missing.
        Assert.Same(canonical, QuestDialogueResolver.Resolve(new Wizard { CharId = 1 }, quest, goal, canonical, Providers));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(null!, quest, goal, canonical, Providers));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(wizard, quest, null, canonical, Providers));
    }

    [Fact]
    public void StateAndDialogueAreTheSameAfterAReconnect() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest();
        var canonical = Dialogue(quest, "Goal 4", "Completion");
        foreach (var school in new[] { MagicSchool.Shadow, MagicSchool.Fire }) {
            var wizard = WizardAt(school, Stage.Nightside);
            var before = Keys(QuestDialogueResolver.Resolve(wizard, quest, quest.m_goals[4], canonical, Providers));
            var reloaded = Reload(wizard);
            Assert.True(reloaded.HasCompletedQuest("WC-HAUNTED-MAIN-002"));
            Assert.Equal(before, Keys(QuestDialogueResolver.Resolve(reloaded, quest, quest.m_goals[4], canonical, Providers)));
        }
    }

    [Fact]
    public void TheNewLinesAreInTheLocalizationAndStayInCharacter() {
        var text = ShadowText.Load(ModFolder());
        var strings = text.Section(ShadowSchoolContent.LANG_NAME).ToDictionary(e => e.Key, e => e.Text);
        var adapted = ShadowMainlineDialogue.Adaptations.Where(a => a.QuestName == Nightside).ToArray();
        var added = adapted.SelectMany(a => a.Script.Where(l => l.Text is not null).Select(l => l.Text!)).ToArray();

        Assert.Equal(6, added.Length);
        Assert.Equal(added.Length, added.Distinct().Count());

        var lang = ShadowSchoolFiles.ReadLang(
            ShadowSchoolFiles.WriteLang(ShadowSchoolContent.LANG_NAME, text.Section(ShadowSchoolContent.LANG_NAME)), out var name);
        foreach (var key in added) {
            Assert.True(strings.ContainsKey(key), key);
            var line = strings[key];
            Assert.InRange(line.Length, 40, 320);
            Assert.Equal(line, lang.Single(e => e.Key == key).Text);
            Assert.StartsWith(name + "_", ShadowMainlineDialogue.TextKey(key));
            Assert.DoesNotContain("\r", line);

            // This stage is a first hint only: no name for the power, no pact, no conquest, no later story.
            foreach (var banned in new[] { "Morganthe", "Umbral", "pact", "binding", "Krokotopia", "Ravenwood", "conquer", "army", "ability", "gift" }) {
                Assert.DoesNotContain(banned, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Ambrose warns against forcing it, and neither character claims to understand it.
        Assert.Contains("do not force it", strings["NightsideEnchantCaution"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not seen its like", strings["NightsideEnchantCaution"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("do not know", strings["NightsideMortis"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("afterimage", strings["NightsideAfterimage"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("do not yet understand", strings["NightsideAfterimageHold"], StringComparison.OrdinalIgnoreCase);

        // The afterimage appears only on the return; the start and the visits do not mention one.
        foreach (var key in new[] { "NightsideEnchantReaction", "NightsideEnchantCaution", "NightsideMortis", "NightsideDworgyn" }) {
            Assert.DoesNotContain("afterimage", strings[key], StringComparison.OrdinalIgnoreCase);
        }

        // Dworgyn stays the confused caretaker: no scheming, no Ravenwood, no offer of anything.
        foreach (var banned in new[] { "Malistaire", "plan", "join", "serve", "teach" }) {
            Assert.DoesNotContain(banned, strings["NightsideDworgyn"], StringComparison.OrdinalIgnoreCase);
        }
    }

    // --- helpers ---

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

    private static string Snapshot(GoalTemplate goal) => JsonSerializer.Serialize(goal, SpiralJsonOptions.Options);

    private static string PersonaOf(string goalJson) {
        using var document = JsonDocument.Parse(goalJson);
        foreach (var property in document.RootElement.EnumerateObject()) {
            if (property.Name.Contains("persona", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String
                && !string.IsNullOrEmpty(property.Value.GetString())) {
                return property.Value.GetString()!;
            }
        }

        return string.Empty;
    }

    private static string GoalJsonWithoutDialogue(GoalTemplate goal)
        => JsonSerializer.Serialize(goal with { m_dialogList = null }, SpiralJsonOptions.Options);

    /// <summary>A wizard who has everything the stage needs and, at Nightside, holds the Nightside quest unfinished.</summary>
    private static Wizard WizardAt(MagicSchool school, Stage stage, string? skip = null, bool enrolled = true) {
        var wizard = NewWizard(school);
        void Done(string quest) {
            if (quest != skip) {
                wizard.QuestBehavior.SetQuestRegistryValue(quest, "Complete", 1);
            }
        }

        if (enrolled) {
            Done(school == MagicSchool.Shadow
                ? ShadowMainlineDialogue.SHADOW_ENROLLMENT
                : $"WC-COMMONS-MAIN-002-{school.ToString().ToUpperInvariant()}");
        }

        foreach (var street in ShadowMainlineDialogue.StreetFinals) {
            Done(street);
        }

        if (stage >= Stage.Nightshade) {
            Done("WC-OLDE-MAIN-003A");
        }

        if (stage >= Stage.Things) {
            Done("WC-HAUNTED-MAIN-001");
        }

        if (stage >= Stage.Nightside) {
            Done("WC-HAUNTED-MAIN-002");
            wizard.QuestBehavior.AddQuest(new QuestInstance(LoadQuest(), wizard.CharId));
            // Stage C3: a wizard at the return has walked there (accepted, Nightside, Mortis, Death School, Dworgyn).
            if (school == MagicSchool.Shadow) {
                wizard.QuestBehavior.SetRegistryValue(UmbralSight.TRAIL_KEY, UmbralSight.TrailBeforeReturn);
            }
        }

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

    private static QuestTemplate LoadQuest()
        => JsonSerializer.Deserialize<QuestTemplate>(File.ReadAllText(Path.Combine(RepoRoot(), "spiraldb-cache", "QuestTemplates", FixtureFile)), SpiralJsonOptions.Options)!;

    private static string ModFolder() => Path.Combine(RepoRoot(), "src", "Imlight.Director", "mods", "ShadowSchool");

    private static string RepoRoot() {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Imlight.Director", "mods", "ShadowSchool"))) {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not find the repository above the test folder.");
    }

    private static void InitializeIsolatedTestConfiguration() {
        var path = Path.Combine(Path.GetTempPath(), $"imlight-shadow-nightside-tests-{Environment.ProcessId}-{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, $"[Logging]{Environment.NewLine}LogPath = {Path.Combine(Path.GetTempPath(), "imlight-shadow-nightside-tests.log")}{Environment.NewLine}LogLevel = ERROR{Environment.NewLine}SeqSinkUrl = http://127.0.0.1:1{Environment.NewLine}");
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
