using System.Text.Json;
using System.Threading;
using Imcodec.Cryptography;
using Imcodec.MessageLayer.Generated;
using Imcodec.ObjectProperty.TypeCache;
using Imlight.Common;
using Imlight.CoreLib.Game.Combat;
using Imlight.CoreLib.Game.Quests;
using Imlight.CoreLib.Game.Zone.Core;
using Imlight.CoreLib.Game.World;
using Imlight.CoreLib.Mods.ShadowSchool;
using Imlight.CoreLib.Shared.Packets;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.WizardData;
using Imlight.CoreLib.WizardData.Models.Player;
using Xunit;

namespace Imlight.CoreLib.Tests.Mods.ShadowSchool;

/// <summary>
/// Stage D2: the overarching conquest quest and its second encounter, Lydia Greyrose. Server-side rules and data
/// contracts only: a battle is never played, and the client is not involved. The real ShadowSchoolMod is driven the way
/// the server drives it, over the mod's own quests, with a recording world in place of the server.
/// </summary>
[Collection("SharedConfiguration")]
public sealed partial class ConquestCampaignTests {

    private const string Campaign = "ShadowSchool-Conquest";
    private const string FalmeaQuest = "ShadowSchool-Conquest-Falmea";
    private const string GreyroseQuest = "ShadowSchool-Conquest-Greyrose";
    private const string FireSchool = "WizardCity/Interiors/WC_SchoolFire";
    private const string FrostSchool = "WizardCity/Interiors/WC_SchoolFrost";
    private const string Ravenwood = "WizardCity/WC_Ravenwood";
    private const string FireVariant = "ShadowConquest-Falmea";
    private const string FrostVariant = "ShadowConquest-Greyrose";
    private const int GreyroseCustom = 1880002;
    private const ulong GreyroseTrainer = 38172;
    private const ulong GreyroseCombat = 1475612;
    private const ulong FalmeaTrainer = 38208;
    private const ulong FalmeaCombat = 403089;

    // At Greyrose's place in the Frost School (761.52, 20.66, 147.81), and across the room.
    private static readonly (float X, float Y) GreyroseAnchor = (765, 25);
    private static readonly (float X, float Y) GreyroseFar = (200, 500);
    private static readonly (float X, float Y) FalmeaAnchor = (790, 20);

    private static readonly ConquestEncounter Falmea = ConquestEncounters.ById("Falmea")!;
    private static readonly ConquestEncounter Greyrose = ConquestEncounters.ById("Greyrose")!;

    private static readonly MagicSchool[] OriginalSchools = [
        MagicSchool.Fire, MagicSchool.Ice, MagicSchool.Storm, MagicSchool.Myth,
        MagicSchool.Life, MagicSchool.Death, MagicSchool.Balance,
    ];

    // ------------------------------------------------------------------ The overarching quest

    [Fact]
    public void TheCampaignIsShadowOnlyWithSevenOrderedMilestonesAndNoRewards() {
        var quest = LoadQuest(Campaign);
        var strings = Strings();
        var requirements = quest.m_requirements.m_requirements;

        Assert.Contains(requirements, r => r is ReqIsSchool { m_applyNOT: false } s && (string) s.m_magicSchoolName == "Shadow");
        Assert.Contains(requirements, r => r is ReqHasEntry { m_isQuestRegistry: false } e
            && (string) e.m_entryName == UmbralSight.STATE_KEY);
        foreach (var done in new[] { UmbralSight.ORIGIN_QUEST, ConquestEncounters.LESSON_QUEST }) {
            Assert.Contains(requirements, r => r is ReqHasEntry { m_isQuestRegistry: true } e
                && (string) e.m_entryName == "Complete" && (string) e.m_questName == done);
        }

        Assert.Equal(ConquestEncounters.CampaignMilestones, quest.m_goals.Select(g => ((string) g.m_goalName).Split('-')[^1]).ToArray());
        Assert.Equal(ConquestEncounters.CampaignMilestones.Select(ConquestEncounters.CampaignGoal),
            quest.m_goals.Select(g => (string) g.m_goalName).ToArray());
        Assert.Equal([ConquestEncounters.CampaignGoal("Falmea")], quest.m_startGoals.Select(g => (string) g).ToArray());
        Assert.Equal(7, quest.m_goalLogic.Count);
        // Six entries add the next milestone; the seventh adds none and completes the quest (nothing follows until Stage D3).
        Assert.All(quest.m_goalLogic.Take(6), l => Assert.False(l.m_completeQuest));
        Assert.True(quest.m_goalLogic[^1].m_completeQuest);
        for (var i = 0; i + 1 < quest.m_goalLogic.Count; i++) {
            Assert.Equal([ConquestEncounters.CampaignGoal(ConquestEncounters.CampaignMilestones[i + 1])],
                quest.m_goalLogic[i].m_goalsToAdd.Select(g => (string) g).ToArray());
        }
        Assert.Empty(quest.m_goalLogic[^1].m_goalsToAdd);

        // Each milestone is completed only by the encounter's verified defeat event; no creature adjective can match.
        foreach (var (goal, id) in quest.m_goals.Zip(ConquestEncounters.CampaignMilestones)) {
            var bounty = Assert.IsType<BountyGoalTemplate>(goal);
            Assert.Equal([$"ShadowSchool_Conquest_{id}_Defeated"], bounty.m_genericEvents.Select(e => (string) e));
            Assert.Empty(bounty.m_npcAdjectives);
            Assert.Null(bounty.m_tallyCounter);
            Assert.Equal(StringHash.Compute(goal.m_goalName), (uint) goal.m_goalNameID);
            Assert.True(strings.ContainsKey(((string) goal.m_goalTitle)["ShadowSchool_".Length..]));
        }

        Assert.Empty(quest.m_startResults.m_results);
        Assert.Empty(quest.m_endResults.m_results);
        Assert.All(quest.m_goals, g => Assert.Empty(g.m_completeResults.m_results));
        Assert.All(quest.m_goals, g => Assert.Empty(g.m_activateResults.m_results));
        foreach (var key in new[] { (string) quest.m_questTitle, (string) quest.m_questInfo }) {
            Assert.True(strings.ContainsKey(key["ShadowSchool_".Length..]), key);
        }
        Assert.InRange(strings["ConquestInfo"].Length, 40, 320);
    }

    [Fact]
    public void TheCampaignIsOfferedByVesperaAndTheTwoImplementedMilestonesNavigateToTheirSchools() {
        var quest = LoadQuest(Campaign);
        var prep = ((ActorDialogList) quest.m_dialogList).m_dialogs.Single(d => d.m_dialogTag == "Prep");
        Assert.All(prep.m_dialogEntries, e => Assert.Equal(ShadowSchoolMod.TUTOR_TEMPLATE_ID, (uint) e.m_actorTemplateID));
        Assert.All(prep.m_dialogEntries, e => Assert.Equal("ShadowSchool-Tutor_Persona", (string) ((NPCDialogEntry) e).m_personaName));

        var falmea = quest.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal("Falmea"));
        var greyrose = quest.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal("Greyrose"));
        Assert.Equal("ShadowSchool_ConquestFalmeaLocation", (string) falmea.m_locationName);
        Assert.Equal(FireSchool, (string) falmea.m_destinationZone);
        Assert.Equal("ShadowSchool_ConquestGreyroseLocation", (string) greyrose.m_locationName);
        Assert.Equal(FrostSchool, (string) greyrose.m_destinationZone);
        Assert.All(quest.m_goals, g => Assert.False(string.IsNullOrWhiteSpace((string) g.m_locationName)));

        var strings = Strings();
        Assert.Contains("Ice School", strings["ConquestGreyroseLocation"]);
        Assert.Contains("binding", strings["ConquestOffer"] + strings["ConquestTerms"]);
    }

    [Fact]
    public void AVictoryInTheFireCopyAdvancesOnlyFalmeasCampaignMilestone() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var chapters = Accept(wizard, LoadQuest(FalmeaQuest), mod);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);
        EnterFalmea(world, mod, wizard);

        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, FireSchool, FireVariant, FalmeaCombat, 1880001), [mod]);

        var defeat = Assert.Single(world.Events, e => e.Event == Falmea.DefeatEvent);
        Assert.Equal(wizard.CharId, defeat.CharId);
        Assert.True(ConquestEncounters.ActiveCampaign(wizard)!.IsGoalActive(ConquestEncounters.CampaignGoal("Falmea")));
        Assert.False(ConquestEncounters.ActiveCampaign(wizard)!.IsGoalActive(ConquestEncounters.CampaignGoal("Greyrose")));

        // The server's event path completes every active goal that names the event: the chapter duel and the campaign
        // milestone. The chapter registry advances once, from its own goal's play source.
        CompleteEvent(wizard, chapters, Falmea.DefeatEvent, QuestProgressSource.Play, mod);
        CompleteEvent(wizard, campaign, Falmea.DefeatEvent, QuestProgressSource.Play, mod);
        Assert.True(ConquestEncounters.IsMilestoneComplete(wizard, "Falmea"));
        Assert.False(ConquestEncounters.IsMilestoneComplete(wizard, "Greyrose"));
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, Falmea));

        // The remaining milestones are still locked: only Greyrose's became active.
        var instance = ConquestEncounters.ActiveCampaign(wizard)!;
        Assert.True(instance.IsGoalActive(ConquestEncounters.CampaignGoal("Greyrose")));
        foreach (var id in ConquestEncounters.CampaignMilestones.Skip(2)) {
            Assert.False(instance.IsGoalActive(ConquestEncounters.CampaignGoal(id)));
            Assert.False(instance.IsGoalCompleted(ConquestEncounters.CampaignGoal(id)));
        }
    }

    [Fact]
    public void TheCampaignMilestoneIsPostedEvenAfterTheChapterQuestIsDropped() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var chapters = Accept(wizard, LoadQuest(FalmeaQuest), mod);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);
        EnterFalmea(world, mod, wizard);

        // D1.6's coupling: the event used to need the chapter's duel goal to be active. Drop the chapter quest now and
        // the verified win must still reach the campaign.
        wizard.QuestBehavior.CurrentQuestInstances.RemoveAll(q => q.QuestName == chapters.m_questName);
        Assert.Null(ConquestEncounters.ActiveQuest(wizard, Falmea));

        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, FireSchool, FireVariant, FalmeaCombat, 1880001), [mod]);

        Assert.Single(world.Events, e => e.Event == Falmea.DefeatEvent);
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, Falmea));
        CompleteEvent(wizard, campaign, Falmea.DefeatEvent, QuestProgressSource.Play, mod);
        Assert.True(ConquestEncounters.IsMilestoneComplete(wizard, "Falmea"));
    }

    [Fact]
    public void RepeatingTheSameVictoryPostsNoSecondMilestoneEvent() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        Accept(wizard, LoadQuest(FalmeaQuest), mod);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);
        EnterFalmea(world, mod, wizard);

        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, FireSchool, FireVariant, FalmeaCombat, 1880001), [mod]);
        CompleteEvent(wizard, campaign, Falmea.DefeatEvent, QuestProgressSource.Play, mod);
        var events = world.Events.Count;

        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, FireSchool, FireVariant, FalmeaCombat, 1880001), [mod]);
        Assert.Equal(events, world.Events.Count);
        Assert.Single(world.Events, e => e.Event == Falmea.DefeatEvent);
    }

    // ------------------------------------------------------------------ Greyrose, encounter two

    [Fact]
    public void GreyroseIsOrderTwoAndIsLockedUntilFalmeaIsComplete() {
        InitializeIsolatedTestConfiguration();
        var wizard = Ready(MagicSchool.Shadow);
        Assert.Equal(2, Greyrose.Order);
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, Greyrose));

        CompleteFalmeaChapter(wizard, mod: null);
        Assert.True(ConquestEncounters.IsComplete(wizard, Falmea));
        Assert.True(ConquestEncounters.MeetsPrerequisites(wizard, Greyrose));
        Assert.Equal([1, 2, 3, 4, 5, 6, 7], ConquestEncounters.Encounters.Select(e => e.Order).ToArray());
        Assert.Null(ConquestEncounters.ById("Ambrose"));
    }

    [Fact]
    public void GreyrosesAnchorIsAnUmbralSightBindingAtHerPlace() {
        InitializeIsolatedTestConfiguration();
        var binding = UmbralPerception.Bindings.Single(b => b.Id == Greyrose.AnchorBinding);

        Assert.Equal(FrostSchool, binding.Zone);
        Assert.Equal(761, binding.X);
        Assert.Equal(21, binding.Y);
        Assert.True(ConquestEncounters.AtAnchor(Greyrose, GreyroseAnchor.X, GreyroseAnchor.Y));
        Assert.False(ConquestEncounters.AtAnchor(Greyrose, GreyroseFar.X, GreyroseFar.Y));
        Assert.Equal("Umbral_FrostClassroom", binding.TextKey);
        Assert.Contains("binding", Strings()[binding.TextKey]);
    }

    [Fact]
    public void TheGreyroseQuestIsHerOwnChapterWithFrostNavigationAndNoRewards() {
        var quest = LoadQuest(GreyroseQuest);
        var strings = Strings();

        Assert.Equal([Greyrose.AnchorGoal], quest.m_startGoals.Select(g => (string) g).ToArray());
        Assert.Equal([Greyrose.AnchorGoal, Greyrose.DuelGoal, Greyrose.ReturnGoal],
            quest.m_goalLogic.Select(l => (string) l.m_goalsAND.Single()).ToArray());
        Assert.Equal([Greyrose.DuelGoal, Greyrose.ReturnGoal],
            quest.m_goalLogic.Take(2).Select(l => (string) l.m_goalsToAdd.Single()).ToArray());
        Assert.True(quest.m_goalLogic[^1].m_completeQuest);
        foreach (var goal in quest.m_goals) {
            Assert.Equal(StringHash.Compute(goal.m_goalName), (uint) goal.m_goalNameID);
            Assert.False(string.IsNullOrWhiteSpace((string) goal.m_locationName));
            Assert.Equal(goal.m_goalName == Greyrose.ReturnGoal ? "WizardCity/Interiors/WC_LibraryBookStacks" : FrostSchool,
                (string) goal.m_destinationZone);
            Assert.True(strings.ContainsKey(((string) goal.m_goalTitle)["ShadowSchool_".Length..]), (string) goal.m_goalTitle);
        }

        var anchor = Assert.IsType<WaypointGoalTemplate>(Goal(quest, Greyrose.AnchorGoal));
        Assert.Equal([Greyrose.AnchorEvent], anchor.m_genericEvents.Select(e => (string) e));
        Assert.Equal("ShadowSchool-Conquest-Greyrose-Anchor", (string) anchor.m_proximityTag);

        var duel = Assert.IsType<BountyGoalTemplate>(Goal(quest, Greyrose.DuelGoal));
        Assert.Equal([Greyrose.DefeatEvent], duel.m_genericEvents.Select(e => (string) e));
        Assert.Empty(duel.m_npcAdjectives);
        Assert.Null(duel.m_tallyCounter);
        Assert.Equal("ShadowSchool_ConquestGreyroseGoalDuel", (string) duel.m_goalTitle);

        var back = Assert.IsType<PersonaGoalTemplate>(Goal(quest, Greyrose.ReturnGoal));
        Assert.Equal(ShadowSchoolMod.TUTOR_OBJECT_NAME, (string) back.m_personaName);
        Assert.Equal("WizardCity/Interiors/WC_LibraryBookStacks", (string) back.m_destinationZone);

        Assert.Empty(quest.m_startResults.m_results);
        Assert.Empty(quest.m_endResults.m_results);
        Assert.All(quest.m_goals, g => Assert.Empty(g.m_completeResults.m_results));
        var prep = ((ActorDialogList) quest.m_dialogList).m_dialogs.Single(d => d.m_dialogTag == "Prep");
        Assert.All(prep.m_dialogEntries, e => Assert.Equal(ShadowSchoolMod.TUTOR_TEMPLATE_ID, (uint) e.m_actorTemplateID));
    }

    [Fact]
    public void TheGreyroseEncounterReloadsIntoItsOwnCopyAndLeavesToRavenwood() {
        InitializeIsolatedTestConfiguration();
        var wizard = Ready(MagicSchool.Shadow);
        var seen = new List<object>();
        var session = s_system.ActorOf(Akka.Actor.Props.Create(() => new Capture(seen)));
        var live = new ConquestWorld(_ => { });

        try {
            live.Enter(session, wizard, Greyrose);
            WaitFor(seen, 1);
            var enter = Assert.IsType<ZONE_102_PROTOCOL.MSG_ZONETRANSFER>(seen[0]);
            Assert.Equal(FrostSchool, enter.DestinationZone);
            Assert.Equal(Greyrose.ArrivalLoc, enter.DestinationLocation);
            Assert.True(enter.ForceReload);
            Assert.True(enter.IsPrivate);
            Assert.True(enter.ResetInstance);
            Assert.Equal(wizard.CharId, enter.OwnerCharId);
            Assert.Equal(FrostVariant, enter.Variant);
            Assert.Equal(FrostSchool, ZoneVariants.Of(wizard.CharId)?.Zone);
            Assert.Equal(FrostVariant, ZoneVariants.Of(wizard.CharId)?.Variant);

            live.Leave(session, wizard, Greyrose, TimeSpan.Zero);
            WaitFor(seen, 2);
            var exit = Assert.IsType<ZONE_102_PROTOCOL.MSG_ZONETRANSFER>(seen[1]);
            Assert.Equal(Ravenwood, exit.DestinationZone);
            Assert.Equal(Greyrose.ExitLoc, exit.DestinationLocation);
            Assert.Null(ZoneVariants.Of(wizard.CharId));
        }
        finally {
            s_system.Stop(session);
        }
    }

    [Fact]
    public void ACharacterHasAtMostOneEncounterSession() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var (wizard, _) = InCopyFalmea(world, mod);

        Look(mod, wizard, FalmeaAnchor);

        Assert.Single(world.Entered);
        Assert.Equal(Falmea.Id, mod.Encounters.SessionOf(wizard.CharId)!.EncounterId);
    }

    [Fact]
    public void ALaterMilestoneIsLockedAndCannotBeCompletedByAnEventBeforeTheOnesBeforeIt() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);

        Assert.NotNull(ConquestEncounters.ById("Wu"));
        Assert.False(ConquestEncounters.ActiveCampaign(wizard)!.IsGoalActive(ConquestEncounters.CampaignGoal("Wu")));
        CompleteEvent(wizard, campaign, "ShadowSchool_Conquest_Wu_Defeated", QuestProgressSource.Play, mod);
        Assert.False(ConquestEncounters.IsMilestoneComplete(wizard, "Wu"));
    }

    [Fact]
    public void TheGreyroseRoomIsAPrivateFrostCopyWithHerAtHerPlaceAndTheCircleOnHerPlatform() {
        var room = LoadRoom();

        Assert.Equal(FrostSchool, room.Zone);
        Assert.Equal(FrostVariant, room.Variant);
        Assert.Equal(FrostSchool, Greyrose.Zone);
        Assert.Equal(FrostVariant, Greyrose.Variant);
        Assert.Equal($"{FrostSchool.Replace('/', '-')}@{FrostVariant}.json", ZoneObjects.FileNameOf(room.Zone, room.Variant));

        Assert.Equal([GreyroseTrainer], room.Remove);
        var creature = Assert.Single(room.Creatures);
        Assert.Equal(GreyroseCombat, creature.TemplateID);
        Assert.Equal(761.52f, creature.X, 0.5f);
        Assert.Equal(20.66f, creature.Y, 0.5f);
        Assert.InRange(creature.Z, 147f, 149f);
        Assert.Equal(GreyroseCustom, room.ReplaceSpawns[GreyroseCombat.ToString()]);

        var circle = Assert.Single(room.Objects);
        Assert.Equal(560ul, circle.TemplateID);
        Assert.Equal("CombatSigil8Actor", circle.CombatSigil.SigilType);
        Assert.InRange(MathF.Sqrt(MathF.Pow(circle.X - creature.X, 2) + MathF.Pow(circle.Y - creature.Y, 2)), 0, circle.CombatSigil.Radius);
        Assert.InRange(circle.Z, creature.Z - 1f, creature.Z + 1f);

        Assert.Equal(Ravenwood, room.Exit.Zone);
        Assert.Equal(Greyrose.ExitLoc, room.Exit.Loc);
        Assert.Empty(room.Exit.Triggers);
    }

    [Fact]
    public void GreyrosesProfileIsADefensiveIceFightRatherThanFalmeasFireTempo() {
        InitializeIsolatedTestConfiguration();
        var json = File.ReadAllText(Path.Combine(ModFolder(), "spiraldb", "Customs", $"{GreyroseCustom}.json"));
        var profile = BossOverrides.Parse(json, Customs.Key(GreyroseCustom))!;

        Assert.Equal(GreyroseCustom, profile.CustomID);
        Assert.Equal(GreyroseCombat, profile.BaseTemplateID);
        Assert.Equal("Ice", profile.School);
        Assert.Equal(11, profile.Level);
        Assert.InRange(profile.Health!.Value, 800, 1400);
        Assert.Equal(0, profile.StartingPips);
        Assert.Equal(0f, profile.PowerPipChance);
        Assert.Equal(0f, profile.FlatDamage);
        Assert.Equal(0, profile.XP);
        Assert.Equal(0, profile.GoldMax);
        Assert.Empty(profile.Drops!);
        Assert.Empty(BossOverrides.DropTablesFor(profile, ["base-creature-drop"]));
        Assert.Equal(["base-creature-drop"], BossOverrides.DropTablesFor(new BossOverrides.BossOverride(), ["base-creature-drop"]));
        foreach (var row in new[] { profile.DamageBySchool, profile.ResistBySchool, profile.PierceBySchool, profile.CriticalBySchool, profile.BlockBySchool }) {
            Assert.All(row!.Values, v => Assert.Equal(0f, v));
        }

        // Verified Ice spell templates: damage, shields and one controlled stun; no Fire spell and no unsupported id.
        Assert.Equal([1496157882u, 219826034u, 829983860u, 1050086956u, 432324968u, 2032132496u, 1887199642u],
            profile.Deck!.Select(c => c.SpellID).ToArray());
        Assert.Contains(profile.Deck, c => c.SpellID == 432324968 && c.Copies == 2);
        Assert.Contains(profile.Deck, c => c.SpellID == 1887199642 && c.Copies == 1);
        Assert.DoesNotContain(profile.Deck, c => c.SpellID is 103007158 or 1243857701 or 1599065470);
    }

    [Fact]
    public void AVictoryInTheGreyroseCopyAdvancesExactlyHerMilestoneAndHerChapter() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        CompleteFalmeaChapter(wizard, mod);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);
        CompleteEvent(wizard, campaign, Falmea.DefeatEvent, QuestProgressSource.Play, mod);
        var chapter = Accept(wizard, LoadQuest(GreyroseQuest), mod);
        EnterGreyrose(world, mod, wizard);
        var events = world.Events.Count;

        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, FrostSchool, FrostVariant, GreyroseCombat, GreyroseCustom), [mod]);

        Assert.Single(world.Events.Skip(events), e => e.Event == Greyrose.DefeatEvent);
        CompleteEvent(wizard, chapter, Greyrose.DefeatEvent, QuestProgressSource.Play, mod);
        CompleteEvent(wizard, campaign, Greyrose.DefeatEvent, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, Greyrose));
        Assert.True(ConquestEncounters.IsMilestoneComplete(wizard, "Falmea"));
        Assert.True(ConquestEncounters.IsMilestoneComplete(wizard, "Greyrose"));

        // The next instructor is now the active milestone, but its encounter stays locked until Greyrose's chapter completes.
        Assert.True(ConquestEncounters.ActiveCampaign(wizard)!.IsGoalActive(ConquestEncounters.CampaignGoal("Balestrom")));
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, ConquestEncounters.ById("Balestrom")!));
        Assert.False(ConquestEncounters.IsMilestoneComplete(wizard, "Balestrom"));
    }

    [Theory]
    [InlineData("lost")]
    [InlineData("fled")]
    [InlineData("left")]
    public void ALossAFleeOrADisconnectNeverPostsTheDefeatEvent(string how) {
        var result = how switch { "lost" => BattleResult.Lost, "fled" => BattleResult.Fled, _ => BattleResult.Left };
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        CompleteFalmeaChapter(wizard, mod);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);
        CompleteEvent(wizard, campaign, Falmea.DefeatEvent, QuestProgressSource.Play, mod);
        Accept(wizard, LoadQuest(GreyroseQuest), mod);
        EnterGreyrose(world, mod, wizard);
        var events = world.Events.Count;

        BattleOutcomeEvents.Report(Outcome(world, wizard, result, FrostSchool, FrostVariant, GreyroseCombat, GreyroseCustom), [mod]);

        Assert.Equal(events, world.Events.Count);
        Assert.False(ConquestEncounters.IsMilestoneComplete(wizard, "Greyrose"));
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, Greyrose));
    }

    [Theory]
    [InlineData("public-zone")]
    [InlineData("not-an-instance")]
    [InlineData("another-variant")]
    [InlineData("no-session")]
    [InlineData("another-creature")]
    public void OnlyTheOwnersWinInTheFrostCopyOverGreyroseCounts(string why) {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        CompleteFalmeaChapter(wizard, mod);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);
        CompleteEvent(wizard, campaign, Falmea.DefeatEvent, QuestProgressSource.Play, mod);
        Accept(wizard, LoadQuest(GreyroseQuest), mod);
        EnterGreyrose(world, mod, wizard);
        var events = world.Events.Count;
        if (why == "no-session") {
            mod.OnDetached(wizard.CharId);
        }

        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won,
            FrostSchool,
            why is "another-variant" ? "RavenwoodMalistaire" : why is "public-zone" ? "" : FrostVariant,
            GreyroseCombat,
            why is "another-creature" ? 0 : GreyroseCustom,
            why is not ("public-zone" or "not-an-instance")), [mod]);

        Assert.Equal(events, world.Events.Count);
        Assert.False(ConquestEncounters.IsMilestoneComplete(wizard, "Greyrose"));
    }

    [Fact]
    public void AnotherCharactersWinInTheCopyCountsForNobody() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var owner = Ready(MagicSchool.Shadow);
        CompleteFalmeaChapter(owner, mod);
        var campaign = Accept(owner, LoadQuest(Campaign), mod);
        CompleteEvent(owner, campaign, Falmea.DefeatEvent, QuestProgressSource.Play, mod);
        Accept(owner, LoadQuest(GreyroseQuest), mod);
        EnterGreyrose(world, mod, owner);
        var other = Ready(MagicSchool.Shadow);
        var events = world.Events.Count;

        // The other wizard is not the session's owner, even if they stand in the same copy's zone.
        mod.OnBattleEnded(Outcome(world, other, BattleResult.Won, FrostSchool, FrostVariant, GreyroseCombat, GreyroseCustom));

        Assert.Equal(events, world.Events.Count);
        Assert.False(ConquestEncounters.IsMilestoneComplete(owner, "Greyrose"));
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(owner, Greyrose));
    }

    [Theory]
    [MemberData(nameof(Originals))]
    public void AnOriginalSchoolWizardCannotAcceptOrAdvanceTheCampaign(MagicSchool school) {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(school);
        var campaign = LoadQuest(Campaign);

        Assert.NotEqual(MagicSchool.Shadow, school);
        Assert.Contains(campaign.m_requirements.m_requirements, r => r is ReqIsSchool s
            && (string) s.m_magicSchoolName == "Shadow" && !s.m_applyNOT);

        Accept(wizard, campaign, mod);
        Accept(wizard, LoadQuest(FalmeaQuest), mod);
        Arrive(mod, world, wizard, FireSchool);
        Look(mod, wizard, FalmeaAnchor);
        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, FireSchool, FireVariant, FalmeaCombat, 1880001), [mod]);

        Assert.Empty(world.Events);
        Assert.Empty(world.Entered);
        Assert.False(ConquestEncounters.IsMilestoneComplete(wizard, "Falmea"));
        Assert.Equal(ConquestStage.None, ConquestEncounters.StageOf(wizard, Falmea));
    }

    public static IEnumerable<object[]> Originals() => OriginalSchools.Select(s => new object[] { s });

    [Fact]
    public void ZoneEventReplayPreservesProviderOrderAndDeduplicatesEvents() {
        var wizard = Ready(MagicSchool.Shadow);
        var quest = LoadQuest(Campaign);
        var replay = QuestProgressEvents.GetZoneEventsToReplay(wizard, quest,
            [new ReplayProvider(["first", "second", "first"])]);

        Assert.Equal(["first", "second"], replay);
    }

    [Fact]
    public void ACommandCompletionOfAMilestoneDoesNotUnlockTheEncounterOrTheFinale() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);

        var milestone = campaign.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal("Falmea"));
        Assert.False(QuestProgressEvents.CanCompleteGoalFromZoneEvent(wizard, campaign, milestone, Falmea.DefeatEvent, [mod]));
        Assert.False(QuestProgressEvents.CanCompleteGoalFromCommand(wizard, campaign, milestone, [mod]));
        Assert.False(QuestProgressEvents.CanCompleteQuestFromCommand(wizard, campaign, [mod]));
        var chapter = LoadQuest(FalmeaQuest);
        Assert.False(QuestProgressEvents.CanCompleteGoalFromZoneEvent(wizard, chapter, Goal(chapter, Falmea.DuelGoal), Falmea.DefeatEvent, [mod]));
        Assert.False(QuestProgressEvents.CanCompleteGoalFromCommand(wizard, chapter, Goal(chapter, Falmea.DuelGoal), [mod]));
        Assert.False(QuestProgressEvents.CanCompleteQuestFromCommand(wizard, chapter, [mod]));
        Assert.Empty(QuestProgressEvents.GetZoneEventsToReplay(wizard, campaign, [mod]));

        CompleteGoal(wizard, campaign, ConquestEncounters.CampaignGoal("Falmea"), QuestProgressSource.Command, mod);
        CompleteEvent(wizard, campaign, Falmea.DefeatEvent, QuestProgressSource.Command, mod);

        // The registry is the gate, not the quest flag: a QA completion neither completes the encounter nor unlocks the
        // next instructor, and the campaign remains a data-only flag.
        Assert.Equal(ConquestStage.None, ConquestEncounters.StageOf(wizard, Falmea));
        Assert.False(ConquestEncounters.IsComplete(wizard, Falmea));
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, Greyrose));
        Assert.Null(ConquestEncounters.ById("Ambrose"));
        Assert.DoesNotContain(ConquestEncounters.Encounters, e => e.Order > 7);
        Assert.DoesNotContain(LoadQuest(Campaign).m_goals, g => (string) g.m_goalName == "ShadowSchool-Conquest-Milestone-Ambrose");
    }

    [Fact]
    public void ThePublicFrostSchoolAndGreyroseTrainerAreUntouched() {
        var zoneObjects = Path.Combine(ModFolder(), "spiraldb", "ZoneObjects");
        var files = Directory.GetFiles(zoneObjects, "*.json");

        Assert.DoesNotContain(files, f => Path.GetFileName(f).Equals("WizardCity-Interiors-WC_SchoolFrost.json", StringComparison.OrdinalIgnoreCase));
        foreach (var file in files) {
            var room = JsonSerializer.Deserialize<ZoneObjects.ZoneObjectFile>(File.ReadAllText(file), JsonOptions)!;
            if (room.Remove.Contains(GreyroseTrainer) || room.Creatures.Any(c => c.TemplateID == GreyroseCombat)) {
                Assert.False(string.IsNullOrEmpty(room.Variant), Path.GetFileName(file));
            }
        }

        var quests = Directory.GetFiles(Path.Combine(ModFolder(), "spiraldb", "QuestTemplates"), "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f)["questtemplates_".Length..]).ToList();
        Assert.DoesNotContain("WC-COMMONS-MAIN-002-ICE", quests);
        var chapter = LoadQuest(GreyroseQuest);
        Assert.DoesNotContain(chapter.m_goals.OfType<PersonaGoalTemplate>(), g => ((string) g.m_personaName).StartsWith("WC-RAV-NPC03"));
    }

    [Fact]
    public void AVerifiedVictorySurvivesADisconnectAndAYetUnfinishedEncounterCanBeRetried() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        CompleteFalmeaChapter(wizard, mod);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);
        CompleteEvent(wizard, campaign, Falmea.DefeatEvent, QuestProgressSource.Play, mod);
        Accept(wizard, LoadQuest(GreyroseQuest), mod);
        EnterGreyrose(world, mod, wizard);

        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, FrostSchool, FrostVariant, GreyroseCombat, GreyroseCustom), [mod]);
        CompleteEvent(wizard, campaign, Greyrose.DefeatEvent, QuestProgressSource.Play, mod);
        mod.OnDetached(wizard.CharId);
        Assert.True(ConquestEncounters.IsMilestoneComplete(wizard, "Greyrose"));

        // The server still notes the copy for them (the delayed return has not run); reconnecting in it sends them out.
        var events = world.Events.Count;
        world.Noted[wizard.CharId] = (FrostSchool, FrostVariant);
        Arrive(mod, world, wizard, FrostSchool);
        Assert.Equal((wizard.CharId, Greyrose.Id, TimeSpan.Zero), world.Left[^1]);
        Assert.Single(world.Events.Skip(events), e => e.Event == Greyrose.DefeatEvent);
        Assert.True(ConquestEncounters.IsMilestoneComplete(wizard, "Greyrose"));
    }

    [Fact]
    public void VictoryIsSavedBeforeTheEventAndReplayedAfterDisconnect() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var chapter = Accept(wizard, LoadQuest(FalmeaQuest), mod);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);
        EnterFalmea(world, mod, wizard);

        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, FireSchool, FireVariant, FalmeaCombat, 1880001), [mod]);

        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, Falmea));
        Assert.Equal(2, world.Saves);
        Assert.Single(world.Events, e => e.Event == Falmea.DefeatEvent);
        Assert.False(ConquestEncounters.IsMilestoneComplete(wizard, "Falmea"));

        mod.OnDetached(wizard.CharId);
        var events = world.Events.Count;
        mod.OnAttached(world.Session(wizard), wizard);

        Assert.Single(world.Events.Skip(events), e => e.Event == Falmea.DefeatEvent);
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, Falmea));
        Assert.True(QuestProgressEvents.CanCompleteGoalFromZoneEvent(wizard, campaign,
            campaign.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal("Falmea")), Falmea.DefeatEvent, [mod]));
        Assert.Equal([Falmea.AnchorEvent, Falmea.DefeatEvent], QuestProgressEvents.GetZoneEventsToReplay(wizard, chapter, [mod]));
        Assert.Contains(Falmea.DefeatEvent, QuestProgressEvents.GetZoneEventsToReplay(wizard, campaign, [mod]));
        Assert.True(ConquestEncounters.ActiveQuest(wizard, Falmea).IsGoalActive(Falmea.DuelGoal));

        wizard.QuestBehavior.CurrentQuestInstances.RemoveAll(q => q.QuestName == chapter.m_questName);
        Accept(wizard, LoadQuest(FalmeaQuest), mod);
        Assert.Equal([Falmea.AnchorEvent, Falmea.DefeatEvent], QuestProgressEvents.GetZoneEventsToReplay(wizard, chapter, [mod]));
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, Falmea));
        Assert.NotNull(chapter);
    }

    // ------------------------------------------------------------------ helpers

    private static readonly Akka.Actor.ActorSystem s_system = Akka.Actor.ActorSystem.Create("conquest-campaign-tests");

    private sealed class ReplayProvider(string[] events) : IQuestZoneEventValidator {
        public bool CanCompleteGoalFromZoneEvent(Wizard wizard, QuestTemplate quest, GoalTemplate goal, string eventName)
            => true;

        public IEnumerable<string> GetZoneEventsToReplay(Wizard wizard, QuestTemplate quest) => events;
    }

    private sealed class Capture : Akka.Actor.ReceiveActor {

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

    private sealed class World : IConquestWorld, IDisposable {

        private readonly Dictionary<ulong, Akka.Actor.IActorRef> _sessions = [];
        private readonly Dictionary<Akka.Actor.IActorRef, ulong> _owners = [];

        internal int Saves { get; private set; }
        internal List<(ulong CharId, string Event)> Events { get; } = [];
        internal List<(ulong CharId, IReadOnlyList<string> Notify, IReadOnlyList<string> Chat)> Shown { get; } = [];
        internal List<(ulong CharId, string Encounter)> Entered { get; } = [];
        internal List<(ulong CharId, string Encounter, TimeSpan Delay)> Left { get; } = [];
        internal Dictionary<ulong, (string Zone, string Variant)> Noted { get; } = [];
        internal DateTime Now { get; set; } = new(2026, 10, 23, 18, 0, 0, DateTimeKind.Utc);

        internal Akka.Actor.IActorRef Session(Wizard wizard) {
            if (!_sessions.TryGetValue(wizard.CharId, out var session)) {
                session = s_system.ActorOf(Akka.Actor.Props.Empty);
                _sessions[wizard.CharId] = session;
                _owners[session] = wizard.CharId;
            }

            return session;
        }

        public DateTime UtcNow => Now;

        public void Save(Wizard wizard) => Saves++;

        public void PostQuestEvent(Akka.Actor.IActorRef session, string eventName) => Events.Add((_owners[session], eventName));

        public void Show(Akka.Actor.IActorRef session, IReadOnlyList<string> notifyKeys, IReadOnlyList<string> chatKeys)
            => Shown.Add((_owners[session], notifyKeys, chatKeys));

        public void Enter(Akka.Actor.IActorRef session, Wizard wizard, ConquestEncounter encounter) {
            Entered.Add((wizard.CharId, encounter.Id));
            Noted[wizard.CharId] = (encounter.Zone, encounter.Variant);
        }

        public void Leave(Akka.Actor.IActorRef session, Wizard wizard, ConquestEncounter encounter, TimeSpan delay) {
            Left.Add((wizard.CharId, encounter.Id, delay));
            Noted.Remove(wizard.CharId);
        }

        public string NotedVariant(ulong charId, string zone)
            => Noted.TryGetValue(charId, out var noted) && noted.Zone == zone ? noted.Variant : "";

        public void Dispose() {
            foreach (var session in _sessions.Values) {
                s_system.Stop(session);
            }

            _sessions.Clear();
            _owners.Clear();
        }

    }

    private static ShadowSchoolMod Mod(World world) {
        var mod = new ShadowSchoolMod(_ => { }, world);
        s_worlds.AddOrUpdate(mod, world);

        return mod;
    }

    private static BattleOutcome Outcome(World world, Wizard wizard, BattleResult result, string zone, string variant,
                                         ulong creature, int customId, bool isInstance = true)
        => new(result, wizard, world.Session(wizard), zone, variant, isInstance,
            [new BattleCreature(creature, customId, 0x1A11_0000_0000_0002)]);

    private static QuestTemplate LoadQuest(string name)
        => JsonSerializer.Deserialize<QuestTemplate>(File.ReadAllText(Path.Combine(ModFolder(), "spiraldb", "QuestTemplates",
            $"questtemplates_{name}.json")), SpiralJsonOptions.Options)!;

    private static ZoneObjects.ZoneObjectFile LoadRoom()
        => JsonSerializer.Deserialize<ZoneObjects.ZoneObjectFile>(File.ReadAllText(Path.Combine(ModFolder(), "spiraldb",
            "ZoneObjects", $"WizardCity-Interiors-WC_SchoolFrost@{FrostVariant}.json")), JsonOptions)!;

    private static GoalTemplate Goal(QuestTemplate quest, string name) => quest.m_goals.Single(g => g.m_goalName == name);

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

    /// <summary>Falmea's chapter completed through play, which is what unlocks Greyrose (stage 3 and the quest done).</summary>
    private static void CompleteFalmeaChapter(Wizard wizard, ShadowSchoolMod mod) {
        if (mod is null) {
            wizard.QuestBehavior.SetRegistryValue(Falmea.StateKey, (ulong) ConquestStage.Complete);
            wizard.QuestBehavior.SetQuestRegistryValue(FalmeaQuest, "Complete", 1);

            return;
        }

        var quest = Accept(wizard, LoadQuest(FalmeaQuest), mod);
        CompleteGoal(wizard, quest, Falmea.AnchorGoal, QuestProgressSource.Play, mod);
        CompleteGoal(wizard, quest, Falmea.DuelGoal, QuestProgressSource.Play, mod);
        CompleteGoal(wizard, quest, Falmea.ReturnGoal, QuestProgressSource.Play, mod);
        CompleteQuest(wizard, quest, QuestProgressSource.Play, mod);
        Assert.True(ConquestEncounters.IsComplete(wizard, Falmea));
    }

    private static (Wizard Wizard, QuestTemplate Quest) InCopyFalmea(World world, ShadowSchoolMod mod) {
        var wizard = Ready(MagicSchool.Shadow);
        Accept(wizard, LoadQuest(FalmeaQuest), mod);
        EnterFalmea(world, mod, wizard);

        return (wizard, LoadQuest(FalmeaQuest));
    }

    private static void EnterFalmea(World world, ShadowSchoolMod mod, Wizard wizard) {
        if (ConquestEncounters.ActiveQuest(wizard, Falmea) is null) {
            Accept(wizard, LoadQuest(FalmeaQuest), mod);
        }

        var quest = LoadQuest(FalmeaQuest);
        Arrive(mod, world, wizard, FireSchool);
        Look(mod, wizard, FalmeaAnchor);
        CompleteGoal(wizard, quest, Falmea.AnchorGoal, QuestProgressSource.Play, mod);
        Look(mod, wizard, FalmeaAnchor);
        Arrive(mod, world, wizard, FireSchool);
        Assert.Equal(ConquestEncounters.Phase.InCopy, mod.Encounters.SessionOf(wizard.CharId)!.Phase);
    }

    private static void EnterGreyrose(World world, ShadowSchoolMod mod, Wizard wizard) {
        if (ConquestEncounters.ActiveQuest(wizard, Greyrose) is null) {
            Accept(wizard, LoadQuest(GreyroseQuest), mod);
        }

        var quest = LoadQuest(GreyroseQuest);
        Arrive(mod, world, wizard, FrostSchool);
        Look(mod, wizard, GreyroseAnchor);
        CompleteGoal(wizard, quest, Greyrose.AnchorGoal, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, Greyrose));
        Look(mod, wizard, GreyroseAnchor);
        Arrive(mod, world, wizard, FrostSchool);
        Assert.Equal(ConquestEncounters.Phase.InCopy, mod.Encounters.SessionOf(wizard.CharId)!.Phase);
    }

    private static void Arrive(ShadowSchoolMod mod, World world, Wizard wizard, string zone) {
        wizard.Zone = zone;
        mod.OnAttached(world.Session(wizard), wizard);
    }

    private static void Look(ShadowSchoolMod mod, Wizard wizard, (float X, float Y) at) {
        var session = SessionOf(mod, wizard);
        mod.OnMoved(session, wizard, at.X, at.Y, 0);
        mod.Encounters.OnStill(session, wizard, at.X, at.Y, UmbralPerception.FocusDelay);
    }

    // The test mod's world is needed for the session actor; it is kept here for Look.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ShadowSchoolMod, World> s_worlds = [];

    private static Akka.Actor.IActorRef SessionOf(ShadowSchoolMod mod, Wizard wizard)
        => s_worlds.TryGetValue(mod, out var world) ? world.Session(wizard) : throw new InvalidOperationException("not a test mod");

    private static QuestTemplate Accept(Wizard wizard, QuestTemplate quest, ShadowSchoolMod mod) {
        wizard.QuestBehavior.AddQuest(new QuestInstance(quest, wizard.CharId));
        QuestProgressEvents.QuestStarted(wizard, quest, QuestProgressSource.Play, [mod]);

        return quest;
    }

    private static void CompleteGoal(Wizard wizard, QuestTemplate quest, string goalName, QuestProgressSource source, ShadowSchoolMod mod) {
        var instance = wizard.QuestBehavior.CurrentQuestInstances.Single(q => q.QuestName == quest.m_questName);
        instance.CompleteGoal(goalName);
        var logic = quest.m_goalLogic.Single(l => (string) l.m_goalsAND.Single() == goalName);
        foreach (var next in logic.m_goalsToAdd) {
            instance.StartGoal(next);
        }

        QuestProgressEvents.GoalCompleted(wizard, quest, Goal(quest, goalName), source, [mod]);
    }

    /// <summary>A zone event, completed only for goals that are active and name it, as QuestService does.</summary>
    private static void CompleteEvent(Wizard wizard, QuestTemplate quest, string eventName, QuestProgressSource source, ShadowSchoolMod mod) {
        var instance = wizard.QuestBehavior.CurrentQuestInstances.Single(q => q.QuestName == quest.m_questName);
        var goals = quest.m_goals.Where(g => g.m_genericEvents.Any(e => (string) e == eventName)
                                            && instance.IsGoalActive(g.m_goalName)).ToList();
        foreach (var goal in goals) {
            CompleteGoal(wizard, quest, goal.m_goalName, source, mod);
        }
    }

    private static void CompleteQuest(Wizard wizard, QuestTemplate quest, QuestProgressSource source, ShadowSchoolMod mod) {
        Assert.True(wizard.QuestBehavior.CompleteQuest(quest.m_questName));
        QuestProgressEvents.QuestCompleted(wizard, quest, source, [mod]);
    }

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

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private static void InitializeIsolatedTestConfiguration() {
        var path = Path.Combine(Path.GetTempPath(), $"imlight-shadow-campaign-tests-{Environment.ProcessId}-{Guid.NewGuid():N}.ini");
        File.WriteAllText(path, $"[Logging]{Environment.NewLine}LogPath = {Path.Combine(Path.GetTempPath(), "imlight-shadow-campaign-tests.log")}{Environment.NewLine}LogLevel = ERROR{Environment.NewLine}SeqSinkUrl = http://127.0.0.1:1{Environment.NewLine}");
        ConfigurationManager.Initialize(path);
        File.Delete(path);
    }

}
