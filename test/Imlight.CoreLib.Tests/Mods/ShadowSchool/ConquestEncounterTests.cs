using System.Text.Json;
using System.Threading;
using Akka.Actor;
using Imcodec.Cryptography;
using Imcodec.ObjectProperty.TypeCache;
using Imcodec.Types;
using Imlight.Common;
using Imlight.CoreLib.Game.Combat;
using Imlight.CoreLib.Game.Quests;
using Imlight.CoreLib.Game.World;
using Imlight.CoreLib.Game.Zone.Core;
using Imlight.CoreLib.Mods.ShadowSchool;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.Shared.Packets;
using Imlight.CoreLib.WizardData;
using Imlight.CoreLib.WizardData.Models.Player;
using Xunit;

namespace Imlight.CoreLib.Tests.Mods.ShadowSchool;

/// <summary>
/// Stage D1: the Ravenwood conquest framework and its first encounter, Dalia Falmea. The rules are driven through the
/// real ShadowSchoolMod, the way the server drives them (QuestProgressEvents for quest steps, the presence observer for
/// moving and standing still, the battle outcome observer for a battle's end), over the mod's own quest, with a
/// recording world in place of the server (no database, session, zone or client). These are server-side rule and
/// data-contract tests: a battle is never played here, and none of this is graphical-client evidence.
/// </summary>
[Collection("SharedConfiguration")]
public sealed class ConquestEncounterTests {

    private const string Quest = "ShadowSchool-Conquest-Falmea";
    private const string FireSchool = "WizardCity/Interiors/WC_SchoolFire";
    private const string Ravenwood = "WizardCity/WC_Ravenwood";
    private const string Variant = "ShadowConquest-Falmea";
    private const int CustomId = 1880001;
    private const ulong FalmeaTrainer = 38208;
    private const ulong FalmeaCombat = 403089;

    // At Falmea's place in the Fire School (the anchor), and across the room from it.
    private static readonly (float X, float Y) AtAnchor = (790, 20);
    private static readonly (float X, float Y) AcrossTheRoom = (-134, -23);

    private static readonly ConquestEncounter Falmea = ConquestEncounters.ById("Falmea")!;

    private static readonly MagicSchool[] OriginalSchools = [
        MagicSchool.Fire, MagicSchool.Ice, MagicSchool.Storm, MagicSchool.Myth,
        MagicSchool.Life, MagicSchool.Death, MagicSchool.Balance,
    ];

    // ------------------------------------------------------------------ Eligibility and prerequisites

    [Fact]
    public void ANewShadowWizardCannotBeginTheConquest() {
        InitializeIsolatedTestConfiguration();
        var wizard = NewWizard(MagicSchool.Shadow);

        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, Falmea));
        Assert.Equal(ConquestStage.None, ConquestEncounters.StageOf(wizard, Falmea));
    }

    [Theory]
    [InlineData("no-sight")]
    [InlineData("malformed-sight")]
    [InlineData("no-nightside")]
    [InlineData("no-lesson")]
    public void EveryPrerequisiteIsNeeded(string missing) {
        InitializeIsolatedTestConfiguration();
        var wizard = Ready(MagicSchool.Shadow);
        switch (missing) {
            case "no-sight":
                wizard.QuestBehavior.RemoveRegistryValue(UmbralSight.STATE_KEY);
                break;
            case "malformed-sight":
                wizard.QuestBehavior.SetRegistryValue(UmbralSight.STATE_KEY, 7);
                break;
            case "no-nightside":
                wizard.QuestBehavior.RemoveRegistryValue($"{UmbralSight.ORIGIN_QUEST}_Complete");
                break;
            case "no-lesson":
                wizard.QuestBehavior.RemoveRegistryValue($"{ConquestEncounters.LESSON_QUEST}_Complete");
                break;
        }

        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, Falmea));
        Assert.True(ConquestEncounters.MeetsPrerequisites(Ready(MagicSchool.Shadow), Falmea));
    }

    [Theory]
    [MemberData(nameof(Originals))]
    public void AnOriginalSchoolWizardCannotTakePartEvenWithCopiedEntries(MagicSchool school) {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var wizard = Ready(school);
        var quest = LoadQuest();

        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, Falmea));

        // Even holding the quest (as if granted) and standing at the anchor, nothing happens and nothing is written.
        Accept(wizard, quest, mod);
        Look(mod, wizard, AtAnchor);
        CompleteGoal(wizard, quest, Falmea.AnchorGoal, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.None, ConquestEncounters.StageOf(wizard, Falmea));
        Assert.Empty(world.Events);
        Assert.Empty(world.Entered);
        Assert.Equal(0, world.Saves);
    }

    public static IEnumerable<object[]> Originals() => OriginalSchools.Select(s => new object[] { s });

    // ------------------------------------------------------------------ The quest

    [Fact]
    public void TheQuestIsShadowOnlyOfferedByVesperaAndGivesNothing() {
        var quest = LoadQuest();
        var requirements = quest.m_requirements.m_requirements;

        Assert.Contains(requirements, r => r is ReqIsSchool { m_applyNOT: false } s && (string) s.m_magicSchoolName == "Shadow");
        Assert.Contains(requirements, r => r is ReqHasEntry { m_isQuestRegistry: false, m_applyNOT: false } e
            && (string) e.m_entryName == UmbralSight.STATE_KEY);
        foreach (var done in new[] { UmbralSight.ORIGIN_QUEST, ConquestEncounters.LESSON_QUEST }) {
            Assert.Contains(requirements, r => r is ReqHasEntry { m_isQuestRegistry: true, m_applyNOT: false } e
                && (string) e.m_entryName == "Complete" && (string) e.m_questName == done);
        }

        Assert.All(requirements, r => Assert.Equal("ROP_AND", r.m_operator.ToString()));

        // Vespera offers it; the quest and its goals give no reward of any kind, so nothing can be paid twice.
        var prep = ((ActorDialogList) quest.m_dialogList).m_dialogs.Single(d => d.m_dialogTag == "Prep");
        Assert.All(prep.m_dialogEntries, e => Assert.Equal(ShadowSchoolMod.TUTOR_TEMPLATE_ID, (uint) e.m_actorTemplateID));
        Assert.Empty(quest.m_startResults.m_results);
        Assert.Empty(quest.m_endResults.m_results);
        Assert.All(quest.m_goals, g => Assert.Empty(g.m_completeResults.m_results));
        Assert.All(quest.m_goals, g => Assert.Empty(g.m_activateResults.m_results));
    }

    [Fact]
    public void TheQuestsGoalsAreAnchorThenDuelThenVespera() {
        var quest = LoadQuest();

        Assert.Equal([Falmea.AnchorGoal], quest.m_startGoals.Select(g => (string) g).ToArray());
        Assert.Equal([Falmea.AnchorGoal, Falmea.DuelGoal, Falmea.ReturnGoal],
            quest.m_goalLogic.Select(l => (string) l.m_goalsAND.Single()).ToArray());
        Assert.Equal([Falmea.DuelGoal, Falmea.ReturnGoal],
            quest.m_goalLogic.Take(2).Select(l => (string) l.m_goalsToAdd.Single()).ToArray());
        Assert.True(quest.m_goalLogic[^1].m_completeQuest);
        foreach (var goal in quest.m_goals) {
            Assert.Equal(StringHash.Compute(goal.m_goalName), (uint) goal.m_goalNameID);
        }

        // The anchor goal waits for the framework's event only: no zone, proximity volume or client message names it.
        var anchor = Assert.IsType<WaypointGoalTemplate>(Goal(quest, Falmea.AnchorGoal));
        Assert.Equal(GOAL_TYPE.GOAL_TYPE_WAYPOINT, anchor.m_goalType);
        Assert.Equal([Falmea.AnchorEvent], anchor.m_genericEvents.Select(e => (string) e).ToArray());
        Assert.False(anchor.m_zoneEntry);
        Assert.False(anchor.m_zoneExit);
        Assert.Equal("", (string) anchor.m_zoneTag);

        // The duel goal: a bounty no creature's adjectives match (not even Falmea's combat template's), completed only
        // by the framework's event after a win in the private copy.
        var duel = Assert.IsType<BountyGoalTemplate>(Goal(quest, Falmea.DuelGoal));
        Assert.Equal(GOAL_TYPE.GOAL_TYPE_BOUNTY, duel.m_goalType);
        Assert.Equal([Falmea.DefeatEvent], duel.m_genericEvents.Select(e => (string) e).ToArray());
        Assert.Equal(["ShadowSchool-Conquest-Falmea"], duel.m_npcAdjectives.Select(a => (string) a).ToArray());
        Assert.DoesNotContain("DaliaFalmea-WCBoss-R7.AdjRef", duel.m_npcAdjectives.Select(a => (string) a));
        Assert.Equal("WizardMobs_00000845", (string) duel.m_tallyCounter.m_descriptor2);

        var back = Assert.IsType<PersonaGoalTemplate>(Goal(quest, Falmea.ReturnGoal));
        Assert.Equal(ShadowSchoolMod.TUTOR_OBJECT_NAME, (string) back.m_personaName);
    }

    [Fact]
    public void EveryGoalCarriesTheClientNavigationMetadata() {
        // The offer cache and the journal take each goal's location name and destination zone from the template
        // (InteractQuestOfferComponent's GoalEntryFull), so an empty location is the "unknown zone" presentation
        // the earlier Shadow enrollment shipped. Every goal names its place, and the return goal is Vespera's, not
        // the Sergeant Muldoon tag the check-in templates were copied from.
        var quest = LoadQuest();
        var strings = Strings();
        var anchor = Goal(quest, Falmea.AnchorGoal);
        var duel = Goal(quest, Falmea.DuelGoal);
        var back = Goal(quest, Falmea.ReturnGoal);

        Assert.All(quest.m_goals, g => Assert.False(string.IsNullOrWhiteSpace((string) g.m_locationName),
            $"Goal {g.m_goalName} has no client location"));
        Assert.Equal("ShadowSchool_ConquestFalmeaLocation", (string) anchor.m_locationName);
        Assert.Equal("ShadowSchool_ConquestFalmeaLocation", (string) duel.m_locationName);
        Assert.Equal("ShadowSchool_EnrollmentLocation", (string) back.m_locationName);

        Assert.Equal(FireSchool, (string) anchor.m_destinationZone);
        Assert.Equal(FireSchool, (string) duel.m_destinationZone);
        Assert.Equal("WizardCity/Interiors/WC_LibraryBookStacks", (string) back.m_destinationZone);

        // The keys resolve in the mod's own text, which the installer writes to the client locale.
        Assert.Contains("Fire School", strings["ConquestFalmeaLocation"]);
        Assert.Contains("Library Book Stacks", strings["EnrollmentLocation"]);

        Assert.Empty(back.m_clientTags);
        Assert.Equal(ShadowSchoolMod.TUTOR_OBJECT_NAME, (string) Assert.IsType<PersonaGoalTemplate>(back).m_personaName);
    }

    [Fact]
    public void EveryLineIsTheModsOwnUnvoicedAndInCharacter() {
        var quest = LoadQuest();
        var strings = Strings();
        var lines = quest.m_goals.SelectMany(g => ((ActorDialogList) g.m_dialogList).m_dialogs.SelectMany(d => d.m_dialogEntries))
            .Concat(((ActorDialogList) quest.m_dialogList).m_dialogs.SelectMany(d => d.m_dialogEntries))
            .ToList();

        Assert.Equal(8, lines.Count);
        foreach (var line in lines) {
            Assert.Equal("", (string) line.m_soundFile);
            Assert.StartsWith("ShadowSchool_", (string) line.m_dialog);
            var text = strings[((string) line.m_dialog)["ShadowSchool_".Length..]];
            Assert.InRange(text.Length, 40, 320);
            // D1 is not the finale: no pact, no Morganthe, no recruitment.
            foreach (var banned in new[] { "Morganthe", "pact", "apprentice", "Malistaire", "Krokotopia" }) {
                Assert.DoesNotContain(banned, text, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Falmea speaks as herself (her persona and portrait), Vespera as herself.
        Assert.All(((ActorDialogList) Goal(quest, Falmea.AnchorGoal).m_dialogList).m_dialogs.Single().m_dialogEntries,
            e => Assert.Equal("WC-RAV-NPC04_Persona", (string) ((NPCDialogEntry) e).m_personaName));
        Assert.All(((ActorDialogList) Goal(quest, Falmea.ReturnGoal).m_dialogList).m_dialogs.Single().m_dialogEntries,
            e => Assert.Equal(ShadowSchoolMod.TUTOR_PERSONA, (string) ((NPCDialogEntry) e).m_personaName));

        // The motivations the plan asks for: the wizard chooses, Vespera does not send them, Falmea feels betrayed.
        Assert.Contains("your own choice", strings["ConquestFalmeaChoice"]);
        Assert.Contains("not sending you", strings["ConquestFalmeaChoice"]);
        Assert.Contains("Firecat", strings["ConquestFalmeaNotices"]);
        Assert.Contains("hold back most", strings["ConquestFalmeaWarns"]);
        Assert.Contains("still open", strings["ConquestFalmeaMeasure"]);

        foreach (var key in new[] { (string) quest.m_questTitle, (string) quest.m_questInfo }
                     .Concat(quest.m_goals.Select(g => (string) g.m_goalTitle).Where(t => t.StartsWith("ShadowSchool_")))) {
            Assert.True(strings.ContainsKey(key["ShadowSchool_".Length..]), key);
        }

        foreach (var key in new[] { Falmea.EnterKey, Falmea.WonKey, Falmea.LostKey }) {
            Assert.InRange(strings[key].Length, 40, 260);
            Assert.DoesNotContain("pact", strings[key], StringComparison.OrdinalIgnoreCase);
        }
    }

    // ------------------------------------------------------------------ Umbral Sight and the anchor

    [Fact]
    public void TheAnchorIsAnUmbralSightBindingAtFalmeasPlace() {
        InitializeIsolatedTestConfiguration();
        var binding = UmbralPerception.Bindings.Single(b => b.Id == Falmea.AnchorBinding);

        Assert.Equal(FireSchool, binding.Zone);
        Assert.True(ConquestEncounters.AtAnchor(Falmea, 787, 9));
        Assert.True(ConquestEncounters.AtAnchor(Falmea, AtAnchor.X, AtAnchor.Y));
        Assert.False(ConquestEncounters.AtAnchor(Falmea, AcrossTheRoom.X, AcrossTheRoom.Y));

        // A wizard with the sight perceives it whether or not they are on the quest; one without perceives nothing.
        var seer = Ready(MagicSchool.Shadow);
        seer.Zone = FireSchool;
        UmbralPerception.Attach(seer);
        UmbralPerception.Moved(seer, AtAnchor.X, AtAnchor.Y);
        Assert.Contains(binding.TextKey, UmbralPerception.Still(seer, AtAnchor.X, AtAnchor.Y, UmbralPerception.FocusDelay).ChatKeys);
        UmbralPerception.Detach(seer.CharId);

        var fire = Ready(MagicSchool.Fire);
        fire.Zone = FireSchool;
        UmbralPerception.Attach(fire);
        UmbralPerception.Moved(fire, AtAnchor.X, AtAnchor.Y);
        Assert.True(UmbralPerception.Still(fire, AtAnchor.X, AtAnchor.Y, UmbralPerception.FocusDelay).IsEmpty);
        UmbralPerception.Detach(fire.CharId);
    }

    [Fact]
    public void LookingAtTheAnchorOnTheQuestFindsItOnce() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var quest = LoadQuest();
        Accept(wizard, quest, mod);
        Arrive(mod, wizard, FireSchool);

        // Too short a look, or from across the room: nothing.
        mod.OnMoved(world.Session(wizard), wizard, AtAnchor.X, AtAnchor.Y, 0);
        mod.Encounters.OnStill(world.Session(wizard), wizard, AtAnchor.X, AtAnchor.Y, TimeSpan.FromMilliseconds(250));
        Look(mod, wizard, AcrossTheRoom);
        Assert.Empty(world.Events);

        Look(mod, wizard, AtAnchor);
        Assert.Equal([(wizard.CharId, Falmea.AnchorEvent)], world.Events);

        // Standing on, the look does not act again; it needs a step first, and then the goal is no longer waiting.
        mod.Encounters.OnStill(world.Session(wizard), wizard, AtAnchor.X, AtAnchor.Y, TimeSpan.FromSeconds(10));
        Assert.Single(world.Events);
    }

    [Fact]
    public void WithoutTheQuestTheAnchorIsOnlyPerceived() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        Arrive(mod, wizard, FireSchool);

        Look(mod, wizard, AtAnchor);

        Assert.Empty(world.Events);
        Assert.Empty(world.Entered);
        Assert.Null(mod.Encounters.SessionOf(wizard.CharId));
    }

    // ------------------------------------------------------------------ Initiation and the battle copy

    [Fact]
    public void AfterTheAnchorTheNextLookOpensThePrivateCopy() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, _) = AtDuel(world, mod);

        Look(mod, wizard, AtAnchor);

        Assert.Equal([(wizard.CharId, Falmea.Id)], world.Entered);
        Assert.Equal(ConquestEncounters.Phase.Entering, mod.Encounters.SessionOf(wizard.CharId)!.Phase);
        Assert.Contains(world.Shown, s => s.CharId == wizard.CharId && s.Notify.Contains(Falmea.EnterKey));

        // Standing on, or looking again while the session is open, opens nothing more.
        Look(mod, wizard, AtAnchor);
        Assert.Single(world.Entered);

        // The transfer arrives: the wizard is in their own copy.
        world.Noted[wizard.CharId] = (FireSchool, Variant);
        Arrive(mod, wizard, FireSchool);
        Assert.Equal(ConquestEncounters.Phase.InCopy, mod.Encounters.SessionOf(wizard.CharId)!.Phase);

        // Inside the copy the same spot is the anchor too; it does not send them in again.
        Look(mod, wizard, AtAnchor);
        Assert.Single(world.Entered);
    }

    [Theory]
    [InlineData("in-duel")]
    [InlineData("in-combat-grace")]
    [InlineData("anchor-by-command")]
    public void TheCopyDoesNotOpen(string why) {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var quest = LoadQuest();
        Accept(wizard, quest, mod);
        Arrive(mod, wizard, FireSchool);
        CompleteGoal(wizard, quest, Falmea.AnchorGoal,
            why == "anchor-by-command" ? QuestProgressSource.Command : QuestProgressSource.Play, mod);
        wizard.IsInDuel = why == "in-duel";
        wizard.IsInCombatGrace = why == "in-combat-grace";

        Look(mod, wizard, AtAnchor);

        Assert.Empty(world.Entered);
        Assert.Null(mod.Encounters.SessionOf(wizard.CharId));
    }

    [Fact]
    public void TheEncounterReloadsIntoAFreshPrivateCopyInsteadOfMovingInsideThePublicSchool() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var wizard = Ready(MagicSchool.Shadow);
        var seen = new List<object>();
        var session = world.Recorder(seen);
        var live = new ConquestWorld(_ => { });

        live.Enter(session, wizard, Falmea);
        WaitFor(seen, 1);
        var enter = Assert.IsType<ZONE_102_PROTOCOL.MSG_ZONETRANSFER>(seen[0]);

        // The anchor is inside the Fire School, so this is a same-zone transfer. ZoneService only teleports the wizard
        // inside the PUBLIC school unless ForceReload is set; the private copy, its battle circle and the hostile
        // Falmea would never load. The reload is what makes this a fresh private variant copy.
        Assert.Equal(FireSchool, enter.DestinationZone);
        Assert.Equal(Falmea.ArrivalLoc, enter.DestinationLocation);
        Assert.True(enter.SendToClient);
        Assert.True(enter.ForceReload);
        Assert.True(enter.IsPrivate);
        Assert.True(enter.ResetInstance);
        Assert.Equal(wizard.CharId, enter.OwnerCharId);
        Assert.Equal(Variant, enter.Variant);

        // The variant is noted for the attach that follows the client's reload.
        var noted = ZoneVariants.Of(wizard.CharId);
        Assert.NotNull(noted);
        Assert.Equal(FireSchool, noted!.Zone);
        Assert.Equal(Variant, noted.Variant);

        // Leaving forgets the note and sends the wizard out of the copy, never back into it.
        live.Leave(session, wizard, Falmea, TimeSpan.Zero);
        WaitFor(seen, 2);
        var exit = Assert.IsType<ZONE_102_PROTOCOL.MSG_ZONETRANSFER>(seen[1]);
        Assert.Equal(Ravenwood, exit.DestinationZone);
        Assert.Equal(Falmea.ExitLoc, exit.DestinationLocation);
        Assert.Equal(wizard.CharId, exit.OwnerCharId);
        Assert.False(exit.ForceReload);
        Assert.Null(ZoneVariants.Of(wizard.CharId));
    }

    [Fact]
    public void TheBattleRoomIsAPrivateVariantOfTheFireSchool() {
        var room = LoadRoom();

        Assert.Equal(FireSchool, room.Zone);
        Assert.Equal(Variant, room.Variant);
        Assert.Equal(Falmea.Zone, room.Zone);
        Assert.Equal(Falmea.Variant, room.Variant);
        Assert.Equal($"{FireSchool.Replace('/', '-')}@{Variant}.json", ZoneObjects.FileNameOf(room.Zone, room.Variant));

        // In this copy only: the trainer is left out and the game's combat Falmea stands at her place, as the custom.
        Assert.Equal([FalmeaTrainer], room.Remove);
        var creature = Assert.Single(room.Creatures);
        Assert.Equal(FalmeaCombat, creature.TemplateID);
        Assert.Equal(787f, creature.X, 0.5f);
        Assert.Equal(9.45f, creature.Y, 0.5f);
        Assert.Equal(CustomId, room.ReplaceSpawns[FalmeaCombat.ToString()]);
        Assert.Equal(Falmea.CustomId, room.ReplaceSpawns[Falmea.CreatureTemplateId.ToString()]);

        // A battle circle of the kind the Wizard City boss rooms use, on her platform, within reach of her.
        var circle = Assert.Single(room.Objects);
        Assert.Equal(560ul, circle.TemplateID);
        Assert.Equal("CombatSigil8Actor", circle.CombatSigil.SigilType);
        Assert.InRange(MathF.Sqrt(MathF.Pow(circle.X - creature.X, 2) + MathF.Pow(circle.Y - creature.Y, 2)), 0, circle.CombatSigil.Radius);

        // The third coordinate is height (the zone's Start is Z 0.5, Falmea stands at Z 99.75), so the circle must sit
        // at her platform's height, not on the floor at the school's entrance. A circle left at Z 0 would float a floor
        // below her and the duel could never start there.
        Assert.InRange(circle.Z, creature.Z - 1f, creature.Z + 1f);
        Assert.InRange(circle.Z, 99f, 101f);

        // Every way out goes to Ravenwood, outside the Fire School: never back into the copy.
        Assert.Equal(Ravenwood, room.Exit.Zone);
        Assert.Equal(Falmea.ExitZone, room.Exit.Zone);
        Assert.Equal(Falmea.ExitLoc, room.Exit.Loc);
        Assert.Empty(room.Exit.Triggers);
    }

    [Fact]
    public void FalmeasBattleProfileIsAWizardCityFireFight() {
        InitializeIsolatedTestConfiguration();
        var json = File.ReadAllText(Path.Combine(ModFolder(), "spiraldb", "Customs", $"{CustomId}.json"));
        var custom = BossOverrides.Parse(json, Customs.Key(CustomId))!;

        Assert.Equal(CustomId, custom.CustomID);
        Assert.Equal(FalmeaCombat, custom.BaseTemplateID);
        Assert.Equal("Fire", custom.School);
        Assert.Equal(10, custom.Level);
        Assert.InRange(custom.Health!.Value, 600, 1500);
        Assert.Equal(0, custom.StartingPips);
        Assert.Equal(0f, custom.PowerPipChance);
        Assert.Equal(0, custom.XP);
        Assert.Equal(0, custom.GoldMax);
        Assert.Empty(custom.Drops!);
        foreach (var row in new[] { custom.DamageBySchool, custom.ResistBySchool, custom.PierceBySchool, custom.CriticalBySchool, custom.BlockBySchool }) {
            Assert.All(row!.Values, v => Assert.Equal(0f, v));
        }

        // Fire damage over time and tempo, from the game's own Fire spells (ids checked on the live server).
        Assert.Equal([103007158u, 1243857701u, 1599065470u, 400321766u, 603748810u], custom.Deck!.Select(c => c.SpellID).ToArray());
        // (Laying it over a creature's stats needs the game's school table: checked on the live server, not here.)
    }

    [Fact]
    public void TheDuelFindsFalmeasProfileThroughTheCustomTheSpawnerMarks() {
        // CombatDuelSubCircle looks a creature's changes up by Customs.KeyOf: the custom the spawner marked it as
        // (ZoneObjects ReplaceSpawns -> ZonePath), not the creature's own template. The base Falmea is Myth with
        // 4,000 health and a reused Myth deck; the duel must receive this profile instead: Fire, level 10, 950.
        InitializeIsolatedTestConfiguration();
        const ulong globalId = 0x1A11_0000_0000_0F41;
        Customs.Mark(globalId, CustomId);
        try {
            var spawned = new CoreObject {
                m_globalID = new GID(globalId),
                m_templateID = new GID(FalmeaCombat),
            };

            Assert.Equal(CustomId, Customs.Of(globalId));
            Assert.Equal(Customs.Key(CustomId), Customs.KeyOf(spawned));
            Assert.True(Customs.IsCustom(Customs.KeyOf(spawned)));
            Assert.Equal(CustomId, Customs.IdOf(Customs.KeyOf(spawned)));

            // The key reaches the mod's own custom file. (BaseOf cannot be asserted here: a mod's files are read only
            // for an enabled mod, and the isolated test server enables none, so the file is read directly.)
            var json = File.ReadAllText(Path.Combine(ModFolder(), "spiraldb", "Customs", $"{CustomId}.json"));
            var profile = BossOverrides.Parse(json, Customs.KeyOf(spawned))!;
            Assert.Equal(Customs.KeyOf(spawned), profile.TemplateID);
            Assert.Equal(FalmeaCombat, profile.BaseTemplateID);
            Assert.Equal(950, profile.Health);
            Assert.Equal(10, profile.Level);
            Assert.Equal("Fire", profile.School);
            Assert.Equal(0, profile.XP);
            Assert.Equal(0, profile.GoldMax);
            Assert.Empty(profile.Drops!);
            Assert.Equal([103007158u, 1243857701u, 1599065470u, 400321766u, 603748810u],
                profile.Deck!.Select(c => c.SpellID).ToArray());
        }
        finally {
            Customs.Forget(globalId);
            Assert.Equal(0, Customs.Of(globalId));
        }
    }

    // ------------------------------------------------------------------ Victory, defeat and credit

    [Fact]
    public void AWinInTheOwnersCopyDefeatsFalmeaOnceAndReturnsThem() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, quest) = InCopy(world, mod);

        mod.OnBattleEnded(Outcome(world, wizard, BattleResult.Won));

        Assert.Equal((wizard.CharId, Falmea.DefeatEvent), world.Events[^1]);
        Assert.Contains(world.Shown, s => s.CharId == wizard.CharId && s.Chat.Contains(Falmea.WonKey));
        Assert.Equal([(wizard.CharId, Falmea.Id, ConquestEncounters.ReturnDelay)], world.Left);

        // The quest service completes the duel goal from that event, as the wizard's own play.
        CompleteGoal(wizard, quest, Falmea.DuelGoal, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, Falmea));

        // Back to Vespera: the quest completes and the encounter is complete.
        CompleteGoal(wizard, quest, Falmea.ReturnGoal, QuestProgressSource.Play, mod);
        CompleteQuest(wizard, quest, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.Complete, ConquestEncounters.StageOf(wizard, Falmea));
        Assert.True(ConquestEncounters.IsComplete(wizard, Falmea));
    }

    [Theory]
    [InlineData("lost")]
    [InlineData("fled")]
    public void ALossOrFleeLeavesTheDuelOpenForAnotherTry(string how) {
        var result = how == "lost" ? BattleResult.Lost : BattleResult.Fled;
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, quest) = InCopy(world, mod);
        var events = world.Events.Count;

        // Through the pipeline the duel reports on (BattleOutcomeEvents), not by calling the mod directly.
        BattleOutcomeEvents.Report(Outcome(world, wizard, result), [mod]);

        Assert.Equal(events, world.Events.Count);
        Assert.Contains(world.Shown, s => s.CharId == wizard.CharId && s.Chat.Contains(Falmea.LostKey));
        Assert.Empty(world.Left);
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, Falmea));
        Assert.True(ConquestEncounters.ActiveQuest(wizard, Falmea)!.IsGoalActive(Falmea.DuelGoal));

        // The loss sends them to Ravenwood (the copy's exit); the session is over there, and a new look opens a new one.
        world.Noted.Remove(wizard.CharId);
        Arrive(mod, wizard, Ravenwood);
        Assert.Null(mod.Encounters.SessionOf(wizard.CharId));
        Arrive(mod, wizard, FireSchool);
        Look(mod, wizard, AtAnchor);
        Assert.Equal(2, world.Entered.Count);
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, Falmea));
        _ = quest;
    }

    [Fact]
    public void AWinThroughTheRealBattlePipelineDefeatsFalmeaOnce() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, quest) = InCopy(world, mod);

        // CombatDuelComponent reports through BattleOutcomeEvents.Report; the mod is an observer of it.
        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won), [mod]);

        Assert.Single(world.Events, e => e.CharId == wizard.CharId && e.Event == Falmea.DefeatEvent);
        Assert.Contains(world.Shown, s => s.CharId == wizard.CharId && s.Chat.Contains(Falmea.WonKey));
        Assert.Equal([(wizard.CharId, Falmea.Id, ConquestEncounters.ReturnDelay)], world.Left);

        // One event is one goal: the quest service's completion advances the stage once, and a repeated win in the copy
        // (a stale report) adds nothing after the goal is done.
        CompleteGoal(wizard, quest, Falmea.DuelGoal, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, Falmea));
        var events = world.Events.Count;
        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won), [mod]);
        Assert.Equal(events, world.Events.Count);
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, Falmea));
    }

    [Fact]
    public void AFleeThenARetryWinsAndCompletesOnlyOnce() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, quest) = InCopy(world, mod);

        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Fled), [mod]);
        Assert.Contains(world.Shown, s => s.Chat.Contains(Falmea.LostKey));
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, Falmea));

        // The defeat message the duel sends on a flee/defeat carries the copy's exit, so the client leaves; the attach in
        // Ravenwood forgets the copy's variant (ZoneVariants.ForAttach), as the World here does.
        world.Noted.Remove(wizard.CharId);
        Arrive(mod, wizard, Ravenwood);
        Assert.Null(mod.Encounters.SessionOf(wizard.CharId));

        // Back at the anchor: a new copy, a win, and the goal completed once. (The look opens the transfer; the client's
        // reload is the attach that follows it, which puts the wizard in the copy.)
        Arrive(mod, wizard, FireSchool);
        Look(mod, wizard, AtAnchor);
        Arrive(mod, wizard, FireSchool);
        Assert.Equal(2, world.Entered.Count);
        Assert.Equal(ConquestEncounters.Phase.InCopy, mod.Encounters.SessionOf(wizard.CharId)!.Phase);
        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won), [mod]);
        CompleteGoal(wizard, quest, Falmea.DuelGoal, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, Falmea));

        // The return to Vespera completes the encounter; a repeated report or goal does not move it again.
        CompleteGoal(wizard, quest, Falmea.ReturnGoal, QuestProgressSource.Play, mod);
        CompleteQuest(wizard, quest, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.Complete, ConquestEncounters.StageOf(wizard, Falmea));
        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won), [mod]);
        QuestProgressEvents.GoalCompleted(wizard, quest, Goal(quest, Falmea.DuelGoal), QuestProgressSource.Play, [mod]);
        Assert.Equal(ConquestStage.Complete, ConquestEncounters.StageOf(wizard, Falmea));
        Assert.Single(world.Events, e => e.Event == Falmea.DefeatEvent);
    }

    [Fact]
    public void ADisconnectDuringTheBattleLeavesNoSessionAndARetryOpensAFreshCopy() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, _) = InCopy(world, mod);

        // The duel reports Left for a dropped connection or logout; the session closes with the detach.
        var events = world.Events.Count;
        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Left), [mod]);
        mod.OnDetached(wizard.CharId);
        Assert.Null(mod.Encounters.SessionOf(wizard.CharId));
        Assert.Equal(events, world.Events.Count);
        Assert.Empty(world.Left);

        // Reconnecting inside the copy sends the wizard to the exit (the copy's variant is still noted), and the next
        // look at the anchor, after a step, opens a fresh one.
        Arrive(mod, wizard, FireSchool);
        Assert.Equal([(wizard.CharId, Falmea.Id, TimeSpan.Zero)], world.Left);
        Assert.Single(world.Entered);

        world.Noted.Remove(wizard.CharId);
        Arrive(mod, wizard, Ravenwood);
        Arrive(mod, wizard, FireSchool);
        Look(mod, wizard, AtAnchor);
        Arrive(mod, wizard, FireSchool);
        Assert.Equal(2, world.Entered.Count);
        Assert.Equal(ConquestEncounters.Phase.InCopy, mod.Encounters.SessionOf(wizard.CharId)!.Phase);
    }

    [Theory]
    [InlineData("public-zone")]
    [InlineData("not-an-instance")]
    [InlineData("another-variant")]
    [InlineData("no-session")]
    [InlineData("another-creature")]
    public void OnlyTheOwnersWinInTheirCopyOverFalmeaCounts(string why) {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, _) = InCopy(world, mod);
        var events = world.Events.Count;
        if (why == "no-session") {
            mod.OnDetached(wizard.CharId);
        }

        mod.OnBattleEnded(Outcome(world, wizard, BattleResult.Won,
            variant: why is "public-zone" ? "" : why is "another-variant" ? "RavenwoodMalistaire" : Variant,
            isInstance: why is not ("public-zone" or "not-an-instance"),
            customId: why == "another-creature" ? 0 : CustomId));

        Assert.Equal(events, world.Events.Count);
        if (why != "another-creature") {
            Assert.Empty(world.Left);
        }

        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, Falmea));
    }

    [Fact]
    public void RepeatedWinsAndStepsNeverAddTwice() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, quest) = InCopy(world, mod);

        mod.OnBattleEnded(Outcome(world, wizard, BattleResult.Won));
        CompleteGoal(wizard, quest, Falmea.DuelGoal, QuestProgressSource.Play, mod);
        var saves = world.Saves;
        var registry = JsonSerializer.Serialize(wizard.QuestBehavior.Registry);

        // A second victory report (the goal is done) posts no event; the same goal or step again writes nothing.
        var events = world.Events.Count;
        mod.OnBattleEnded(Outcome(world, wizard, BattleResult.Won));
        Assert.Equal(events, world.Events.Count);
        QuestProgressEvents.GoalCompleted(wizard, quest, Goal(quest, Falmea.DuelGoal), QuestProgressSource.Play, [mod]);
        QuestProgressEvents.GoalCompleted(wizard, quest, Goal(quest, Falmea.AnchorGoal), QuestProgressSource.Play, [mod]);
        Assert.Equal(saves, world.Saves);
        Assert.Equal(registry, JsonSerializer.Serialize(wizard.QuestBehavior.Registry));

        CompleteGoal(wizard, quest, Falmea.ReturnGoal, QuestProgressSource.Play, mod);
        CompleteQuest(wizard, quest, QuestProgressSource.Play, mod);
        saves = world.Saves;
        QuestProgressEvents.QuestCompleted(wizard, quest, QuestProgressSource.Play, [mod]);
        Assert.Equal(saves, world.Saves);
        Assert.Equal(ConquestStage.Complete, ConquestEncounters.StageOf(wizard, Falmea));
    }

    [Theory]
    [InlineData("command")]
    [InlineData("catch-up")]
    [InlineData("shared")]
    public void ACommandAReplayOrAHelpersGoalNeverAdvancesIt(string how) {
        var source = how switch {
            "command" => QuestProgressSource.Command,
            "catch-up" => QuestProgressSource.CatchUp,
            _ => QuestProgressSource.Shared,
        };
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var quest = LoadQuest();
        Accept(wizard, quest, mod);

        CompleteGoal(wizard, quest, Falmea.AnchorGoal, source, mod);
        CompleteGoal(wizard, quest, Falmea.DuelGoal, source, mod);
        CompleteGoal(wizard, quest, Falmea.ReturnGoal, source, mod);
        CompleteQuest(wizard, quest, source, mod);

        Assert.Equal(ConquestStage.None, ConquestEncounters.StageOf(wizard, Falmea));
        Assert.False(ConquestEncounters.IsComplete(wizard, Falmea));
        Assert.Equal(0, world.Saves);
    }

    // ------------------------------------------------------------------ Persistence and isolation

    [Fact]
    public void TheStageSurvivesAReloadAndARestart() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, quest) = InCopy(world, mod);
        mod.OnBattleEnded(Outcome(world, wizard, BattleResult.Won));
        CompleteGoal(wizard, quest, Falmea.DuelGoal, QuestProgressSource.Play, mod);

        var reloaded = Reload(wizard);
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(reloaded, Falmea));

        // The server stopped after the quest was saved complete but before the stage was: the next attach finishes it.
        reloaded.QuestBehavior.SetQuestRegistryValue(Quest, "Complete", 1);
        var restarted = Mod(new World());
        Arrive(restarted, reloaded, Ravenwood);
        Assert.Equal(ConquestStage.Complete, ConquestEncounters.StageOf(reloaded, Falmea));
        Assert.Equal(ConquestStage.Complete, ConquestEncounters.StageOf(Reload(reloaded), Falmea));
    }

    [Fact]
    public void AReconnectOrRestartIntoACopySendsTheWizardOut() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, _) = InCopy(world, mod);

        // The client drops: the duel ends for them and the session closes. The server still notes the copy for them.
        mod.OnBattleEnded(Outcome(world, wizard, BattleResult.Left));
        mod.OnDetached(wizard.CharId);
        Assert.Null(mod.Encounters.SessionOf(wizard.CharId));

        // Logging in again lands in a fresh copy (the note); with no session open for it, they are sent to the exit.
        Arrive(mod, wizard, FireSchool);
        Assert.Equal([(wizard.CharId, Falmea.Id, TimeSpan.Zero)], world.Left);

        // The same after a restart (a new mod, no sessions at all).
        var restartedWorld = new World();
        restartedWorld.Noted[wizard.CharId] = (FireSchool, Variant);
        var restarted = Mod(restartedWorld);
        Arrive(restarted, wizard, FireSchool);
        Assert.Single(restartedWorld.Left);
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, Falmea));
    }

    [Fact]
    public void ATransferThatNeverArrivedIsResolvedSafely() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, _) = AtDuel(world, mod);
        Look(mod, wizard, AtAnchor);

        // Much later the wizard attaches in the Fire School. The server still notes the copy for them (it would load
        // it), but the session that asked for it is stale: it is not taken for the copy, and they are sent out.
        world.Now += ConquestEncounters.EnterTimeout + TimeSpan.FromSeconds(1);
        Arrive(mod, wizard, FireSchool);

        Assert.Null(mod.Encounters.SessionOf(wizard.CharId));
        Assert.Equal([(wizard.CharId, Falmea.Id, TimeSpan.Zero)], world.Left);

        // Without the note (the public Fire School), a stale session is simply closed.
        Look(mod, wizard, AtAnchor);
        world.Noted.Remove(wizard.CharId);
        world.Now += ConquestEncounters.EnterTimeout + TimeSpan.FromSeconds(1);
        Arrive(mod, wizard, FireSchool);
        Assert.Null(mod.Encounters.SessionOf(wizard.CharId));
        Assert.Single(world.Left);
    }

    [Fact]
    public void TwoShadowWizardsKeepTheirOwnEncounters() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (first, firstQuest) = InCopy(world, mod);
        var second = Ready(MagicSchool.Shadow);
        var secondQuest = LoadQuest();
        Accept(second, secondQuest, mod);
        Arrive(mod, second, FireSchool);

        // The first wins in their copy; only their session hears of it, and only their registry moves.
        mod.OnBattleEnded(Outcome(world, first, BattleResult.Won));
        CompleteGoal(first, firstQuest, Falmea.DuelGoal, QuestProgressSource.Play, mod);
        Assert.All(world.Events.Where(e => e.Event == Falmea.DefeatEvent), e => Assert.Equal(first.CharId, e.CharId));
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(first, Falmea));
        Assert.Equal(ConquestStage.None, ConquestEncounters.StageOf(second, Falmea));
        Assert.Null(mod.Encounters.SessionOf(second.CharId));

        // The second wizard's win report in the first one's copy (not their session) counts for nobody.
        var events = world.Events.Count;
        mod.OnBattleEnded(Outcome(world, second, BattleResult.Won));
        Assert.Equal(events, world.Events.Count);

        // A copied registry entry does not carry progress to another character either: it has no quest, no session.
        second.QuestBehavior.SetRegistryValue(Falmea.StateKey, (ulong) ConquestStage.Defeated);
        CompleteQuest(second, secondQuest, QuestProgressSource.Command, mod);
        Assert.False(ConquestEncounters.IsComplete(second, Falmea));
    }

    [Fact]
    public void AMalformedStageIsNeverOverwritten() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var quest = LoadQuest();
        wizard.QuestBehavior.SetRegistryValue(Falmea.StateKey, 9);
        Accept(wizard, quest, mod);

        CompleteGoal(wizard, quest, Falmea.AnchorGoal, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.Malformed, ConquestEncounters.StageOf(wizard, Falmea));
        Assert.Equal(9ul, wizard.QuestBehavior.GetRegistryValue(Falmea.StateKey));
        Assert.Equal(0, world.Saves);
    }

    [Fact]
    public void StartingTheQuestAgainStartsAnUnfinishedEncounterOver() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, quest) = AtDuel(world, mod);
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, Falmea));

        wizard.QuestBehavior.CurrentQuestInstances.RemoveAll(q => q.QuestName == Quest);
        Accept(wizard, quest, mod);

        Assert.Equal(ConquestStage.None, ConquestEncounters.StageOf(wizard, Falmea));
    }

    // ------------------------------------------------------------------ The canonical world is untouched

    [Fact]
    public void ThePublicFireSchoolAndItsTrainerAreUntouched() {
        var zoneObjects = Path.Combine(ModFolder(), "spiraldb", "ZoneObjects");
        var files = Directory.GetFiles(zoneObjects, "*.json");

        // No mod file applies to every copy of the Fire School: only the variant file names it.
        Assert.DoesNotContain(files, f => Path.GetFileName(f).Equals("WizardCity-Interiors-WC_SchoolFire.json", StringComparison.OrdinalIgnoreCase));
        foreach (var file in files) {
            var room = JsonSerializer.Deserialize<ZoneObjects.ZoneObjectFile>(File.ReadAllText(file), JsonOptions)!;
            if (room.Remove.Contains(FalmeaTrainer) || room.Creatures.Any(c => c.TemplateID == FalmeaCombat)) {
                Assert.False(string.IsNullOrEmpty(room.Variant), Path.GetFileName(file));
            }
        }

        // No mod quest replaces a canonical quest of Falmea's or the Fire enrollment, and none targets her as a persona.
        var quests = Directory.GetFiles(Path.Combine(ModFolder(), "spiraldb", "QuestTemplates"), "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f)["questtemplates_".Length..]).ToList();
        Assert.DoesNotContain("WC-COMMONS-MAIN-002-FIRE", quests);
        var conquest = LoadQuest();
        Assert.DoesNotContain(conquest.m_goals.OfType<PersonaGoalTemplate>(), g => ((string) g.m_personaName).StartsWith("WC-RAV-NPC04"));
    }

    [Fact]
    public void ABattleObserverThatThrowsDoesNotStopTheBattlesEnd() {
        InitializeIsolatedTestConfiguration();
        var world = new World();
        var mod = Mod(world);
        var (wizard, _) = InCopy(world, mod);

        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won), [new Throwing(), mod]);

        Assert.Equal(Falmea.DefeatEvent, world.Events[^1].Event);
    }

    private sealed class Throwing : IBattleOutcomeObserver {
        public void OnBattleEnded(BattleOutcome outcome) => throw new InvalidOperationException("observer failure");
    }

    private sealed class Capture : ReceiveActor {

        // Public: Akka makes an actor through its constructor by reflection.
        public Capture(List<object> seen) {
            ReceiveAny(message => {
                lock (seen) {
                    seen.Add(message);
                }
            });
        }

    }

    private static void WaitFor(List<object> seen, int count)
        => Assert.True(SpinWait.SpinUntil(() => {
            lock (seen) {
                return seen.Count >= count;
            }
        }, TimeSpan.FromSeconds(5)), $"Expected {count} message(s), got {seen.Count}.");

    // ------------------------------------------------------------------ helpers

    /// <summary>The server, as the framework sees it: every effect recorded.</summary>
    private sealed class World : IConquestWorld {

        private static readonly ActorSystem s_system = ActorSystem.Create("conquest-tests");
        private readonly Dictionary<ulong, IActorRef> _sessions = [];
        private readonly Dictionary<IActorRef, ulong> _owners = [];

        internal int Saves { get; private set; }
        internal List<(ulong CharId, string Event)> Events { get; } = [];
        internal List<(ulong CharId, IReadOnlyList<string> Notify, IReadOnlyList<string> Chat)> Shown { get; } = [];
        internal List<(ulong CharId, string Encounter)> Entered { get; } = [];
        internal List<(ulong CharId, string Encounter, TimeSpan Delay)> Left { get; } = [];
        internal Dictionary<ulong, (string Zone, string Variant)> Noted { get; } = [];
        internal DateTime Now { get; set; } = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

        internal IActorRef Session(Wizard wizard) {
            if (!_sessions.TryGetValue(wizard.CharId, out var session)) {
                session = s_system.ActorOf(Props.Empty);
                _sessions[wizard.CharId] = session;
                _owners[session] = wizard.CharId;
            }

            return session;
        }

        /// <summary>An actor that records every message sent to it (the live world's session stand-in).</summary>
        internal IActorRef Recorder(List<object> seen) => s_system.ActorOf(Props.Create(() => new Capture(seen)));

        public DateTime UtcNow => Now;

        public void Save(Wizard wizard) => Saves++;

        public void PostQuestEvent(IActorRef session, string eventName) => Events.Add((_owners[session], eventName));

        public void Show(IActorRef session, IReadOnlyList<string> notifyKeys, IReadOnlyList<string> chatKeys)
            => Shown.Add((_owners[session], notifyKeys, chatKeys));

        public void Enter(IActorRef session, Wizard wizard, ConquestEncounter encounter) {
            Entered.Add((wizard.CharId, encounter.Id));
            Noted[wizard.CharId] = (encounter.Zone, encounter.Variant);
        }

        public void Leave(IActorRef session, Wizard wizard, ConquestEncounter encounter, TimeSpan delay) {
            Left.Add((wizard.CharId, encounter.Id, delay));
            Noted.Remove(wizard.CharId);
        }

        public string NotedVariant(ulong charId, string zone)
            => Noted.TryGetValue(charId, out var noted) && noted.Zone == zone ? noted.Variant : "";

    }

    private static ShadowSchoolMod Mod(World world) {
        var mod = new ShadowSchoolMod(_ => { }, world);
        s_worlds.AddOrUpdate(mod, world);

        return mod;
    }

    private static BattleOutcome Outcome(World world, Wizard wizard, BattleResult result, string variant = Variant,
                                         bool isInstance = true, int customId = CustomId)
        => new(result, wizard, world.Session(wizard), FireSchool, variant, isInstance,
            [new BattleCreature(FalmeaCombat, customId, 0x1A11_0000_0000_0001)]);

    /// <summary>A Shadow wizard on the quest whose anchor is found, in the public Fire School.</summary>
    private static (Wizard Wizard, QuestTemplate Quest) AtDuel(World world, ShadowSchoolMod mod) {
        var wizard = Ready(MagicSchool.Shadow);
        var quest = LoadQuest();
        Accept(wizard, quest, mod);
        Arrive(mod, wizard, FireSchool);
        Look(mod, wizard, AtAnchor);
        CompleteGoal(wizard, quest, Falmea.AnchorGoal, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, Falmea));
        Assert.True(ConquestEncounters.ActiveQuest(wizard, Falmea)!.IsGoalActive(Falmea.DuelGoal));

        return (wizard, quest);
    }

    /// <summary>The same wizard, sent into and arrived in their private copy.</summary>
    private static (Wizard Wizard, QuestTemplate Quest) InCopy(World world, ShadowSchoolMod mod) {
        var (wizard, quest) = AtDuel(world, mod);
        Look(mod, wizard, AtAnchor);
        Arrive(mod, wizard, FireSchool);
        Assert.Equal(ConquestEncounters.Phase.InCopy, mod.Encounters.SessionOf(wizard.CharId)!.Phase);

        return (wizard, quest);
    }

    private static void Arrive(ShadowSchoolMod mod, Wizard wizard, string zone) {
        wizard.Zone = zone;
        mod.OnAttached(SessionOf(mod, wizard), wizard);
    }

    // A step to the spot, then the deliberate look (standing still for the focus delay).
    private static void Look(ShadowSchoolMod mod, Wizard wizard, (float X, float Y) at) {
        // The mod's OnStill passes the wizard's own location on; a test wizard has no game object to keep it in.
        var session = SessionOf(mod, wizard);
        mod.OnMoved(session, wizard, at.X, at.Y, 0);
        mod.Encounters.OnStill(session, wizard, at.X, at.Y, UmbralPerception.FocusDelay);
    }

    // Each mod a test made, with its world (the sessions the presence events carry come from it).
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ShadowSchoolMod, World> s_worlds = [];

    private static IActorRef SessionOf(ShadowSchoolMod mod, Wizard wizard)
        => s_worlds.TryGetValue(mod, out var world) ? world.Session(wizard) : throw new InvalidOperationException("not a test mod");

    private static void Accept(Wizard wizard, QuestTemplate quest, ShadowSchoolMod mod) {
        wizard.QuestBehavior.AddQuest(new QuestInstance(quest, wizard.CharId));
        QuestProgressEvents.QuestStarted(wizard, quest, QuestProgressSource.Play, [mod]);
    }

    /// <summary>A goal completed the way QuestService does it: saved on the quest, the next goal started, then reported.</summary>
    private static void CompleteGoal(Wizard wizard, QuestTemplate quest, string goalName, QuestProgressSource source, ShadowSchoolMod mod) {
        var instance = wizard.QuestBehavior.CurrentQuestInstances.Single(q => q.QuestName == quest.m_questName);
        instance.CompleteGoal(goalName);
        var logic = quest.m_goalLogic.Single(l => (string) l.m_goalsAND.Single() == goalName);
        foreach (var next in logic.m_goalsToAdd) {
            instance.StartGoal(next);
        }

        QuestProgressEvents.GoalCompleted(wizard, quest, Goal(quest, goalName), source, [mod]);
    }

    private static void CompleteQuest(Wizard wizard, QuestTemplate quest, QuestProgressSource source, ShadowSchoolMod mod) {
        Assert.True(wizard.QuestBehavior.CompleteQuest(quest.m_questName));
        QuestProgressEvents.QuestCompleted(wizard, quest, source, [mod]);
    }

    private static GoalTemplate Goal(QuestTemplate quest, string name) => quest.m_goals.Single(g => g.m_goalName == name);

    /// <summary>Everything the encounter asks for: the school, Umbral Sight, the Nightside and Vespera's lesson.</summary>
    private static Wizard Ready(MagicSchool school) {
        var wizard = NewWizard(school);
        wizard.QuestBehavior.SetRegistryValue(UmbralSight.STATE_KEY, UmbralSight.UNLOCKED_BY_WALK);
        wizard.QuestBehavior.SetQuestRegistryValue(UmbralSight.ORIGIN_QUEST, "Complete", 1);
        wizard.QuestBehavior.SetQuestRegistryValue(ConquestEncounters.LESSON_QUEST, "Complete", 1);

        return wizard;
    }

    private static Wizard NewWizard(MagicSchool school) => new() {
        CharId = (ulong) Random.Shared.NextInt64(1, long.MaxValue),
        MagicSchoolBehavior = new ServerMagicSchoolBehavior { MagicSchool = school, Level = 12 },
        QuestBehavior = new ServerQuestBehavior(),
    };

    private static Wizard Reload(Wizard wizard) {
        var behavior = new ServerQuestBehavior();
        var registry = JsonSerializer.Deserialize<Dictionary<string, ulong>>(JsonSerializer.Serialize(wizard.QuestBehavior.Registry))!;
        foreach (var (key, value) in registry) {
            behavior.Registry[key] = value;
        }

        return new Wizard {
            CharId = wizard.CharId,
            Zone = wizard.Zone,
            MagicSchoolBehavior = new ServerMagicSchoolBehavior {
                MagicSchool = wizard.MagicSchoolBehavior.MagicSchool, Level = wizard.MagicSchoolBehavior.Level,
            },
            QuestBehavior = behavior,
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private static ZoneObjects.ZoneObjectFile LoadRoom()
        => JsonSerializer.Deserialize<ZoneObjects.ZoneObjectFile>(File.ReadAllText(Path.Combine(ModFolder(), "spiraldb",
            "ZoneObjects", $"WizardCity-Interiors-WC_SchoolFire@{Variant}.json")), JsonOptions)!;

    private static QuestTemplate LoadQuest()
        => JsonSerializer.Deserialize<QuestTemplate>(File.ReadAllText(Path.Combine(ModFolder(), "spiraldb", "QuestTemplates",
            $"questtemplates_{Quest}.json")), SpiralJsonOptions.Options)!;

    private static Dictionary<string, string> Strings()
        => ShadowText.Load(ModFolder()).Section(ShadowSchoolContent.LANG_NAME).ToDictionary(e => e.Key, e => e.Text);

    private static string ModFolder() => Path.Combine(RepoRoot(), "src", "Imlight.Director", "mods", "ShadowSchool");

    private static string RepoRoot() {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Imlight.Director", "mods", "ShadowSchool"))) {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not find the repository above the test folder.");
    }

    private static void InitializeIsolatedTestConfiguration() {
        var path = Path.Combine(Path.GetTempPath(), $"imlight-shadow-conquest-tests-{Environment.ProcessId}-{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, $"[Logging]{Environment.NewLine}LogPath = {Path.Combine(Path.GetTempPath(), "imlight-shadow-conquest-tests.log")}{Environment.NewLine}LogLevel = ERROR{Environment.NewLine}SeqSinkUrl = http://127.0.0.1:1{Environment.NewLine}");
        ConfigurationManager.Initialize(path);
        File.Delete(path);
    }

}
