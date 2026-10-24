/*
 * Staff commands for Umbral Sight (UmbralSight.cs). They read state, and one of them reconciles a character who walked
 * the Nightside before the walk was recorded; none grants the sight to a character who has not completed the chain.
 *
 *   .umbral status      your character's Umbral Sight state and recorded Nightside trail
 *   .umbral look        the bindings of your zone, how far each is, and whether you perceive it
 *   .umbral reconcile   unlock it for your own character, if it is Shadow and has completed the Nightside chain
 */

using System.Linq;
using Imlight.CoreLib.Game.Commands;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal sealed class UmbralSightCommands : CommandProtocol {

    internal override string Group { get; set; } = "umbral";

    [Command("status")]
    [AuthRequired(AuthLevel.QualityAssurance)]
    private void StatusCommand() {
        if (!ShadowSchoolMod.Enabled) {
            InformSenderClient("Shadow School is not enabled.");
            return;
        }

        InformSenderClient($"Umbral Sight: {UmbralSight.Describe(Context.Character)}.");
    }

    [Command("look")]
    [AuthRequired(AuthLevel.QualityAssurance)]
    private void LookCommand() {
        if (!ShadowSchoolMod.Enabled) {
            InformSenderClient("Shadow School is not enabled.");
            return;
        }

        var wizard = Context.Character;
        var around = UmbralPerception.Around(wizard).ToList();
        if (around.Count == 0) {
            InformSenderClient($"No Umbral Sight bindings in {wizard.Zone}.");
            return;
        }

        var perceives = UmbralSight.CanPerceive(wizard);
        foreach (var (binding, distance) in around) {
            var reach = distance <= binding.Radius ? "within reach" : "out of reach";
            InformSenderClient($"{binding.Id} ({binding.Kind}): {distance:0} away, radius {binding.Radius:0}, {reach}"
                + (perceives ? "." : "; you do not perceive bindings."));
        }
    }

    // Writes the character's registry, so it is for administrators, on their own logged-in character only (another
    // character's in-memory copy, if they are online, would not see the change and could save over it).
    [Command("reconcile")]
    [AuthRequired(AuthLevel.Administrator)]
    private void ReconcileCommand() {
        if (!ShadowSchoolMod.Enabled) {
            InformSenderClient("Shadow School is not enabled.");
            return;
        }

        UmbralSight.StaffReconcile(Context.Character, UmbralSight.SaveToDatabase, out var outcome);
        InformSenderClient(outcome);
    }

}
