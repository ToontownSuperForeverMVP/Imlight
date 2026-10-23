/*
 * Shadow School: an eighth starting school with its own Wizard City story (docs/shadow-school/).
 *
 * The school itself is the game's Shadow school (id 1429009101, MagicSchool.Shadow); this mod lets a new wizard
 * pick it, and gives it a curriculum, health, a tutor and its story. Everything here only applies to wizards whose
 * school of focus is Shadow, so the other seven schools play exactly as before.
 */

using System.Collections.Generic;
using Imcodec.Wad;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal sealed class ShadowSchoolMod : IMod {

    internal const string NAME = "ShadowSchool";

    // The Unlisted Tutor (Vespera Quillmoor): the mod's own NPC, at the end of the Library's book stacks.
    internal const uint TUTOR_TEMPLATE_ID = 1880000100;
    internal const string TUTOR_OBJECT_NAME = "ShadowSchool-Tutor";
    internal const string TUTOR_PERSONA = "ShadowSchool-Tutor_Persona";
    internal const string TUTOR_FILE = "ObjectData/ShadowSchool/ShadowSchool-Tutor.xml";
    // The tutor's look: Velma von Venkman's model, an Arcanum professor no Wizard City wizard has met.
    internal const uint TUTOR_MODEL_DONOR = 1340974;

    // The Shadow vendor (the tutor's colleague, at the other end of the aisle): the same Arcanum look as the
    // tutor, with the shop of a Wizard City shopkeeper. It sells ShadowGear.Pieces (80 items) from spiraldb.
    internal const uint VENDOR_TEMPLATE_ID = 1880000101;
    internal const string VENDOR_OBJECT_NAME = "ShadowSchool-Vendor";
    internal const string VENDOR_PERSONA = "ShadowSchool-Vendor_Persona";
    internal const string VENDOR_FILE = "ObjectData/ShadowSchool/ShadowSchool-Vendor.xml";
    // The shopkeeper whose shopping behavior the vendor borrows (KT-HUB-NPC05, a Krokotopia equipment seller).
    internal const uint VENDOR_SHOP_DONOR = 39385;

    public string Name => NAME;

    internal static bool Enabled => ModRegistry.IsEnabled(NAME);

    internal static bool IsShadowWizard(Wizard wizard)
        => Enabled && wizard?.MagicSchoolBehavior?.MagicSchool == MagicSchool.Shadow;

    public void BuildRootFiles(Archive root, IDictionary<string, byte[]> files)
        => ShadowSchoolContent.Build(root, files, ModRegistry.FolderOf(this));

}
