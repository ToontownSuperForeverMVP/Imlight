/*
 * Player presence events: lets a mod follow where one wizard is in their zone, to react privately to that wizard.
 *
 * MoveService reports a zone attach (a login, reconnect or zone change), each move the client sends, and, while the
 * wizard stands still, how long they have been still (on its 250 ms interaction tick). The session is the wizard's
 * SessionActor: a client message told to it goes to that one player's game client only.
 *
 * An observer may only send to that session and keep its own per-wizard state; a throwing observer is logged once
 * per kind of failure and skipped, so moving around is never affected.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Imlight.Common;
using Imlight.CoreLib.Mods;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Game.World;

/// <summary>An enabled mod that follows one wizard's presence in their zone.</summary>
internal interface IPlayerPresenceObserver {

    /// <summary>The wizard is in a zone now (login, reconnect or zone change); forget anything about the last one.</summary>
    void OnAttached(IActorRef session, Wizard wizard);

    /// <summary>The wizard moved to (x, y, z) in <see cref="Wizard.Zone"/>.</summary>
    void OnMoved(IActorRef session, Wizard wizard, float x, float y, float z);

    /// <summary>The wizard has not moved for <paramref name="still"/>, standing at <see cref="Wizard.Location"/>.</summary>
    void OnStill(IActorRef session, Wizard wizard, TimeSpan still);

    /// <summary>The wizard's session ended.</summary>
    void OnDetached(ulong charId);

}

internal static class PlayerPresenceEvents {

    private static readonly ConcurrentDictionary<string, bool> s_reported = new(StringComparer.Ordinal);

    internal static void Attached(IActorRef session, Wizard wizard) {
        if (wizard is not null) {
            Notify("attach", o => o.OnAttached(session, wizard));
        }
    }

    internal static void Moved(IActorRef session, Wizard wizard, float x, float y, float z) {
        if (wizard is not null) {
            Notify("move", o => o.OnMoved(session, wizard, x, y, z));
        }
    }

    internal static void Still(IActorRef session, Wizard wizard, TimeSpan still) {
        if (wizard is not null) {
            Notify("still", o => o.OnStill(session, wizard, still));
        }
    }

    internal static void Detached(ulong charId) {
        if (charId != 0) {
            Notify("detach", o => o.OnDetached(charId));
        }
    }

    private static void Notify(string step, Action<IPlayerPresenceObserver> notify) {
        foreach (var observer in Observers) {
            try {
                notify(observer);
            }
            catch (Exception ex) {
                if (s_reported.TryAdd($"{observer.GetType().Name}/{step}/{ex.GetType().Name}", true)) {
                    Logger.Error("Player presence observer {0} failed on {1}; it is skipped: {2}",
                        Logger.Args(observer.GetType().Name, step, ex));
                }
            }
        }
    }

    private static IEnumerable<IPlayerPresenceObserver> Observers
        => ModRegistry.Enabled.OfType<IPlayerPresenceObserver>();

}
