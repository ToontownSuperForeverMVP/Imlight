/*
 * The Shadow school's fourteen trainable spells, levels 1 to 50.
 *
 * Each is a separate player spell made from a Shadow spell the game already has (the donor): the card art, spell
 * base (cinematic), type and school stay the donor's; only the numbers, costs and effects below change. The donors
 * themselves are never changed, so creatures that cast them keep doing what they did.
 *
 * Costs: Pips is the spell's ordinary pip cost (Power Pips count double for a Shadow wizard, as for any school spell);
 * ShadowPips is its Shadow Pip cost. Accuracy: 85 for attacks (80 for Triton and Frost Giant), 100 for the rest.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Imcodec.Cryptography;
using Imcodec.ObjectProperty.TypeCache;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal sealed record ShadowSpell(
    string Key,             // stable key: ShadowSchool.lang Spell_<Key>, Desc_<Key>
    int Level,              // the level the tutor teaches it at
    string Donor,           // the game's Shadow spell it is made from (Root.wad path)
    byte Pips,
    byte ShadowPips,
    int Accuracy,
    bool OwnDescription,    // true: the description is Desc_<Key> in ShadowSchool.lang; false: the donor's
    Action<SpellTemplate> Effects) {

    /// <summary>The spell's template name (its id in the spell tables and in commands).</summary>
    public string Name => "ShadowSchool " + Key;

    /// <summary>A spell template's id is the hash of its name; the game client keys its templates by it.</summary>
    public uint TemplateId => StringHash.Compute(Name);

    /// <summary>The template's file in Root.wad.</summary>
    public string FilePath => $"Spells/ShadowSchool/{Key}.xml";

}

internal static class ShadowCurriculum {

    internal static readonly ShadowSpell[] Spells = [
        new("DarkSprite", 1, "Spells/Dark Sprite - Shadowbones.xml", 1, 0, 85, false,
            t => SetRandomDamage(t, 75, 80, 85, 90, 95)),
        new("DarkEmpower", 1, "Spells/Dark Empower.xml", 0, 1, 100, true,
            // The donor gives a Power Pip and a pip; the student's version gives one ordinary pip.
            t => t.m_effects.RemoveAll(e => e.m_effectType == kSpellEffects.kModifyPowerPips)),
        new("ShadowTroll", 5, "Spells/Troll Death - Shadow.xml", 2, 0, 85, false,
            t => SetRandomDamage(t, 160, 170, 180, 190, 200)),
        new("TowerShield", 8, "Spells/Tower Shield - Shadow Protected.xml", 0, 0, 100, false,
            t => {
                var ward = Single(t, kSpellEffects.kModifyIncomingDamage);
                ward.m_effectParam = -40;
                ward.m_protected = false;
            }),
        new("ShadowBanshee", 10, "Spells/Banshee Shadow.xml", 3, 0, 85, true,
            t => {
                // The donor hits for 275 and also traps the target; the student's version only hits, 245 to 285.
                var hit = Single(t, kSpellEffects.kDamage);
                t.m_effects.Clear();
                t.m_effects.Add(RandomOf(hit, 245, 255, 265, 275, 285));
            }),
        new("ShadowTrap", 16, "Spells/Shadow Spells/Shadow Trap.xml", 0, 0, 100, false,
            t => Single(t, kSpellEffects.kModifyIncomingDamage).m_effectParam = 25),
        new("Shadowblade", 18, "Spells/Shadow Spells/Shadow Creature Shadow Blade.xml", 0, 0, 100, true,
            t => Single(t, kSpellEffects.kModifyOutgoingDamage).m_effectParam = 30),
        new("Humongofrog", 22, "Spells/Humongofrog - Shadow.xml", 4, 0, 85, true,
            t => SetRandomDamage(t, 265, 280, 295, 310, 325)),
        new("Helephant", 26, "Spells/Helephant Shadow.xml", 4, 0, 85, false,
            t => SetRandomDamage(t, 345, 360, 375, 390, 405)),
        new("Shadowspear", 30, "Spells/Shadowspear.xml", 0, 1, 100, false,
            t => Single(t, kSpellEffects.kModifyOutgoingArmorPiercing).m_effectParam = 10),
        new("Minotaur", 33, "Spells/Minotaur - Shadow.xml", 5, 0, 85, false,
            t => {
                var hits = t.m_effects.Where(e => e.m_effectType == kSpellEffects.kDamage).ToList();
                if (hits.Count != 2) {
                    throw new InvalidOperationException("Minotaur - Shadow no longer has two hits.");
                }

                hits[0].m_effectParam = 50;
                hits[1].m_effectParam = 420;
            }),
        new("StormShark", 38, "Spells/Storm Shark - Shadow.xml", 3, 1, 85, true,
            t => SetRandomDamage(t, 380, 390, 400, 410, 420)),
        new("Triton", 42, "Spells/Triton - Shadow.xml", 6, 0, 80, false,
            t => SetRandomDamage(t, 610, 620, 630, 640, 650)),
        new("FrostGiant", 50, "Spells/Frost Giant - Shadow.xml", 7, 1, 80, false,
            t => {
                Single(t, kSpellEffects.kDamage).m_effectParam = 475;
                var stun = Single(t, kSpellEffects.kStun);
                stun.m_numRounds = 1;
            }),
    ];

    /// <summary>The two spells a Shadow wizard starts with.</summary>
    internal static IEnumerable<ShadowSpell> StartingSpells => Spells.Where(s => s.Level <= 1);

    internal static ShadowSpell ByTemplateId(uint templateId) => Spells.FirstOrDefault(s => s.TemplateId == templateId);

    internal static bool IsCurriculumSpell(uint templateId) => ByTemplateId(templateId) is not null;

    private static SpellEffect Single(SpellTemplate t, kSpellEffects type) {
        var found = t.m_effects.Where(e => e.m_effectType == type).ToList();

        return found.Count == 1
            ? found[0]
            : throw new InvalidOperationException($"{t.m_name} has {found.Count} {type} effects, not one.");
    }

    /// <summary>Sets the values of the spell's one random damage effect (one value per possible roll).</summary>
    private static void SetRandomDamage(SpellTemplate t, params int[] values) {
        var random = t.m_effects.OfType<RandomSpellEffect>().ToList();
        if (random.Count != 1 || random[0].m_effectList.Count == 0
                || random[0].m_effectList.Any(e => e.m_effectType != kSpellEffects.kDamage)) {
            throw new InvalidOperationException($"{t.m_name} does not have exactly one random damage effect.");
        }

        var template = random[0].m_effectList[0];
        random[0].m_effectList.Clear();
        foreach (var value in values) {
            var roll = ShadowSchoolFiles.Clone(template);
            roll.m_effectParam = value;
            random[0].m_effectList.Add(roll);
        }
    }

    /// <summary>A random effect over copies of <paramref name="hit"/> with the given values.</summary>
    private static RandomSpellEffect RandomOf(SpellEffect hit, params int[] values) {
        var random = new RandomSpellEffect {
            m_effectType = kSpellEffects.kInvalidSpellEffect,
            m_effectParam = -1,
            m_disposition = hit.m_disposition,
            m_sDamageType = "",
            m_effectTarget = hit.m_effectTarget,
            m_pipNum = 1,
            m_healModifier = 1,
            m_shadowPactCasterSC = -1,
            m_effectList = [],
        };
        foreach (var value in values) {
            var roll = ShadowSchoolFiles.Clone(hit);
            roll.m_effectParam = value;
            random.m_effectList.Add(roll);
        }

        return random;
    }

}
