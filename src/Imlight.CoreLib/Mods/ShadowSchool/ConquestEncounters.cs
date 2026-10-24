/*
 * The Ravenwood conquest (Stage D1): the framework for the instructor encounters of the Shadow story, and the first of
 * them, Dalia Falmea. The plan (Shadow_School_Wizard_City_Opus_5_5_Final_Plan.md, 7.2-7.4) has seven instructors and
 * Ambrose; each is one ConquestEncounter row plus its data files,.
 *
 * An encounter, as the wizard plays it:
 *   1. Vespera offers its quest (spiraldb/QuestTemplates/questtemplates_<quest>.json) to a Shadow wizard with Umbral
 *      Sight who has had her lesson; the quest's own requirements check that, and every step below checks it again.
 *   2. Anchor: in the instructor's school, the wizard stands still at the binding Umbral Sight already perceives there
 *      (an UmbralPerception row). With the quest's anchor goal active, that look completes the goal.
 *   3. Duel: standing still at the anchor again, with the duel goal active and not in battle, opens a session: the
 *      wizard is sent alone into a private copy of the school loaded with the encounter's ZoneObjects variant, where the
 *      game's own combat template of the instructor stands in the trainer's place as a mod custom (spiraldb/Customs)
 *      beside a battle circle. The battle is the server's ordinary battle; nothing here plays it.
 *   4. A win there, reported by the duel (BattleOutcomeEvents), completes the duel goal through the quest's generic
 *      event, and the wizard is sent back to Ravenwood. A loss or flee goes to Ravenwood too (the copy's Exit); the goal
 *      stays open for another try.
 *   5. Return: the quest's last goal is talking to Vespera; the quest's completion records the encounter complete.
 *
 * State, per character, in the quest registry (saved with the wizard, like Umbral Sight's): ShadowSchool_Conquest_<Id>
 * = 1 (anchor found), 2 (instructor defeated), 3 (complete). Absent = not begun. Any other value is malformed: nothing
 * is written over it and the encounter does not advance. A stage is written only from play (QuestProgressSource.Play,
 * never a command, a late-join replay or a goal shared from another player), only in order, and never twice, so a
 * repeated win, a QA shortcut or a helper cannot add to it. The quest gives no reward, so nothing can be paid twice.
 *
 * What it never does: change the canonical instructor, trainer, school, quests or public zone (the hostile instructor
 * exists only in the one wizard's private copy, which the server drops when it empties), touch another wizard, or give
 * Umbral Sight any power over a binding. Seeing the anchor is perception; the challenge is the quest's, through the
 * ordinary battle. A later stage can add controlled binding work as a new stage value or key (see Binding below).
 *
 * The world it acts on (saving, posting quest events, telling a session, sending a wizard into or out of a copy) is
 * IConquestWorld, so the rules are tested without a server.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Imcodec.ObjectProperty.TypeCache;
using Imlight.Common;
using Imlight.CoreLib.Game.Combat;
using Imlight.CoreLib.Game.Quests;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal enum ConquestStage {

    None = 0,
    AnchorFound = 1,
    Defeated = 2,
    Complete = 3,
    /// <summary>The state entry holds a value this version does not know: no progress, never overwritten.</summary>
    Malformed = -1,

}

/// <summary>
/// One instructor encounter: its quest, the Umbral Sight binding that is its anchor, the private copy of the zone it is
/// fought in and the creature fought there.
/// </summary>
/// <param name="Id">Short name, used in registry keys, quest events and text keys (Falmea).</param>
/// <param name="Order">Its place in the plan's sequence (1 = Falmea, 8 = Ambrose); each needs the one before.</param>
/// <param name="Instructor">Who is fought, for logs and staff.</param>
/// <param name="InstructorTemplateId">The canonical NPC (never changed; only left out of the private copy).</param>
/// <param name="QuestName">The encounter's quest.</param>
/// <param name="AnchorBinding">The UmbralPerception binding the wizard looks at.</param>
/// <param name="Zone">The zone of the anchor and of the private copy.</param>
/// <param name="Variant">The ZoneObjects variant of the private copy.</param>
/// <param name="CreatureTemplateId">The game creature fought in the copy.</param>
/// <param name="CustomId">The mod custom it is spawned as (spiraldb/Customs).</param>
/// <param name="ArrivalLoc">Where the wizard arrives in the copy (a named place or X,Y,Z,Yaw).</param>
/// <param name="ExitZone">Where the wizard goes after the battle: the zone...</param>
/// <param name="ExitLoc">...and the spot (the copy's ZoneObjects Exit says the same for a loss, a flee or a door).</param>
internal sealed record ConquestEncounter(
    string Id, int Order, string Instructor, ulong InstructorTemplateId, string QuestName, string AnchorBinding,
    string Zone, string Variant, ulong CreatureTemplateId, int CustomId, string ArrivalLoc, string ExitZone,
    string ExitLoc) {

    internal string StateKey => $"ShadowSchool_Conquest_{Id}";
    internal string AnchorGoal => $"{QuestName}-Anchor";
    internal string DuelGoal => $"{QuestName}-Duel";
    internal string ReturnGoal => $"{QuestName}-Return";
    internal string AnchorEvent => $"ShadowSchool_Conquest_{Id}_AnchorFound";
    internal string DefeatEvent => $"ShadowSchool_Conquest_{Id}_Defeated";

    // What the wizard reads at each step ([ShadowSchool] Conquest_<Id>_*): chat lines, the first also as notify text.
    internal string EnterKey => $"Conquest_{Id}_Enter";
    internal string WonKey => $"Conquest_{Id}_Won";
    internal string LostKey => $"Conquest_{Id}_Lost";

}

/// <summary>What the framework does to the world, so the rules can be tested without a server.</summary>
internal interface IConquestWorld {

    /// <summary>Saves the wizard's quest registry.</summary>
    void Save(Wizard wizard);

    /// <summary>Tells the wizard's session a quest event, which completes an active goal that waits for it.</summary>
    void PostQuestEvent(IActorRef session, string eventName);

    /// <summary>Shows the wizard text keys: on screen (notify) and in their chat log.</summary>
    void Show(IActorRef session, IReadOnlyList<string> notifyKeys, IReadOnlyList<string> chatKeys);

    /// <summary>Sends the wizard alone into a fresh private copy of the encounter's zone, loaded with its variant.</summary>
    void Enter(IActorRef session, Wizard wizard, ConquestEncounter encounter);

    /// <summary>Sends the wizard to the encounter's exit, after the delay, and forgets the copy for them.</summary>
    void Leave(IActorRef session, Wizard wizard, ConquestEncounter encounter, TimeSpan delay);

    /// <summary>The variant the server will load the wizard's zone with at their next attach, or empty.</summary>
    string NotedVariant(ulong charId, string zone);

    DateTime UtcNow { get; }

}

internal sealed class ConquestEncounters(IConquestWorld world) {

    internal const string LESSON_QUEST = "ShadowSchool-UmbralSight-Lesson";

    // The overarching story quest (Stage D2): one milestone goal per instructor, in the plan's order. The goals are
    // completed by the same defeat events the per-instructor quests use; the per-instructor registry stays the gate for
    // the finale. All seven instructors are implemented (Stage D2.3); Ambrose's finale is not (Stage D3) and has no row.
    internal const string CAMPAIGN_QUEST = "ShadowSchool-Conquest";

    internal static readonly IReadOnlyList<string> CampaignMilestones =
        ["Falmea", "Greyrose", "Balestrom", "Drake", "Wu", "Ashthorn", "Wethersfield"];

    // How long after a win before the wizard is sent out (the victory and the defeated lines first).
    internal static TimeSpan ReturnDelay => s_returnDelay.Value;

    private static readonly Lazy<TimeSpan> s_returnDelay = new(() => TimeSpan.FromSeconds(Math.Clamp(
        ConfigurationManager.GetValue("ShadowSchool.ConquestReturnSeconds", 6.0), 1, 60)));

    // A transfer into a copy that has not arrived by then is given up (the next attach is not taken for it).
    internal static readonly TimeSpan EnterTimeout = TimeSpan.FromSeconds(60);

    internal static readonly IReadOnlyList<ConquestEncounter> Encounters = [
        // 1. Dalia Falmea, Fire (plan 7.3: aggressive Fire damage over time and tempo). WC-RAV-NPC04 (38208) stands at
        // (787, 9.45, 99.75) in the Fire School; DaliaFalmea-WCBoss-R7 (403089) is the game's own combat Falmea.
        new("Falmea", 1, "Dalia Falmea", 38208, "ShadowSchool-Conquest-Falmea", "FireHearth",
            "WizardCity/Interiors/WC_SchoolFire", "ShadowConquest-Falmea", 403089, 1880001,
            "Start", "WizardCity/WC_Ravenwood", "1616.214,5983.343,0.0001,0.3877"),
        // 2. Lydia Greyrose, Ice (plan 7.3: defensive shields, moderate damage, controlled freezing). WC-RAV-NPC03 (38172)
        // stands at (761.52, 20.66, 147.81) in the Frost School. Her combat representation is the game's own Lydia Greyrose
        // creature (1475612, Ice, the Treasure Tower floor boss), spawned as custom 1880002 for the battle profile.
        new("Greyrose", 2, "Lydia Greyrose", 38172, "ShadowSchool-Conquest-Greyrose", "FrostClassroom",
            "WizardCity/Interiors/WC_SchoolFrost", "ShadowConquest-Greyrose", 1475612, 1880002,
            "Start", "WizardCity/WC_Ravenwood", "3416.191,3605.734,0.0001,1.3566"),
        // 3. Halston Balestrom, Storm (plan 7.3: high damage, low accuracy, fragile). WC-RAV-NPC05 (38141) stands at
        // (938.90, -1.20, 202.09) in the Storm School; his combat representation is the game's own Halston Balestrom
        // creature (1475613, Storm, the Treasure Tower floor boss), spawned as custom 1880003.
        new("Balestrom", 3, "Halston Balestrom", 38141, "ShadowSchool-Conquest-Balestrom", "StormClassroom",
            "WizardCity/Interiors/WC_SchoolStorm", "ShadowConquest-Balestrom", 1475613, 1880003,
            "Start", "WizardCity/WC_Ravenwood", "2752.043,818.4474,0.0001,2.3025"),
        // 4. Cyrus Drake, Myth (plan 7.3: ward disruption and tactical pressure). WC-RAV-NPC02 (38206) stands at
        // (807.46, 13.78, 100.00) in the Myth School; his combat representation is the game's own Cyrus Drake creature
        // (1475614, Myth, the Treasure Tower floor boss), spawned as custom 1880004.
        new("Drake", 4, "Cyrus Drake", 38206, "ShadowSchool-Conquest-Drake", "MythClassroom",
            "WizardCity/Interiors/WC_SchoolMyth", "ShadowConquest-Drake", 1475614, 1880004,
            "Start", "WizardCity/WC_Ravenwood", "-3281.591,3876.725,0.0001,-1.3973"),
        // 5. Moolinda Wu, Life (plan 7.3: heals, shields, survivability; never eager to hurt). WC-RAV-NPC01 (38204) stands at
        // (760.20, 15.49, 100.00) in the Life School; her combat representation is the game's own Moolinda Wu creature
        // (1475615, Life, the Treasure Tower floor boss), spawned as custom 1880005.
        new("Wu", 5, "Moolinda Wu", 38204, "ShadowSchool-Conquest-Wu", "LifeClassroom",
            "WizardCity/Interiors/WC_SchoolLife", "ShadowConquest-Wu", 1475615, 1880005,
            "Start", "WizardCity/WC_Ravenwood", "-2646.668,797.2547,0.0001,-2.2802"),
        // 6. Malorn Ashthorn, Death (plan 7.3: drain and sustain, modest by design). WC-RAV-NPC08 (81102) is not in a school:
        // he stands in Ravenwood itself, at (229.29, 6034.79, -30.48). The game has no creature of his own, so the Death
        // floor boss of the Treasure Tower (1475616) stands in for him, named for him by custom 1880006. The anchor, the
        // private copy and the exit are all in WC_Ravenwood; the wizard arrives and leaves at the game's own
        // Loc_WC-COMMONS-MAIN-002-DEATH, 413 units from him.
        new("Ashthorn", 6, "Malorn Ashthorn", 81102, "ShadowSchool-Conquest-Ashthorn", "DeathInterim",
            "WizardCity/WC_Ravenwood", "ShadowConquest-Ashthorn", 1475616, 1880006,
            "641.8373,5968.792,-28.7859,1.7236", "WizardCity/WC_Ravenwood", "641.8373,5968.792,-28.7859,1.7236"),
        // 7. Arthur Wethersfield, Balance (plan 7.3: weakness, precision debuffs, mixed-school pressure). WC-RAV-NPC09
        // (81103) also stands in Ravenwood, at (558.59, 444.97, 7.18); the Balance floor boss of the Treasure Tower
        // (1475617) stands in for him as custom 1880007. Arrival and exit: the game's Loc_WC-COMMONS-MAIN-002-BALANCE.
        new("Wethersfield", 7, "Arthur Wethersfield", 81103, "ShadowSchool-Conquest-Wethersfield", "BalanceCircle",
            "WizardCity/WC_Ravenwood", "ShadowConquest-Wethersfield", 1475617, 1880007,
            "322.9819,-171.081,-28.7859,-2.774", "WizardCity/WC_Ravenwood", "322.9819,-171.081,-28.7859,-2.774"),
    ];

    internal static string CampaignGoal(string id) => $"{CAMPAIGN_QUEST}-Milestone-{id}";

    internal static ConquestEncounter ByQuest(string questName)
        => Encounters.FirstOrDefault(e => e.QuestName == questName);

    internal static ConquestEncounter ById(string id)
        => Encounters.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));

    // ------------------------------------------------------------------ State

    internal static ConquestStage StageOf(Wizard wizard, ConquestEncounter encounter) {
        if (wizard?.QuestBehavior?.Registry is not { } registry || !registry.TryGetValue(encounter.StateKey, out var value)) {
            return ConquestStage.None;
        }

        return value is >= 1 and <= 3 ? (ConquestStage) (int) value : ConquestStage.Malformed;
    }

    /// <summary>The encounter is done: its quest completed through play, with the instructor defeated in its copy.</summary>
    internal static bool IsComplete(Wizard wizard, ConquestEncounter encounter)
        => StageOf(wizard, encounter) == ConquestStage.Complete && wizard.HasCompletedQuest(encounter.QuestName);

    /// <summary>
    /// All seven instructors are beaten in the durable registry (each stage Defeated or Complete), for a Shadow wizard whose
    /// sight works. Written by no one: it is derived from the seven stages, each of which only a verified battle writes. This
    /// is what Ambrose's finale (Stage D3) will gate on together with <see cref="AllInstructorsComplete"/>; nothing starts
    /// it yet.
    /// </summary>
    internal static bool AllInstructorsDefeated(Wizard wizard)
        => UmbralSight.CanPerceive(wizard) && Encounters.All(e => StageOf(wizard, e) is ConquestStage.Defeated or ConquestStage.Complete);

    /// <summary>All seven chapters are complete: every instructor beaten and every return to Vespera made.</summary>
    internal static bool AllInstructorsComplete(Wizard wizard)
        => UmbralSight.CanPerceive(wizard) && Encounters.All(e => IsComplete(wizard, e));

    /// <summary>
    /// Whether this wizard may take part in the encounter at all: Shadow, Umbral Sight that works (not a copied or
    /// malformed entry), the Nightside and Vespera's lesson done, and the encounter before it complete.
    /// </summary>
    internal static bool MeetsPrerequisites(Wizard wizard, ConquestEncounter encounter) {
        if (!UmbralSight.CanPerceive(wizard) || !wizard.HasCompletedQuest(UmbralSight.ORIGIN_QUEST)
            || !wizard.HasCompletedQuest(LESSON_QUEST)) {
            return false;
        }

        // Every earlier instructor, not just the one before: copied or hand-written entries for a later chapter cannot
        // open a door whose earlier doors were never passed.
        return Encounters.Where(e => e.Order < encounter.Order).All(e => IsComplete(wizard, e));
    }

    internal static QuestInstance ActiveQuest(Wizard wizard, ConquestEncounter encounter)
        => wizard?.QuestBehavior?.CurrentQuestInstances?.FirstOrDefault(q => q.QuestName == encounter.QuestName);

    /// <summary>The overarching quest, if the wizard has accepted it.</summary>
    internal static QuestInstance ActiveCampaign(Wizard wizard)
        => wizard?.QuestBehavior?.CurrentQuestInstances?.FirstOrDefault(q => q.QuestName == CAMPAIGN_QUEST);

    /// <summary>Whether one instructor's milestone in the overarching quest is done (by play, through its event).</summary>
    internal static bool IsMilestoneComplete(Wizard wizard, string id)
        => ActiveCampaign(wizard)?.IsGoalCompleted(CampaignGoal(id)) ?? false;

    // ------------------------------------------------------------------ Sessions (who is in which private copy)

    internal enum Phase { Entering, InCopy }

    internal sealed record Session(string EncounterId, Phase Phase, DateTime Since, bool Credited = false);

    private readonly ConcurrentDictionary<ulong, Session> _sessions = new();

    // Wizards who moved since their last look: a look acts once, and the next needs a step first.
    private readonly ConcurrentDictionary<ulong, bool> _armed = new();

    internal Session SessionOf(ulong charId) => _sessions.TryGetValue(charId, out var session) ? session : null;

    // ------------------------------------------------------------------ Quest progress (the wizard's session)

    internal void OnQuestStarted(Wizard wizard, QuestTemplate quest) {
        if (ByQuest(quest?.m_questName) is not { } encounter) {
            return;
        }

        // A new run resets an unfinished anchor. A verified defeat is durable and must survive a dropped/restarted
        // chapter so the return conversation can be completed after reconnecting.
        var stage = StageOf(wizard, encounter);
        if (stage == ConquestStage.AnchorFound) {
            wizard.QuestBehavior.RemoveRegistryValue(encounter.StateKey);
            world.Save(wizard);
            Logger.Information("Shadow School: {0} started {1} again; the {2} encounter starts over.",
                Logger.Args(wizard.CharId, encounter.QuestName, encounter.Id));
        }
    }

    internal void OnGoalCompleted(Wizard wizard, QuestTemplate quest, GoalTemplate goal, QuestProgressSource source) {
        if (ByQuest(quest?.m_questName) is not { } encounter || goal is null) {
            return;
        }

        var name = (string) goal.m_goalName;
        var next = name == encounter.AnchorGoal ? ConquestStage.AnchorFound
            : name == encounter.DuelGoal ? ConquestStage.Defeated
            : ConquestStage.None;
        if (next == ConquestStage.None) {
            return;
        }

        // Only the wizard's own play: a QA command, a late-join replay or a goal another player completed in the same
        // instance (a helper) never advances anyone's conquest.
        if (source != QuestProgressSource.Play) {
            Logger.Information("Shadow School: {0} {1} completed by {2}; the {3} encounter does not advance.",
                Logger.Args(wizard.CharId, name, source, encounter.Id));

            return;
        }

        Advance(wizard, encounter, next, "play");
    }

    internal void OnQuestCompleted(Wizard wizard, QuestTemplate quest, QuestProgressSource source) {
        if (ByQuest(quest?.m_questName) is { } encounter && source == QuestProgressSource.Play) {
            Advance(wizard, encounter, ConquestStage.Complete, "play");
        }
    }

    /// <summary>
    /// At every attach: a wizard whose quest completed after the defeat was recorded, but whose completion was not
    /// saved (the server stopped between the two writes), is completed now. Nothing else is granted here.
    /// </summary>
    internal void Reconcile(Wizard wizard) {
        foreach (var encounter in Encounters) {
            if (StageOf(wizard, encounter) == ConquestStage.Defeated && wizard.HasCompletedQuest(encounter.QuestName)) {
                Advance(wizard, encounter, ConquestStage.Complete, "a recorded defeat and completed quest, at login");
            }
        }
    }

    // One step forward, from the stage just before it only, for an eligible wizard; never twice.
    private bool Advance(Wizard wizard, ConquestEncounter encounter, ConquestStage next, string how) {
        var stage = StageOf(wizard, encounter);
        if (stage != next - 1 || !MeetsPrerequisites(wizard, encounter)) {
            if (stage != next) {
                Logger.Information("Shadow School: {0} is at {1} of the {2} encounter; {3} is not recorded.",
                    Logger.Args(wizard.CharId, stage, encounter.Id, next));
            }

            return false;
        }

        if (!wizard.QuestBehavior.SetRegistryValue(encounter.StateKey, (ulong) next)) {
            return false;
        }

        world.Save(wizard);
        Logger.Information("Shadow School: {0} reached {1} of the {2} encounter by {3}.",
            Logger.Args(wizard.CharId, next, encounter.Id, how));
        if (next == ConquestStage.Defeated && AllInstructorsDefeated(wizard)) {
            Logger.Information("Shadow School: {0} has now defeated all seven instructors. Ambrose's finale is not available yet.",
                Logger.Args(wizard.CharId));
        }

        return true;
    }

    // ------------------------------------------------------------------ Presence (the wizard's session)

    internal void OnAttached(IActorRef session, Wizard wizard) {
        if (wizard is null) {
            return;
        }

        _armed.TryRemove(wizard.CharId, out _);
        Reconcile(wizard);

        // The durable state is written at victory before the quest event is queued. If that session disconnected while
        // the event was in flight, replay it after the character's new QuestService is attached. The goals themselves
        // decide whether it applies, so this is harmless when either quest is absent or the milestone is not active.
        foreach (var encounter in Encounters.Where(e => StageOf(wizard, e) is ConquestStage.Defeated or ConquestStage.Complete)) {
            if (ActiveQuest(wizard, encounter)?.IsGoalActive(encounter.AnchorGoal) == true) {
                world.PostQuestEvent(session, encounter.AnchorEvent);
            }

            world.PostQuestEvent(session, encounter.DefeatEvent);
        }

        var open = SessionOf(wizard.CharId);
        foreach (var encounter in Encounters.Where(e => string.Equals(e.Zone, wizard.Zone, StringComparison.OrdinalIgnoreCase))) {
            // The transfer this framework asked for has arrived: the wizard is in their own copy now.
            if (open is { Phase: Phase.Entering } entering && entering.EncounterId == encounter.Id
                && world.UtcNow - entering.Since <= EnterTimeout) {
                _sessions[wizard.CharId] = entering with { Phase = Phase.InCopy, Since = world.UtcNow };

                return;
            }

            // In a copy of the encounter without a session opened for it: a reconnect or a restart brought the wizard
            // back into a fresh copy. It is closed and they go to the exit; they can open it again at the anchor.
            if (world.NotedVariant(wizard.CharId, encounter.Zone) == encounter.Variant) {
                _sessions.TryRemove(wizard.CharId, out _);
                Logger.Information("Shadow School: {0} came back into a {1} encounter copy with no open session; sent to the exit.",
                    Logger.Args(wizard.CharId, encounter.Id));
                world.Leave(session, wizard, encounter, TimeSpan.Zero);

                return;
            }
        }

        // Anywhere else (or the public zone): any session is over.
        _sessions.TryRemove(wizard.CharId, out _);
    }

    internal void OnMoved(Wizard wizard) {
        if (wizard is not null) {
            _armed[wizard.CharId] = true;
        }
    }

    internal void OnDetached(ulong charId) {
        _sessions.TryRemove(charId, out _);
        _armed.TryRemove(charId, out _);
    }

    /// <summary>Standing still: at an encounter's anchor, long enough, the deliberate look acts (once per step).</summary>
    internal void OnStill(IActorRef session, Wizard wizard, float x, float y, TimeSpan still) {
        if (wizard is null || still < UmbralPerception.FocusDelay || !_armed.TryGetValue(wizard.CharId, out var armed)
            || !armed) {
            return;
        }

        foreach (var encounter in Encounters.Where(e => string.Equals(e.Zone, wizard.Zone, StringComparison.OrdinalIgnoreCase))) {
            if (!AtAnchor(encounter, x, y) || SessionOf(wizard.CharId) is not null || !MeetsPrerequisites(wizard, encounter)
                || ActiveQuest(wizard, encounter) is not { } quest) {
                continue;
            }

            if (quest.IsGoalActive(encounter.AnchorGoal)) {
                _armed[wizard.CharId] = false;
                Logger.Information("Shadow School: {0} found the {1} anchor.", Logger.Args(wizard.CharId, encounter.Id));
                world.PostQuestEvent(session, encounter.AnchorEvent);

                return;
            }

            if (quest.IsGoalActive(encounter.DuelGoal) && StageOf(wizard, encounter) == ConquestStage.AnchorFound
                && !wizard.IsInDuel && !wizard.IsInCombatGrace) {
                _armed[wizard.CharId] = false;
                _sessions[wizard.CharId] = new Session(encounter.Id, Phase.Entering, world.UtcNow);
                Logger.Information("Shadow School: {0} steps into the {1} encounter.", Logger.Args(wizard.CharId, encounter.Id));
                world.Show(session, [encounter.EnterKey], [encounter.EnterKey]);
                world.Enter(session, wizard, encounter);

                return;
            }
        }
    }

    internal static bool AtAnchor(ConquestEncounter encounter, float x, float y) {
        var binding = UmbralPerception.Bindings.FirstOrDefault(b => b.Id == encounter.AnchorBinding);
        if (binding is null) {
            return false;
        }

        var dx = x - binding.X;
        var dy = y - binding.Y;

        return dx * dx + dy * dy <= binding.Radius * binding.Radius;
    }

    // ------------------------------------------------------------------ Battles (the duel's actor)

    /// <summary>
    /// A battle ended. Only a battle in the encounter's own private copy, fought by the wizard whose session opened
    /// it, counts: a win over the encounter's custom completes the duel goal; a loss or flee leaves it open.
    /// </summary>
    internal void OnBattleEnded(BattleOutcome outcome) {
        var encounter = Encounters.FirstOrDefault(e => string.Equals(e.Zone, outcome.ZonePath, StringComparison.OrdinalIgnoreCase)
                                                       && e.Variant == outcome.Variant);
        if (encounter is null || outcome.Wizard is not { } wizard) {
            return;
        }

        if (!outcome.IsInstance || SessionOf(wizard.CharId) is not { Phase: Phase.InCopy } session
            || session.EncounterId != encounter.Id) {
            Logger.Warning("Shadow School: a battle in a {0} encounter copy ended for {1} without their open session; not counted.",
                Logger.Args(encounter.Id, wizard.CharId));

            return;
        }

        switch (outcome.Result) {
            case BattleResult.Won when outcome.Creatures.Any(c => c.CustomId == encounter.CustomId):
                // One verified win, one defeat event: both the chapter's duel goal and the overarching quest's milestone
                // wait for it. Posting it does not depend on the chapter's goal being active any more (D2), so dropping
                // the chapter quest mid-encounter cannot stall the campaign; the session's flag makes a repeated report
                // of the same victory post nothing. The registry still advances only from the chapter goal's own
                // completion, so there is no second credit path.
                if (!session.Credited && MeetsPrerequisites(wizard, encounter)) {
                    // Persist the verified encounter result before sending the journal event. QuestService runs on the
                    // player's actor and may stop during a disconnect; the registry is the authoritative recovery record.
                    if (StageOf(wizard, encounter) == ConquestStage.AnchorFound) {
                        Advance(wizard, encounter, ConquestStage.Defeated, "verified private-copy victory");
                    }

                    if (StageOf(wizard, encounter) is ConquestStage.Defeated or ConquestStage.Complete) {
                        _sessions[wizard.CharId] = session with { Credited = true };
                        Logger.Information("Shadow School: {0} defeated {1} in the {2} encounter.",
                            Logger.Args(wizard.CharId, encounter.Instructor, encounter.Id));
                        world.PostQuestEvent(outcome.Session, encounter.DefeatEvent);
                    }
                }

                world.Show(outcome.Session, [], [encounter.WonKey]);
                world.Leave(outcome.Session, wizard, encounter, ReturnDelay);
                break;
            case BattleResult.Lost or BattleResult.Fled:
                Logger.Information("Shadow School: {0} {1} the {2} encounter battle; the goal stays open.",
                    Logger.Args(wizard.CharId, outcome.Result == BattleResult.Lost ? "lost" : "fled", encounter.Id));
                world.Show(outcome.Session, [], [encounter.LostKey]);
                break;
        }
    }

    /// <summary>Only a verified victory already in the durable encounter registry may complete a campaign milestone.</summary>
    internal static bool IsVerifiedCampaignVictory(Wizard wizard, string eventName)
        => Encounters.Any(e => e.DefeatEvent == eventName
            && StageOf(wizard, e) is ConquestStage.Defeated or ConquestStage.Complete);

    /// <summary>Plain words for staff (.conquest status).</summary>
    internal string Describe(Wizard wizard, ConquestEncounter encounter) {
        var raw = wizard?.QuestBehavior?.Registry is { } registry && registry.TryGetValue(encounter.StateKey, out var value)
            ? value.ToString()
            : "none";
        var quest = ActiveQuest(wizard, encounter);
        var goal = quest is null ? "not on the quest"
            : quest.IsGoalActive(encounter.AnchorGoal) ? "looking for the anchor"
            : quest.IsGoalActive(encounter.DuelGoal) ? "to defeat the instructor"
            : quest.IsGoalActive(encounter.ReturnGoal) ? "to return to Vespera"
            : "on the quest";
        var session = SessionOf(wizard.CharId);

        return $"{encounter.Order}. {encounter.Instructor} ({encounter.Id}): stage {StageOf(wizard, encounter)} ({raw}), "
             + $"{(wizard.HasCompletedQuest(encounter.QuestName) ? "quest complete" : goal)}, "
             + $"eligible: {(MeetsPrerequisites(wizard, encounter) ? "yes" : "no")}, "
             + $"session: {(session is null ? "none" : $"{session.Phase} since {session.Since:HH:mm:ss}")}";
    }

}
