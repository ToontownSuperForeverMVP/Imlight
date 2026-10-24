/*
 * Umbral Sight (Stage C3): a Shadow wizard's lasting perception of magical bindings, anchors and residual connections.
 *
 * Its origin is Take a Walk on the Nightside (WC-COMMONS-MAIN-004). Ambrose's Pumpkin-vision enchantment at the quest's
 * start resonates with a Shadow wizard; after the whole investigation (enter the Nightside, Mortis, the Death School,
 * Dworgyn, back to Ambrose) the enchantment fades but an afterimage stays. Only then is the sight unlocked.
 *
 * State lives in the wizard's own registry (ServerQuestBehavior.Registry, saved with the character), so it needs no new
 * schema and follows the character through logout, reconnect, zone changes and server restarts:
 *
 *   ShadowSchool_UmbralSight         1 = unlocked by the walk, 2 = unlocked by a staff reconciliation. Absent = locked.
 *                                    Any other value is malformed: the sight stays off and the value is left for staff.
 *   ShadowSchool_UmbralSight_Trail   bits recording this run of the quest: started by accepting it (1), then each of
 *                                    the five goals in order (2, 4, 8, 16, 32). Starting the quest again clears it.
 *
 * Only play counts (QuestProgressSource.Play or Shared). A QA command, a late-join replay, a goal out of order or a
 * quest begun by a command leaves a gap in the trail, and a trail with a gap never unlocks anything: a shortcut is not
 * evidence of the walk. The quest itself (goals, rewards, completion flag) is the game's and is never touched here.
 *
 * Umbral Sight is perception only: UmbralPerception.cs shows the wizard what is bound around them. It breaks, controls
 * and opens nothing, and is not a spell (see the Stage C3 notes in docs/continuation-status.md for why).
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Imcodec.ObjectProperty.TypeCache;
using Imlight.Common;
using Imlight.CoreLib.Game.Quests;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.WizardData.Collections;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal enum UmbralSightState {

    Locked,
    Unlocked,
    /// <summary>The state entry holds a value this version does not know: treated as locked, never overwritten by play.</summary>
    Malformed,

}

internal static class UmbralSight {

    internal const string STATE_KEY = "ShadowSchool_UmbralSight";
    internal const string TRAIL_KEY = "ShadowSchool_UmbralSight_Trail";

    internal const ulong UNLOCKED_BY_WALK = 1;
    internal const ulong UNLOCKED_BY_STAFF = 2;

    internal const string ORIGIN_QUEST = ShadowMainlineDialogue.NIGHTSIDE_QUEST;

    /// <summary>The Nightside's goals in the order the quest's goal logic gives them.</summary>
    internal static readonly string[] OriginGoals = ["Goal", "Goal 1", "Goal 2", "Goal 3", "Goal 4"];

    internal const ulong TRAIL_STARTED = 1;
    internal static ulong GoalBit(int index) => 2UL << index;

    /// <summary>Accepted and walked to Dworgyn: what Ambrose's return dialogue (Goal 4) needs.</summary>
    internal static readonly ulong TrailBeforeReturn = TRAIL_STARTED | GoalBit(0) | GoalBit(1) | GoalBit(2) | GoalBit(3);
    internal static readonly ulong TrailComplete = TrailBeforeReturn | GoalBit(4);

    /// <summary>How a change is saved: the character's quest behavior, as every registry write is.</summary>
    internal static readonly Action<Wizard> SaveToDatabase = wizard => WizardCollection.UpdateCharacterQuestBehavior(wizard);

    private static readonly ConcurrentDictionary<ulong, bool> s_reportedMalformed = new();

    internal static UmbralSightState StateOf(Wizard wizard) {
        if (wizard?.QuestBehavior?.Registry is not { } registry || !registry.TryGetValue(STATE_KEY, out var value)) {
            return UmbralSightState.Locked;
        }

        if (value is UNLOCKED_BY_WALK or UNLOCKED_BY_STAFF) {
            return UmbralSightState.Unlocked;
        }

        if (s_reportedMalformed.TryAdd(wizard.CharId, true)) {
            Logger.Warning("Shadow School: {0} has Umbral Sight state {1}, which is not a known value; the sight stays off and the entry is left as it is.",
                Logger.Args(wizard.CharId, value));
        }

        return UmbralSightState.Malformed;
    }

    /// <summary>Whether this wizard perceives bindings now: a Shadow wizard with the sight unlocked.</summary>
    internal static bool CanPerceive(Wizard wizard) => IsShadow(wizard) && StateOf(wizard) == UmbralSightState.Unlocked;

    internal static ulong TrailOf(Wizard wizard) => wizard?.QuestBehavior?.GetRegistryValue(TRAIL_KEY) ?? 0;

    /// <summary>The wizard accepted the Nightside and walked every goal before the return to Ambrose.</summary>
    internal static bool ReadyForReturn(Wizard wizard) => (TrailOf(wizard) & TrailBeforeReturn) == TrailBeforeReturn;

    internal static void OnQuestStarted(Wizard wizard, QuestTemplate quest, QuestProgressSource source,
                                        Action<Wizard> save) {
        if (!IsOrigin(quest) || !IsShadow(wizard)) {
            return;
        }

        // A new run starts a new trail. Only a quest the wizard accepted (from Ambrose) can lead anywhere.
        var trail = source == QuestProgressSource.Play && ShadowMainlineDialogue.AtNightside(wizard) ? TRAIL_STARTED : 0;
        WriteTrail(wizard, trail, save);
    }

    internal static void OnGoalCompleted(Wizard wizard, QuestTemplate quest, GoalTemplate goal, QuestProgressSource source,
                                         Action<Wizard> save) {
        if (!IsOrigin(quest) || !IsShadow(wizard) || !IsPlay(source) || !ShadowMainlineDialogue.AtNightside(wizard)) {
            return;
        }

        var index = Array.IndexOf(OriginGoals, (string) goal?.m_goalName);
        if (index < 0) {
            return;
        }

        // Every earlier step must be on the trail, so a goal skipped or completed by a command breaks it for this run.
        var trail = TrailOf(wizard);
        var before = TRAIL_STARTED;
        for (var i = 0; i < index; i++) {
            before |= GoalBit(i);
        }

        if ((trail & before) != before || (trail & GoalBit(index)) != 0) {
            return;
        }

        WriteTrail(wizard, trail | GoalBit(index), save);
    }

    internal static void OnQuestCompleted(Wizard wizard, QuestTemplate quest, QuestProgressSource source,
                                          Action<Wizard> save) {
        if (IsOrigin(quest) && IsPlay(source)) {
            UnlockFromTrail(wizard, save, "the Nightside walk");
        }
    }

    /// <summary>
    /// At every zone attach: a wizard whose walk is complete on record but whose unlock was not saved (the server
    /// stopped between the two writes) gets it now. Nothing else is granted here.
    /// </summary>
    internal static bool Reconcile(Wizard wizard, Action<Wizard> save)
        => StateOf(wizard) == UmbralSightState.Locked && UnlockFromTrail(wizard, save, "a recorded walk, at login");

    /// <summary>
    /// For a Shadow wizard who walked the Nightside before Stage C3 recorded trails: staff, having checked the
    /// character's history, may unlock it. The canonical chain must be complete on record. Never for another school.
    /// </summary>
    internal static bool StaffReconcile(Wizard wizard, Action<Wizard> save, out string outcome) {
        if (!IsShadow(wizard)) {
            outcome = "Only a Shadow wizard can have Umbral Sight.";
            return false;
        }

        if (StateOf(wizard) == UmbralSightState.Unlocked) {
            outcome = "Umbral Sight is already unlocked.";
            return false;
        }

        if (!ShadowMainlineDialogue.ReachedNightside(wizard) || !wizard.HasCompletedQuest(ORIGIN_QUEST)) {
            outcome = $"The character has not completed the chain through {ORIGIN_QUEST} on record.";
            return false;
        }

        // An explicit staff action may also repair a malformed entry; play never does.
        if (!TryUnlock(wizard, UNLOCKED_BY_STAFF, save, "staff reconciliation", repairMalformed: true)) {
            outcome = "The unlock could not be written.";
            return false;
        }

        outcome = "Umbral Sight unlocked (staff reconciliation).";
        return true;
    }

    /// <summary>Plain words for staff (.umbral status).</summary>
    internal static string Describe(Wizard wizard) {
        var trail = TrailOf(wizard);
        var steps = new List<string>();
        if ((trail & TRAIL_STARTED) != 0) {
            steps.Add("accepted");
        }

        for (var i = 0; i < OriginGoals.Length; i++) {
            if ((trail & GoalBit(i)) != 0) {
                steps.Add(OriginGoals[i]);
            }
        }

        var raw = wizard?.QuestBehavior?.Registry is { } registry && registry.TryGetValue(STATE_KEY, out var value)
            ? value.ToString()
            : "none";

        return $"school {wizard?.MagicSchoolBehavior?.MagicSchool}, state {StateOf(wizard)} ({raw}), "
             + $"trail [{string.Join(", ", steps)}], {ORIGIN_QUEST} {(wizard?.HasCompletedQuest(ORIGIN_QUEST) == true ? "complete" : "not complete")}, "
             + $"perceives: {(CanPerceive(wizard) ? "yes" : "no")}";
    }

    private static bool UnlockFromTrail(Wizard wizard, Action<Wizard> save, string how)
        => IsShadow(wizard)
           && ShadowMainlineDialogue.ReachedNightside(wizard)
           && wizard.HasCompletedQuest(ORIGIN_QUEST)
           && (TrailOf(wizard) & TrailComplete) == TrailComplete
           && TryUnlock(wizard, UNLOCKED_BY_WALK, save, how);

    private static bool TryUnlock(Wizard wizard, ulong value, Action<Wizard> save, string how, bool repairMalformed = false) {
        var state = StateOf(wizard);
        if (state == UmbralSightState.Unlocked || (state == UmbralSightState.Malformed && !repairMalformed)) {
            return false;
        }

        if (!wizard.QuestBehavior.SetRegistryValue(STATE_KEY, value)) {
            return false;
        }

        save(wizard);
        Logger.Information("Shadow School: Umbral Sight unlocked for {0} by {1}.", Logger.Args(wizard.CharId, how));
        UmbralPerception.OnUnlocked(wizard);

        return true;
    }

    private static void WriteTrail(Wizard wizard, ulong trail, Action<Wizard> save) {
        var behavior = wizard.QuestBehavior;
        if (trail == 0) {
            if (behavior.HasRegistryValue(TRAIL_KEY)) {
                behavior.RemoveRegistryValue(TRAIL_KEY);
                save(wizard);
            }

            return;
        }

        if (behavior.GetRegistryValue(TRAIL_KEY) != trail || !behavior.HasRegistryValue(TRAIL_KEY)) {
            behavior.SetRegistryValue(TRAIL_KEY, trail);
            save(wizard);
        }
    }

    private static bool IsOrigin(QuestTemplate quest) => (string) quest?.m_questName == ORIGIN_QUEST;

    private static bool IsPlay(QuestProgressSource source)
        => source is QuestProgressSource.Play or QuestProgressSource.Shared;

    private static bool IsShadow(Wizard wizard)
        => wizard?.MagicSchoolBehavior?.MagicSchool == MagicSchool.Shadow && wizard.QuestBehavior is not null;

}
