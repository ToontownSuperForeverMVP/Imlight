using Imcodec.Cryptography;
using Imlight.CoreLib.Game.Quests;
using Imlight.CoreLib.Game.Combat;
using Imlight.CoreLib.Game.Services;
using Imlight.CoreLib.Game.World;
using Imlight.CoreLib.Mods.ShadowSchool;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.Shared.Packets;
using Imlight.CoreLib.Game.Zone.Core;
using Imlight.CoreLib.WizardData.Models.Player;
using Xunit;

namespace Imlight.CoreLib.Tests.Mods.ShadowSchool;

/// <summary>
/// Stage D2.3: what is particular to Moolinda Wu (Life), Malorn Ashthorn (Death) and Arthur Wethersfield (Balance): their
/// scripted heals, drains and wards (the creature AI was checked on a QA server and cannot be trusted with them), the two
/// that stand in Ravenwood itself, and the seven-instructor campaign. The generic encounter rules for all five later
/// instructors are in ConquestCampaignTests.StormMyth.cs.
/// </summary>
public sealed partial class ConquestCampaignTests {

    private static readonly string[] s_lastThree = ["Wu", "Ashthorn", "Wethersfield"];

    public static IEnumerable<object[]> LastThreeIds => s_lastThree.Select(s => new object[] { s });

    // The triggers and steps the engine's BossCheats documents (CombatResolver.BossCheats.cs, CombatResolver.CheatSteps.cs) and
    // that these three use. Anything else in their files is a mistake.
    private static readonly string[] s_cheatTriggers = ["BattleStart", "RoundStart", "RoundEnd", "HealthBelow", "PlayerCasts", "FromRound"];
    private static readonly string[] s_cheatSteps = ["Cast", "Boost", "AddCharm", "StealPips"];
    private static readonly string[] s_cheatTargets = ["Self", "FirstEnemy", "Caster"];

    private static List<BossCheats.BossCheat> CheatsOf(string id) => LoadProfile(SpecOf(id)).Cheats!.Added;

    // Every spell an Added cheat casts (Cast/Casts, and Cast steps in Actions).
    private static IEnumerable<BossCheats.CheatCast> CastsOf(BossCheats.BossCheat cheat)
        => cheat.CastList(all: true).Concat(cheat.Actions.Where(a => a.Is("Cast")).Select(a => new BossCheats.CheatCast { Spell = a.Spell, SpellID = a.SpellID }));

    // ------------------------------------------------------------------ The scripts

    [Theory]
    [MemberData(nameof(LastThreeIds))]
    public void EveryScriptedMoveUsesAKnownTriggerStepTargetAndAVerifiedSpellOfTheirSchool(string id) {
        var profile = LoadProfile(SpecOf(id));
        var cheats = CheatsOf(id);

        Assert.NotEmpty(cheats);
        Assert.Empty(profile.Cheats!.Changed);
        foreach (var cheat in cheats) {
            Assert.Contains(cheat.When, s_cheatTriggers);
            Assert.False(string.IsNullOrWhiteSpace(cheat.Notes), "every move says what it is for");
            Assert.True(cheat.Target is "" or "Self" or "FirstEnemy" or "Caster", cheat.Target);
            Assert.All(cheat.Actions, a => {
                Assert.Contains(a.Do, s_cheatSteps);
                Assert.True(a.Target is "" or "Self" or "FirstEnemy" or "Caster", a.Target);
            });
            Assert.All(CastsOf(cheat), c => {
                Assert.True(s_verifiedSpells.TryGetValue(c.SpellID, out var spell), $"unverified cheat spell {c.SpellID}");
                Assert.Equal(spell.Name, c.Spell);
                Assert.Equal(SpecOf(id).School, spell.School);
                Assert.Contains(c.Target, s_cheatTargets);
            });
            if (cheat.Is("HealthBelow")) {
                Assert.InRange(cheat.Percent, 1, 99);
                Assert.Equal(1, cheat.MaxTimes);
            }
        }

        Assert.NotEmpty(profile.Deck!);
    }

    [Theory]
    [MemberData(nameof(LastThreeIds))]
    public void TheFightIsBoundedByAGrowingBoostAndEveryRestorativeMoveHasALimit(string id) {
        var cheats = CheatsOf(id);

        var enrage = Assert.Single(cheats, c => c.Is("FromRound"));
        Assert.InRange(enrage.Round, 8, 10);
        Assert.InRange(enrage.Every, 1, 2);
        var boost = Assert.Single(enrage.Actions);
        Assert.Equal("Boost", boost.Do);
        Assert.Equal("Damage", boost.Kind);
        Assert.InRange(boost.Amount, 10, 15);

        // Anything that heals, drains, shields or steals can only happen a limited number of times.
        foreach (var cheat in cheats.Where(c => !c.Is("FromRound") && !c.Is("BattleStart"))) {
            Assert.True(cheat.MaxTimes > 0, $"{cheat.When} {cheat.Notes}");
        }
    }

    [Fact]
    public void WuCanOnlyMendTwiceAndGuardsHerselfWithALimitedNumberOfWards() {
        var profile = LoadProfile(SpecOf("Wu"));
        var cheats = CheatsOf("Wu");
        const uint pixie = 74836240, armor = 2071077573;

        // Pixie: kHeal 400 on herself, once under 65% and once under 35% health. Nothing else mends her.
        var mends = cheats.Where(c => CastsOf(c).Any(x => x.SpellID == pixie)).ToList();
        Assert.Equal(2, mends.Count);
        Assert.All(mends, m => {
            Assert.True(m.Is("HealthBelow"));
            Assert.Equal(1, m.MaxTimes);
        });
        Assert.Equal([65, 35], mends.Select(m => m.Percent).OrderByDescending(p => p));
        const int pixieHeal = 400;
        var mostHealed = mends.Count * pixieHeal;
        Assert.True(mostHealed <= profile.Health * 0.65, "she can restore under two thirds of her health");

        // Spirit Armor (kAbsorbDamage 400): at the start, with the last mend, and in rounds 4 and 8: four wards at most.
        var wards = cheats.Sum(c => CastsOf(c).Count(x => x.SpellID == armor) * (c.Is("RoundEnd") ? c.MaxTimes : 1));
        Assert.Equal(4, wards);
        var rounds = Assert.Single(cheats, c => c.Is("RoundEnd"));
        Assert.Equal((4, 4, 2), (rounds.Round, rounds.Every, rounds.MaxTimes));

        // No heal or ward is in her deck, where the creature AI would cast it at the wrong circle or not at all.
        Assert.Empty(profile.Deck!.Select(c => c.SpellID).Intersect([pixie, armor]));
        Assert.Contains(profile.Deck!, c => c.Copies == 0);
        Assert.Equal("Life", profile.School);
        Assert.Equal(0f, RowValue(profile.PierceBySchool, "Shadow"));
    }

    [Fact]
    public void AshthornDrainsOnlyThroughScriptedCastsAndTheMostHeCanTakeBackIsBounded() {
        var profile = LoadProfile(SpecOf("Ashthorn"));
        var cheats = CheatsOf("Ashthorn");
        const uint vampire = 524731385, wraith = 1788506505, curse = 75022979;

        // Drain is not in the deck: the creature AI never chooses a kStealHealth card (checked on a QA server), so a deck
        // drain would not be cast. It is a free cast of the real spell, whose heal the engine gives the caster.
        Assert.Empty(profile.Deck!.Select(c => c.SpellID).Intersect([vampire, wraith, 82556199u]));
        var drain = Assert.Single(cheats, c => CastsOf(c).Any(x => x.SpellID == vampire));
        Assert.Equal((2, 3, 4), (drain.Round, drain.Every, drain.MaxTimes));
        Assert.True(drain.Is("RoundStart"));
        Assert.Equal("FirstEnemy", drain.Target);
        var last = Assert.Single(cheats, c => CastsOf(c).Any(x => x.SpellID == wraith));
        Assert.True(last.Is("HealthBelow"));
        Assert.Equal((40, 1), (last.Percent, last.MaxTimes));
        Assert.Contains(cheats, c => c.Is("BattleStart") && CastsOf(c).Any(x => x.SpellID == curse));

        // Vampire kStealHealth 335 and Wraith kStealHealth 500, each healing half of what it deals (healModifier 0.5):
        // at most 4 x 167.5 + 250 = 920, which is under 80% of his 1200 health, so no drain can undo the fight.
        var mostBack = (4 * 335 + 500) * 0.5;
        Assert.Equal(920, mostBack);
        Assert.True(mostBack <= profile.Health * 0.8);

        // Pressure on the wizard's resources: a pip taken for a costly card, three times at most.
        var steal = Assert.Single(cheats, c => c.Is("PlayerCasts"));
        Assert.Equal(4, steal.CostMin);
        Assert.Equal(3, steal.MaxTimes);
        var step = Assert.Single(steal.Actions);
        Assert.Equal(("StealPips", 1, "Normal"), (step.Do, step.Amount, step.Kind));
        Assert.Equal("Malorn Ashthorn", profile.Name);
        Assert.Equal(1475616ul, profile.BaseTemplateID);
        Assert.Contains(profile.Deck!, c => s_verifiedSpells[c.SpellID].Name == "Plague");
        Assert.Contains(profile.Deck!, c => s_verifiedSpells[c.SpellID].Name == "Feint");
    }

    [Fact]
    public void WethersfieldWeakensAdaptsAndNeverHealsAndHasNoShieldInHisDeck() {
        var profile = LoadProfile(SpecOf("Wethersfield"));
        var cheats = CheatsOf("Wethersfield");

        Assert.Equal("Arthur Wethersfield", profile.Name);
        Assert.Equal(1475617ul, profile.BaseTemplateID);
        Assert.Equal(10f, RowValue(profile.PierceBySchool, "Shadow"));
        Assert.True(profile.Health >= LoadProfile(SpecOf("Wu")).Health && profile.Level > LoadProfile(SpecOf("Drake")).Level,
            "the last faculty fight is the strongest");
        Assert.Equal(1500, profile.Health);

        // Weakness and a Muzzle (damage and accuracy down), a blade, a shield that answers a wizard who hits him, and one
        // re-balance against the wizard's own school: all of it scripted, none of it a heal.
        var muzzle = Assert.Single(cheats, c => CastsOf(c).Any(x => x.SpellID == 1897110899));
        Assert.Equal((3, 4, 2), (muzzle.Round, muzzle.Every, muzzle.MaxTimes));
        Assert.Contains(cheats, c => c.Is("BattleStart") && CastsOf(c).Any(x => x.SpellID == 514206205));
        var adapt = Assert.Single(cheats, c => c.Is("PlayerCasts"));
        Assert.True(adapt.OnBoss);
        Assert.Equal("Damage", adapt.Category);
        Assert.Equal(3, adapt.MaxTimes);
        Assert.Equal(("AddCharm", "Shield", 25), (adapt.Actions[0].Do, adapt.Actions[0].Kind, adapt.Actions[0].Amount));
        var rebalance = Assert.Single(cheats, c => c.Is("HealthBelow"));
        Assert.Equal(50, rebalance.Percent);
        Assert.Contains(rebalance.Actions, a => a.Kind == "Shield" && a.School == "Shadow");
        Assert.DoesNotContain(cheats.SelectMany(CastsOf), c => c.SpellID is 74836240 or 2071077573);

        // The deck: Balance attacks of three ranks, mixed-school pressure (Spectral Blast), and two debuffs the AI can aim.
        var names = profile.Deck!.Select(c => s_verifiedSpells[c.SpellID].Name).ToArray();
        Assert.Equal(["Scorpion", "Locust Swarm", "Spectral Blast", "Weakness", "Hex"], names);
        Assert.DoesNotContain(names, n => n.Contains("Shield") || n.Contains("blade", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(LastThreeIds))]
    public void TheDeckHoldsNothingTheCreatureAiMisplacesAndAlwaysHasAnAttackItCanCast(string id) {
        var profile = LoadProfile(SpecOf(id));
        var scripted = CheatsOf(id).SelectMany(CastsOf).Select(c => c.SpellID).ToHashSet();

        // No spell is both in the deck and in a script, and the deck never holds a friendly ward, blade, heal or a drain.
        Assert.Empty(profile.Deck!.Select(c => c.SpellID).Where(scripted.Contains));
        Assert.DoesNotContain(profile.Deck!, c => s_verifiedSpells[c.SpellID].Name is "Pixie" or "Spirit Armor" or "Balanceblade" or "Vampire" or "Wraith");

        // Unlimited attacks the AI's damage filter accepts, so it never has to pass: rank 2 or more at the cheapest.
        var attacks = profile.Deck!.Where(c => c.Copies == 0).ToList();
        Assert.True(attacks.Count >= 2, "two unlimited attacks");
        Assert.All(attacks, c => Assert.InRange(s_verifiedSpells[c.SpellID].Rank, 2, 3));
        Assert.Equal(attacks.Count, attacks.Select(c => s_verifiedSpells[c.SpellID].Rank).Distinct().Count());
    }

    [Fact]
    public void TheThreeProfilesRiseInStrengthAndStayInsideTheBandOfTheFirstFour() {
        var all = ConquestEncounters.Encounters.OrderBy(e => e.Order).Select(e => LoadFalmeaDeckProfile(e.CustomId)).ToList();

        Assert.Equal([10, 11, 12, 13, 14, 15, 16], all.Select(p => p.Level!.Value));
        Assert.Equal(["Fire", "Ice", "Storm", "Myth", "Life", "Death", "Balance"], all.Select(p => p.School));
        Assert.All(all, p => Assert.InRange(p.Health!.Value, 900, 1500));
        Assert.All(all, p => Assert.Empty(p.Drops!));
        Assert.All(all, p => Assert.Equal(0, p.XP));
        Assert.All(all, p => Assert.Equal((0, 0), (p.GoldMin, p.GoldMax)));
    }

    private static BossOverrides.BossOverride LoadFalmeaDeckProfile(int custom) {
        InitializeIsolatedTestConfiguration();

        return BossOverrides.Parse(File.ReadAllText(Path.Combine(ModFolder(), "spiraldb", "Customs", $"{custom}.json")), Customs.Key(custom))!;
    }

    // ------------------------------------------------------------------ Ravenwood itself

    [Fact]
    public void AshthornAndWethersfieldStandInRavenwoodSoTheirAnchorsAreThere() {
        foreach (var id in new[] { "Ashthorn", "Wethersfield" }) {
            var encounter = EncounterOf(SpecOf(id));
            var binding = UmbralPerception.Bindings.Single(b => b.Id == encounter.AnchorBinding);

            Assert.Equal(Ravenwood, binding.Zone);
            Assert.Equal(Ravenwood, encounter.Zone);
            Assert.Equal(Ravenwood, encounter.ExitZone);
            Assert.InRange(binding.Radius, 200, 400);
        }

        // gamedata.bin: NPC08 at (229.29, 6034.79), NPC09 at (558.59, 444.97). The anchors are where the trainers are.
        Assert.True(ConquestEncounters.AtAnchor(ConquestEncounters.ById("Ashthorn")!, 229.29f, 6034.79f));
        Assert.True(ConquestEncounters.AtAnchor(ConquestEncounters.ById("Wethersfield")!, 558.59f, 444.97f));
        Assert.False(ConquestEncounters.AtAnchor(ConquestEncounters.ById("Ashthorn")!, 558.59f, 444.97f));
        Assert.False(ConquestEncounters.AtAnchor(ConquestEncounters.ById("Wethersfield")!, 229.29f, 6034.79f));
    }

    [Fact]
    public void NoArrivalOrExitPointIsInsideAnAnchorOrAnotherBindingSoLeavingNeverRestartsAnEncounter() {
        foreach (var encounter in ConquestEncounters.Encounters.Where(e => e.Zone == Ravenwood)) {
            // The point the wizard arrives at in the copy and the point the exit leaves to are the same game location.
            Assert.Equal(encounter.ArrivalLoc, encounter.ExitLoc);
            var at = encounter.ExitLoc.Split(',').Select(float.Parse).ToArray();
            foreach (var binding in UmbralPerception.BindingsIn(Ravenwood)) {
                var distance = MathF.Sqrt(MathF.Pow(at[0] - binding.X, 2) + MathF.Pow(at[1] - binding.Y, 2));
                Assert.True(distance > binding.Radius * 1.25f, $"{encounter.Id} exit is within reach of {binding.Id}");
            }
        }

        // The Ravenwood anchors do not overlap the older Ravenwood binding or each other.
        var roots = UmbralPerception.Bindings.Single(b => b.Id == "RavenwoodRoots");
        var death = UmbralPerception.Bindings.Single(b => b.Id == "DeathInterim");
        var balance = UmbralPerception.Bindings.Single(b => b.Id == "BalanceCircle");
        foreach (var (a, b) in new[] { (roots, death), (roots, balance), (death, balance) }) {
            Assert.True(MathF.Sqrt(MathF.Pow(a.X - b.X, 2) + MathF.Pow(a.Y - b.Y, 2)) > a.Radius + b.Radius, $"{a.Id} and {b.Id}");
        }
    }

    [Theory]
    [InlineData("Ashthorn", "Wethersfield")]
    [InlineData("Wethersfield", "Ashthorn")]
    public void ACopyOfTheOtherRavenwoodEncounterIsNeverTakenForThisOne(string mine, string other) {
        var spec = SpecOf(mine);
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = ReadyBefore(spec, mod);
        EnterEncounter(world, mod, wizard, spec);
        var otherEncounter = ConquestEncounters.ById(other)!;

        // The wizard is somehow attached to the other encounter's copy while this one's session is open: that copy is closed
        // (they are sent to the exit), the session is gone, and nothing is credited either way.
        world.Noted[wizard.CharId] = (Ravenwood, otherEncounter.Variant);
        Arrive(mod, world, wizard, Ravenwood);
        Assert.Equal((wizard.CharId, other, TimeSpan.Zero), world.Left[^1]);
        Assert.Null(mod.Encounters.SessionOf(wizard.CharId));

        var events = world.Events.Count;
        BattleOutcomeEvents.Report(Win(world, wizard, SpecOf(other)), [mod]);
        BattleOutcomeEvents.Report(Win(world, wizard, spec), [mod]);
        Assert.Equal(events, world.Events.Count);
    }

    [Fact]
    public void AVariantCopyOfTheZoneYouAreInIsLeftByAReloadButEntryAndOtherZonesAreNot() {
        var copy = new ZoneVariants.Entry(Ravenwood, "ShadowConquest-Ashthorn");
        ZONE_102_PROTOCOL.MSG_ZONETRANSFER Message(string zone, string variant = "", bool force = false)
            => new() { DestinationZone = zone, Variant = variant, ForceReload = force, SendToClient = true };

        // The exit of a Ravenwood copy to the plain Ravenwood: a reload, or the wizard would only be moved inside the copy.
        Assert.True(ZoneService.LeavesVariantCopy(Message(Ravenwood), Ravenwood, copy));
        Assert.False(ZoneService.LeavesVariantCopy(Message(Ravenwood, force: true), Ravenwood, copy));
        // Entering a copy, going to another zone, or being in no copy at all is not leaving one.
        Assert.False(ZoneService.LeavesVariantCopy(Message(Ravenwood, "ShadowConquest-Wethersfield"), Ravenwood, copy));
        Assert.False(ZoneService.LeavesVariantCopy(Message("WizardCity/Interiors/WC_SchoolLife"), Ravenwood, copy));
        Assert.False(ZoneService.LeavesVariantCopy(Message(Ravenwood), Ravenwood, null));
        Assert.False(ZoneService.LeavesVariantCopy(Message(Ravenwood), Ravenwood, new ZoneVariants.Entry(Ravenwood, "")));
        // The note belongs to another zone: the wizard is not in a copy of this one.
        Assert.False(ZoneService.LeavesVariantCopy(Message(Ravenwood), Ravenwood, new ZoneVariants.Entry("WizardCity/Interiors/WC_SchoolLife", "ShadowConquest-Wu")));
        Assert.False(ZoneService.LeavesVariantCopy(Message(Ravenwood), null, copy));
    }

    // ------------------------------------------------------------------ The campaign

    [Fact]
    public void TheSevenAreInOrderSoEachNeedsEveryEarlierChapterAndNothingElseUnlocksIt() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);

        foreach (var encounter in ConquestEncounters.Encounters.OrderBy(e => e.Order)) {
            // Each is locked until all the earlier ones are complete, and open the moment they are.
            Assert.True(ConquestEncounters.MeetsPrerequisites(wizard, encounter), encounter.Id);
            foreach (var later in ConquestEncounters.Encounters.Where(e => e.Order > encounter.Order)) {
                Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, later), $"{later.Id} before {encounter.Id}");
            }

            CompleteChapterByPlay(wizard, mod, encounter);
        }

        Assert.True(ConquestEncounters.AllInstructorsComplete(wizard));
    }

    [Theory]
    [MemberData(nameof(LastThreeIds))]
    public void AFalseEarlierChapterCannotUnlockALaterInstructor(string id) {
        InitializeIsolatedTestConfiguration();
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        var wizard = Ready(MagicSchool.Shadow);

        // Every earlier instructor complete except the one directly before: the gap holds, whichever half is missing.
        var before = ConquestEncounters.Encounters.Where(e => e.Order < encounter.Order).OrderBy(e => e.Order).ToList();
        foreach (var earlier in before) {
            wizard.QuestBehavior.SetRegistryValue(earlier.StateKey, (ulong) ConquestStage.Complete);
            wizard.QuestBehavior.SetQuestRegistryValue(earlier.QuestName, "Complete", 1);
        }

        Assert.True(ConquestEncounters.MeetsPrerequisites(wizard, encounter));
        var gap = before[^1];
        wizard.QuestBehavior.SetRegistryValue(gap.StateKey, (ulong) ConquestStage.Defeated);
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, encounter));
        wizard.QuestBehavior.SetRegistryValue(gap.StateKey, (ulong) ConquestStage.Complete);
        wizard.QuestBehavior.RemoveQuestRegistryValue(gap.QuestName, "Complete");
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, encounter));
        var first = before[0];
        wizard.QuestBehavior.SetQuestRegistryValue(gap.QuestName, "Complete", 1);
        wizard.QuestBehavior.RemoveRegistryValue(first.StateKey);
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, encounter));
    }

    [Fact]
    public void TheSeventhVictoryRecordsAllSevenAndStartsNothingMore() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var spec = SpecOf("Wethersfield");
        var encounter = EncounterOf(spec);
        var wizard = ReadyBefore(spec, mod);
        var campaign = CampaignBefore(wizard, spec, mod);
        var chapter = Accept(wizard, LoadQuest(encounter.QuestName), mod);
        EnterEncounter(world, mod, wizard, spec);
        Assert.False(ConquestEncounters.AllInstructorsDefeated(wizard));

        BattleOutcomeEvents.Report(Win(world, wizard, spec), [mod]);

        // Written before the event: six chapters complete and the seventh defeated is exactly "all seven defeated".
        Assert.True(ConquestEncounters.AllInstructorsDefeated(wizard));
        Assert.False(ConquestEncounters.AllInstructorsComplete(wizard));
        CompleteEvent(wizard, chapter, encounter.DefeatEvent, QuestProgressSource.Play, mod);
        CompleteEvent(wizard, campaign, encounter.DefeatEvent, QuestProgressSource.Play, mod);
        Assert.True(ConquestEncounters.IsMilestoneComplete(wizard, "Wethersfield"));
        CompleteGoal(wizard, chapter, encounter.ReturnGoal, QuestProgressSource.Play, mod);
        CompleteQuest(wizard, chapter, QuestProgressSource.Play, mod);
        Assert.True(ConquestEncounters.AllInstructorsComplete(wizard));

        // Nothing else was entered, offered or granted: one transfer out of the copy, no second encounter, no finale.
        Assert.Equal(7, ConquestEncounters.Encounters.Count);
        Assert.Single(world.Entered.Where(e => e.CharId == wizard.CharId && e.Encounter == "Wethersfield"));
        Assert.Null(ConquestEncounters.ById("Ambrose"));
        Assert.DoesNotContain(UmbralPerception.Bindings, b => b.Id.Contains("Ambrose", StringComparison.OrdinalIgnoreCase) && b.Kind == UmbralPerception.Kind.Anchor
            && b.Zone == Ravenwood);
        Assert.False(ConquestEncounters.IsVerifiedCampaignVictory(wizard, "ShadowSchool_Conquest_Ambrose_Defeated"));
    }

    // ------------------------------------------------------------------ Voice

    [Fact]
    public void WuIsWarmAndFirmAshthornIsDryAndWaryWethersfieldIsFairAndLooksAtAmbrose() {
        var strings = Strings();
        var wu = string.Join(' ', new[] { "Notices", "Warns", "Yields", "Measure" }.Select(p => strings[$"ConquestWu{p}"]));
        var ashthorn = string.Join(' ', new[] { "Notices", "Warns", "Yields", "Measure" }.Select(p => strings[$"ConquestAshthorn{p}"]));
        var wethersfield = string.Join(' ', new[] { "Notices", "Warns", "Yields", "Measure" }.Select(p => strings[$"ConquestWethersfield{p}"]));

        // Quiet, unvoiced, nobody shouts, and each says the thing that is theirs.
        Assert.DoesNotContain("!", wu + ashthorn + wethersfield);
        Assert.Contains("caring", strings["ConquestWuWarns"]);
        Assert.Contains("Hold on to that", strings["ConquestWuYields"]);
        Assert.DoesNotContain("pathetic", wu, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("forbidden", strings["ConquestAshthornNotices"]);
        Assert.Contains("command you", strings["ConquestAshthornWarns"]);
        Assert.Contains("Malistaire", strings["ConquestAshthornMeasure"] + strings["ConquestAshthornChoice"]);
        Assert.Contains("coercion", strings["ConquestWethersfieldWarns"]);
        Assert.Contains("Ambrose", strings["ConquestWethersfieldMeasure"]);
        Assert.Contains("Ambrose", strings["ConquestWethersfieldVesperaCost"]);

        // Ashthorn is not a professor: nothing calls him one, and he is not turned into someone cruel.
        Assert.DoesNotContain("Professor", ashthorn + strings["ConquestAshthornOffer"] + strings["ConquestAshthornInfo"]);
        Assert.Contains("Ravenwood", strings["ConquestAshthornLocation"]);
        Assert.Contains("Ravenwood", strings["ConquestWethersfieldLocation"]);
        Assert.Contains("Life School", strings["ConquestWuLocation"]);
    }

    [Fact]
    public void TheLastThreeBindingsAreDescribedAsPerceptionNotPower() {
        var strings = Strings();
        foreach (var key in new[] { "Umbral_LifeClassroom", "Umbral_DeathInterim", "Umbral_BalanceCircle" }) {
            Assert.StartsWith("[Umbral Sight] ", strings[key]);
            Assert.Contains("you cannot see", strings[key]);
            Assert.Contains("anchored", strings[key]);
            Assert.DoesNotContain("control", strings[key], StringComparison.OrdinalIgnoreCase);
        }
    }

}
