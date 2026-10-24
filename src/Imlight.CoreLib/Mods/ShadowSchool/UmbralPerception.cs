/*
 * Umbral Sight, the perception itself: what a Shadow wizard with the sight unlocked (UmbralSight.CanPerceive) notices
 * around them, and how they are told.
 *
 * A small fixed set of bindings, each an anchor in one Wizard City zone (the spot of the NPC or place it belongs to,
 * read from the installed zone data) with a radius:
 *   - walking within the radius gives a short hint, once, that something here is bound;
 *   - standing still within it for FocusDelay is the deliberate look: the wizard is told what they perceive.
 * Stepping well away (1.25 x the radius) or changing zone lets the same binding be noticed again.
 *
 * Everything goes to the one wizard's own session: the hint as on-screen notify text, the perception as notify text
 * plus a line in their chat log (the log is the fallback if the notify text is not shown). No effect, object or
 * message reaches another player, and the zone, its NPCs and the quests are not touched.
 *
 * The words are in the mod's text/en-US.txt ([ShadowSchool] Umbral_*). A binding only says that something is bound,
 * and what kind of thing; it never says who bound it, what it protects or how to break it.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Actor;
using Imcodec.MessageLayer.Generated;
using Imlight.Common;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal static class UmbralPerception {

    /// <summary>What kind of thing a binding is (the first word the wizard reads).</summary>
    internal enum Kind { Binding, Anchor, Residue }

    /// <summary>A binding the sight can find: where (zone, X, Y on the ground plane, radius) and its text key.</summary>
    internal sealed record Binding(string Id, Kind Kind, string Zone, float X, float Y, float Radius, string TextKey);

    // Anchors are the placements in the installed zone data (gamedata.bin), read on the live server 2026-10-23.
    internal static readonly IReadOnlyList<Binding> Bindings = [
        // Bartleby's face (WC-RAV-NPC07): the old roots under Ravenwood. Where the Stage D faculty bindings will start.
        new("RavenwoodRoots", Kind.Binding, "WizardCity/WC_Ravenwood", 45, 1420, 700, "Umbral_RavenwoodRoots"),
        // Merle Ambrose in his house (WC-HUB-NPC01): the wards of the study.
        new("AmbroseWards", Kind.Binding, "WizardCity/Interiors/WC_Headmistress_House", -377, 280, 400, "Umbral_AmbroseWards"),
        // Mortis (WC-RVW-DeathTree): the loose thread of Ambrose's spent enchantment, where it carried the wizard in.
        new("NightsideThread", Kind.Residue, "WizardCity/WC_NightSide", -776, -1197, 600, "Umbral_NightsideThread"),
        // Dworgyn's hall (WC-NIS-NPC01): a binding that was cut.
        new("DeathSchoolCut", Kind.Residue, "WizardCity/Interiors/WC_SchoolDeath", 749, 8, 500, "Umbral_DeathSchoolCut"),
        // Vespera's aisle (the mod's own placement, ShadowSchool-Tutor): a charm to be overlooked.
        new("UnlistedAisle", Kind.Anchor, "WizardCity/Interiors/WC_LibraryBookStacks", 6400, -1104, 400, "Umbral_UnlistedAisle"),
        // Stage D1, the first of the Ravenwood faculty bindings: Dalia Falmea (WC-RAV-NPC04) at her place in the Fire
        // School. It is also the anchor of her conquest encounter (ConquestEncounters.cs); perceiving it does nothing more.
        new("FireHearth", Kind.Binding, "WizardCity/Interiors/WC_SchoolFire", 787, 9, 350, "Umbral_FireHearth"),
        // Stage D2, the second: Lydia Greyrose (WC-RAV-NPC03) at her place in the Frost School (761.52, 20.66, 147.81);
        // the horizontal position is what a binding uses. It is also her encounter's anchor.
        new("FrostClassroom", Kind.Binding, "WizardCity/Interiors/WC_SchoolFrost", 761, 21, 350, "Umbral_FrostClassroom"),
        // Stage D2.2, the third and fourth: Halston Balestrom (WC-RAV-NPC05) at (938.90, -1.20, 202.09) in the Storm
        // School, and Cyrus Drake (WC-RAV-NPC02) at (807.46, 13.78, 100.00) in the Myth School. Each is also its
        // encounter's anchor.
        new("StormClassroom", Kind.Binding, "WizardCity/Interiors/WC_SchoolStorm", 939, -1, 350, "Umbral_StormClassroom"),
        new("MythClassroom", Kind.Binding, "WizardCity/Interiors/WC_SchoolMyth", 807, 14, 350, "Umbral_MythClassroom"),
        // Stage D2.3, the last three. Moolinda Wu (WC-RAV-NPC01) at (760.20, 15.49, 100.00) in the Life School. Malorn
        // Ashthorn (WC-RAV-NPC08) and Arthur Wethersfield (WC-RAV-NPC09) are not in a school: they stand in Ravenwood itself,
        // at (229.29, 6034.79, -30.48) and (558.59, 444.97, 7.18), so their bindings are in WC_Ravenwood. Each is also its
        // encounter's anchor.
        new("LifeClassroom", Kind.Binding, "WizardCity/Interiors/WC_SchoolLife", 760, 15, 350, "Umbral_LifeClassroom"),
        new("DeathInterim", Kind.Binding, "WizardCity/WC_Ravenwood", 229, 6035, 300, "Umbral_DeathInterim"),
        new("BalanceCircle", Kind.Binding, "WizardCity/WC_Ravenwood", 559, 445, 300, "Umbral_BalanceCircle"),
    ];

    internal const string HINT_KEY = "Umbral_Hint";
    internal const string NOTICE_KEY = "Umbral_Notice";
    private const float REARM_FACTOR = 1.25f;

    internal static readonly TimeSpan FocusDelay = TimeSpan.FromSeconds(Math.Clamp(
        ConfigurationManager.GetValue("ShadowSchool.UmbralSightFocusSeconds", 2.0), 0.5, 30));

    // What one wizard has noticed in their current zone. Only that wizard's session actor touches it, apart from
    // CanPerceive, which an unlock (from their quest service) may switch on.
    internal sealed class Watch(ulong charId, string zone, bool canPerceive) {

        internal ulong CharId { get; } = charId;
        internal string Zone { get; } = zone;
        internal volatile bool CanPerceive = canPerceive;
        internal readonly HashSet<string> Hinted = new(StringComparer.Ordinal);
        internal readonly HashSet<string> Perceived = new(StringComparer.Ordinal);

    }

    /// <summary>What to show one wizard: notify text (on screen) and chat lines, as text keys.</summary>
    internal sealed record Shown(IReadOnlyList<string> NotifyKeys, IReadOnlyList<string> ChatKeys) {

        internal static readonly Shown Nothing = new([], []);

        internal bool IsEmpty => NotifyKeys.Count == 0 && ChatKeys.Count == 0;

    }

    private static readonly ConcurrentDictionary<ulong, Watch> s_watches = new();

    internal static Watch WatchOf(ulong charId) => s_watches.TryGetValue(charId, out var watch) ? watch : null;

    internal static IEnumerable<Binding> BindingsIn(string zone)
        => Bindings.Where(b => string.Equals(b.Zone, zone, StringComparison.OrdinalIgnoreCase));

    // ------------------------------------------------------------------ Events (one wizard each)

    /// <summary>A zone attach: a fresh watch for this zone. Nothing is shown on arrival.</summary>
    internal static Watch Attach(Wizard wizard) {
        if (wizard is null) {
            return null;
        }

        var watch = new Watch(wizard.CharId, wizard.Zone ?? "", UmbralSight.CanPerceive(wizard));
        s_watches[wizard.CharId] = watch;

        return watch;
    }

    internal static void Detach(ulong charId) => s_watches.TryRemove(charId, out _);

    /// <summary>The sight was just unlocked: a wizard already in a zone perceives from now on, without relogging.</summary>
    internal static void OnUnlocked(Wizard wizard) {
        if (wizard is not null && WatchOf(wizard.CharId) is { } watch) {
            watch.CanPerceive = UmbralSight.CanPerceive(wizard);
        }
    }

    /// <summary>A move: the hint for each binding the wizard has just come within reach of.</summary>
    internal static Shown Moved(Wizard wizard, float x, float y) {
        if (Current(wizard) is not { } watch) {
            return Shown.Nothing;
        }

        var hints = 0;
        foreach (var binding in BindingsIn(watch.Zone)) {
            var distance = Distance(binding, x, y);
            if (distance > binding.Radius * REARM_FACTOR) {
                watch.Hinted.Remove(binding.Id);
                watch.Perceived.Remove(binding.Id);
            }
            else if (distance <= binding.Radius && watch.Hinted.Add(binding.Id)) {
                hints++;
            }
        }

        return hints == 0 ? Shown.Nothing : new Shown([HINT_KEY], []);
    }

    /// <summary>Standing still: once long enough, what the wizard perceives within reach.</summary>
    internal static Shown Still(Wizard wizard, float x, float y, TimeSpan still) {
        if (still < FocusDelay || Current(wizard) is not { } watch) {
            return Shown.Nothing;
        }

        var lines = new List<string>();
        foreach (var binding in BindingsIn(watch.Zone)) {
            if (Distance(binding, x, y) <= binding.Radius && watch.Perceived.Add(binding.Id)) {
                watch.Hinted.Add(binding.Id);
                lines.Add(binding.TextKey);
            }
        }

        return lines.Count == 0 ? Shown.Nothing : new Shown([NOTICE_KEY], lines);
    }

    /// <summary>Staff (.umbral look): every binding of the wizard's zone and how far it is, perceived or not.</summary>
    internal static IEnumerable<(Binding Binding, float Distance)> Around(Wizard wizard) {
        var location = wizard.Location;

        return BindingsIn(wizard.Zone).Select(b => (b, Distance(b, location.X, location.Y)));
    }

    // The watch, if this wizard can perceive and it belongs to the zone they are in (a stale watch is replaced).
    private static Watch Current(Wizard wizard) {
        if (wizard is null || WatchOf(wizard.CharId) is not { } watch) {
            return null;
        }

        if (!string.Equals(watch.Zone, wizard.Zone ?? "", StringComparison.Ordinal)) {
            watch = Attach(wizard);
        }

        return watch.CanPerceive ? watch : null;
    }

    private static float Distance(Binding binding, float x, float y) {
        var dx = x - binding.X;
        var dy = y - binding.Y;

        return MathF.Sqrt(dx * dx + dy * dy);
    }

    // ------------------------------------------------------------------ Sending (the wizard's own session only)

    internal static void Send(IActorRef session, Shown shown) {
        if (session is null || shown.IsEmpty) {
            return;
        }

        foreach (var key in shown.NotifyKeys) {
            session.Tell(new GAME_5_PROTOCOL.MSG_CLIENTNOTIFYTEXT { NotifyText = Text(key), Type = 0 });
        }

        foreach (var key in shown.ChatKeys) {
            session.Tell(new EXTENDEDBASE_2_PROTOCOL.MSG_SERVERMESSAGE { Message = Text(key), Modal = 0 });
        }
    }

    private static readonly Lazy<IReadOnlyDictionary<string, string>> s_text = new(LoadText);

    /// <summary>The English text of a key from the mod's text file; the key itself if it is missing.</summary>
    internal static string Text(string key)
        => s_text.Value.TryGetValue(key, out var text) ? text : key;

    private static IReadOnlyDictionary<string, string> LoadText() {
        try {
            var folder = ModRegistry.FolderOf(ModRegistry.Enabled.First(m => m is ShadowSchoolMod));

            return ShadowText.Load(folder).Section(ShadowSchoolContent.LANG_NAME)
                .ToDictionary(e => e.Key, e => e.Text.Replace("\\n", "\n"), StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or FormatException) {
            Logger.Error("Shadow School: the Umbral Sight text could not be read; keys are shown instead: {0}", Logger.Args(ex));

            return new Dictionary<string, string>();
        }
    }

}
