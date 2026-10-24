/*
 * The Shadow story told inside the canonical Wizard City quests: what a Shadow wizard hears in place of a few of the
 * game's own quest dialogues (docs/quest-dependency-map.md, Stage C2: the reunion, the Foulgaze investigation,
 * Throwing Nightshade, Things and Stuff and the Nightside; Stage C3: Ambrose once Umbral Sight has lasted).
 *
 * Each adaptation names one canonical dialogue (quest, goal, tag) and the exact text keys it is expected to hold, the
 * state the receiving wizard must be in, and a script: the canonical lines it keeps (with their recorded voice) and
 * the lines it adds. An added line borrows a canonical line's presentation (speaker, portrait, camera, animation)
 * and has no voice, since no recording says its words. QuestDialogueResolver asks for it per wizard, so another
 * wizard in the same quest, zone or party still sees the canonical dialogue, and the quest itself is unchanged.
 *
 * If the game's dialogue no longer holds the expected keys (a new game revision), the adaptation is left out and the
 * canonical dialogue is shown.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Imcodec.ObjectProperty.TypeCache;
using Imlight.Common;
using Imlight.CoreLib.Game.Quests;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal static class ShadowMainlineDialogue {

    internal const string SHADOW_ENROLLMENT = "WC-COMMONS-MAIN-002-SHADOW";
    internal static readonly string[] StreetFinals = ["WC-TRITON-MAIN-009", "WC-FIRECAT-MAIN-007", "WC-CYCLOPS-MAIN-005"];

    // A Brief History of the Spiral: available once all three streets are done; its first goal is to report to Ambrose.
    internal const string REUNION_QUEST = "WC-OLDE-MAIN-003A";
    internal const string REUNION_AMBROSE_GOAL = "Goal";

    /// <summary>
    /// One line of an adapted dialogue: the canonical line at <paramref name="Canonical"/> as it is, or, with
    /// <paramref name="Text"/>, the mod's line (ShadowSchool_<c>Text</c>) shown with that canonical line's presentation.
    /// </summary>
    internal sealed record Line(int Canonical, string Text = null);

    internal sealed record Adaptation(string Id, string QuestName, string GoalName, string DialogTag,
                                      string[] CanonicalKeys, Func<Wizard, bool> Applies, Line[] Script);

    /// <summary>
    /// Ambrose's reunion response. The canonical lines stay, voice and all: Malistaire's caves, Sylvia, the
    /// forbidden rituals, the Order of the Fang, Bartleby's Eyes. Three unvoiced lines add his recognition of the
    /// Shadow student's work on the streets, why his warning about Malistaire is meant for them too, and their
    /// interest in the missing Eye of History. He still does not know who took the Eye (he learns that from
    /// Bartleby in the next goal), and nothing here gives the wizard Umbral Sight or ties them to Morganthe.
    /// </summary>
    internal static readonly Adaptation AmbroseReunion = new(
        Id: "AmbroseReunion",
        QuestName: REUNION_QUEST,
        GoalName: REUNION_AMBROSE_GOAL,
        DialogTag: "Completion",
        CanonicalKeys: [
            "WizQst1731AB_00000003", "WizQst1731AB_00000004", "WizQst1731AB_00000005",
            "WizQst1731AB_00000006", "WizQst1731AB_00000007", "WizQst1731AB_00000008",
        ],
        Applies: AtAmbroseReunion,
        Script: [
            new Line(0, "ReunionAmbroseStreets"),
            new Line(0), new Line(1), new Line(2),
            new Line(2, "ReunionAmbroseForbidden"),
            new Line(3), new Line(4),
            new Line(4, "ReunionAmbroseEye"),
            new Line(5),
        ]);

    // Stage C2.2: the Foulgaze investigation (the rest of A Brief History of the Spiral), Throwing Nightshade and
    // Things and Stuff. Every canonical line stays, voiced; the added lines are unvoiced and borrow the presentation
    // (speaker, portrait, camera) of the canonical line named by their Line.
    internal const string NIGHTSHADE_QUEST = "WC-HAUNTED-MAIN-001";
    internal const string THINGS_QUEST = "WC-HAUNTED-MAIN-002";

    private static string[] Keys(string family, params int[] numbers)
        => [.. numbers.Select(n => $"{family}_{n:D8}")];

    // Bartleby's scene: Ambrose notices the Shadow student asking who may look through an Eye of History.
    internal static readonly Adaptation BartlebyEye = new(
        Id: "BartlebyEye", QuestName: REUNION_QUEST, GoalName: "Goal 1", DialogTag: "Completion",
        CanonicalKeys: Keys("WizQst1731AB", 9, 10, 11, 12, 13, 14, 15, 0),
        Applies: DuringReunionQuest,
        Script: [
            new Line(0), new Line(1), new Line(2), new Line(3),
            new Line(2, "BartlebyEye"),
            new Line(4), new Line(5), new Line(6), new Line(7),
        ]);

    // Foulgaze on entering Sapphyra's Tower: hostile, and only says what he can see (the student's school).
    internal static readonly Adaptation FoulgazeTaunt = new(
        Id: "FoulgazeTaunt", QuestName: REUNION_QUEST, GoalName: "Goal 2", DialogTag: "Completion",
        CanonicalKeys: Keys("WizQst1731AB", 18, 19, 20),
        Applies: DuringReunionQuest,
        Script: [new Line(0), new Line(1), new Line(2), new Line(2, "FoulgazeTaunt")]);

    // Foulgaze after the battle: the books were hoarded for a master; it points at who holds the keys, nothing more.
    internal static readonly Adaptation FoulgazeBooks = new(
        Id: "FoulgazeBooks", QuestName: REUNION_QUEST, GoalName: "Goal 3", DialogTag: "Completion",
        CanonicalKeys: Keys("WizQst1731AB", 22, 23, 24),
        Applies: DuringReunionQuest,
        Script: [new Line(0), new Line(1), new Line(2), new Line(2, "FoulgazeBooks")]);

    // Daisy at the Haunted Cave: she has searched the Library for books on Shadow magic.
    internal static readonly Adaptation DaisyCave = new(
        Id: "DaisyCave", QuestName: REUNION_QUEST, GoalName: "Goal 5", DialogTag: "Completion",
        CanonicalKeys: Keys("WizQst1731AB", 27),
        Applies: DuringReunionQuest,
        Script: [new Line(0), new Line(0, "DaisyCave")]);

    // Throwing Nightshade: Daisy's request, then Nightshade's attempt to claim the Shadow wizard, hostile throughout.
    internal static readonly Adaptation NightshadeOffer = new(
        Id: "NightshadeOffer", QuestName: NIGHTSHADE_QUEST, GoalName: null, DialogTag: "Prep",
        CanonicalKeys: Keys("WizQst1731AD", 1, 2, 3, 4),
        Applies: AtThrowingNightshade,
        Script: [new Line(0), new Line(1), new Line(2), new Line(3), new Line(3, "NightshadeOffer")]);

    internal static readonly Adaptation NightshadeTower = new(
        Id: "NightshadeTower", QuestName: NIGHTSHADE_QUEST, GoalName: "Goal", DialogTag: "Completion",
        CanonicalKeys: Keys("WizQst1731AD", 6, 7),
        Applies: AtThrowingNightshade,
        Script: [new Line(0), new Line(1), new Line(1, "NightshadeTower")]);

    internal static readonly Adaptation NightshadeDefeat = new(
        Id: "NightshadeDefeat", QuestName: NIGHTSHADE_QUEST, GoalName: "Goal 1", DialogTag: "Completion",
        CanonicalKeys: Keys("WizQst1731AD", 8, 9),
        Applies: AtThrowingNightshade,
        Script: [new Line(0), new Line(1), new Line(1, "NightshadeDefeat")]);

    internal static readonly Adaptation DaisyNightshade = new(
        Id: "DaisyNightshade", QuestName: NIGHTSHADE_QUEST, GoalName: "Goal 2", DialogTag: "Completion",
        CanonicalKeys: Keys("WizQst1731AD", 10),
        Applies: AtThrowingNightshade,
        Script: [new Line(0), new Line(0, "DaisyNightshade")]);

    // Things and Stuff: Daisy's experiment and her referral to Ambrose stay; the Pumpkin Heads notice the wizard.
    internal static readonly Adaptation PumpkinOffer = new(
        Id: "PumpkinOffer", QuestName: THINGS_QUEST, GoalName: null, DialogTag: "Prep",
        CanonicalKeys: Keys("WizQst1731AE", 1, 2, 3, 4),
        Applies: AtThingsAndStuff,
        Script: [new Line(0), new Line(1), new Line(2), new Line(2, "PumpkinOffer"), new Line(3)]);

    internal static readonly Adaptation PumpkinReturn = new(
        Id: "PumpkinReturn", QuestName: THINGS_QUEST, GoalName: "Goal 1", DialogTag: "Completion",
        CanonicalKeys: Keys("WizQst1731AE", 5, 6, 7),
        Applies: AtThingsAndStuff,
        Script: [new Line(0), new Line(1), new Line(1, "PumpkinReturn"), new Line(2)]);

    // Stage C2.3: Take a Walk on the Nightside. Ambrose's temporary Pumpkin-vision enchantment, the Mortis and Dworgyn
    // visits and the return. First hint only: nothing here grants, unlocks or records anything, and the quest's goals,
    // rewards and completion flag are the game's own. Added lines are unvoiced and borrow a canonical line's presentation.
    internal const string NIGHTSIDE_QUEST = "WC-COMMONS-MAIN-004";

    // The quest start: the canonical enchantment and "Go now" stay, then Ambrose notices the unusual reaction.
    internal static readonly Adaptation NightsideEnchantment = new(
        Id: "NightsideEnchantment", QuestName: NIGHTSIDE_QUEST, GoalName: null, DialogTag: "Prep",
        CanonicalKeys: Keys("WizQst1731AF", 1, 2, 3, 4),
        Applies: AtNightside,
        Script: [
            new Line(0), new Line(1), new Line(2), new Line(3),
            new Line(3, "NightsideEnchantReaction"), new Line(3, "NightsideEnchantCaution"),
        ]);

    // Mortis, after his canonical account of the Death School and the stolen Eye.
    internal static readonly Adaptation NightsideMortis = new(
        Id: "NightsideMortis", QuestName: NIGHTSIDE_QUEST, GoalName: "Goal 1", DialogTag: "Completion",
        CanonicalKeys: Keys("WizQst1731AF", 7, 8, 9, 10),
        Applies: AtNightside,
        Script: [new Line(0), new Line(1), new Line(2), new Line(3), new Line(3, "NightsideMortis")]);

    // Dworgyn, after his canonical send-off to the Headmaster; he is a caretaker and stays one.
    internal static readonly Adaptation NightsideDworgyn = new(
        Id: "NightsideDworgyn", QuestName: NIGHTSIDE_QUEST, GoalName: "Goal 3", DialogTag: "Completion",
        CanonicalKeys: Keys("WizQst1731AF", 20, 21, 22, 23, 24),
        Applies: AtNightside,
        Script: [new Line(0), new Line(1), new Line(2), new Line(3), new Line(4), new Line(4, "NightsideDworgyn")]);

    // The return to Ambrose, reached only by finishing the Nightside goals: the enchantment has faded, an afterimage has not.
    internal static readonly Adaptation NightsideReturn = new(
        Id: "NightsideReturn", QuestName: NIGHTSIDE_QUEST, GoalName: "Goal 4", DialogTag: "Completion",
        CanonicalKeys: Keys("WizQst1731AF", 25),
        Applies: AtNightsideReturn,
        Script: [new Line(0), new Line(0, "NightsideAfterimage"), new Line(0, "NightsideAfterimageHold")]);

    // Stage C3: the afterimage lasted (Umbral Sight is unlocked). Before his canonical Krokotopia briefing, Ambrose
    // says so: protective, not accusing, and he sends the student to Vespera to learn what it is, not to use it.
    internal const string KROKOTOPIA_QUEST = "WC-COMMONS-MAIN-005";

    internal static readonly Adaptation UmbralSightAmbrose = new(
        Id: "UmbralSightAmbrose", QuestName: KROKOTOPIA_QUEST, GoalName: null, DialogTag: "Prep",
        CanonicalKeys: Keys("WizQst1731B0", 0, 1, 2, 3, 4),
        Applies: AtKrokotopiaNews,
        Script: [
            new Line(0, "UmbralSightAmbroseLasting"), new Line(0, "UmbralSightAmbroseVespera"),
            new Line(0), new Line(1), new Line(2), new Line(3), new Line(4),
        ]);

    internal static readonly IReadOnlyList<Adaptation> Adaptations = [
        AmbroseReunion, BartlebyEye, FoulgazeTaunt, FoulgazeBooks, DaisyCave,
        NightshadeOffer, NightshadeTower, NightshadeDefeat, DaisyNightshade,
        PumpkinOffer, PumpkinReturn,
        NightsideEnchantment, NightsideMortis, NightsideDworgyn, NightsideReturn,
        UmbralSightAmbrose,
    ];

    private static readonly ConcurrentDictionary<string, bool> s_reportedMismatch = new(StringComparer.Ordinal);

    /// <summary>The dialogue <see cref="QuestDialogueRequest.Recipient"/> sees, or null for the canonical one.</summary>
    internal static ActorDialog Resolve(QuestDialogueRequest request) {
        foreach (var adaptation in Adaptations) {
            if (adaptation.QuestName != request.QuestName
                || adaptation.GoalName != request.GoalName
                || adaptation.DialogTag != request.DialogTag) {
                continue;
            }

            if (!adaptation.Applies(request.Recipient)) {
                return null;
            }

            var entries = request.Canonical.m_dialogEntries;
            if (entries is null || !entries.Select(e => (string) e.m_dialog).SequenceEqual(adaptation.CanonicalKeys)) {
                if (s_reportedMismatch.TryAdd(adaptation.Id, true)) {
                    Logger.Warning("Shadow School: {0} expects {1} goal {2} {3} to hold {4}, but the game's dialogue differs; the canonical dialogue is shown.",
                        Logger.Args(adaptation.Id, adaptation.QuestName, adaptation.GoalName ?? "-", adaptation.DialogTag,
                            string.Join(", ", adaptation.CanonicalKeys)));
                }

                return null;
            }

            return Build(request.Canonical, adaptation);
        }

        return null;
    }

    /// <summary>A new dialogue for one wizard; the canonical dialogue and its lines are not changed.</summary>
    internal static ActorDialog Build(ActorDialog canonical, Adaptation adaptation) {
        var source = canonical.m_dialogEntries;
        var lines = adaptation.Script
            .Select(line => line.Text is null
                ? source[line.Canonical]
                : source[line.Canonical] with { m_dialog = TextKey(line.Text), m_soundFile = "" })
            .ToList();

        return canonical with { m_dialogEntries = lines };
    }

    internal static string TextKey(string text) => $"{ShadowSchoolContent.LANG_NAME}_{text}";

    private static bool IsShadow(Wizard wizard)
        => wizard?.MagicSchoolBehavior?.MagicSchool == MagicSchool.Shadow && wizard.QuestBehavior is not null;

    // The reunion is only reachable after all three streets; the lines also speak of Vespera, met at enrollment.
    // Once the quest is done its dialogue is the game's again.
    private static bool EnrolledShadow(Wizard wizard)
        => IsShadow(wizard)
           && wizard.HasCompletedQuest(SHADOW_ENROLLMENT)
           && StreetFinals.All(wizard.HasCompletedQuest);

    // Bartleby, Foulgaze and Daisy's cave are goals of the same quest as the reunion.
    private static bool DuringReunionQuest(Wizard wizard) => AtAmbroseReunion(wizard);

    private static bool AtThrowingNightshade(Wizard wizard)
        => EnrolledShadow(wizard)
           && wizard.HasCompletedQuest(REUNION_QUEST)
           && !wizard.HasCompletedQuest(NIGHTSHADE_QUEST);

    private static bool AtThingsAndStuff(Wizard wizard)
        => EnrolledShadow(wizard)
           && wizard.HasCompletedQuest(REUNION_QUEST)
           && wizard.HasCompletedQuest(NIGHTSHADE_QUEST)
           && !wizard.HasCompletedQuest(THINGS_QUEST);

    // Every quest before the Nightside done (Umbral Sight checks the same chain at the Nightside's end).
    internal static bool ReachedNightside(Wizard wizard)
        => EnrolledShadow(wizard)
           && wizard.HasCompletedQuest(REUNION_QUEST)
           && wizard.HasCompletedQuest(NIGHTSHADE_QUEST)
           && wizard.HasCompletedQuest(THINGS_QUEST);

    internal static bool AtNightside(Wizard wizard)
        => ReachedNightside(wizard) && !wizard.HasCompletedQuest(NIGHTSIDE_QUEST);

    // The afterimage is only real at the end of a walk the wizard took (Umbral Sight's trail): a QA command that
    // completes Goal 4 directly shows the canonical return instead.
    private static bool AtNightsideReturn(Wizard wizard)
        => AtNightside(wizard) && UmbralSight.ReadyForReturn(wizard);

    // Wit Your Way to Krokotopia: Ambrose, once the afterimage has turned out to last.
    private static bool AtKrokotopiaNews(Wizard wizard)
        => IsShadow(wizard)
           && UmbralSight.CanPerceive(wizard)
           && wizard.HasCompletedQuest(NIGHTSIDE_QUEST)
           && !wizard.HasCompletedQuest(KROKOTOPIA_QUEST);

    private static bool AtAmbroseReunion(Wizard wizard)
        => IsShadow(wizard)
           && wizard.HasCompletedQuest(SHADOW_ENROLLMENT)
           && StreetFinals.All(wizard.HasCompletedQuest)
           && !wizard.HasCompletedQuest(REUNION_QUEST);

}
