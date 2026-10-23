/*
 * Imlight
 * Copyright (C) 2025 Revive101
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 * ========================================================================
 * TRAIN SERVICE
 * ========================================================================
 * 
 * PURPOSE:
 * Manages player spell training mechanics, including spell learning 
 * and training point management.
 * 
 * USAGE EXAMPLE:
 * Internal service handling spell training interactions within 
 * the game server session.
 * 
 * NOTE:
 * 
 * TODO:
 * 
 * Created by: Joji
 * Version: KALI 1.0
 * Last Updated: 10/14/2026
 */

using Akka.Actor;
using Imlight.CoreLib.Shared.Networking;
using Imlight.CoreLib.Shared.Packets;
using Imlight.Common;
using Imlight.CoreLib.Game.Badges;
using Imlight.CoreLib.Game.Zone.Components;
using Imlight.CoreLib.Shared.Resources;
using Imlight.CoreLib.Game.Spells;
using Imlight.CoreLib.Game.Randomization;
using Imcodec.MessageLayer.Generated;
using Imcodec.ObjectProperty.TypeCache;

namespace Imlight.CoreLib.Game.Services;

internal class TrainService(SessionActor sessionActor) : MessageService(sessionActor) {
    
    protected static Props Props(SessionActor parentActor)
        => Akka.Actor.Props.Create(() => new TrainService(parentActor));

    [MessageHandler(typeof(WIZARD_12_PROTOCOL.MSG_TRAIN))]
    private void ReceiveTrain(WIZARD_12_PROTOCOL.MSG_TRAIN message) {
        // Query the zone for the NPC by the ID
        var msg = new ZONE_102_PROTOCOL.MSG_QUERYZONEENTITY() {
            GlobalID = message.MobileID
        };
        var response = AskOtherService<ZONE_102_PROTOCOL.MSG_QUERYZONEENTITYRSP>(msg);
        if (response is null || response.ZoneObject is null) {
            var wizardName = GetActiveWizard().PlayerNameBehavior.GetWizardName();
            Logger.Error("{0} searched for training NPC {1} (mobile ID), but it was not found within the zone.",
                Logger.Args(wizardName, message.MobileID));

            return;
        }

        // Ensure that the interacted object has a TrainerComponent.
        var trainerComponent = response.ZoneObject.GetComponentOfType<InteractTrainerComponent>();
        if (trainerComponent is null) {
            Logger.Error("NPC {0} does not have a TrainerComponent.", Logger.Args(response.ZoneObject.ActiveGameObject.m_debugName));

            return;
        }

        // We don't need to inform the TrainerComponent of our interaction, as the client
        // doesn't even send an interaction message upon training.
        var wizard = GetActiveWizard();

        // The same rules as the list the trainer shows (InteractTrainerComponent.Evaluate): a request for a spell the
        // list would not let this wizard train (too low a level, too few training points, a missing earlier spell,
        // an index past the list) is refused, whatever the client sent.
        var check = trainerComponent.Evaluate(wizard, message.TrainingIndex);
        if (check is null || !check.CanTrain) {
            Logger.Warning("Wizard {0} (level {1}) asked to train index {2} of {3}, which the trainer does not allow: {4}.",
                Logger.Args(wizard.PlayerNameBehavior.GetWizardName(), wizard.MagicSchoolBehavior.Level, message.TrainingIndex,
                    response.ZoneObject.ActiveGameObject.m_debugName,
                    check is null ? "no such spell" : check.Known ? "already known" : check.TooLowOrTooPoor
                        ? $"needs level {check.RequiredLevel} and {check.Cost} training points" : "needs an earlier spell"));

            return;
        }

        var spellEntry = trainerComponent.SpellInventory[message.TrainingIndex];
        var spellId = check.SpellId;
        var spellTemplate = check.Spell;

        if (BadgeGate.Blocks(spellTemplate?.m_purchaseRequirements, spellEntry.TemplateID, SessionActor.ActorRef, GetActiveGameObject(), wizard, SessionActor.GetZoneActor())) {
            Logger.Warning("Wizard {0} attempted to train spell {1} without the badge it requires.",
                Logger.Args(wizard.PlayerNameBehavior.GetWizardName(), spellEntry.TemplateID));

            return;
        }

        var spellCost = check.Cost;

        var spell = SpellFactory.GetSpell(spellId);
        var spellLearnedSuccess = wizard.LearnSpell(spell);
        if (!spellLearnedSuccess) {
            Logger.Error("Wizard {0} attempted to train spell {1} but failed to learn it.",
                Logger.Args(wizard.PlayerNameBehavior.GetWizardName(), spellId));

            return;
        }

        var addSpellMsg = new WIZARD_12_PROTOCOL.MSG_ADDSPELLTOBOOK() {
            SpellID = (int) spellId
        };
        SendToSocket(addSpellMsg);

        var newTrainingPoints = wizard.MagicSchoolBehavior.TrainingPoints - spellCost;
        wizard.UpdateTrainingPoints(newTrainingPoints);

        var updateTrainingMsg = new WIZARD_12_PROTOCOL.MSG_UPDATETRAINING() {
            TrainingPoints = newTrainingPoints
        };
        SendToSocket(updateTrainingMsg);

        var trainCompleteMsg = new WIZARD_12_PROTOCOL.MSG_SPELLTRAINCOMPLETE() {
            SpellID = spellId,
            DisplayText = "WizTraining_00000040",
            Success = 1
        };
        SendToSocket(trainCompleteMsg);
    }

}
