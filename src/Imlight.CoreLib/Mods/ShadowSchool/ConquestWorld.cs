/*
 * The live server side of the Ravenwood conquest (ConquestEncounters.cs): saving a character's registry, posting a
 * quest event, showing text, and moving one wizard into or out of an encounter's private copy. Every message goes to
 * the one wizard's own SessionActor, through the server's ordinary paths:
 *   - a quest event is ZONE_102 MSG_ZONEEVENTFORQUESTS, which QuestService turns into the completion of an active goal
 *     whose generic events name it (as a zone trigger's ResPostQuestEvent does);
 *   - entering is ZONE_102 MSG_ZONETRANSFER into a fresh private copy owned by the wizard, loaded with the encounter's
 *     ZoneObjects variant, noted in ZoneVariants so the game's attach after the transfer lands in that copy (as a
 *     dungeon sigil does);
 *   - leaving forgets that note first, then transfers the wizard to the exit in Ravenwood, so no attach after it, and
 *     no reconnect, lands in the copy again.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Imlight.Common;
using Imlight.CoreLib.Game.World;
using Imlight.CoreLib.Shared.Packets;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal sealed class ConquestWorld(Action<Wizard> save) : IConquestWorld {

    public DateTime UtcNow => DateTime.UtcNow;

    public void Save(Wizard wizard) => save(wizard);

    public void PostQuestEvent(IActorRef session, string eventName)
        => session?.Tell(new ZONE_102_PROTOCOL.MSG_ZONEEVENTFORQUESTS { EventName = eventName });

    public void Show(IActorRef session, IReadOnlyList<string> notifyKeys, IReadOnlyList<string> chatKeys)
        => UmbralPerception.Send(session, new UmbralPerception.Shown(notifyKeys, chatKeys));

    public void Enter(IActorRef session, Wizard wizard, ConquestEncounter encounter) {
        ZoneInstanceOwners.Clear(wizard.CharId, encounter.Zone);
        ZoneVariants.Remember(wizard.CharId, encounter.Zone, encounter.Variant);
        session.Tell(new ZONE_102_PROTOCOL.MSG_ZONETRANSFER {
            DestinationZone = encounter.Zone,
            DestinationLocation = encounter.ArrivalLoc,
            SendToClient = true,
            IsPrivate = true,
            OwnerCharId = wizard.CharId,
            ResetInstance = true,
            Variant = encounter.Variant,
            // The anchor is inside the encounter's own zone, so the wizard is already there when this is sent.
            // Without ForceReload, ZoneService takes its same-zone shortcut: it only teleports the wizard inside
            // the PUBLIC school (DoTeleport), leaving the trainer in place and never loading the variant copy, so
            // the battle circle and the hostile instructor would never exist. The flag makes the client reload the
            // zone, which lands it in the owner's private copy with the variant (AttachService, ZoneVariants).
            ForceReload = true,
        });
    }

    public void Leave(IActorRef session, Wizard wizard, ConquestEncounter encounter, TimeSpan delay) {
        ZoneVariants.Remember(wizard.CharId, encounter.Zone, "");
        var transfer = new ZONE_102_PROTOCOL.MSG_ZONETRANSFER {
            DestinationZone = encounter.ExitZone,
            DestinationLocation = encounter.ExitLoc,
            SendToClient = true,
            OwnerCharId = wizard.CharId,
            // Ashthorn and Wethersfield stand in Ravenwood itself, so their copy and their exit are the same zone: without
            // ForceReload ZoneService only teleports the wizard inside the copy and never brings them back to the public
            // zone (the same shortcut Enter has to get past).
            ForceReload = string.Equals(encounter.ExitZone, encounter.Zone, StringComparison.OrdinalIgnoreCase),
        };

        if (delay <= TimeSpan.Zero) {
            session.Tell(transfer);

            return;
        }

        // Only if the wizard is still in the copy's zone by then (they may have walked out of its door already).
        _ = Task.Delay(delay).ContinueWith(_ => {
            if (string.Equals(wizard.Zone, encounter.Zone, StringComparison.OrdinalIgnoreCase)) {
                session.Tell(transfer);
            }
        }, TaskScheduler.Default);
    }

    public string NotedVariant(ulong charId, string zone)
        => ZoneVariants.Of(charId) is { } noted && string.Equals(noted.Zone, zone, StringComparison.OrdinalIgnoreCase)
            ? noted.Variant
            : "";

}
