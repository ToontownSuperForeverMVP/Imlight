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
/// Stage C3: Umbral Sight. The unlock is driven the way QuestService drives it, through QuestProgressEvents to the real
/// ShadowSchoolMod (with a recorder in place of the database save), over the installed Take a Walk on the Nightside
/// quest from the installed SpiralDB cache. Perception is driven through UmbralPerception's per-wizard watch.
/// </summary>
[Collection("SharedConfiguration")]
public sealed class UmbralSightTests {

    private const string Nightside = "WC-COMMONS-MAIN-004";
    private const string Krokotopia = "WC-COMMONS-MAIN-005";
    private const string NightsideFixture = "questtemplates_WC-COMMONS-MAIN-004.json";
    private const string KrokotopiaFixture = "questtemplates_WC-COMMONS-MAIN-005.json";
    private const string Lesson = "ShadowSchool-UmbralSight-Lesson";

    private static readonly MagicSchool[] OriginalSchools = [
        MagicSchool.Fire, MagicSchool.Ice, MagicSchool.Storm, MagicSchool.Myth,
        MagicSchool.Life, MagicSchool.Death, MagicSchool.Balance,
    ];

    // Ravenwood: Bartleby's roots, and a point well outside their reach.
    private const string Ravenwood = "WizardCity/WC_Ravenwood";
    private static readonly (float X, float Y) AtRoots = (100, 1500);
    private static readonly (float X, float Y) FarFromRoots = (3000, 6000);

    // ------------------------------------------------------------------ The unlock

    [Fact]
    public void ANewShadowWizardCannotUseUmbralSight() {
        InitializeIsolatedTestConfiguration();
        var wizard = NewWizard(MagicSchool.Shadow);

        Assert.Equal(UmbralSightState.Locked, UmbralSight.StateOf(wizard));
        Assert.False(UmbralSight.CanPerceive(wizard));
        Assert.True(Perceives(wizard).IsEmpty);
    }

    [Fact]
    public void AShadowWizardBeforeTheNightsideIsCompleteCannotUseIt() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var save = new Recorder();
        var wizard = WizardBeforeNightside(MagicSchool.Shadow);

        Accept(wizard, quest, save);
        Assert.False(UmbralSight.CanPerceive(wizard));

        // Every goal walked, the return to Ambrose included, but the quest not yet completed: still locked.
        foreach (var goal in quest.m_goals) {
            QuestProgressEvents.GoalCompleted(wizard, quest, goal, QuestProgressSource.Play, Mod(save));
            Assert.Equal(UmbralSightState.Locked, UmbralSight.StateOf(wizard));
        }

        Assert.Equal(UmbralSight.TrailComplete, UmbralSight.TrailOf(wizard));
        Assert.False(UmbralSight.CanPerceive(wizard));
    }

    [Fact]
    public void TheFullNightsideWalkUnlocksItAtTheQuestsEnd() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var before = Snapshot(quest);
        var save = new Recorder();
        var wizard = WizardBeforeNightside(MagicSchool.Shadow);

        Walk(wizard, quest, save);

        Assert.Equal(UmbralSightState.Unlocked, UmbralSight.StateOf(wizard));
        Assert.Equal(UmbralSight.UNLOCKED_BY_WALK, wizard.QuestBehavior.GetRegistryValue(UmbralSight.STATE_KEY));
        Assert.True(UmbralSight.CanPerceive(wizard));
        Assert.True(save.Count > 0);
        Assert.True(wizard.HasCompletedQuest(Nightside));

        // The quest is the game's: its template, goals and the six school spells are unchanged.
        Assert.Equal(before, Snapshot(quest));
        Assert.Equal(6, quest.m_endResults.m_results.OfType<ResLearnSpell>().Count());
        Assert.Equal(1, quest.m_endResults.m_results.OfType<ResDropTable>().Count());
    }

    [Theory]
    [MemberData(nameof(Originals))]
    public void AnOriginalSchoolNeverReceivesIt(MagicSchool school) {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var save = new Recorder();
        var wizard = WizardBeforeNightside(school);
        var registryBefore = wizard.QuestBehavior.Registry.Keys.ToHashSet();

        Walk(wizard, quest, save);

        Assert.Equal(UmbralSightState.Locked, UmbralSight.StateOf(wizard));
        Assert.False(UmbralSight.CanPerceive(wizard));
        Assert.Equal(0, save.Count);
        // Only the quest's own completion flag is new; nothing of Umbral Sight is written for this wizard.
        Assert.Equal([$"{Nightside}_Complete"], wizard.QuestBehavior.Registry.Keys.Except(registryBefore).ToArray());

        // A copied entry on a non-Shadow wizard is not honoured either.
        wizard.QuestBehavior.SetRegistryValue(UmbralSight.STATE_KEY, UmbralSight.UNLOCKED_BY_WALK);
        Assert.False(UmbralSight.CanPerceive(wizard));
        Assert.False(UmbralSight.StaffReconcile(wizard, save.Save, out _));
    }

    public static IEnumerable<object[]> Originals() => OriginalSchools.Select(s => new object[] { s });

    [Theory]
    [InlineData("command-goal")]
    [InlineData("command-start")]
    [InlineData("catch-up")]
    [InlineData("out-of-order")]
    [InlineData("skipped-goal")]
    [InlineData("command-completion")]
    [InlineData("not-enrolled")]
    [InlineData("missing-predecessor")]
    public void AShortcutIsNotEvidenceOfTheWalk(string shortcut) {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var save = new Recorder();
        var wizard = WizardBeforeNightside(MagicSchool.Shadow,
            enrolled: shortcut != "not-enrolled", skip: shortcut == "missing-predecessor" ? "WC-HAUNTED-MAIN-001" : null);
        var mod = Mod(save);

        wizard.QuestBehavior.AddQuest(new QuestInstance(quest, wizard.CharId));
        QuestProgressEvents.QuestStarted(wizard, quest,
            shortcut == "command-start" ? QuestProgressSource.Command : QuestProgressSource.Play, mod);

        var goals = quest.m_goals.ToList();
        if (shortcut == "out-of-order") {
            (goals[1], goals[2]) = (goals[2], goals[1]);
        }

        if (shortcut == "skipped-goal") {
            goals.RemoveAt(2);
        }

        for (var i = 0; i < goals.Count; i++) {
            var source = (shortcut, i) switch {
                ("command-goal", 2) => QuestProgressSource.Command,
                ("catch-up", 3) => QuestProgressSource.CatchUp,
                // .quest complete walks the goals that are left as commands, then completes the quest.
                ("command-completion", 4) => QuestProgressSource.Command,
                _ => QuestProgressSource.Play,
            };
            QuestProgressEvents.GoalCompleted(wizard, quest, goals[i], source, mod);
        }

        wizard.QuestBehavior.CompleteQuest(Nightside);
        QuestProgressEvents.QuestCompleted(wizard, quest,
            shortcut == "command-completion" ? QuestProgressSource.Command : QuestProgressSource.Play, mod);

        Assert.True(wizard.HasCompletedQuest(Nightside));
        Assert.NotEqual(UmbralSightState.Unlocked, UmbralSight.StateOf(wizard));
        Assert.False(UmbralSight.CanPerceive(wizard));
        // A login afterwards does not turn the broken trail into an unlock either.
        Assert.False(UmbralSight.Reconcile(wizard, save.Save));
    }

    [Fact]
    public void TheUnlockIsGrantedOnceAndSurvivesARepeatedCompletion() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var save = new Recorder();
        var wizard = WizardBeforeNightside(MagicSchool.Shadow);

        Walk(wizard, quest, save);
        var saves = save.Count;
        var registry = JsonSerializer.Serialize(wizard.QuestBehavior.Registry.OrderBy(e => e.Key));

        QuestProgressEvents.QuestCompleted(wizard, quest, QuestProgressSource.Play, Mod(save));
        Assert.False(UmbralSight.Reconcile(wizard, save.Save));
        Assert.False(UmbralSight.StaffReconcile(wizard, save.Save, out var outcome));
        Assert.Contains("already", outcome);

        Assert.Equal(saves, save.Count);
        Assert.Equal(registry, JsonSerializer.Serialize(wizard.QuestBehavior.Registry.OrderBy(e => e.Key)));
        Assert.Equal(1, wizard.QuestBehavior.Registry.Keys.Count(k => k == UmbralSight.STATE_KEY));
    }

    [Fact]
    public void TheUnlockPersistsThroughSerializationAndReload() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var wizard = WizardBeforeNightside(MagicSchool.Shadow);
        Walk(wizard, quest, new Recorder());

        var reloaded = Reload(wizard);

        Assert.Equal(UmbralSightState.Unlocked, UmbralSight.StateOf(reloaded));
        Assert.True(UmbralSight.CanPerceive(reloaded));
        Assert.Equal(UmbralSight.TrailComplete, UmbralSight.TrailOf(reloaded));
        Assert.True(reloaded.HasCompletedQuest(Nightside));
        // Reloaded twice (a second restart): still the same.
        Assert.True(UmbralSight.CanPerceive(Reload(reloaded)));
    }

    [Fact]
    public void StartingTheQuestAgainClearsAnEarlierTrail() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var save = new Recorder();
        var wizard = WizardBeforeNightside(MagicSchool.Shadow);
        var mod = Mod(save);

        // A full first run that was never completed (the quest removed), then a run of QA commands.
        Accept(wizard, quest, save);
        foreach (var goal in quest.m_goals) {
            QuestProgressEvents.GoalCompleted(wizard, quest, goal, QuestProgressSource.Play, mod);
        }

        wizard.QuestBehavior.RemoveQuest(Nightside);
        wizard.QuestBehavior.AddQuest(new QuestInstance(quest, wizard.CharId));
        QuestProgressEvents.QuestStarted(wizard, quest, QuestProgressSource.Command, mod);
        Assert.Equal(0UL, UmbralSight.TrailOf(wizard));
        Assert.False(wizard.QuestBehavior.HasRegistryValue(UmbralSight.TRAIL_KEY));

        foreach (var goal in quest.m_goals) {
            QuestProgressEvents.GoalCompleted(wizard, quest, goal, QuestProgressSource.Command, mod);
        }

        wizard.QuestBehavior.CompleteQuest(Nightside);
        QuestProgressEvents.QuestCompleted(wizard, quest, QuestProgressSource.Play, mod);
        Assert.False(UmbralSight.CanPerceive(wizard));
    }

    [Fact]
    public void AnInterruptedUnlockIsRecoveredAtLoginFromTheRecordedWalk() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var save = new Recorder();
        var wizard = WizardBeforeNightside(MagicSchool.Shadow);
        Accept(wizard, quest, save);
        foreach (var goal in quest.m_goals) {
            QuestProgressEvents.GoalCompleted(wizard, quest, goal, QuestProgressSource.Play, Mod(save));
        }

        // The server stopped after the quest's own flag was saved and before the completion was reported.
        wizard.QuestBehavior.CompleteQuest(Nightside);
        Assert.False(UmbralSight.CanPerceive(wizard));

        new ShadowSchoolMod(save.Save).OnAttached(null!, wizard);

        Assert.True(UmbralSight.CanPerceive(wizard));
        Assert.Equal(UmbralSight.UNLOCKED_BY_WALK, wizard.QuestBehavior.GetRegistryValue(UmbralSight.STATE_KEY));
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(3UL)]
    [InlineData(ulong.MaxValue)]
    public void AMalformedStateFailsSafe(ulong value) {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var save = new Recorder();
        var wizard = WizardBeforeNightside(MagicSchool.Shadow);
        wizard.QuestBehavior.SetRegistryValue(UmbralSight.STATE_KEY, value);

        Assert.Equal(UmbralSightState.Malformed, UmbralSight.StateOf(wizard));
        Assert.False(UmbralSight.CanPerceive(wizard));
        Assert.True(Perceives(wizard).IsEmpty);

        // Play does not overwrite it, even with a complete walk; the quest still completes normally.
        Walk(wizard, quest, save);
        Assert.True(wizard.HasCompletedQuest(Nightside));
        Assert.Equal(value, wizard.QuestBehavior.GetRegistryValue(UmbralSight.STATE_KEY));
        Assert.False(UmbralSight.CanPerceive(wizard));

        // An explicit staff reconciliation may repair it.
        Assert.True(UmbralSight.StaffReconcile(wizard, save.Save, out _));
        Assert.Equal(UmbralSight.UNLOCKED_BY_STAFF, wizard.QuestBehavior.GetRegistryValue(UmbralSight.STATE_KEY));
        Assert.True(UmbralSight.CanPerceive(wizard));
    }

    [Fact]
    public void AMalformedTrailNeverUnlocks() {
        InitializeIsolatedTestConfiguration();
        var save = new Recorder();
        var wizard = WizardBeforeNightside(MagicSchool.Shadow);
        wizard.QuestBehavior.SetQuestRegistryValue(Nightside, "Complete", 1);

        foreach (var trail in new[] { 0UL, UmbralSight.TrailBeforeReturn, UmbralSight.TrailComplete & ~UmbralSight.TRAIL_STARTED }) {
            wizard.QuestBehavior.SetRegistryValue(UmbralSight.TRAIL_KEY, trail);
            Assert.False(UmbralSight.Reconcile(wizard, save.Save));
        }

        // Extra bits do not hurt a complete walk (a later version may record more), but a gap always does.
        wizard.QuestBehavior.SetRegistryValue(UmbralSight.TRAIL_KEY, ulong.MaxValue);
        Assert.True(UmbralSight.Reconcile(wizard, save.Save));
    }

    [Fact]
    public void StaffReconciliationIsOnlyForAShadowWizardWhoCompletedTheChain() {
        InitializeIsolatedTestConfiguration();
        var save = new Recorder();

        var notThere = WizardBeforeNightside(MagicSchool.Shadow);
        Assert.False(UmbralSight.StaffReconcile(notThere, save.Save, out _));

        var fresh = NewWizard(MagicSchool.Shadow);
        fresh.QuestBehavior.SetQuestRegistryValue(Nightside, "Complete", 1);
        Assert.False(UmbralSight.StaffReconcile(fresh, save.Save, out _));

        var legacy = WizardBeforeNightside(MagicSchool.Shadow);
        legacy.QuestBehavior.SetQuestRegistryValue(Nightside, "Complete", 1);
        Assert.True(UmbralSight.StaffReconcile(legacy, save.Save, out var outcome));
        Assert.Contains("unlocked", outcome);
        Assert.Equal(UmbralSight.UNLOCKED_BY_STAFF, legacy.QuestBehavior.GetRegistryValue(UmbralSight.STATE_KEY));
        Assert.Equal(1, save.Count);
        Assert.False(UmbralSight.StaffReconcile(legacy, save.Save, out _));
        Assert.Equal(1, save.Count);
    }

    [Fact]
    public void AFailingObserverNeverStopsTheQuestOrTheOthers() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var save = new Recorder();
        var wizard = WizardBeforeNightside(MagicSchool.Shadow);
        IQuestProgressObserver[] observers = [new Throwing(), new ShadowSchoolMod(save.Save)];

        wizard.QuestBehavior.AddQuest(new QuestInstance(quest, wizard.CharId));
        QuestProgressEvents.QuestStarted(wizard, quest, QuestProgressSource.Play, observers);
        foreach (var goal in quest.m_goals) {
            QuestProgressEvents.GoalCompleted(wizard, quest, goal, QuestProgressSource.Play, observers);
        }

        wizard.QuestBehavior.CompleteQuest(Nightside);
        QuestProgressEvents.QuestCompleted(wizard, quest, QuestProgressSource.Play, observers);
        Assert.True(UmbralSight.CanPerceive(wizard));

        // Missing inputs are ignored rather than passed on.
        QuestProgressEvents.GoalCompleted(null!, quest, quest.m_goals[0], QuestProgressSource.Play, observers);
        QuestProgressEvents.GoalCompleted(wizard, quest, null!, QuestProgressSource.Play, observers);
        QuestProgressEvents.QuestCompleted(wizard, null!, QuestProgressSource.Play, observers);
    }

    private sealed class Throwing : IQuestProgressObserver {
        public void OnQuestStarted(Wizard wizard, QuestTemplate quest, QuestProgressSource source) => throw new InvalidOperationException("observer failure");
        public void OnGoalCompleted(Wizard wizard, QuestTemplate quest, GoalTemplate goal, QuestProgressSource source) => throw new InvalidOperationException("observer failure");
        public void OnQuestCompleted(Wizard wizard, QuestTemplate quest, QuestProgressSource source) => throw new InvalidOperationException("observer failure");
    }

    // ------------------------------------------------------------------ Dialogue

    [Fact]
    public void TheAfterimageIsShownOnlyAtTheEndOfAWalkedInvestigation() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var goal4 = quest.m_goals.Single(g => g.m_goalName == "Goal 4");
        var canonical = ((ActorDialogList) goal4.m_dialogList).m_dialogs.Single(d => d.m_dialogTag == "Completion");

        // Walked through Dworgyn: Ambrose sees the afterimage (this is shown before Goal 4 is recorded).
        var walked = WizardBeforeNightside(MagicSchool.Shadow);
        Accept(walked, quest, new Recorder());
        foreach (var goal in quest.m_goals.Take(4)) {
            QuestProgressEvents.GoalCompleted(walked, quest, goal, QuestProgressSource.Play, Mod(new Recorder()));
        }

        var shown = QuestDialogueResolver.Resolve(walked, quest, goal4, canonical, Providers);
        Assert.Equal(["WizQst1731AF_00000025", "ShadowSchool_NightsideAfterimage", "ShadowSchool_NightsideAfterimageHold"], Keys(shown));

        // The C2.3 QA edge case: Goal 4 completed straight away by a command. The canonical return, no afterimage.
        var shortcut = WizardBeforeNightside(MagicSchool.Shadow);
        Accept(shortcut, quest, new Recorder());
        Assert.Same(canonical, QuestDialogueResolver.Resolve(shortcut, quest, goal4, canonical, Providers));

        // Mortis and Dworgyn done by command, the rest walked: still no afterimage.
        var mixed = WizardBeforeNightside(MagicSchool.Shadow);
        Accept(mixed, quest, new Recorder());
        foreach (var (goal, i) in quest.m_goals.Take(4).Select((g, i) => (g, i))) {
            QuestProgressEvents.GoalCompleted(mixed, quest, goal,
                i is 1 or 3 ? QuestProgressSource.Command : QuestProgressSource.Play, Mod(new Recorder()));
        }

        Assert.Same(canonical, QuestDialogueResolver.Resolve(mixed, quest, goal4, canonical, Providers));
    }

    [Fact]
    public void AmbroseAcknowledgesTheLastingSightOnlyOnceItIsUnlocked() {
        InitializeIsolatedTestConfiguration();
        var nightside = LoadQuest(NightsideFixture);
        var krokotopia = LoadQuest(KrokotopiaFixture);
        var before = Snapshot(krokotopia);
        var prep = ((ActorDialogList) krokotopia.m_dialogList).m_dialogs.Single(d => d.m_dialogTag == "Prep");

        var unlocked = WizardBeforeNightside(MagicSchool.Shadow);
        Walk(unlocked, nightside, new Recorder());
        var shown = QuestDialogueResolver.Resolve(unlocked, krokotopia, null, prep, Providers);

        Assert.Equal([
            "ShadowSchool_UmbralSightAmbroseLasting", "ShadowSchool_UmbralSightAmbroseVespera",
            "WizQst1731B0_00000000", "WizQst1731B0_00000001", "WizQst1731B0_00000002", "WizQst1731B0_00000003", "WizQst1731B0_00000004",
        ], Keys(shown));
        for (var i = 0; i < 5; i++) {
            Assert.Same(prep.m_dialogEntries[i], shown.m_dialogEntries[i + 2]);
        }

        foreach (var added in shown.m_dialogEntries.Take(2)) {
            Assert.Equal("", (string) added.m_soundFile);
            Assert.Equal(((NPCDialogEntry) prep.m_dialogEntries[0]).m_personaName, ((NPCDialogEntry) added).m_personaName);
        }

        // Locked Shadow (the quest finished by a shortcut), each original school after a full walk, and a Shadow
        // wizard who already left for Krokotopia all get the canonical briefing.
        var shortcut = WizardBeforeNightside(MagicSchool.Shadow);
        shortcut.QuestBehavior.SetQuestRegistryValue(Nightside, "Complete", 1);
        Assert.Same(prep, QuestDialogueResolver.Resolve(shortcut, krokotopia, null, prep, Providers));
        foreach (var school in OriginalSchools) {
            var wizard = WizardBeforeNightside(school);
            Walk(wizard, nightside, new Recorder());
            Assert.Same(prep, QuestDialogueResolver.Resolve(wizard, krokotopia, null, prep, Providers));
            Assert.Same(krokotopia, QuestDialogueResolver.ResolveTemplate(wizard, krokotopia, Providers));
        }

        unlocked.QuestBehavior.SetQuestRegistryValue(Krokotopia, "Complete", 1);
        Assert.Same(prep, QuestDialogueResolver.Resolve(unlocked, krokotopia, null, prep, Providers));
        Assert.Equal(before, Snapshot(krokotopia));
    }

    [Fact]
    public void AnUnlockedShadowWizardSeesNoOtherQuestChangeAcrossTheWholeQuestDatabase() {
        InitializeIsolatedTestConfiguration();
        var folder = Path.Combine(RepoRoot(), "spiraldb-cache", "QuestTemplates");
        var files = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.json") : [];
        var quests = files.Length > 0
            ? files.Select(f => JsonSerializer.Deserialize<QuestTemplate>(File.ReadAllText(f), SpiralJsonOptions.Options)!).ToList()
            : [LoadQuest(NightsideFixture), LoadQuest(KrokotopiaFixture)];

        var shadow = WizardBeforeNightside(MagicSchool.Shadow);
        Walk(shadow, quests.Single(q => q.m_questName == Nightside), new Recorder());
        var fire = WizardBeforeNightside(MagicSchool.Fire);
        Walk(fire, quests.Single(q => q.m_questName == Nightside), new Recorder());

        var changed = new List<string>();
        foreach (var quest in quests) {
            Assert.Same(quest, QuestDialogueResolver.ResolveTemplate(fire, quest, Providers));
            if (!ReferenceEquals(quest, QuestDialogueResolver.ResolveTemplate(shadow, quest, Providers))) {
                changed.Add(quest.m_questName);
            }
        }

        Assert.Equal([Krokotopia], changed);
    }

    // ------------------------------------------------------------------ Perception

    [Fact]
    public void PerceptionIsAHintOnApproachThenTheBindingAfterStandingStill() {
        InitializeIsolatedTestConfiguration();
        var wizard = UnlockedIn(Ravenwood);

        Assert.True(UmbralPerception.Moved(wizard, FarFromRoots.X, FarFromRoots.Y).IsEmpty);

        var hint = UmbralPerception.Moved(wizard, AtRoots.X, AtRoots.Y);
        Assert.Equal([UmbralPerception.HINT_KEY], hint.NotifyKeys);
        Assert.Empty(hint.ChatKeys);
        Assert.True(UmbralPerception.Moved(wizard, AtRoots.X + 10, AtRoots.Y).IsEmpty);

        // Not long enough still: nothing. Then the deliberate look shows the binding, once.
        Assert.True(UmbralPerception.Still(wizard, AtRoots.X, AtRoots.Y, UmbralPerception.FocusDelay / 2).IsEmpty);
        var seen = UmbralPerception.Still(wizard, AtRoots.X, AtRoots.Y, UmbralPerception.FocusDelay);
        Assert.Equal([UmbralPerception.NOTICE_KEY], seen.NotifyKeys);
        Assert.Equal(["Umbral_RavenwoodRoots"], seen.ChatKeys);
        Assert.True(UmbralPerception.Still(wizard, AtRoots.X, AtRoots.Y, TimeSpan.FromMinutes(1)).IsEmpty);

        // Walking well away and back lets it be noticed again.
        Assert.True(UmbralPerception.Moved(wizard, FarFromRoots.X, FarFromRoots.Y).IsEmpty);
        Assert.Equal([UmbralPerception.HINT_KEY], UmbralPerception.Moved(wizard, AtRoots.X, AtRoots.Y).NotifyKeys);
        Assert.Equal(["Umbral_RavenwoodRoots"], UmbralPerception.Still(wizard, AtRoots.X, AtRoots.Y, UmbralPerception.FocusDelay).ChatKeys);
        UmbralPerception.Detach(wizard.CharId);
    }

    [Fact]
    public void OtherPlayersPerceptionStaysIsolated() {
        InitializeIsolatedTestConfiguration();
        var seer = UnlockedIn(Ravenwood);
        var lockedShadow = In(WizardBeforeNightside(MagicSchool.Shadow), Ravenwood);
        var fire = In(WizardBeforeNightside(MagicSchool.Fire), Ravenwood);
        var elsewhere = UnlockedIn("WizardCity/WC_NightSide");

        Assert.False(UmbralPerception.Moved(seer, AtRoots.X, AtRoots.Y).IsEmpty);
        Assert.True(UmbralPerception.Moved(lockedShadow, AtRoots.X, AtRoots.Y).IsEmpty);
        Assert.True(UmbralPerception.Moved(fire, AtRoots.X, AtRoots.Y).IsEmpty);
        // The same coordinates in another zone are not the roots.
        Assert.True(UmbralPerception.Moved(elsewhere, AtRoots.X, AtRoots.Y).IsEmpty);

        Assert.False(UmbralPerception.Still(seer, AtRoots.X, AtRoots.Y, UmbralPerception.FocusDelay).IsEmpty);
        Assert.True(UmbralPerception.Still(lockedShadow, AtRoots.X, AtRoots.Y, UmbralPerception.FocusDelay).IsEmpty);
        Assert.True(UmbralPerception.Still(fire, AtRoots.X, AtRoots.Y, UmbralPerception.FocusDelay).IsEmpty);

        // One wizard's noticing does not use up another's: a second seer arriving later is told too.
        var second = UnlockedIn(Ravenwood);
        Assert.False(UmbralPerception.Moved(second, AtRoots.X, AtRoots.Y).IsEmpty);
        Assert.Equal(["Umbral_RavenwoodRoots"], UmbralPerception.Still(second, AtRoots.X, AtRoots.Y, UmbralPerception.FocusDelay).ChatKeys);
        Assert.NotSame(UmbralPerception.WatchOf(seer.CharId), UmbralPerception.WatchOf(second.CharId));

        foreach (var w in new[] { seer, lockedShadow, fire, elsewhere, second }) {
            UmbralPerception.Detach(w.CharId);
        }
    }

    [Fact]
    public void TheAbilityStillWorksAfterZoneChangesAndAReconnect() {
        InitializeIsolatedTestConfiguration();
        var wizard = UnlockedIn(Ravenwood);
        Assert.Equal(["Umbral_RavenwoodRoots"], Look(wizard, AtRoots));

        // Zone change: the Nightside's own binding, and nothing of Ravenwood's.
        In(wizard, "WizardCity/WC_NightSide");
        Assert.True(UmbralPerception.Still(wizard, AtRoots.X, AtRoots.Y, UmbralPerception.FocusDelay).IsEmpty);
        Assert.Equal(["Umbral_NightsideThread"], Look(wizard, (-700, -1150)));

        // Back to Ravenwood: a fresh zone visit, so the roots can be perceived again.
        In(wizard, Ravenwood);
        Assert.Equal(["Umbral_RavenwoodRoots"], Look(wizard, AtRoots));

        // Reconnect: the session ends, the character is loaded again from its record.
        UmbralPerception.Detach(wizard.CharId);
        Assert.Null(UmbralPerception.WatchOf(wizard.CharId));
        var reloaded = In(Reload(wizard), Ravenwood);
        Assert.Equal(["Umbral_RavenwoodRoots"], Look(reloaded, AtRoots));

        // A zone change the attach missed (a stale watch) is noticed by the next move.
        reloaded.Zone = "WizardCity/Interiors/WC_LibraryBookStacks";
        Assert.Equal(["Umbral_UnlistedAisle"], Look(reloaded, (6420, -1080)));
        UmbralPerception.Detach(reloaded.CharId);
    }

    [Fact]
    public void AnUnlockWhileInAZoneTakesEffectWithoutRelogging() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadQuest(NightsideFixture);
        var wizard = In(WizardBeforeNightside(MagicSchool.Shadow), "WizardCity/Interiors/WC_Headmistress_House");
        Assert.True(UmbralPerception.Moved(wizard, -377, 280).IsEmpty);

        Walk(wizard, quest, new Recorder());

        Assert.Equal(["Umbral_AmbroseWards"], Look(wizard, (-360, 300)));
        UmbralPerception.Detach(wizard.CharId);
    }

    [Fact]
    public void EveryBindingHasTextThatStaysWithinItsLimits() {
        var strings = ShadowText.Load(ModFolder()).Section(ShadowSchoolContent.LANG_NAME).ToDictionary(e => e.Key, e => e.Text);
        var keys = UmbralPerception.Bindings.Select(b => b.TextKey)
            .Concat([UmbralPerception.HINT_KEY, UmbralPerception.NOTICE_KEY])
            .ToList();

        Assert.Equal(UmbralPerception.Bindings.Count, UmbralPerception.Bindings.Select(b => b.Id).Distinct().Count());
        foreach (var binding in UmbralPerception.Bindings) {
            Assert.StartsWith("WizardCity/", binding.Zone);
            Assert.InRange(binding.Radius, 100, 1000);
        }

        foreach (var key in keys) {
            Assert.True(strings.ContainsKey(key), key);
            Assert.InRange(strings[key].Length, 20, 260);
            // Perception only: nothing bound is broken, controlled or explained, and the later story stays unsaid.
            foreach (var banned in new[] { "Morganthe", "pact", "break", "control", "command", "unlock", "defeat", "Malistaire", "Krokotopia" }) {
                Assert.DoesNotContain(banned, strings[key], StringComparison.OrdinalIgnoreCase);
            }
        }

        foreach (var key in new[] { "UmbralSightAmbroseLasting", "UmbralSightAmbroseVespera", "UmbralLessonOffer",
                                    "UmbralLessonSeeing", "UmbralLessonLimits", "UmbralLessonPractice" }) {
            Assert.InRange(strings[key].Length, 40, 320);
            foreach (var banned in new[] { "Morganthe", "pact", "conquer", "defeat", "army" }) {
                Assert.DoesNotContain(banned, strings[key], StringComparison.OrdinalIgnoreCase);
            }
        }

        // Vespera's point: perceiving is not understanding or controlling. Ambrose stays protective.
        Assert.Contains("not understanding", strings["UmbralLessonLimits"]);
        Assert.Contains("cannot loosen", strings["UmbralLessonLimits"]);
        Assert.Contains("responsibility", strings["UmbralSightAmbroseLasting"]);
    }

    // ------------------------------------------------------------------ Vespera's lesson (a mod quest)

    [Fact]
    public void VesperasLessonIsOfferedOnlyWithTheSightAndGivesNothing() {
        InitializeIsolatedTestConfiguration();
        var path = Path.Combine(ModFolder(), "spiraldb", "QuestTemplates", $"questtemplates_{Lesson}.json");
        var lesson = JsonSerializer.Deserialize<QuestTemplate>(File.ReadAllText(path), SpiralJsonOptions.Options)!;
        var strings = ShadowText.Load(ModFolder()).Section(ShadowSchoolContent.LANG_NAME).ToDictionary(e => e.Key, e => e.Text);

        Assert.Equal(Lesson, (string) lesson.m_questName);
        var requirements = lesson.m_requirements.m_requirements;
        Assert.Contains(requirements, r => r is ReqIsSchool { m_applyNOT: false } s && (string) s.m_magicSchoolName == "Shadow");
        Assert.Contains(requirements, r => r is ReqHasEntry { m_isQuestRegistry: false, m_applyNOT: false } e
            && (string) e.m_entryName == UmbralSight.STATE_KEY);
        Assert.Contains(requirements, r => r is ReqHasEntry { m_isQuestRegistry: true } e
            && (string) e.m_entryName == "Complete" && (string) e.m_questName == Nightside);
        Assert.Empty(lesson.m_endResults.m_results);
        Assert.Empty(lesson.m_startResults.m_results);
        var goal = Assert.Single(lesson.m_goals);
        Assert.Empty(goal.m_completeResults.m_results);

        // Vespera offers it and is the one the wizard talks to; every line is the mod's own, unvoiced.
        var prep = ((ActorDialogList) lesson.m_dialogList).m_dialogs.Single(d => d.m_dialogTag == "Prep");
        Assert.Equal(ShadowSchoolMod.TUTOR_TEMPLATE_ID, (uint) prep.m_dialogEntries[0].m_actorTemplateID);
        var lines = ((ActorDialogList) goal.m_dialogList).m_dialogs.SelectMany(d => d.m_dialogEntries).Concat(prep.m_dialogEntries).ToList();
        foreach (var line in lines) {
            Assert.Equal("", (string) line.m_soundFile);
            Assert.StartsWith("ShadowSchool_", (string) line.m_dialog);
            Assert.True(strings.ContainsKey(((string) line.m_dialog)["ShadowSchool_".Length..]), line.m_dialog);
        }

        foreach (var key in new[] { (string) lesson.m_questTitle, (string) lesson.m_questInfo, (string) goal.m_goalTitle }) {
            Assert.True(strings.ContainsKey(key["ShadowSchool_".Length..]), key);
        }
    }

    // ------------------------------------------------------------------ helpers

    private sealed class Recorder {

        internal int Count { get; private set; }

        internal void Save(Wizard wizard) => Count++;

    }

    private static readonly Imlight.CoreLib.Game.Quests.IQuestDialogueProvider[] Providers = [new ShadowSchoolMod(_ => { })];

    private static IQuestProgressObserver[] Mod(Recorder save) => [new ShadowSchoolMod(save.Save)];

    private static void Accept(Wizard wizard, QuestTemplate quest, Recorder save) {
        wizard.QuestBehavior.AddQuest(new QuestInstance(quest, wizard.CharId));
        QuestProgressEvents.QuestStarted(wizard, quest, QuestProgressSource.Play, Mod(save));
    }

    /// <summary>The whole quest as a player plays it: accepted, each goal in the goal logic's order, then completed.</summary>
    private static void Walk(Wizard wizard, QuestTemplate quest, Recorder save) {
        Accept(wizard, quest, save);
        var order = quest.m_goalLogic.Select(l => (string) l.m_goalsAND.Single()).ToList();
        Assert.Equal(UmbralSight.OriginGoals, order);
        foreach (var name in order) {
            QuestProgressEvents.GoalCompleted(wizard, quest, quest.m_goals.Single(g => g.m_goalName == name), QuestProgressSource.Play, Mod(save));
        }

        Assert.True(wizard.QuestBehavior.CompleteQuest(Nightside));
        QuestProgressEvents.QuestCompleted(wizard, quest, QuestProgressSource.Play, Mod(save));
    }

    private static UmbralPerception.Shown Perceives(Wizard wizard) {
        In(wizard, Ravenwood);
        UmbralPerception.Moved(wizard, AtRoots.X, AtRoots.Y);
        var shown = UmbralPerception.Still(wizard, AtRoots.X, AtRoots.Y, UmbralPerception.FocusDelay);
        UmbralPerception.Detach(wizard.CharId);

        return shown;
    }

    private static IReadOnlyList<string> Look(Wizard wizard, (float X, float Y) at) {
        UmbralPerception.Moved(wizard, at.X, at.Y);

        return UmbralPerception.Still(wizard, at.X, at.Y, UmbralPerception.FocusDelay).ChatKeys;
    }

    private static Wizard In(Wizard wizard, string zone) {
        wizard.Zone = zone;
        UmbralPerception.Attach(wizard);

        return wizard;
    }

    private static Wizard UnlockedIn(string zone) {
        var wizard = WizardBeforeNightside(MagicSchool.Shadow);
        Walk(wizard, LoadQuest(NightsideFixture), new Recorder());

        return In(wizard, zone);
    }

    /// <summary>Enrolled, the three streets and the three quests before the Nightside done.</summary>
    private static Wizard WizardBeforeNightside(MagicSchool school, bool enrolled = true, string? skip = null) {
        var wizard = NewWizard(school);
        var done = new List<string>();
        if (enrolled) {
            done.Add(school == MagicSchool.Shadow
                ? ShadowMainlineDialogue.SHADOW_ENROLLMENT
                : $"WC-COMMONS-MAIN-002-{school.ToString().ToUpperInvariant()}");
        }

        done.AddRange(ShadowMainlineDialogue.StreetFinals);
        done.AddRange(["WC-OLDE-MAIN-003A", "WC-HAUNTED-MAIN-001", "WC-HAUNTED-MAIN-002"]);
        foreach (var quest in done.Where(q => q != skip)) {
            wizard.QuestBehavior.SetQuestRegistryValue(quest, "Complete", 1);
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

        return new Wizard {
            CharId = wizard.CharId,
            Zone = wizard.Zone,
            MagicSchoolBehavior = new ServerMagicSchoolBehavior {
                MagicSchool = wizard.MagicSchoolBehavior.MagicSchool, Level = wizard.MagicSchoolBehavior.Level,
            },
            QuestBehavior = behavior,
        };
    }

    private static string[] Keys(ActorDialog dialog) => [.. dialog.m_dialogEntries.Select(e => (string) e.m_dialog)];

    private static string Snapshot(QuestTemplate quest) => JsonSerializer.Serialize(quest, SpiralJsonOptions.Options);

    private static QuestTemplate LoadQuest(string file)
        => JsonSerializer.Deserialize<QuestTemplate>(File.ReadAllText(Path.Combine(RepoRoot(), "spiraldb-cache", "QuestTemplates", file)), SpiralJsonOptions.Options)!;

    private static string ModFolder() => Path.Combine(RepoRoot(), "src", "Imlight.Director", "mods", "ShadowSchool");

    private static string RepoRoot() {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Imlight.Director", "mods", "ShadowSchool"))) {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not find the repository above the test folder.");
    }

    private static void InitializeIsolatedTestConfiguration() {
        var path = Path.Combine(Path.GetTempPath(), $"imlight-umbral-sight-tests-{Environment.ProcessId}-{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, $"[Logging]{Environment.NewLine}LogPath = {Path.Combine(Path.GetTempPath(), "imlight-umbral-sight-tests.log")}{Environment.NewLine}LogLevel = ERROR{Environment.NewLine}SeqSinkUrl = http://127.0.0.1:1{Environment.NewLine}");
        ConfigurationManager.Initialize(path);
        File.Delete(path);
    }

}
