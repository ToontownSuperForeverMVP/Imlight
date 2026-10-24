using System.Text.Json;
using Imcodec.ObjectProperty;
using Imcodec.ObjectProperty.TypeCache;
using Imlight.Common;
using Imlight.CoreLib.Game.Quests;
using Imlight.CoreLib.Game.Requirements;
using Imlight.CoreLib.Game.Requirements.Contexts;
using Imlight.CoreLib.Mods.ShadowSchool;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.WizardData;
using Imlight.CoreLib.WizardData.Models.Player;
using Xunit;

namespace Imlight.CoreLib.Tests.Mods.ShadowSchool;

/// <summary>
/// Stage C2.1: the per-recipient quest dialogue resolver and Ambrose's Shadow reunion response. The canonical quest is
/// read from the installed SpiralDB cache, and the provider is the
/// ShadowSchoolMod object the server registers, resolved through QuestDialogueResolver as every dialogue send does.
/// </summary>
[Collection("SharedConfiguration")]
public sealed class ShadowMainlineDialogueTests {

    private const string Quest = "WC-OLDE-MAIN-003A";
    private static readonly string[] CanonicalAmbroseKeys = [
        "WizQst1731AB_00000003", "WizQst1731AB_00000004", "WizQst1731AB_00000005",
        "WizQst1731AB_00000006", "WizQst1731AB_00000007", "WizQst1731AB_00000008",
    ];
    private static readonly string[] ShadowAmbroseKeys = [
        "ShadowSchool_ReunionAmbroseStreets",
        "WizQst1731AB_00000003", "WizQst1731AB_00000004", "WizQst1731AB_00000005",
        "ShadowSchool_ReunionAmbroseForbidden",
        "WizQst1731AB_00000006", "WizQst1731AB_00000007",
        "ShadowSchool_ReunionAmbroseEye",
        "WizQst1731AB_00000008",
    ];
    private static readonly MagicSchool[] OriginalSchools = [
        MagicSchool.Fire, MagicSchool.Ice, MagicSchool.Storm, MagicSchool.Myth,
        MagicSchool.Life, MagicSchool.Death, MagicSchool.Balance,
    ];
    private static readonly IQuestDialogueProvider[] Providers = [new ShadowSchoolMod()];

    [Fact]
    public void AShadowWizardAtTheReunionHearsAmbrosesShadowResponse() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadCanonicalQuest();
        var wizard = WizardAtReunion(MagicSchool.Shadow, quest);

        var shown = ResolveAmbrose(wizard, quest);

        Assert.Equal(ShadowAmbroseKeys, Keys(shown));
        Assert.Equal("Completion", (string) shown.m_dialogTag);
        var canonical = AmbroseCompletion(quest);
        Assert.Same(canonical.m_madlibs, shown.m_madlibs);

        // The canonical lines are the canonical objects, voice and all; each added line borrows the presentation of
        // the canonical line named in the script and has no voice.
        foreach (var (line, entry) in ShadowMainlineDialogue.AmbroseReunion.Script.Zip(shown.m_dialogEntries)) {
            var source = canonical.m_dialogEntries[line.Canonical];
            if (line.Text is null) {
                Assert.Same(source, entry);
                Assert.False(string.IsNullOrEmpty(entry.m_soundFile));
                continue;
            }

            Assert.Equal(string.Empty, (string) entry.m_soundFile);
            Assert.Equal(string.Empty, (string) entry.m_soundEffectFile);
            Assert.Equal(string.Empty, (string) entry.m_musicFile);
            Assert.Equal((string) source.m_picture, (string) entry.m_picture);
            Assert.Equal("GUI/NpcPortraits/Art_Portrait_Ambrose.dds", (string) entry.m_picture);
            Assert.Equal(source.m_actorTemplateID, entry.m_actorTemplateID);
            Assert.Equal((string) source.m_cameraName, (string) entry.m_cameraName);
            Assert.Equal((string) source.m_cameraZoneName, (string) entry.m_cameraZoneName);
            Assert.Equal((string) source.m_action, (string) entry.m_action);
            Assert.Same(source.m_dialogAnimationList, entry.m_dialogAnimationList);
            Assert.Equal(source.GetType(), entry.GetType());
        }
    }

    [Theory]
    [InlineData(MagicSchool.Fire)]
    [InlineData(MagicSchool.Death)]
    [InlineData(MagicSchool.Life)]
    [InlineData(MagicSchool.Ice)]
    [InlineData(MagicSchool.Storm)]
    [InlineData(MagicSchool.Myth)]
    [InlineData(MagicSchool.Balance)]
    public void AnOriginalSchoolWizardInTheSameStateHearsTheCanonicalResponse(MagicSchool school) {
        InitializeIsolatedTestConfiguration();
        var quest = LoadCanonicalQuest();
        var wizard = WizardAtReunion(school, quest);

        var shown = ResolveAmbrose(wizard, quest);

        Assert.Same(AmbroseCompletion(quest), shown);
        Assert.Equal(CanonicalAmbroseKeys, Keys(shown));
    }

    [Fact]
    public void RecipientsOfTheSameQuestNeverSeeEachOthersDialogue() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadCanonicalQuest();
        var before = Snapshot(quest);
        var shadow = WizardAtReunion(MagicSchool.Shadow, quest);
        var secondShadow = WizardAtReunion(MagicSchool.Shadow, quest);
        var fire = WizardAtReunion(MagicSchool.Fire, quest);
        var death = WizardAtReunion(MagicSchool.Death, quest);

        // Interleaved, as two party members in one zone would trigger it.
        var shownShadow = ResolveAmbrose(shadow, quest);
        var shownFire = ResolveAmbrose(fire, quest);
        var shownSecondShadow = ResolveAmbrose(secondShadow, quest);
        var shownDeath = ResolveAmbrose(death, quest);
        var shownShadowAgain = ResolveAmbrose(shadow, quest);

        Assert.Equal(ShadowAmbroseKeys, Keys(shownShadow));
        Assert.Equal(ShadowAmbroseKeys, Keys(shownSecondShadow));
        Assert.Equal(ShadowAmbroseKeys, Keys(shownShadowAgain));
        Assert.Equal(CanonicalAmbroseKeys, Keys(shownFire));
        Assert.Equal(CanonicalAmbroseKeys, Keys(shownDeath));

        // Each Shadow wizard gets an object of their own, so nothing one receives is shared state for another.
        Assert.NotSame(shownShadow, shownSecondShadow);
        Assert.NotSame(shownShadow.m_dialogEntries, shownSecondShadow.m_dialogEntries);
        Assert.Equal(before, Snapshot(quest));

        // A Shadow wizard who has not reached the reunion, next to one who has.
        var shadowMissingAStreet = WizardAtReunion(MagicSchool.Shadow, quest, skipStreet: "WC-CYCLOPS-MAIN-005");
        Assert.Equal(CanonicalAmbroseKeys, Keys(ResolveAmbrose(shadowMissingAStreet, quest)));
        Assert.Equal(ShadowAmbroseKeys, Keys(ResolveAmbrose(shadow, quest)));
    }

    [Fact]
    public void ResolvingNeverChangesTheSharedCanonicalTemplate() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadCanonicalQuest();
        var before = Snapshot(quest);
        var canonicalDialog = AmbroseCompletion(quest);
        var canonicalEntries = canonicalDialog.m_dialogEntries.ToArray();
        var canonicalSounds = canonicalEntries.Select(e => (string) e.m_soundFile).ToArray();

        foreach (var school in OriginalSchools.Append(MagicSchool.Shadow)) {
            var wizard = WizardAtReunion(school, quest);
            ResolveAmbrose(wizard, quest);
            QuestDialogueResolver.ResolveTemplate(wizard, quest, Providers);
            foreach (var dialog in AllDialogs(quest)) {
                QuestDialogueResolver.Resolve(wizard, quest, dialog.Goal, dialog.Dialog, Providers);
            }
        }

        Assert.Equal(before, Snapshot(quest));
        Assert.Same(canonicalDialog, AmbroseCompletion(quest));
        Assert.Equal(canonicalEntries, canonicalDialog.m_dialogEntries);
        Assert.Equal(CanonicalAmbroseKeys, Keys(canonicalDialog));
        Assert.Equal(canonicalSounds, canonicalDialog.m_dialogEntries.Select(e => (string) e.m_soundFile));
    }

    [Fact]
    public void TheShadowCopyOfTheQuestKeepsEveryGoalRequirementRewardAndFlag() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadCanonicalQuest();
        var shadow = WizardAtReunion(MagicSchool.Shadow, quest);

        // The quest offer cache sends the whole template; a Shadow wizard gets a copy differing only in that dialogue.
        var copy = QuestDialogueResolver.ResolveTemplate(shadow, quest, Providers);

        Assert.NotSame(quest, copy);
        Assert.Equal((string) quest.m_questName, (string) copy.m_questName);
        Assert.Equal((string) quest.m_questTitle, (string) copy.m_questTitle);
        Assert.Same(quest.m_requirements, copy.m_requirements);
        Assert.Same(quest.m_endResults, copy.m_endResults);
        Assert.Same(quest.m_startGoals, copy.m_startGoals);
        Assert.Same(quest.m_goalLogic, copy.m_goalLogic);
        Assert.Same(quest.m_dialogList, copy.m_dialogList);
        Assert.Equal(quest.m_mainline, copy.m_mainline);
        Assert.Equal(quest.m_questLevel, copy.m_questLevel);
        Assert.Equal(quest.m_goals.Count, copy.m_goals.Count);
        for (var i = 0; i < quest.m_goals.Count; i++) {
            var original = quest.m_goals[i];
            var shown = copy.m_goals[i];
            // Stage C2.2 adapts Bartleby, Foulgaze and Daisy's cave as well (ShadowCampaignDialogueTests).
            if (!new[] { "Goal", "Goal 1", "Goal 2", "Goal 3", "Goal 5" }.Contains((string) original.m_goalName)) {
                Assert.Same(original, shown);
                continue;
            }

            Assert.Equal(original.GetType(), shown.GetType());
            Assert.Equal(GoalJsonWithoutDialogue(original), GoalJsonWithoutDialogue(shown));
            if (original.m_goalName == ShadowMainlineDialogue.REUNION_AMBROSE_GOAL) {
                Assert.Equal(ShadowAmbroseKeys, Keys(Completion(shown)));
            }
        }

        // The installed contract itself: three AND street flags, six goals, the reunion's own drop table.
        var requirements = quest.m_requirements.m_requirements.Cast<ReqHasEntry>().ToArray();
        Assert.Equal(["WC-TRITON-MAIN-009", "WC-CYCLOPS-MAIN-005", "WC-FIRECAT-MAIN-007"],
            requirements.Select(r => (string) r.m_questName));
        Assert.All(requirements, r => {
            Assert.Equal("Complete", (string) r.m_entryName);
            Assert.True(r.m_isQuestRegistry);
            Assert.False(r.m_applyNOT);
        });
        Assert.Equal(["Goal", "Goal 1", "Goal 2", "Goal 3", "Goal 4", "Goal 5"], copy.m_goals.Select(g => (string) g.m_goalName));
        Assert.Equal(GOAL_TYPE.GOAL_TYPE_PERSONA, copy.m_goals[0].m_goalType);
        Assert.Equal("WC-OLDE-MAIN-003A", (string) Assert.IsType<ResDropTable>(Assert.Single(copy.m_endResults.m_results)).m_tableName);

        // Completion is the quest's own flag, whatever dialogue was shown (the quest behavior a save persists).
        Assert.True(shadow.QuestBehavior.CompleteQuest(Quest));
        Assert.True(shadow.QuestBehavior.HasQuestRegistryValue(Quest, "Complete"));
        Assert.True(shadow.HasCompletedQuest(Quest));
    }

    [Fact]
    public void TheShadowResponseWaitsForEveryStreetTheEnrollmentAndAnUnfinishedReunion() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadCanonicalQuest();

        foreach (var street in ShadowMainlineDialogue.StreetFinals) {
            var missingOne = WizardAtReunion(MagicSchool.Shadow, quest, skipStreet: street);
            Assert.Same(AmbroseCompletion(quest), ResolveAmbrose(missingOne, quest));

            // The canonical quest is not offered to them either: the same flags gate both.
            Assert.False(RequirementsMet(quest, missingOne));
        }

        var notEnrolled = WizardAtReunion(MagicSchool.Shadow, quest, enrolled: false);
        Assert.Same(AmbroseCompletion(quest), ResolveAmbrose(notEnrolled, quest));

        var fresh = NewWizard(MagicSchool.Shadow);
        Assert.Same(AmbroseCompletion(quest), ResolveAmbrose(fresh, quest));

        var finished = WizardAtReunion(MagicSchool.Shadow, quest);
        Assert.True(finished.QuestBehavior.CompleteQuest(Quest));
        Assert.Same(AmbroseCompletion(quest), ResolveAmbrose(finished, quest));

        // Ready for the reunion: the canonical requirements pass and the response is the Shadow one.
        var ready = WizardAtReunion(MagicSchool.Shadow, quest);
        Assert.True(RequirementsMet(quest, ready));
        Assert.Equal(ShadowAmbroseKeys, Keys(ResolveAmbrose(ready, quest)));
    }

    [Fact]
    public void EverythingElseFallsBackToTheCanonicalDialogue() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadCanonicalQuest();
        var shadow = WizardAtReunion(MagicSchool.Shadow, quest);

        // Every dialogue of the same quest that has no Shadow version is the game's own.
        foreach (var (goal, dialog) in AllDialogs(quest)) {
            var shown = QuestDialogueResolver.Resolve(shadow, quest, goal, dialog, Providers);
            // The reunion and, from Stage C2.2, Bartleby, Foulgaze and Daisy's cave have a Shadow version; the offer
            // (Muldoon) and the cave entrance (goal 4) are still the game's own.
            if (goal is not null && (string) dialog.m_dialogTag == "Completion"
                && new[] { "Goal", "Goal 1", "Goal 2", "Goal 3", "Goal 5" }.Contains((string) goal.m_goalName)) {
                Assert.NotSame(dialog, shown);
                continue;
            }

            Assert.Same(dialog, shown);
        }

        var canonical = AmbroseCompletion(quest);
        var ambroseGoal = quest.m_goals[0];

        // The same dialogue under another quest's name or another goal.
        var otherQuest = quest with { m_questName = "WC-HAUNTED-MAIN-001" };
        Assert.Same(canonical, QuestDialogueResolver.Resolve(shadow, otherQuest, ambroseGoal, canonical, Providers));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(shadow, quest, quest.m_goals[1], canonical, Providers));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(shadow, quest, null, canonical, Providers));

        // A game revision whose dialogue no longer holds the expected lines.
        var revised = canonical with {
            m_dialogEntries = [.. canonical.m_dialogEntries.Select((e, i) => i == 2 ? e with { m_dialog = "WizQst1731AB_00000099" } : e)],
        };
        Assert.Same(revised, QuestDialogueResolver.Resolve(shadow, quest, ambroseGoal, revised, Providers));
        var shorter = canonical with { m_dialogEntries = [.. canonical.m_dialogEntries.Take(5)] };
        Assert.Same(shorter, QuestDialogueResolver.Resolve(shadow, quest, ambroseGoal, shorter, Providers));

        // No recipient, no quest, no dialogue, no provider.
        Assert.Same(canonical, QuestDialogueResolver.Resolve(null, quest, ambroseGoal, canonical, Providers));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(shadow, null, ambroseGoal, canonical, Providers));
        Assert.Null(QuestDialogueResolver.Resolve(shadow, quest, ambroseGoal, null, Providers));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(shadow, quest, ambroseGoal, canonical, []));
        Assert.Same(quest, QuestDialogueResolver.ResolveTemplate(shadow, quest, []));
        Assert.Same(quest, QuestDialogueResolver.ResolveTemplate(WizardAtReunion(MagicSchool.Fire, quest), quest, Providers));

        // A provider that fails or answers with something unusable is ignored; the next one still gets its turn.
        Assert.Same(canonical, QuestDialogueResolver.Resolve(shadow, quest, ambroseGoal, canonical, [new Throwing()]));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(shadow, quest, ambroseGoal, canonical,
            [new Answering(canonical with { m_dialogTag = "Prep" })]));
        Assert.Same(canonical, QuestDialogueResolver.Resolve(shadow, quest, ambroseGoal, canonical,
            [new Answering(canonical with { m_dialogEntries = [] })]));
        Assert.Equal(ShadowAmbroseKeys, Keys(QuestDialogueResolver.Resolve(shadow, quest, ambroseGoal, canonical,
            [new Throwing(), .. Providers])));
    }

    [Fact]
    public void QuestStateAndTheResponseAreTheSameAfterAReconnect() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadCanonicalQuest();
        foreach (var school in new[] { MagicSchool.Shadow, MagicSchool.Fire }) {
            var wizard = WizardAtReunion(school, quest);
            var before = Keys(ResolveAmbrose(wizard, quest));

            // What a save keeps (the registry and the quest instances) read back into a new wizard object.
            var reloaded = Reload(wizard);

            Assert.True(reloaded.HasQuest(Quest));
            Assert.False(reloaded.HasCompletedQuest(Quest));
            Assert.All(ShadowMainlineDialogue.StreetFinals, flag => Assert.True(reloaded.HasCompletedQuest(flag)));
            Assert.Equal(before, Keys(ResolveAmbrose(reloaded, quest)));
            Assert.Equal(school == MagicSchool.Shadow ? ShadowAmbroseKeys : CanonicalAmbroseKeys, before);
        }

        // A Shadow wizard who finished the reunion before the reconnect is back to the game's dialogue.
        var finished = WizardAtReunion(MagicSchool.Shadow, quest);
        Assert.True(finished.QuestBehavior.CompleteQuest(Quest));
        Assert.Same(AmbroseCompletion(quest), ResolveAmbrose(Reload(finished), quest));
    }

    [Fact]
    public void TheShadowResponseSurvivesTheActorDialogWireFormat() {
        InitializeIsolatedTestConfiguration();
        var quest = LoadCanonicalQuest();
        var shown = ResolveAmbrose(WizardAtReunion(MagicSchool.Shadow, quest), quest);

        // The same serializer and flags QuestService.SendActorDialog uses for MSG_ACTORDIALOG.
        var serializer = new ObjectSerializer(Versionable: false);
        Assert.True(serializer.Serialize(shown, 16, out var data));
        Assert.True(new ObjectSerializer(Versionable: false).Deserialize<ActorDialog>(data, 16u, out var received));

        Assert.Equal(ShadowAmbroseKeys, Keys(received));
        Assert.Equal(shown.m_dialogEntries.Select(e => (string) e.m_soundFile), received.m_dialogEntries.Select(e => (string) e.m_soundFile));
        Assert.Equal(shown.m_dialogEntries.Select(e => (string) e.m_cameraName), received.m_dialogEntries.Select(e => (string) e.m_cameraName));
        Assert.Equal(shown.m_dialogEntries.Select(e => (string) e.m_picture), received.m_dialogEntries.Select(e => (string) e.m_picture));
    }

    [Fact]
    public void TheNewLinesExistAndKeepTheStoryEmbargo() {
        var text = ShadowText.Load(ModFolder());
        var strings = text.Section(ShadowSchoolContent.LANG_NAME).ToDictionary(e => e.Key, e => e.Text);
        var added = ShadowMainlineDialogue.AmbroseReunion.Script.Where(l => l.Text is not null).Select(l => l.Text).ToArray();

        Assert.Equal(["ReunionAmbroseStreets", "ReunionAmbroseForbidden", "ReunionAmbroseEye"], added);
        foreach (var key in added) {
            var line = strings[key];
            Assert.InRange(line.Length, 40, 240);
            Assert.DoesNotContain("Morganthe", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Umbral", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Nightside", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pact", line, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("Triton Avenue", strings["ReunionAmbroseStreets"]);
        Assert.Contains("Firecat Alley", strings["ReunionAmbroseStreets"]);
        Assert.Contains("Cyclops Lane", strings["ReunionAmbroseStreets"]);
        Assert.Contains("Vespera Quillmoor", strings["ReunionAmbroseStreets"]);
        Assert.Contains("forbade", strings["ReunionAmbroseForbidden"]);
        Assert.Contains("Eye", strings["ReunionAmbroseEye"]);

        // Ambrose only learns from Bartleby, in the next goal, that Malistaire took the Eye.
        Assert.DoesNotContain("Malistaire", strings["ReunionAmbroseEye"]);

        // The lines reach the client in the mod's own lang file, under the keys the dialogue names.
        var lang = ShadowSchoolFiles.ReadLang(
            ShadowSchoolFiles.WriteLang(ShadowSchoolContent.LANG_NAME, text.Section(ShadowSchoolContent.LANG_NAME)), out var name);
        Assert.Equal(ShadowSchoolContent.LANG_NAME, name);
        foreach (var key in added) {
            Assert.Equal(strings[key], lang.Single(e => e.Key == key).Text);
            Assert.StartsWith(name + "_", ShadowMainlineDialogue.TextKey(key));
        }
    }

    [Fact]
    public void TheModIsTheRegisteredDialogueProvider() {
        Assert.IsAssignableFrom<IQuestDialogueProvider>(new ShadowSchoolMod());
    }

    [Fact]
    public void NoOtherQuestOrSchoolIsAffectedAcrossTheWholeQuestDatabase() {
        InitializeIsolatedTestConfiguration();
        var folder = Path.Combine(RepoRoot(), "spiraldb-cache", "QuestTemplates");
        var files = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.json") : [];

        // Without the server's SpiralDB checkout this test covers the fixture alone.
        var quests = files.Length > 0
            ? files.Select(f => JsonSerializer.Deserialize<QuestTemplate>(File.ReadAllText(f), SpiralJsonOptions.Options)!).ToList()
            : [LoadCanonicalQuest()];

        var reunion = quests.Single(q => q.m_questName == Quest);
        var original = OriginalSchools.Select(s => WizardAtReunion(s, reunion)).ToArray();
        var shadow = WizardAtReunion(MagicSchool.Shadow, reunion);
        var changedForShadow = new List<string>();
        foreach (var quest in quests) {
            foreach (var wizard in original) {
                Assert.Same(quest, QuestDialogueResolver.ResolveTemplate(wizard, quest, Providers));
            }

            if (!ReferenceEquals(quest, QuestDialogueResolver.ResolveTemplate(shadow, quest, Providers))) {
                changedForShadow.Add(quest.m_questName);
            }
        }

        // Stage C2.2 adds the two Haunted Cave quests, which a Shadow wizard reaches after the reunion.
        foreach (var name in new[] { "WC-HAUNTED-MAIN-001", "WC-HAUNTED-MAIN-002" }) {
            var haunted = quests.Single(q => q.m_questName == name);
            var afterReunion = WizardAtReunion(MagicSchool.Shadow, reunion);
            afterReunion.QuestBehavior.SetQuestRegistryValue(Quest, "Complete", 1);
            if (name == "WC-HAUNTED-MAIN-002") {
                afterReunion.QuestBehavior.SetQuestRegistryValue("WC-HAUNTED-MAIN-001", "Complete", 1);
            }
            if (!ReferenceEquals(haunted, QuestDialogueResolver.ResolveTemplate(afterReunion, haunted, Providers))) {
                changedForShadow.Add(name);
            }

            foreach (var wizard in original) {
                Assert.Same(haunted, QuestDialogueResolver.ResolveTemplate(wizard, haunted, Providers));
            }
        }

        Assert.Equal([Quest, "WC-HAUNTED-MAIN-001", "WC-HAUNTED-MAIN-002"], changedForShadow);
    }

    // --- helpers ---

    private static ActorDialog ResolveAmbrose(Wizard wizard, QuestTemplate quest)
        => QuestDialogueResolver.Resolve(wizard, quest, quest.m_goals[0], AmbroseCompletion(quest), Providers);

    private static ActorDialog AmbroseCompletion(QuestTemplate quest) {
        var goal = quest.m_goals.Single(g => g.m_goalName == ShadowMainlineDialogue.REUNION_AMBROSE_GOAL);
        return Completion(goal);
    }

    private static ActorDialog Completion(GoalTemplate goal)
        => ((ActorDialogList) goal.m_dialogList).m_dialogs.Single(d => d.m_dialogTag == "Completion");

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

    private static Wizard WizardAtReunion(MagicSchool school, QuestTemplate quest, string? skipStreet = null, bool enrolled = true) {
        var wizard = NewWizard(school);
        if (enrolled) {
            // Each school's own enrollment; only Shadow's is the mod's.
            var enrollment = school == MagicSchool.Shadow
                ? ShadowMainlineDialogue.SHADOW_ENROLLMENT
                : $"WC-COMMONS-MAIN-002-{school.ToString().ToUpperInvariant()}";
            wizard.QuestBehavior.SetQuestRegistryValue(enrollment, "Complete", 1);
        }

        foreach (var street in ShadowMainlineDialogue.StreetFinals.Where(s => s != skipStreet)) {
            wizard.QuestBehavior.SetQuestRegistryValue(street, "Complete", 1);
        }

        wizard.QuestBehavior.AddQuest(new QuestInstance(quest, wizard.CharId));

        return wizard;
    }

    private static Wizard NewWizard(MagicSchool school) => new() {
        CharId = (ulong) Random.Shared.NextInt64(1, long.MaxValue),
        MagicSchoolBehavior = new ServerMagicSchoolBehavior { MagicSchool = school, Level = 10 },
        QuestBehavior = new ServerQuestBehavior(),
    };

    private static Wizard Reload(Wizard wizard) {
        // The registry is read back into the new behavior's own dictionary, as the document store populates it.
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

    private static QuestTemplate LoadCanonicalQuest()
        => JsonSerializer.Deserialize<QuestTemplate>(File.ReadAllText(Path.Combine(RepoRoot(), "spiraldb-cache", "QuestTemplates", $"questtemplates_{Quest}.json")), SpiralJsonOptions.Options)!;

    private static string ModFolder() => Path.Combine(RepoRoot(), "src", "Imlight.Director", "mods", "ShadowSchool");

    private static string RepoRoot() {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Imlight.Director", "mods", "ShadowSchool"))) {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not find the repository above the test folder.");
    }

    private static void InitializeIsolatedTestConfiguration() {
        var path = Path.Combine(Path.GetTempPath(), $"imlight-shadow-dialogue-tests-{Environment.ProcessId}-{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, $"[Logging]{Environment.NewLine}LogPath = {Path.Combine(Path.GetTempPath(), "imlight-shadow-dialogue-tests.log")}{Environment.NewLine}LogLevel = ERROR{Environment.NewLine}SeqSinkUrl = http://127.0.0.1:1{Environment.NewLine}");
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
