/*
 * Staff command for the Ravenwood conquest (ConquestEncounters.cs). It only reads: it grants, starts and resets
 * nothing, so it cannot put a character ahead in the story.
 *
 *   .conquest status    your character's stage, quest goal, eligibility and open session for each encounter
 */

using Imlight.CoreLib.Game.Commands;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal sealed class ConquestCommands : CommandProtocol {

    internal override string Group { get; set; } = "conquest";

    [Command("status")]
    [AuthRequired(AuthLevel.QualityAssurance)]
    private void StatusCommand() {
        if (!ShadowSchoolMod.Enabled || ShadowSchoolMod.Conquest is not { } conquest) {
            InformSenderClient("Shadow School is not enabled.");
            return;
        }

        foreach (var encounter in ConquestEncounters.Encounters) {
            InformSenderClient($"Conquest {conquest.Describe(Context.Character, encounter)}.");
        }
    }

}
