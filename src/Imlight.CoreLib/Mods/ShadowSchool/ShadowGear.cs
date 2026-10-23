/*
 * The Shadow school's equipment, level 5 to level 50 in ten tiers: a hat, robe, boots, wand, athame, amulet, ring
 * and deck for each tier (80 items), sold by the Shadow vendor (ShadowSchoolVendor.cs).
 *
 * Each piece is made from a Death item the game already has (the donor): the donor's model, icon, rarity and stat
 * values stay as they are, and only the school and the level it asks for change. The stat effects that name Death
 * are renamed to the game's own Shadow effects (CanonicalShadowDamage and so on, all of which the client knows), so
 * a Shadow wizard sees the same bonuses a Death wizard sees on the donor, worn by their own school.
 *
 * A tier with no Death donor of its own (athame 20 and 40, amulet 20, 30 and 40, ring 30, deck 5 and 40) uses the
 * best donor below it and asks for the tier's level: the piece is a little weaker than a donor of its own level
 * would be, never stronger. Donors are never changed, so a Death wizard keeps every item exactly as it was.
 */

using System.Collections.Generic;
using System.Linq;
using Imcodec.Cryptography;

namespace Imlight.CoreLib.Mods.ShadowSchool;

/// <summary>One piece of Shadow equipment: its slot, the level it asks for, and the item it is made from.</summary>
internal sealed record ShadowGearPiece(string Slot, int Level, string Donor) {

    /// <summary>The item's template name: unique, and what the server and the client call it.</summary>
    public string ObjectName => $"ShadowSchool-{Slot}-{Level}";

    /// <summary>A spell template's id is the hash of its name; the game client keys templates by it.</summary>
    public uint TemplateId => StringHash.Compute(ObjectName);

    /// <summary>The template's file in Root.wad.</summary>
    public string FilePath => $"ObjectData/ShadowSchool/{Slot}-{Level}.xml";

    /// <summary>The key of its name in ShadowSchool.lang (Item_&lt;Slot&gt;_&lt;Level&gt;).</summary>
    public string DisplayNameKey => $"{ShadowSchoolContent.LANG_NAME}_Item_{Slot}_{Level}";

    /// <summary>What the vendor calls it: "Adept's Shadow Hat".</summary>
    public string DisplayName => $"{ShadowGear.TierTitle(Level)} Shadow {ShadowGear.SlotLabel(Slot)}";

}

internal static class ShadowGear {

    internal const string SCHOOL = "Shadow";
    /// <summary>The school the donors are taken from; nothing about them is visible on a Shadow piece.</summary>
    internal const string DONOR_SCHOOL = "Death";
    /// <summary>The canonical stat effect a Death effect is renamed to: CanonicalDeathDamage is CanonicalShadowDamage.</summary>
    private const string DONOR_EFFECT_PREFIX = "CanonicalDeath";
    private const string SHADOW_EFFECT_PREFIX = "CanonicalShadow";

    /// <summary>The ten tiers, every five levels through level 50.</summary>
    internal static readonly int[] Tiers = [5, 10, 15, 20, 25, 30, 35, 40, 45, 50];

    /// <summary>The eight slots, in the order the vendor lists them.</summary>
    internal static readonly string[] Slots = ["Hat", "Robe", "Boots", "Wand", "Athame", "Amulet", "Ring", DECK_SLOT];

    internal const string DECK_SLOT = "Deck";

    /// <summary>The Death item each piece is made from, by slot and tier.</summary>
    private static readonly Dictionary<(string Slot, int Level), string> s_donors = new() {
        // Hats
        [("Hat", 5)] = "ObjectData/Tier1/Hats/Hat-T1-013.xml",
        [("Hat", 10)] = "ObjectData/Tier1/Hats/Hat-T1-006.xml",
        [("Hat", 15)] = "ObjectData/Tier2/Hats/Hat-T2-013.xml",
        [("Hat", 20)] = "ObjectData/Tier3/Hats/Hat-T3-020.xml",
        [("Hat", 25)] = "ObjectData/Tier3/Hats/Hat-T3-006.xml",
        [("Hat", 30)] = "ObjectData/Tier3/Hats/Hat-T3-134.xml",
        [("Hat", 35)] = "ObjectData/Tier4/Hats/Hat-T4-006.xml",
        [("Hat", 40)] = "ObjectData/Tier4/Hats/Hat-T4-137.xml",
        [("Hat", 45)] = "ObjectData/Tier5/Hats/Hat-T5-161.xml",
        [("Hat", 50)] = "ObjectData/CraftedEquipment/Hats/Hat-Craft-T5-073.xml",

        // Robes
        [("Robe", 5)] = "ObjectData/Tier1/Robes/Robe-T1-034.xml",
        [("Robe", 10)] = "ObjectData/Tier2/Robes/Robe-T2-006.xml",
        [("Robe", 15)] = "ObjectData/Tier2/Robes/Robe-T2-013.xml",
        [("Robe", 20)] = "ObjectData/Tier2/Robes/Robe-T2-034.xml",
        [("Robe", 25)] = "ObjectData/Tier3/Robes/Robe-T3-013.xml",
        [("Robe", 30)] = "ObjectData/Tier3/Robes/Robe-T3-131.xml",
        [("Robe", 35)] = "ObjectData/Tier4/Robes/Robe-T4-006.xml",
        [("Robe", 40)] = "ObjectData/Tier4/Robes/Robe-T4-131.xml",
        [("Robe", 45)] = "ObjectData/Tier5/Robes/Robe-T5-021.xml",
        [("Robe", 50)] = "ObjectData/CraftedEquipment/Robes/Robe-Craft-T5-073.xml",

        // Boots
        [("Boots", 5)] = "ObjectData/Tier1/Shoes/Shoe-T1-034.xml",
        [("Boots", 10)] = "ObjectData/Tier1/Shoes/Shoe-T1-006.xml",
        [("Boots", 15)] = "ObjectData/Tier2/Shoes/Shoe-T2-027.xml",
        [("Boots", 20)] = "ObjectData/Tier2/Shoes/Shoe-T2-041.xml",
        [("Boots", 25)] = "ObjectData/Tier3/Shoes/Shoe-T3-006.xml",
        [("Boots", 30)] = "ObjectData/Tier3/Shoes/Shoe-T3-118.xml",
        [("Boots", 35)] = "ObjectData/Tier4/Shoes/Shoe-T4-006.xml",
        [("Boots", 40)] = "ObjectData/Tier4/Shoes/Shoe-T4-135.xml",
        [("Boots", 45)] = "ObjectData/Tier5/Shoes/Shoe-T5-033.xml",
        [("Boots", 50)] = "ObjectData/CraftedEquipment/Shoes/Shoe-Craft-T5-073.xml",

        // Wands
        [("Wand", 5)] = "ObjectData/Tier1/Wands/Wand-T1-005.xml",
        [("Wand", 10)] = "ObjectData/Tier1/Wands/Wand-T1-006.xml",
        [("Wand", 15)] = "ObjectData/Tier2/Wands/Wand-T2-004.xml",
        [("Wand", 20)] = "ObjectData/Tier2/Wands/Wand-T2-006.xml",
        [("Wand", 25)] = "ObjectData/Tier3/Wands/Wand-T3-003.xml",
        [("Wand", 30)] = "ObjectData/Tier3/Wands/Wand-T3-004.xml",
        [("Wand", 35)] = "ObjectData/Tier4/Wands/Wand-T4-003.xml",
        [("Wand", 40)] = "ObjectData/Tier4/Wands/Wand-T4-004.xml",
        [("Wand", 45)] = "ObjectData/Tier5/Wands/Wand-T5-004.xml",
        [("Wand", 50)] = "ObjectData/Tier6/Wands/Wand-T6-006.xml",

        // Athames
        [("Athame", 5)] = "ObjectData/Tier1/Athames/Athame-T1-011.xml",
        [("Athame", 10)] = "ObjectData/Tier1/Athames/Athame-T1-002.xml",
        [("Athame", 15)] = "ObjectData/Tier2/Athames/Athame-T2-012.xml",
        [("Athame", 20)] = "ObjectData/Tier2/Athames/Athame-T2-012.xml",
        [("Athame", 25)] = "ObjectData/Tier3/Athames/Athame-T3-011.xml",
        [("Athame", 30)] = "ObjectData/Tier4/Athames/Athame-T4-027.xml",
        [("Athame", 35)] = "ObjectData/Tier4/Athames/Athame-T4-006.xml",
        [("Athame", 40)] = "ObjectData/Tier4/Athames/Athame-T4-006.xml",
        [("Athame", 45)] = "ObjectData/Tier5/Athames/Athame-T5-006.xml",
        [("Athame", 50)] = "ObjectData/Tier6/Athames/Athame-T6-007.xml",

        // Amulets
        [("Amulet", 5)] = "ObjectData/Tier1/Amulets/Amulet-T1-014.xml",
        [("Amulet", 10)] = "ObjectData/Tier1/Amulets/Amulet-T1-015.xml",
        [("Amulet", 15)] = "ObjectData/Tier2/Amulets/Amulet-T2-009.xml",
        [("Amulet", 20)] = "ObjectData/Tier2/Amulets/Amulet-T2-009.xml",
        [("Amulet", 25)] = "ObjectData/Tier3/Amulet/Amulet-T3-009.xml",
        [("Amulet", 30)] = "ObjectData/Tier3/Amulet/Amulet-T3-009.xml",
        [("Amulet", 35)] = "ObjectData/Tier4/Amulets/Amulet-T4-005.xml",
        [("Amulet", 40)] = "ObjectData/Tier4/Amulets/Amulet-T4-005.xml",
        [("Amulet", 45)] = "ObjectData/Tier5/Amulets/Amulet-T5-002.xml",
        [("Amulet", 50)] = "ObjectData/Tier6/Amulets/Amulet-T6-007.xml",

        // Rings
        [("Ring", 5)] = "ObjectData/Tier1/Rings/Ring-T1-015.xml",
        [("Ring", 10)] = "ObjectData/Tier1/Rings/Ring-T1-006.xml",
        [("Ring", 15)] = "ObjectData/Tier2/Rings/Ring-T2-008.xml",
        [("Ring", 20)] = "ObjectData/Tier2/Rings/Ring-T2-053.xml",
        [("Ring", 25)] = "ObjectData/Tier3/Rings/Ring-T3-007.xml",
        [("Ring", 30)] = "ObjectData/Tier3/Rings/Ring-T3-007.xml",
        [("Ring", 35)] = "ObjectData/Tier4/Rings/Ring-T4-007.xml",
        [("Ring", 40)] = "ObjectData/Tier4/Rings/Ring-T4-GH-006.xml",
        [("Ring", 45)] = "ObjectData/Tier5/Rings/Ring-T5-007.xml",
        [("Ring", 50)] = "ObjectData/Gauntlet/MSP/Ring/Ring-G01-002.xml",

        // Decks
        [("Deck", 5)] = "ObjectData/Tier1/Decks/Deck-T1-002.xml",
        [("Deck", 10)] = "ObjectData/Tier1/Decks/Deck-T1-009.xml",
        [("Deck", 15)] = "ObjectData/Tier2/Decks/Deck-T2-009.xml",
        [("Deck", 20)] = "ObjectData/Tier2/Decks/Deck-T2-016.xml",
        [("Deck", 25)] = "ObjectData/Tier3/Decks/Deck-T3-009.xml",
        [("Deck", 30)] = "ObjectData/Tier4/Decks/Deck-T4-008.xml",
        [("Deck", 35)] = "ObjectData/Tier4/Decks/Deck-T4-015.xml",
        [("Deck", 40)] = "ObjectData/Tier4/Decks/Deck-T4-015.xml",
        [("Deck", 45)] = "ObjectData/Tier5/Decks/Deck-T5-015.xml",
        [("Deck", 50)] = "ObjectData/Tier6/Decks/Deck-T6-022.xml",
    };

    /// <summary>Every piece, slot by slot and tier by tier: 8 slots × 10 tiers.</summary>
    internal static IEnumerable<ShadowGearPiece> Pieces {
        get {
            foreach (var slot in Slots) {
                foreach (var level in Tiers) {
                    yield return new ShadowGearPiece(slot, level, s_donors[(slot, level)]);
                }
            }
        }
    }

    /// <summary>The vendor's name for a piece: "Novice's Shadow Hat" through "Umbral Shadow Hat".</summary>
    internal static string TierTitle(int level) => level switch {
        5 => "Novice's",
        10 => "Apprentice's",
        15 => "Adept's",
        20 => "Scholar's",
        25 => "Disciple's",
        30 => "Acolyte's",
        35 => "Cabalist's",
        40 => "Master's",
        45 => "Archon's",
        50 => "Umbral",
        _ => throw new System.ArgumentOutOfRangeException(nameof(level), level, "No Shadow tier at that level."),
    };

    internal static string SlotLabel(string slot) => slot == "Boots" ? "Boots" : slot;

}
