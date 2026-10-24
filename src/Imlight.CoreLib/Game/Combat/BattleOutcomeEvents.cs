/*
 * Battle outcome events: lets a mod learn how a battle against creatures ended for each wizard in it, and where.
 *
 * CombatDuelComponent reports, on the duel's own actor, once per wizard: a win (with the creatures defeated), a loss,
 * a flee, or leaving the battle by a disconnect or logout. The report names the zone path, whether the zone is a
 * private instance, and the ZoneObjects variant the copy was loaded with, so a mod can tell its own private battle
 * room from the public zone of the same name. PvP duels are not reported.
 *
 * An observer only reads. It runs on the duel's actor, not on the wizard's session, so it must not change the wizard:
 * anything it wants done for the wizard (quest progress, a message, a transfer) it tells the wizard's session. A
 * throwing observer is logged and skipped, so the battle ends exactly as before.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Imlight.Common;
using Imlight.CoreLib.Mods;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Game.Combat;

internal enum BattleResult {

    /// <summary>The wizard's side defeated every creature.</summary>
    Won,

    /// <summary>The creatures defeated the wizard's side.</summary>
    Lost,

    /// <summary>The wizard fled.</summary>
    Fled,

    /// <summary>The wizard dropped or logged out during the battle.</summary>
    Left,

}

/// <summary>A creature of the battle: its template, the custom it was spawned as (Customs.cs, 0 for none) and its id.</summary>
internal sealed record BattleCreature(ulong TemplateId, int CustomId, ulong GlobalId);

/// <summary>How a battle ended for one wizard.</summary>
internal sealed record BattleOutcome(
    BattleResult Result,
    Wizard Wizard,
    IActorRef Session,
    string ZonePath,
    string Variant,
    bool IsInstance,
    IReadOnlyList<BattleCreature> Creatures);

/// <summary>An enabled mod that wants to know how battles end.</summary>
internal interface IBattleOutcomeObserver {

    void OnBattleEnded(BattleOutcome outcome);

}

internal static class BattleOutcomeEvents {

    internal static void Report(BattleOutcome outcome)
        => Report(outcome, ModRegistry.Enabled.OfType<IBattleOutcomeObserver>());

    internal static void Report(BattleOutcome outcome, IEnumerable<IBattleOutcomeObserver> observers) {
        if (outcome?.Wizard is null) {
            return;
        }

        foreach (var observer in observers) {
            try {
                observer.OnBattleEnded(outcome);
            }
            catch (Exception ex) {
                Logger.Error("Battle outcome observer {0} failed on {1} for {2} in {3}; the battle ends as before: {4}",
                    Logger.Args(observer.GetType().Name, outcome.Result, outcome.Wizard.CharId, outcome.ZonePath, ex));
            }
        }
    }

}
