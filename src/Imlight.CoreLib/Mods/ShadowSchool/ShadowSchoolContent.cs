/*
 * The Root.wad files of the Shadow School mod, built from the game's own Root.wad at server start:
 *
 *   GUI/CreationClassSelect.gui    the school page gets ShadowButton (below Death, the game's Shadow school icon)
 *                                  and ShadowSigil (the same icon, shown when Shadow is chosen). The client adapter
 *                                  (client/exe_patch.py) makes the page use them.
 *   Locale/<locale>/*.lang         the mod's strings (text/en-US.txt): its own ShadowSchool.lang, and additions to
 *                                  CharCreation.lang and GUI.lang. Every locale gets the English text.
 *   MagicXPConfig.xml              the game's Shadow class has no levels; it gets the midpoint of Fire's and Myth's
 *                                  health at each level, rounded up. Everything else per level is shared by all.
 *   Spells/ShadowSchool/*.xml      the curriculum (ShadowCurriculum.cs), made from the donor spells.
 *   ObjectData/ShadowSchool/*.xml  the tutor, the vendor and the 80 pieces of Shadow equipment (ShadowGear.cs),
 *                                  each made from a game NPC or item.
 *   TemplateManifest.xml           the new spells', NPCs' and items' template ids.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Imcodec.ObjectProperty.TypeCache;
using Imcodec.Wad;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal static class ShadowSchoolContent {

    internal const string LANG_NAME = "ShadowSchool";
    private const string CLASS_SELECT_GUI = "GUI/CreationClassSelect.gui";
    private const string SHADOW_ICON = "GUI/Schools/Art_School_Shadow.dds";

    internal static void Build(Archive root, IDictionary<string, byte[]> files, string modFolder) {
        var text = ShadowText.Load(modFolder);

        files[CLASS_SELECT_GUI] = BuildClassSelectGui(Read(root, CLASS_SELECT_GUI));
        BuildLocales(root, files, text);
        files["MagicXPConfig.xml"] = BuildMagicXpConfig(Read(root, "MagicXPConfig.xml"));

        var manifestFile = Read(root, "TemplateManifest.xml");
        var manifest = ShadowSchoolFiles.ReadBind<TemplateManifest>(manifestFile, "TemplateManifest.xml");
        BuildSpells(root, files, manifest);
        BuildTutor(root, files, manifest);
        BuildVendor(root, files, manifest);
        BuildGear(root, files, manifest);
        files["TemplateManifest.xml"] = ShadowSchoolFiles.WriteBind(manifest, ShadowSchoolFiles.BindFlags(manifestFile));
    }

    /// <summary>Adds a new template to the manifest, refusing an id or file the game already uses.</summary>
    private static void AddTemplate(TemplateManifest manifest, string path, uint id) {
        if (manifest.m_serializedTemplates.Any(t => t.m_id == id
                || string.Equals(t.m_filename.ToString(), path, StringComparison.OrdinalIgnoreCase))) {
            throw new InvalidOperationException($"Template id {id} or file {path} is already used by the game.");
        }

        manifest.m_serializedTemplates.Add(new TemplateLocation { m_filename = path, m_id = id });
    }

    private static byte[] Read(Archive root, string path)
        => root.OpenFile(path)?.ToArray() ?? throw new FileNotFoundException($"Root.wad has no {path}.");

    // ---------------------------------------------------------------- GUI

    private static byte[] BuildClassSelectGui(byte[] file) {
        var gui = Encoding.Latin1.GetString(file);
        if (gui.Contains("<m_sName>ShadowButton</m_sName>")) {
            return file;
        }

        // The Balance button, moved one row (54) below Death, which is the last row.
        var (bStart, bEnd) = ChildBlock(gui, "SelectBalanceButton");
        var button = gui[bStart..bEnd]
            .Replace("<m_sName>SelectBalanceButton</m_sName>", "<m_sName>ShadowButton</m_sName>")
            .Replace("<m_sName>SelectBalanceText</m_sName>", "<m_sName>ShadowButtonText</m_sName>")
            .Replace("&lt;string;GUI_SchoolBalance&gt;", $"&lt;string;{LANG_NAME}_SchoolButton&gt;")
            .Replace("GUI/Schools/Art_School_Balance.dds", SHADOW_ICON);
        button = ReplaceOnce(button, "<m_Window>433,389,734,410</m_Window>", "<m_Window>433,497,734,518</m_Window>");

        // The Balance sigil, hidden until Shadow is chosen; the icon is 64x64, so it is drawn at 160x160 centred.
        var (sStart, sEnd) = ChildBlock(gui, "RightSigil7Sprite");
        var sigil = gui[sStart..sEnd]
            .Replace("<m_sName>RightSigil7Sprite</m_sName>", "<m_sName>ShadowSigil</m_sName>")
            .Replace("GUI/Art/Art_Class_Sigil_Balance.dds", SHADOW_ICON);
        sigil = ReplaceOnce(sigil, "<m_Flags>VISIBLE</m_Flags>", "<m_Flags></m_Flags>");
        sigil = ReplaceOnce(sigil, "<m_Window>446,164,714,432</m_Window>", "<m_Window>500,218,660,378</m_Window>");

        // Insert the later block first so the earlier position stays valid.
        if (bEnd > sEnd) {
            gui = gui.Insert(bEnd, button).Insert(sEnd, sigil);
        }
        else {
            gui = gui.Insert(sEnd, sigil).Insert(bEnd, button);
        }

        return Encoding.Latin1.GetBytes(gui);
    }

    /// <summary>The <m_Children> block (start, end) that holds the window named <paramref name="name"/>.</summary>
    private static (int Start, int End) ChildBlock(string gui, string name) {
        var at = gui.IndexOf($"<m_sName>{name}</m_sName>", StringComparison.Ordinal);
        if (at < 0) {
            throw new InvalidOperationException($"{CLASS_SELECT_GUI} has no {name}.");
        }

        var start = gui.LastIndexOf("<m_Children>", at, StringComparison.Ordinal);
        var depth = 0;
        foreach (Match m in Regex.Matches(gui[start..], "<m_Children>|</m_Children>")) {
            depth += m.Value == "<m_Children>" ? 1 : -1;
            if (depth == 0) {
                return (start, start + m.Index + m.Length);
            }
        }

        throw new InvalidOperationException($"{CLASS_SELECT_GUI}: the block of {name} does not end.");
    }

    private static string ReplaceOnce(string s, string from, string to) {
        var at = s.IndexOf(from, StringComparison.Ordinal);
        if (at < 0 || s.IndexOf(from, at + from.Length, StringComparison.Ordinal) >= 0) {
            throw new InvalidOperationException($"Expected exactly one {from}.");
        }

        return s[..at] + to + s[(at + from.Length)..];
    }

    // ---------------------------------------------------------------- Text

    private static void BuildLocales(Archive root, IDictionary<string, byte[]> files, ShadowText text) {
        var locales = root.Files.Keys
            .Where(k => k.StartsWith("Locale/", StringComparison.Ordinal) && k.EndsWith("/GUI.lang", StringComparison.Ordinal))
            .Select(k => k.Split('/')[1])
            .ToList();

        foreach (var locale in locales) {
            files[$"Locale/{locale}/{LANG_NAME}.lang"] = ShadowSchoolFiles.WriteLang(LANG_NAME, text.Section(LANG_NAME));
            foreach (var section in text.Sections.Where(s => s != LANG_NAME)) {
                var path = $"Locale/{locale}/{section}.lang";
                if (root.ContainsFile(path)) {
                    files[path] = ShadowSchoolFiles.ExtendLang(Read(root, path), text.Section(section));
                }
            }
        }
    }

    // ---------------------------------------------------------------- Health

    private static byte[] BuildMagicXpConfig(byte[] file) {
        var config = ShadowSchoolFiles.ReadBind<MagicXPConfig>(file, "MagicXPConfig.xml");
        var fire = config.m_classInfo.Single(c => c.m_className == "Fire").m_classLevelInfo;
        var myth = config.m_classInfo.Single(c => c.m_className == "Myth").m_classLevelInfo;
        var shadow = config.m_classInfo.Single(c => c.m_className == "Shadow");
        if (fire.Count != myth.Count) {
            throw new InvalidOperationException("Fire and Myth have different level counts.");
        }

        shadow.m_classLevelInfo = [];
        for (var i = 0; i < fire.Count; i++) {
            var level = ShadowSchoolFiles.Clone(fire[i]);
            level.m_hitpoints = ShadowHealth(fire[i].m_hitpoints, myth[i].m_hitpoints);
            shadow.m_classLevelInfo.Add(level);
        }

        return ShadowSchoolFiles.WriteBind(config, ShadowSchoolFiles.BindFlags(file));
    }

    /// <summary>The midpoint of Fire's and Myth's health, rounded up.</summary>
    internal static int ShadowHealth(int fire, int myth) => (fire + myth + 1) / 2;

    // ---------------------------------------------------------------- Spells

    private static void BuildSpells(Archive root, IDictionary<string, byte[]> files, TemplateManifest manifest) {
        foreach (var spell in ShadowCurriculum.Spells) {
            var donorFile = Read(root, spell.Donor);
            var template = ShadowSchoolFiles.ReadBind<SpellTemplate>(donorFile, spell.Donor);
            if (template.m_sMagicSchoolName.ToString() != "Shadow") {
                throw new InvalidOperationException($"The donor {spell.Donor} is not a Shadow spell.");
            }

            template.m_name = spell.Name;
            template.m_displayName = $"{LANG_NAME}_Spell_{spell.Key}";
            if (spell.OwnDescription) {
                template.m_description = $"{LANG_NAME}_Desc_{spell.Key}";
            }

            template.m_accuracy = spell.Accuracy;
            template.m_levelRestriction = 0;
            template.m_Treasure = false;
            template.m_spellRank ??= new SpellRank();
            template.m_spellRank.m_spellRank = spell.Pips;
            template.m_spellRank.m_shadowPips = spell.ShadowPips;
            spell.Effects(template);

            AddTemplate(manifest, spell.FilePath, spell.TemplateId);
            files[spell.FilePath] = ShadowSchoolFiles.WriteBind(template, ShadowSchoolFiles.BindFlags(donorFile));
        }
    }

    // ---------------------------------------------------------------- The Unlisted Tutor

    /// <summary>
    /// The tutor is a new NPC with the look of an existing one (ShadowSchoolMod.TUTOR_MODEL_DONOR): the donor's
    /// model, animations and portrait, with the tutor's own id, name, quest persona and no spell list of its own (the
    /// server's list, spiraldb/NpcSpellInventory, is what the tutor teaches).
    /// </summary>
    private static void BuildTutor(Archive root, IDictionary<string, byte[]> files, TemplateManifest manifest) {
        var donorPath = manifest.m_serializedTemplates.Single(t => t.m_id == ShadowSchoolMod.TUTOR_MODEL_DONOR).m_filename.ToString();
        var donorFile = Read(root, donorPath);
        var tutor = ShadowSchoolFiles.ReadBind<WizGameObjectTemplate>(donorFile, donorPath);

        tutor.m_templateID = ShadowSchoolMod.TUTOR_TEMPLATE_ID;
        tutor.m_objectName = ShadowSchoolMod.TUTOR_OBJECT_NAME;
        tutor.m_displayName = $"{LANG_NAME}_TutorName";
        foreach (var questing in tutor.m_behaviors.OfType<WizardQuestingBehaviorTemplate>()) {
            questing.m_personaName = ShadowSchoolMod.TUTOR_PERSONA;
            questing.m_questList = [];
        }

        foreach (var training in tutor.m_behaviors.OfType<WizTrainingBehaviorTemplate>()) {
            training.m_spells = [];
        }

        AddTemplate(manifest, ShadowSchoolMod.TUTOR_FILE, ShadowSchoolMod.TUTOR_TEMPLATE_ID);
        files[ShadowSchoolMod.TUTOR_FILE] = ShadowSchoolFiles.WriteBind(tutor, ShadowSchoolFiles.BindFlags(donorFile));
    }

    // ---------------------------------------------------------------- The Shadow Vendor

    /// <summary>
    /// The vendor is a second new NPC: the tutor's Arcanum look with the quest list of an NPC that has none, and the
    /// shopping behavior of a game shopkeeper (its shop window, with the mod's own title). Its stock is the JSON list
    /// in the mod's spiraldb/NpcInventory, so a piece the mod does not sell is not in its shop.
    /// </summary>
    private static void BuildVendor(Archive root, IDictionary<string, byte[]> files, TemplateManifest manifest) {
        var donorPath = manifest.m_serializedTemplates.Single(t => t.m_id == ShadowSchoolMod.TUTOR_MODEL_DONOR).m_filename.ToString();
        var donorFile = Read(root, donorPath);
        var vendor = ShadowSchoolFiles.ReadBind<WizGameObjectTemplate>(donorFile, donorPath);

        vendor.m_templateID = ShadowSchoolMod.VENDOR_TEMPLATE_ID;
        vendor.m_objectName = ShadowSchoolMod.VENDOR_OBJECT_NAME;
        vendor.m_displayName = $"{LANG_NAME}_VendorName";
        vendor.m_behaviors.RemoveAll(b => b is WizardQuestingBehaviorTemplate or WizTrainingBehaviorTemplate);

        if (!vendor.m_behaviors.Any(b => b is NPCBehaviorTemplate)) {
            throw new InvalidOperationException($"The vendor donor {donorPath} is not an NPC.");
        }

        var shopPath = manifest.m_serializedTemplates.Single(t => t.m_id == ShadowSchoolMod.VENDOR_SHOP_DONOR).m_filename.ToString();
        var shopFile = Read(root, shopPath);
        var shopDonor = ShadowSchoolFiles.ReadBind<WizGameObjectTemplate>(shopFile, shopPath);
        var shop = shopDonor.m_behaviors.OfType<WizShoppingBehaviorTemplate>().FirstOrDefault()
            ?? throw new InvalidOperationException($"The shop donor {shopPath} has no shopping behavior.");
        var borrowed = ShadowSchoolFiles.Clone(shop);
        borrowed.m_shopTitle = $"{LANG_NAME}_VendorShopTitle";
        // An equipment shop: the donor's own window, with neither the test nor the housing flags set.
        borrowed.m_furnitureShop = 0;
        borrowed.m_CSRTestShop = false;
        vendor.m_behaviors.Add(borrowed);

        AddTemplate(manifest, ShadowSchoolMod.VENDOR_FILE, ShadowSchoolMod.VENDOR_TEMPLATE_ID);
        files[ShadowSchoolMod.VENDOR_FILE] = ShadowSchoolFiles.WriteBind(vendor, ShadowSchoolFiles.BindFlags(donorFile));
    }

    // ---------------------------------------------------------------- Equipment

    /// <summary>
    /// The 80 pieces of Shadow equipment (ShadowGear.cs), each a Death item of the game with its school, level and
    /// stat names changed. The donor itself is never touched, so every other school's gear stays as it was.
    /// </summary>
    private static void BuildGear(Archive root, IDictionary<string, byte[]> files, TemplateManifest manifest) {
        var canonical = CanonicalEffects(root);
        foreach (var piece in ShadowGear.Pieces) {
            var donorFile = Read(root, piece.Donor);
            var item = ShadowSchoolFiles.ReadBind<WizItemTemplate>(donorFile, piece.Donor);
            // A deck carries no school stats (only how many cards and copies it holds), so its donor may be the
            // game's school-neutral deck at that level; every other piece is made from a Death item.
            if (piece.Slot != ShadowGear.DECK_SLOT && (string) item.m_school != ShadowGear.DONOR_SCHOOL) {
                throw new InvalidOperationException($"The donor {piece.Donor} is not a {ShadowGear.DONOR_SCHOOL} item.");
            }

            Retarget(item, piece, canonical);
            AddTemplate(manifest, piece.FilePath, piece.TemplateId);
            files[piece.FilePath] = ShadowSchoolFiles.WriteBind(item, ShadowSchoolFiles.BindFlags(donorFile));
        }
    }

    /// <summary>The names of the game's canonical stat effects, which is what a stat effect has to be called to work.</summary>
    private static HashSet<string> CanonicalEffects(Archive root) {
        const string path = "GameEffectData/CanonicalStatEffects.xml";
        var list = ShadowSchoolFiles.ReadBind<GameEffectTemplateList>(Read(root, path), path);
        var names = (list.m_effectTemplates ?? []).OfType<WizStatisticEffectTemplate>()
            .Select(t => (string) t.m_effectName)
            .Where(n => n is { Length: > 0 })
            .ToHashSet(StringComparer.Ordinal);
        if (names.Count == 0) {
            throw new InvalidOperationException($"{path} names no stat effects.");
        }

        return names;
    }

    /// <summary>Makes a Death item into the Shadow piece: its own name, id and file, worn by a Shadow wizard at its tier.</summary>
    private static void Retarget(WizItemTemplate item, ShadowGearPiece piece, HashSet<string> canonical) {
        item.m_templateID = piece.TemplateId;
        item.m_objectName = piece.ObjectName;
        item.m_displayName = piece.DisplayNameKey;
        item.m_school = ShadowGear.SCHOOL;

        if (item.m_equipRequirements is not RequirementList requirements) {
            throw new InvalidOperationException($"{piece.Donor} has no requirement list to change.");
        }

        var schoolRequirement = false;
        foreach (var requirement in requirements.m_requirements ?? []) {
            switch (requirement) {
                case ReqMagicLevel level:
                    level.m_numericValue = piece.Level;
                    level.m_magicSchool = ShadowGear.SCHOOL;
                    break;
                case ReqSchoolOfFocus focus:
                    focus.m_magicSchool = ShadowGear.SCHOOL;
                    schoolRequirement = true;
                    break;
            }
        }

        if (!schoolRequirement) {
            requirements.m_requirements ??= [];
            requirements.m_requirements.Add(new ReqSchoolOfFocus { m_magicSchool = ShadowGear.SCHOOL });
        }

        // A Death bonus becomes the game's own Shadow bonus; one with no Shadow version is left off, not mislabelled.
        item.m_equipEffects?.RemoveAll(effect => effect is StatisticEffectInfo stat && !RetargetStat(stat, canonical));

        // A Shadow deck counts Shadow spells against its school limit, as a Death deck counts Death spells.
        foreach (var deck in (item.m_behaviors ?? []).OfType<DeckBehaviorTemplate>()) {
            deck.m_primarySchoolName = ShadowGear.SCHOOL;
        }
    }

    /// <summary>The schools a stat effect can name: CanonicalFireAccuracy, CanonicalDeathDamage and so on.</summary>
    private static readonly string[] s_schoolTokens = ["Fire", "Ice", "Storm", "Myth", "Life", "Death", "Balance"];

    private static bool RetargetStat(StatisticEffectInfo stat, HashSet<string> canonical) {
        var name = (string) stat.m_effectName;
        if (name is not { Length: > 0 }) {
            return true;
        }

        // A donor may carry a second school's bonus as well (a crafted Death hat with Fire accuracy on it): every
        // school it names becomes Shadow, so no piece ever boosts a school its owner cannot use.
        var token = s_schoolTokens.FirstOrDefault(s => name.Contains(s, StringComparison.Ordinal));
        if (token is null) {
            return true;
        }

        var shadow = name.Replace(token, ShadowGear.SCHOOL, StringComparison.Ordinal);
        if (!canonical.Contains(shadow)) {
            return false;
        }

        stat.m_effectName = shadow;

        return true;
    }

}
