/*
 * Quest progress events: lets a mod learn that a wizard started a quest, completed a goal or completed a quest, and
 * how that happened.
 *
 * QuestService reports each step after it has been saved on the wizard. The source tells a mod whether the player got
 * there by playing (their own progress, or a goal shared in their instance) or whether a QA command or a late-join
 * replay put it there. A mod that rewards a canonical sequence (Shadow School's Umbral Sight) only counts play.
 *
 * An observer only reads and records its own state. It may not change the quest, its goals or its rewards, and a
 * throwing observer is logged and skipped so the quest goes on exactly as before.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Imcodec.ObjectProperty.TypeCache;
using Imlight.Common;
using Imlight.CoreLib.Mods;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Game.Quests;

/// <summary>How a quest step came about.</summary>
internal enum QuestProgressSource {

    /// <summary>The player's own progress: an NPC talked to, a zone entered, a battle won, a quest accepted.</summary>
    Play,

    /// <summary>Another player in the same instance completed the goal while this player was in it.</summary>
    Shared,

    /// <summary>A QA command (.quest completegoal, complete, completeall, toquest, grant).</summary>
    Command,

    /// <summary>The instance's progress replayed to a player who joined late.</summary>
    CatchUp,

}

/// <summary>An enabled mod that wants to know about quest progress.</summary>
internal interface IQuestProgressObserver {

    void OnQuestStarted(Wizard wizard, QuestTemplate quest, QuestProgressSource source);

    void OnGoalCompleted(Wizard wizard, QuestTemplate quest, GoalTemplate goal, QuestProgressSource source);

    void OnQuestCompleted(Wizard wizard, QuestTemplate quest, QuestProgressSource source);

}

/// <summary>Validates quest goals that are completed by a server zone event.</summary>
internal interface IQuestZoneEventValidator {

    bool CanCompleteGoalFromZoneEvent(Wizard wizard, QuestTemplate quest, GoalTemplate goal, string eventName);

    IEnumerable<string> GetZoneEventsToReplay(Wizard wizard, QuestTemplate quest);

}

/// <summary>Prevents QA quest shortcuts from completing progression that requires an authoritative game event.</summary>
internal interface IQuestCommandValidator {

    bool CanCompleteGoalFromCommand(Wizard wizard, QuestTemplate quest, GoalTemplate goal);

    bool CanCompleteQuestFromCommand(Wizard wizard, QuestTemplate quest);

}

internal static class QuestProgressEvents {

    internal static bool CanCompleteGoalFromZoneEvent(Wizard wizard, QuestTemplate quest, GoalTemplate goal, string eventName)
        => CanCompleteGoalFromZoneEvent(wizard, quest, goal, eventName, ModRegistry.Enabled.OfType<IQuestZoneEventValidator>());

    internal static bool CanCompleteGoalFromZoneEvent(Wizard wizard, QuestTemplate quest, GoalTemplate goal, string eventName,
                                                       IEnumerable<IQuestZoneEventValidator> validators) {
        foreach (var validator in validators) {
            try {
                if (!validator.CanCompleteGoalFromZoneEvent(wizard, quest, goal, eventName)) {
                    return false;
                }
            }
            catch (Exception ex) {
                Logger.Error("Quest zone-event validator {0} failed for quest {1} goal {2} event {3} for {4}; the goal stays active: {5}",
                    Logger.Args(validator.GetType().Name, (string) quest?.m_questName ?? "-", (string) goal?.m_goalName ?? "-",
                        eventName, wizard?.CharId, ex));

                return false;
            }
        }

        return true;
    }

    internal static IEnumerable<string> GetZoneEventsToReplay(Wizard wizard, QuestTemplate quest)
        => GetZoneEventsToReplay(wizard, quest, ModRegistry.Enabled.OfType<IQuestZoneEventValidator>());

    internal static IEnumerable<string> GetZoneEventsToReplay(Wizard wizard, QuestTemplate quest,
                                                               IEnumerable<IQuestZoneEventValidator> validators) {
        var events = new List<string>();
        var seenEvents = new HashSet<string>(StringComparer.Ordinal);
        foreach (var validator in validators) {
            try {
                foreach (var eventName in validator.GetZoneEventsToReplay(wizard, quest) ?? []) {
                    if (!string.IsNullOrWhiteSpace(eventName) && seenEvents.Add(eventName)) {
                        events.Add(eventName);
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error("Quest zone-event replay provider {0} failed for quest {1} for {2}; no events were replayed: {3}",
                    Logger.Args(validator.GetType().Name, (string) quest?.m_questName ?? "-", wizard?.CharId, ex));
            }
        }

        return events;
    }

    internal static bool CanCompleteGoalFromCommand(Wizard wizard, QuestTemplate quest, GoalTemplate goal)
        => CanCompleteGoalFromCommand(wizard, quest, goal, ModRegistry.Enabled.OfType<IQuestCommandValidator>());

    internal static bool CanCompleteGoalFromCommand(Wizard wizard, QuestTemplate quest, GoalTemplate goal,
                                                     IEnumerable<IQuestCommandValidator> validators)
        => validators.All(validator => validator.CanCompleteGoalFromCommand(wizard, quest, goal));

    internal static bool CanCompleteQuestFromCommand(Wizard wizard, QuestTemplate quest)
        => CanCompleteQuestFromCommand(wizard, quest, ModRegistry.Enabled.OfType<IQuestCommandValidator>());

    internal static bool CanCompleteQuestFromCommand(Wizard wizard, QuestTemplate quest,
                                                      IEnumerable<IQuestCommandValidator> validators)
        => validators.All(validator => validator.CanCompleteQuestFromCommand(wizard, quest));

    internal static void QuestStarted(Wizard wizard, QuestTemplate quest, QuestProgressSource source)
        => QuestStarted(wizard, quest, source, EnabledObservers);

    internal static void QuestStarted(Wizard wizard, QuestTemplate quest, QuestProgressSource source,
                                      IEnumerable<IQuestProgressObserver> observers)
        => Notify(observers, wizard, quest, null, "start", o => o.OnQuestStarted(wizard, quest, source));

    internal static void GoalCompleted(Wizard wizard, QuestTemplate quest, GoalTemplate goal, QuestProgressSource source)
        => GoalCompleted(wizard, quest, goal, source, EnabledObservers);

    internal static void GoalCompleted(Wizard wizard, QuestTemplate quest, GoalTemplate goal, QuestProgressSource source,
                                       IEnumerable<IQuestProgressObserver> observers)
        => Notify(observers, wizard, quest, goal, "goal", o => o.OnGoalCompleted(wizard, quest, goal, source));

    internal static void QuestCompleted(Wizard wizard, QuestTemplate quest, QuestProgressSource source)
        => QuestCompleted(wizard, quest, source, EnabledObservers);

    internal static void QuestCompleted(Wizard wizard, QuestTemplate quest, QuestProgressSource source,
                                        IEnumerable<IQuestProgressObserver> observers)
        => Notify(observers, wizard, quest, null, "completion", o => o.OnQuestCompleted(wizard, quest, source));

    private static void Notify(IEnumerable<IQuestProgressObserver> observers, Wizard wizard, QuestTemplate quest,
                               GoalTemplate goal, string step, Action<IQuestProgressObserver> notify) {
        if (wizard is null || quest is null || (step == "goal" && goal is null)) {
            return;
        }

        foreach (var observer in observers) {
            try {
                notify(observer);
            }
            catch (Exception ex) {
                Logger.Error("Quest progress observer {0} failed on the {1} of quest {2} goal {3} for {4}; the quest goes on: {5}",
                    Logger.Args(observer.GetType().Name, step, (string) quest.m_questName, (string) goal?.m_goalName ?? "-",
                        wizard.CharId, ex));
            }
        }
    }

    private static IEnumerable<IQuestProgressObserver> EnabledObservers
        => ModRegistry.Enabled.OfType<IQuestProgressObserver>();

}
