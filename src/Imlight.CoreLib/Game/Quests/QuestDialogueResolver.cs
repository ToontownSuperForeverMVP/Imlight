/*
 * Per-recipient quest dialogue: lets a mod show one wizard a different version of a canonical quest dialogue.
 *
 * Every place that sends a quest's dialogue to a player (QuestService for quest start, goal start, goal completion
 * and quest completion; the quest offer and underway NPC components; the QA offer command) passes the canonical
 * ActorDialog it found through Resolve, together with the wizard who will receive it. An enabled mod that is an
 * IQuestDialogueProvider may answer with a new dialogue for that wizard; anything else gets the canonical one.
 *
 * The quest templates are shared by every player, so nothing here or in a provider may change them: a provider
 * builds new objects (C# record copies) and the canonical dialogue stays as SpiralDB loaded it. What the quest does
 * (goals, requirements, rewards, results, completion flags) is never touched, only the words shown.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Imcodec.ObjectProperty.TypeCache;
using Imlight.Common;
using Imlight.CoreLib.Mods;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Game.Quests;

/// <summary>The canonical dialogue about to be shown, where it belongs in its quest, and who will see it.</summary>
/// <param name="Goal">The goal whose dialogue it is, or null for the quest's own (Prep, Underway, Start, Complete).</param>
internal sealed record QuestDialogueRequest(Wizard Recipient, QuestTemplate Quest, GoalTemplate Goal, ActorDialog Canonical) {

    internal string QuestName => Quest?.m_questName;
    internal string GoalName => Goal?.m_goalName;
    internal string DialogTag => Canonical?.m_dialogTag;

}

/// <summary>An enabled mod that can show one wizard its own version of a canonical quest dialogue.</summary>
internal interface IQuestDialogueProvider {

    /// <summary>
    /// A new dialogue for <see cref="QuestDialogueRequest.Recipient"/>, or null to leave the canonical one. It must
    /// not change the canonical dialogue or its template; it is only shown to that one wizard.
    /// </summary>
    ActorDialog Resolve(QuestDialogueRequest request);

}

internal static class QuestDialogueResolver {

    /// <summary>The dialogue <paramref name="recipient"/> should see in place of <paramref name="canonical"/>.</summary>
    internal static ActorDialog Resolve(Wizard recipient, QuestTemplate quest, GoalTemplate goal, ActorDialog canonical)
        => Resolve(recipient, quest, goal, canonical, EnabledProviders);

    internal static ActorDialog Resolve(Wizard recipient, QuestTemplate quest, GoalTemplate goal, ActorDialog canonical,
                                        IEnumerable<IQuestDialogueProvider> providers) {
        if (recipient is null || quest is null || canonical is null) {
            return canonical;
        }

        var request = new QuestDialogueRequest(recipient, quest, goal, canonical);
        foreach (var provider in providers) {
            ActorDialog resolved;
            try {
                resolved = provider.Resolve(request);
            }
            catch (Exception ex) {
                Logger.Error("Quest dialogue provider {0} failed for quest {1} goal {2} tag {3}; the canonical dialogue is shown: {4}",
                    Logger.Args(provider.GetType().Name, request.QuestName, request.GoalName ?? "-", request.DialogTag, ex));

                continue;
            }

            if (resolved is null || ReferenceEquals(resolved, canonical)) {
                continue;
            }

            // A replacement keeps the dialogue's place: the same tag, and something to show (the client reports an
            // empty dialogue as never closed, which would hold a persona goal open).
            if (resolved.m_dialogTag != canonical.m_dialogTag || resolved.m_dialogEntries is not { Count: > 0 }) {
                Logger.Error("Quest dialogue provider {0} returned an unusable dialogue for quest {1} goal {2} tag {3}; the canonical dialogue is shown.",
                    Logger.Args(provider.GetType().Name, request.QuestName, request.GoalName ?? "-", request.DialogTag));

                continue;
            }

            return resolved;
        }

        return canonical;
    }

    /// <summary>
    /// <paramref name="quest"/> as <paramref name="recipient"/> should receive it whole (the quest offer cache), with
    /// each of its dialogues resolved. The template itself when nothing changes, else a copy for this wizard only.
    /// </summary>
    internal static QuestTemplate ResolveTemplate(Wizard recipient, QuestTemplate quest)
        => ResolveTemplate(recipient, quest, EnabledProviders);

    internal static QuestTemplate ResolveTemplate(Wizard recipient, QuestTemplate quest,
                                                  IEnumerable<IQuestDialogueProvider> providers) {
        if (recipient is null || quest is null) {
            return quest;
        }

        var list = providers as IReadOnlyCollection<IQuestDialogueProvider> ?? providers.ToList();
        if (list.Count == 0) {
            return quest;
        }

        var questDialogs = ResolveList(recipient, quest, null, quest.m_dialogList, list);
        List<GoalTemplate> goals = null;
        for (var i = 0; i < (quest.m_goals?.Count ?? 0); i++) {
            var goal = quest.m_goals[i];
            var goalDialogs = ResolveList(recipient, quest, goal, goal?.m_dialogList, list);
            if (ReferenceEquals(goalDialogs, goal?.m_dialogList)) {
                continue;
            }

            goals ??= [.. quest.m_goals];
            goals[i] = goal with { m_dialogList = goalDialogs };
        }

        if (ReferenceEquals(questDialogs, quest.m_dialogList) && goals is null) {
            return quest;
        }

        return quest with { m_dialogList = questDialogs, m_goals = goals ?? quest.m_goals };
    }

    private static ActorDialogListBase ResolveList(Wizard recipient, QuestTemplate quest, GoalTemplate goal,
                                                   ActorDialogListBase listBase,
                                                   IReadOnlyCollection<IQuestDialogueProvider> providers) {
        if (listBase is not ActorDialogList { m_dialogs: { } dialogs } list) {
            return listBase;
        }

        List<ActorDialog> resolved = null;
        for (var i = 0; i < dialogs.Count; i++) {
            var dialog = Resolve(recipient, quest, goal, dialogs[i], providers);
            if (ReferenceEquals(dialog, dialogs[i])) {
                continue;
            }

            resolved ??= [.. dialogs];
            resolved[i] = dialog;
        }

        return resolved is null ? listBase : list with { m_dialogs = resolved };
    }

    private static IEnumerable<IQuestDialogueProvider> EnabledProviders
        => ModRegistry.Enabled.OfType<IQuestDialogueProvider>();

}
