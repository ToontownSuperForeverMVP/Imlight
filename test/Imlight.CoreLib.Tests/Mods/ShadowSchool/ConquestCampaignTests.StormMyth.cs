using System.Text.RegularExpressions;
using Imcodec.Cryptography;
using Imcodec.MessageLayer.Generated;
using Imcodec.ObjectProperty.TypeCache;
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
/// Stage D2.2 and D2.3: the third to seventh instructor encounters, Halston Balestrom (Storm), Cyrus Drake (Myth), Moolinda
/// Wu (Life), Malorn Ashthorn (Death) and Arthur Wethersfield (Balance). The same framework as Falmea and Greyrose, driven
/// through the real ShadowSchoolMod, the real quest events and the real battle-outcome pipeline with a recording world.
/// Nothing here plays a battle or starts a client.
/// </summary>
public sealed partial class ConquestCampaignTests {

    private sealed record Spec(string Id, string School, string Zone, string Variant, ulong Trainer, ulong Combat, int Custom,
                               (float X, float Y) Anchor, string Persona, string Portrait);

    private static readonly Spec[] s_stormMyth = [
        new("Balestrom", "Storm", "WizardCity/Interiors/WC_SchoolStorm", "ShadowConquest-Balestrom", 38141, 1475613, 1880003,
            (940, 0), "WC-RAV-NPC05_Persona", "Portrait_Frog_Balestrom_A_NPC.dds"),
        new("Drake", "Myth", "WizardCity/Interiors/WC_SchoolMyth", "ShadowConquest-Drake", 38206, 1475614, 1880004,
            (808, 15), "WC-RAV-NPC02_Persona", "Art_Portrait_Cyrus_Drake.dds"),
        new("Wu", "Life", "WizardCity/Interiors/WC_SchoolLife", "ShadowConquest-Wu", 38204, 1475615, 1880005,
            (760, 15), "WC-RAV-NPC01_Persona", "Portrait_Cow_Wu_A_NPC.dds"),
        new("Ashthorn", "Death", "WizardCity/WC_Ravenwood", "ShadowConquest-Ashthorn", 81102, 1475616, 1880006,
            (229, 6035), "WC-RAV-NPC08_Persona", "Art_Portrait_Malorn_Ashthorn.dds"),
        new("Wethersfield", "Balance", "WizardCity/WC_Ravenwood", "ShadowConquest-Wethersfield", 81103, 1475617, 1880007,
            (559, 445), "WC-RAV-NPC09_Persona", "Dog_Wethersfield_A_NPC.dds"),
    ];

    // Read from the installed game data on a QA server (CoreObjectFactory spell templates), 2026-10-23:
    // id -> (name, school, accuracy, rank). The decks below may use nothing outside it.
    private static readonly Dictionary<uint, (string Name, string School, int Accuracy, int Rank)> s_verifiedSpells = new() {
        [2062265892] = ("Thunder Snake", "Storm", 70, 1),
        [1184018227] = ("Lightning Bats", "Storm", 70, 2),
        [975292929] = ("Storm Shark", "Storm", 70, 3),
        [1737191829] = ("Kraken", "Storm", 70, 4),
        [2131701592] = ("Stormblade", "Storm", 100, 0),
        [82998574] = ("Storm Trap", "Storm", 100, 0),
        [82261620] = ("Troll", "Myth", 80, 2),
        [1691813655] = ("Cyclops", "Myth", 80, 3),
        [949570680] = ("Minotaur", "Myth", 80, 5),
        [1911022320] = ("Pierce", "Myth", 100, 0),
        [400226031] = ("Myth Trap", "Myth", 100, 0),
        [603644867] = ("Mythblade", "Myth", 100, 0),
        [1732025431] = ("Myth Shield", "Myth", 100, 0),
        // Stage D2.3, read the same way (CoreObjectFactory spell templates on a QA server, 2026-10-23). Life.
        [2106466410] = ("Leprechaun", "Life", 90, 2),
        [784677378] = ("Nature's Wrath", "Life", 90, 3),
        [400309488] = ("Life Trap", "Life", 100, 0),
        [74836240] = ("Pixie", "Life", 100, 2),
        [2071077573] = ("Spirit Armor", "Life", 100, 3),
        // Death.
        [97383765] = ("Death Troll", "Death", 85, 2),
        [827805677] = ("Banshee", "Death", 85, 3),
        [1730630192] = ("Poison", "Death", 85, 4),
        [732623153] = ("Skeletal Pirate", "Death", 85, 5),
        [90582150] = ("Feint", "Death", 100, 1),
        [1888318032] = ("Plague", "Death", 100, 1),
        [75022979] = ("Curse", "Death", 100, 0),
        [524731385] = ("Vampire", "Death", 85, 4),
        [1788506505] = ("Wraith", "Death", 85, 6),
        // Balance.
        [1462253104] = ("Scorpion", "Balance", 85, 2),
        [1965296812] = ("Locust Swarm", "Balance", 85, 3),
        [254485266] = ("Spectral Blast", "Balance", 85, 4),
        [1321504283] = ("Weakness", "Balance", 100, 0),
        [92296] = ("Hex", "Balance", 100, 0),
        [514206205] = ("Balanceblade", "Balance", 100, 0),
        [1897110899] = ("Muzzle", "Balance", 90, 3),
    };

    // Where a wizard looks at an instructor's place, in words: the Life School, or Ravenwood itself.
    private static string PlaceOf(Spec spec) => spec.Zone == Ravenwood ? "Ravenwood" : spec.Zone.EndsWith("Myth") ? "Myth School"
        : spec.Zone.EndsWith("Life") ? "Life School" : "Storm School";

    // The game's own conversation stand-ins (objects named 'Player Stand In', gamedata.bin of WC_Ravenwood), where the two
    // Ravenwood circles are placed: the only ground Ravenwood's data names near those instructors.
    private static readonly Dictionary<string, (float X, float Y, float Z)> s_standIns = new() {
        ["Ashthorn"] = (297.4953f, 6041.754f, -30.47972f),
        ["Wethersfield"] = (509.9329f, 270.5928f, -30.5f),
    };

    public static IEnumerable<object[]> StormMythIds => s_stormMyth.Select(s => new object[] { s.Id });

    private static Spec SpecOf(string id) => s_stormMyth.Single(s => s.Id == id);

    private static ConquestEncounter EncounterOf(Spec spec) => ConquestEncounters.ById(spec.Id)!;

    private static string RoomFile(Spec spec)
        => Path.Combine(ModFolder(), "spiraldb", "ZoneObjects", ZoneObjects.FileNameOf(spec.Zone, spec.Variant));

    private static ZoneObjects.ZoneObjectFile LoadRoom(Spec spec)
        => System.Text.Json.JsonSerializer.Deserialize<ZoneObjects.ZoneObjectFile>(File.ReadAllText(RoomFile(spec)), JsonOptions)!;

    private static BossOverrides.BossOverride LoadProfile(Spec spec) {
        InitializeIsolatedTestConfiguration();

        return BossOverrides.Parse(File.ReadAllText(Path.Combine(ModFolder(), "spiraldb", "Customs", $"{spec.Custom}.json")),
            Customs.Key(spec.Custom))!;
    }

    /// <summary>One chapter finished the way play finishes it: anchor, duel, return, completion; every step in order.</summary>
    private static void CompleteChapterByPlay(Wizard wizard, ShadowSchoolMod mod, ConquestEncounter encounter) {
        var quest = Accept(wizard, LoadQuest(encounter.QuestName), mod);
        CompleteGoal(wizard, quest, encounter.AnchorGoal, QuestProgressSource.Play, mod);
        CompleteGoal(wizard, quest, encounter.DuelGoal, QuestProgressSource.Play, mod);
        CompleteGoal(wizard, quest, encounter.ReturnGoal, QuestProgressSource.Play, mod);
        CompleteQuest(wizard, quest, QuestProgressSource.Play, mod);
        Assert.True(ConquestEncounters.IsComplete(wizard, encounter));
    }

    /// <summary>A ready Shadow wizard who has completed every earlier chapter by play.</summary>
    private static Wizard ReadyBefore(Spec spec, ShadowSchoolMod mod) {
        var wizard = Ready(MagicSchool.Shadow);
        foreach (var earlier in ConquestEncounters.Encounters.Where(e => e.Order < EncounterOf(spec).Order).OrderBy(e => e.Order)) {
            CompleteChapterByPlay(wizard, mod, earlier);
        }

        return wizard;
    }

    /// <summary>Accepts the campaign and records the earlier instructors' victories, so the chosen one is the active milestone.</summary>
    private static QuestTemplate CampaignBefore(Wizard wizard, Spec spec, ShadowSchoolMod mod) {
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);
        foreach (var earlier in ConquestEncounters.Encounters.Where(e => e.Order < EncounterOf(spec).Order).OrderBy(e => e.Order)) {
            CompleteEvent(wizard, campaign, earlier.DefeatEvent, QuestProgressSource.Play, mod);
        }

        return campaign;
    }

    private static void EnterEncounter(World world, ShadowSchoolMod mod, Wizard wizard, Spec spec) {
        var encounter = EncounterOf(spec);
        if (ConquestEncounters.ActiveQuest(wizard, encounter) is null) {
            Accept(wizard, LoadQuest(encounter.QuestName), mod);
        }

        var quest = LoadQuest(encounter.QuestName);
        Arrive(mod, world, wizard, spec.Zone);
        Look(mod, wizard, spec.Anchor);
        CompleteGoal(wizard, quest, encounter.AnchorGoal, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, encounter));
        Look(mod, wizard, spec.Anchor);
        Arrive(mod, world, wizard, spec.Zone);
        Assert.Equal(ConquestEncounters.Phase.InCopy, mod.Encounters.SessionOf(wizard.CharId)!.Phase);
    }

    private static BattleOutcome Win(World world, Wizard wizard, Spec spec, BattleResult result = BattleResult.Won)
        => Outcome(world, wizard, result, spec.Zone, spec.Variant, spec.Combat, spec.Custom);

    // ------------------------------------------------------------------ Registration and order

    [Fact]
    public void AllSevenInstructorsAreInThePlansOrderAndTheFinaleIsAbsent() {
        string[] seven = ["Falmea", "Greyrose", "Balestrom", "Drake", "Wu", "Ashthorn", "Wethersfield"];
        Assert.Equal(seven, ConquestEncounters.Encounters.OrderBy(e => e.Order).Select(e => e.Id));
        Assert.Equal([1, 2, 3, 4, 5, 6, 7], ConquestEncounters.Encounters.Select(e => e.Order));
        Assert.Equal(seven, ConquestEncounters.CampaignMilestones);
        foreach (var locked in new[] { "Moolinda", "Malorn", "Arthur", "Ambrose" }) {
            Assert.Null(ConquestEncounters.ById(locked));
        }

        // Seven chapter files and no finale; one custom each.
        var quests = Directory.GetFiles(Path.Combine(ModFolder(), "spiraldb", "QuestTemplates"), "questtemplates_ShadowSchool-Conquest-*.json")
            .Select(Path.GetFileNameWithoutExtension).Order().ToArray();
        Assert.Equal(seven.Select(id => $"questtemplates_ShadowSchool-Conquest-{id}").Order(), quests);
        var customs = Directory.GetFiles(Path.Combine(ModFolder(), "spiraldb", "Customs"), "*.json").Select(Path.GetFileNameWithoutExtension).Order();
        Assert.Equal(["1880001", "1880002", "1880003", "1880004", "1880005", "1880006", "1880007"], customs);
        Assert.Equal(ConquestEncounters.Encounters.Select(e => e.CustomId).Order(), Enumerable.Range(1880001, 7));
    }

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void EachEncounterIsRegisteredWithItsVerifiedIdentity(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);

        Assert.Equal(spec.Trainer, encounter.InstructorTemplateId);
        Assert.Equal(spec.Combat, encounter.CreatureTemplateId);
        Assert.Equal(spec.Custom, encounter.CustomId);
        Assert.Equal(spec.Zone, encounter.Zone);
        Assert.Equal(spec.Variant, encounter.Variant);
        Assert.Equal($"ShadowSchool-Conquest-{id}", encounter.QuestName);
        Assert.Equal(id is "Ashthorn" or "Wethersfield" ? encounter.ExitLoc : "Start", encounter.ArrivalLoc);
        Assert.Equal(Ravenwood, encounter.ExitZone);
        Assert.Equal(ConquestEncounters.CampaignMilestones[encounter.Order - 1], id);

        var binding = UmbralPerception.Bindings.Single(b => b.Id == encounter.AnchorBinding);
        Assert.Equal(spec.Zone, binding.Zone);
        Assert.Equal(UmbralPerception.Kind.Binding, binding.Kind);
        Assert.True(ConquestEncounters.AtAnchor(encounter, spec.Anchor.X, spec.Anchor.Y));
        Assert.False(ConquestEncounters.AtAnchor(encounter, -500, -500));
        Assert.True(Strings().ContainsKey(binding.TextKey));
    }

    [Fact]
    public void TheTrainerAndCombatPositionsMatchTheInstalledZoneData() {
        // gamedata.bin, read on a QA server: Balestrom (938.90, -1.20, 202.09), Drake (807.46, 13.78, 100.00).
        var storm = LoadRoom(SpecOf("Balestrom")).Creatures.Single();
        Assert.Equal(938.9f, storm.X, 0.1f);
        Assert.Equal(-1.2f, storm.Y, 0.1f);
        Assert.Equal(202.09f, storm.Z, 0.1f);
        var myth = LoadRoom(SpecOf("Drake")).Creatures.Single();
        Assert.Equal(807.46f, myth.X, 0.1f);
        Assert.Equal(13.78f, myth.Y, 0.1f);
        Assert.Equal(100f, myth.Z, 0.1f);

        // gamedata.bin of the Life School and of Ravenwood (Ashthorn and Wethersfield do not stand in a school).
        var wu = LoadRoom(SpecOf("Wu")).Creatures.Single();
        Assert.Equal((760.1959f, 15.4857f, 100f), (wu.X, wu.Y, wu.Z));
        var ashthorn = LoadRoom(SpecOf("Ashthorn")).Creatures.Single();
        Assert.Equal((229.29f, 6034.794f, -30.47976f), (ashthorn.X, ashthorn.Y, ashthorn.Z));
        var wethersfield = LoadRoom(SpecOf("Wethersfield")).Creatures.Single();
        Assert.Equal((558.59f, 444.9666f, 7.176836f), (wethersfield.X, wethersfield.Y, wethersfield.Z));
        Assert.Equal(Ravenwood, SpecOf("Ashthorn").Zone);
        Assert.Equal(Ravenwood, SpecOf("Wethersfield").Zone);
    }

    [Fact]
    public void AnEncounterIsLockedUntilTheOneBeforeItIsComplete() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var balestrom = ConquestEncounters.ById("Balestrom")!;
        var drake = ConquestEncounters.ById("Drake")!;
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, balestrom));
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, drake));

        CompleteFalmeaChapter(wizard, mod);
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, balestrom));
        CompleteChapterByPlay(wizard, mod, Greyrose);
        Assert.True(ConquestEncounters.MeetsPrerequisites(wizard, balestrom));
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, drake));

        CompleteChapterByPlay(wizard, mod, balestrom);
        Assert.True(ConquestEncounters.MeetsPrerequisites(wizard, drake));
    }

    [Fact]
    public void AnEncounterCannotBeSkippedByRegistryEntriesAloneOrByTheQuestFlagAlone() {
        InitializeIsolatedTestConfiguration();
        var wizard = Ready(MagicSchool.Shadow);
        var greyrose = ConquestEncounters.ById("Greyrose")!;
        var balestrom = ConquestEncounters.ById("Balestrom")!;

        // The quest flag without the registry stage, and the stage without the quest flag, each fail.
        wizard.QuestBehavior.SetQuestRegistryValue(greyrose.QuestName, "Complete", 1);
        Assert.False(ConquestEncounters.IsComplete(wizard, greyrose));
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, balestrom));
        wizard.QuestBehavior.SetRegistryValue(greyrose.StateKey, (ulong) ConquestStage.Complete);
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, balestrom)); // Falmea is still missing, so Greyrose cannot count either
        Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, ConquestEncounters.ById("Drake")!));
    }

    // ------------------------------------------------------------------ Quest content

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void TheChapterIsAShadowOnlyAnchorDuelReturnQuestWithNavigationAndNoRewards(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        var quest = LoadQuest(encounter.QuestName);
        var strings = Strings();

        Assert.Equal([encounter.AnchorGoal], quest.m_startGoals.Select(g => (string) g));
        Assert.Equal([encounter.AnchorGoal, encounter.DuelGoal, encounter.ReturnGoal],
            quest.m_goalLogic.Select(l => (string) l.m_goalsAND.Single()));
        Assert.True(quest.m_goalLogic[^1].m_completeQuest);
        Assert.Equal(0, (int) quest.m_questRepeat);

        // Offered by Vespera to a Shadow wizard with the sight and the lesson, and only after the previous chapter.
        var requirements = quest.m_requirements.m_requirements.OfType<ReqHasEntry>().ToList();
        var previous = ConquestEncounters.Encounters.Single(e => e.Order == encounter.Order - 1);
        Assert.Contains(requirements, r => r.m_isQuestRegistry && (string) r.m_entryName == "Complete" && (string) r.m_questName == previous.QuestName);
        Assert.Contains(requirements, r => r.m_isQuestRegistry && (string) r.m_questName == ConquestEncounters.LESSON_QUEST);
        Assert.Contains(requirements, r => !r.m_isQuestRegistry && (string) r.m_entryName == UmbralSight.STATE_KEY);
        var prep = ((ActorDialogList) quest.m_dialogList).m_dialogs.Single(d => d.m_dialogTag == "Prep");
        Assert.All(prep.m_dialogEntries, e => Assert.Equal(ShadowSchoolMod.TUTOR_TEMPLATE_ID, (uint) e.m_actorTemplateID));

        foreach (var goal in quest.m_goals) {
            Assert.Equal(StringHash.Compute(goal.m_goalName), (uint) goal.m_goalNameID);
            Assert.False(string.IsNullOrWhiteSpace((string) goal.m_locationName));
            Assert.Equal(goal.m_goalName == encounter.ReturnGoal ? "WizardCity/Interiors/WC_LibraryBookStacks" : spec.Zone,
                (string) goal.m_destinationZone);
            Assert.True(strings.ContainsKey(((string) goal.m_goalTitle)["ShadowSchool_".Length..]), (string) goal.m_goalTitle);
        }

        Assert.Equal($"ShadowSchool_Conquest{id}Location", (string) Goal(quest, encounter.AnchorGoal).m_locationName);
        Assert.Equal($"ShadowSchool_Conquest{id}Location", (string) Goal(quest, encounter.DuelGoal).m_locationName);

        var anchor = Assert.IsType<WaypointGoalTemplate>(Goal(quest, encounter.AnchorGoal));
        Assert.Equal([encounter.AnchorEvent], anchor.m_genericEvents.Select(e => (string) e));
        var duel = Assert.IsType<BountyGoalTemplate>(Goal(quest, encounter.DuelGoal));
        Assert.Equal([encounter.DefeatEvent], duel.m_genericEvents.Select(e => (string) e));
        Assert.Empty(duel.m_npcAdjectives);
        Assert.Null(duel.m_tallyCounter);
        var back = Assert.IsType<PersonaGoalTemplate>(Goal(quest, encounter.ReturnGoal));
        Assert.Equal(ShadowSchoolMod.TUTOR_OBJECT_NAME, (string) back.m_personaName);

        // The instructor speaks as themself, Vespera as herself; no reward of any kind.
        Assert.All(((ActorDialogList) Goal(quest, encounter.AnchorGoal).m_dialogList).m_dialogs.Single().m_dialogEntries, e => {
            Assert.Equal(spec.Persona, (string) ((NPCDialogEntry) e).m_personaName);
            Assert.EndsWith(spec.Portrait, (string) ((NPCDialogEntry) e).m_picture);
        });
        Assert.Empty(quest.m_startResults.m_results);
        Assert.Empty(quest.m_endResults.m_results);
        Assert.All(quest.m_goals, g => Assert.Empty(g.m_completeResults.m_results));
    }

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void EveryLocalizationKeyTheEncounterUsesExistsAndStaysUnvoiced(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        var strings = Strings();
        var json = File.ReadAllText(Path.Combine(ModFolder(), "spiraldb", "QuestTemplates", $"questtemplates_{encounter.QuestName}.json"));

        var keys = Regex.Matches(json, "\"m_(?:dialog|goalTitle|questTitle|questInfo|locationName)\": \"(ShadowSchool_[A-Za-z0-9_]+)\"").Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.NotEmpty(keys);
        Assert.All(keys, key => Assert.True(strings.ContainsKey(key["ShadowSchool_".Length..]), key));
        foreach (var key in new[] { encounter.EnterKey, encounter.WonKey, encounter.LostKey, $"Umbral_{encounter.AnchorBinding}" }) {
            Assert.True(strings.ContainsKey(key), key);
        }

        Assert.DoesNotContain("m_soundFile\": \"|", json);
        Assert.All(new[] { "Title", "Info", "GoalAnchor", "GoalDuel", "GoalReturn", "Location", "Offer", "Choice", "Notices", "Warns",
            "Yields", "Measure", "VesperaAfter", "VesperaCost" }, part => Assert.True(strings.ContainsKey($"Conquest{id}{part}"), part));
        Assert.Contains("your own choice", strings[$"Conquest{id}Choice"]);
        Assert.Contains("not sending you", strings[$"Conquest{id}Choice"]);
        Assert.Contains(PlaceOf(spec), strings[$"Conquest{id}Location"]);
    }

    [Fact]
    public void BalestromIsEnthusiasticAndDrakeIsSternAndDismissive() {
        var strings = Strings();
        Assert.Contains("!", strings["ConquestBalestromNotices"]);
        Assert.Contains("!", strings["ConquestBalestromWarns"]);
        Assert.DoesNotContain("!", strings["ConquestDrakeNotices"] + strings["ConquestDrakeWarns"] + strings["ConquestDrakeYields"]);
        Assert.Contains("understanding", strings["ConquestDrakeYields"]);
        Assert.Contains("discipline", strings["ConquestDrakeMeasure"]);
    }

    // ------------------------------------------------------------------ The campaign

    [Fact]
    public void TheCampaignMilestonesForTheLastFiveNavigateToWhereTheirInstructorIs() {
        var campaign = LoadQuest(Campaign);
        foreach (var spec in s_stormMyth) {
            var goal = campaign.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal(spec.Id));
            Assert.Equal(spec.Zone, (string) goal.m_destinationZone);
            Assert.Equal($"ShadowSchool_Conquest{spec.Id}Location", (string) goal.m_locationName);
            Assert.Equal([$"ShadowSchool_Conquest_{spec.Id}_Defeated"], goal.m_genericEvents.Select(e => (string) e));
        }

        // Falmea and Greyrose keep their own schools; the rest point at the Life School and at Ravenwood itself.
        Assert.Equal(FireSchool, (string) campaign.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal("Falmea")).m_destinationZone);
        Assert.Equal(FrostSchool, (string) campaign.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal("Greyrose")).m_destinationZone);
        Assert.Equal(7, campaign.m_goals.Count);
        Assert.Empty(campaign.m_endResults.m_results);
    }

    [Fact]
    public void PlayingTheSevenEncountersInOrderWalksTheCampaignOneMilestoneAtATimeAndCompletesIt() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);
        var finished = new List<string>();

        foreach (var encounter in ConquestEncounters.Encounters.OrderBy(e => e.Order)) {
            // Only the current instructor's milestone is active; the later ones are still locked.
            Assert.True(ConquestEncounters.ActiveCampaign(wizard)!.IsGoalActive(ConquestEncounters.CampaignGoal(encounter.Id)), encounter.Id);
            foreach (var later in ConquestEncounters.CampaignMilestones.Skip(encounter.Order)) {
                Assert.False(ConquestEncounters.ActiveCampaign(wizard)!.IsGoalActive(ConquestEncounters.CampaignGoal(later)), later);
            }

            var chapter = Accept(wizard, LoadQuest(encounter.QuestName), mod);
            var spec = s_stormMyth.FirstOrDefault(s => s.Id == encounter.Id);
            if (spec is null) {
                // Falmea and Greyrose: their own battle, driven by their own helpers.
                if (encounter.Id == "Falmea") {
                    EnterFalmea(world, mod, wizard);
                    BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, FireSchool, FireVariant, FalmeaCombat, 1880001), [mod]);
                }
                else {
                    EnterGreyrose(world, mod, wizard);
                    BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, FrostSchool, FrostVariant, GreyroseCombat, GreyroseCustom), [mod]);
                }
            }
            else {
                EnterEncounter(world, mod, wizard, spec);
                BattleOutcomeEvents.Report(Win(world, wizard, spec), [mod]);
            }

            Assert.Single(world.Events, e => e.Event == encounter.DefeatEvent);
            CompleteEvent(wizard, chapter, encounter.DefeatEvent, QuestProgressSource.Play, mod);
            CompleteEvent(wizard, campaign, encounter.DefeatEvent, QuestProgressSource.Play, mod);
            CompleteGoal(wizard, chapter, encounter.ReturnGoal, QuestProgressSource.Play, mod);
            CompleteQuest(wizard, chapter, QuestProgressSource.Play, mod);
            finished.Add(encounter.Id);

            Assert.True(ConquestEncounters.IsComplete(wizard, encounter));
            Assert.True(ConquestEncounters.IsMilestoneComplete(wizard, encounter.Id));
            Assert.Equal(finished, ConquestEncounters.CampaignMilestones.Where(m => ConquestEncounters.IsMilestoneComplete(wizard, m)));
        }

        // After the seventh, every milestone is done, the registry says all seven are complete, and nothing launches
        // the finale: there is no encounter, quest or goal for Ambrose. The last logic entry completes the campaign.
        Assert.True(ConquestEncounters.CampaignMilestones.All(m => ConquestEncounters.IsMilestoneComplete(wizard, m)));
        Assert.True(ConquestEncounters.AllInstructorsComplete(wizard));
        Assert.True(ConquestEncounters.AllInstructorsDefeated(wizard));
        Assert.True(LoadQuest(Campaign).m_goalLogic[^1].m_completeQuest);
        CompleteQuest(wizard, campaign, QuestProgressSource.Play, mod);
        Assert.True(wizard.HasCompletedQuest(Campaign));
        Assert.Null(ConquestEncounters.ById("Ambrose"));
        Assert.Equal(7, ConquestEncounters.Encounters.Count);
    }

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void NoOtherBattleCompletesThisMilestone(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = ReadyBefore(spec, mod);
        var campaign = CampaignBefore(wizard, spec, mod);
        Accept(wizard, LoadQuest(encounter.QuestName), mod);
        EnterEncounter(world, mod, wizard, spec);
        var events = world.Events.Count;

        // A win over the previous instructor's creature in this copy, in this zone with another variant, or in another
        // instructor's copy is not this battle.
        foreach (var other in ConquestEncounters.Encounters.Where(e => e.Id != id)) {
            BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, other.Zone, other.Variant, other.CreatureTemplateId, other.CustomId), [mod]);
        }

        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, spec.Zone, spec.Variant, spec.Combat, 0), [mod]);
        BattleOutcomeEvents.Report(Outcome(world, wizard, BattleResult.Won, spec.Zone, "", spec.Combat, spec.Custom, false), [mod]);
        Assert.Equal(events, world.Events.Count);
        Assert.False(ConquestEncounters.IsMilestoneComplete(wizard, id));
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, encounter));

        // A command or an unverified event cannot complete it either.
        var milestone = campaign.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal(id));
        Assert.False(QuestProgressEvents.CanCompleteGoalFromCommand(wizard, campaign, milestone, [mod]));
        var fresh = ReadyBefore(spec, mod);
        Assert.False(QuestProgressEvents.CanCompleteGoalFromZoneEvent(fresh, campaign, milestone, encounter.DefeatEvent, [mod]));
    }

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void AnEventForALaterMilestoneNeverCompletesAnEarlierOneOrASkippedOne(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);

        // Nothing is defeated: this event finds no active goal, because the earlier milestones are not done.
        CompleteEvent(wizard, campaign, encounter.DefeatEvent, QuestProgressSource.Play, mod);
        Assert.False(ConquestEncounters.IsMilestoneComplete(wizard, id));
        Assert.True(ConquestEncounters.ActiveCampaign(wizard)!.IsGoalActive(ConquestEncounters.CampaignGoal("Falmea")));
        Assert.False(QuestProgressEvents.CanCompleteGoalFromZoneEvent(wizard, campaign,
            campaign.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal(id)), encounter.DefeatEvent, [mod]));
    }

    // ------------------------------------------------------------------ The private rooms

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void TheRoomIsAPrivateVariantWithTheInstructorAtTheirPlaceAndTheCircleOnTheirOwnHeight(string id) {
        var spec = SpecOf(id);
        var room = LoadRoom(spec);

        Assert.Equal(spec.Zone, room.Zone);
        Assert.Equal(spec.Variant, room.Variant);
        Assert.Contains("@", Path.GetFileName(RoomFile(spec)));
        Assert.Equal([spec.Trainer], room.Remove);
        var creature = Assert.Single(room.Creatures);
        Assert.Equal(spec.Combat, creature.TemplateID);
        Assert.Equal(spec.Custom, room.ReplaceSpawns[spec.Combat.ToString()]);

        // The circle: the Wizard City boss-room sigil, near the instructor and at the height of their placement.
        var circle = Assert.Single(room.Objects);
        Assert.Equal(560ul, circle.TemplateID);
        Assert.Equal("CombatSigil8Actor", circle.CombatSigil.SigilType);
        var distance = MathF.Sqrt(MathF.Pow(circle.X - creature.X, 2) + MathF.Pow(circle.Y - creature.Y, 2));
        Assert.InRange(distance, 1, 200);
        if (s_standIns.TryGetValue(id, out var standIn)) {
            // The two Ravenwood circles stand exactly on the game's own conversation stand-ins, whose ground is the
            // only one named near those instructors; the arrival is outside the circle's radius.
            Assert.Equal(standIn, (circle.X, circle.Y, circle.Z));
            var arrival = EncounterOf(spec).ArrivalLoc.Split(',').Select(float.Parse).ToArray();
            Assert.InRange(MathF.Sqrt(MathF.Pow(circle.X - arrival[0], 2) + MathF.Pow(circle.Y - arrival[1], 2)), circle.CombatSigil.Radius + 1, 2000);
            Assert.InRange(circle.CombatSigil.Radius, 100, 500);
        }
        else {
            Assert.InRange(distance, 0, circle.CombatSigil.Radius);
            Assert.Equal(creature.Z, circle.Z, 0.01f);
        }

        Assert.Equal(Ravenwood, room.Exit.Zone);
        Assert.Equal(EncounterOf(spec).ExitLoc, room.Exit.Loc);
        Assert.Empty(room.Exit.Triggers);
        Assert.Empty(room.Exit.Doors);
    }

    [Fact]
    public void ThePublicStormAndMythSchoolsAndTheirTrainersAreUntouched() {
        var files = Directory.GetFiles(Path.Combine(ModFolder(), "spiraldb", "ZoneObjects")).Select(Path.GetFileName).ToArray();

        // Every zone file for these schools is a variant: none is named for the public zone alone.
        Assert.DoesNotContain("WizardCity-Interiors-WC_SchoolStorm.json", files);
        Assert.DoesNotContain("WizardCity-Interiors-WC_SchoolMyth.json", files);
        Assert.DoesNotContain("WizardCity-Interiors-WC_SchoolLife.json", files);
        Assert.DoesNotContain("WizardCity-WC_Ravenwood.json", files);
        Assert.All(files.Where(f => f!.Contains("SchoolStorm") || f.Contains("SchoolMyth") || f.Contains("SchoolLife") || f.Contains("WC_Ravenwood")),
            f => Assert.Contains("@ShadowConquest-", f));
        Assert.Equal("WizardCity-Interiors-WC_SchoolStorm.json", ZoneObjects.FileNameOf("WizardCity/Interiors/WC_SchoolStorm", ""));

        // The trainer is removed only inside the variant files; no custom or room replaces it anywhere else.
        foreach (var spec in s_stormMyth) {
            Assert.Equal([spec.Trainer], LoadRoom(spec).Remove);
            Assert.NotEqual(spec.Trainer, spec.Combat);
        }

        // Public Ravenwood trainers are keyed by template: neither the trainer nor the combat creature is a mod custom.
        Assert.All(s_stormMyth, s => Assert.False(Customs.IsCustom(s.Trainer)));
    }

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void TheEncounterReloadsIntoItsOwnCopyAndLeavesToRavenwood(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        InitializeIsolatedTestConfiguration();
        var wizard = Ready(MagicSchool.Shadow);
        var seen = new List<object>();
        var session = s_system.ActorOf(Akka.Actor.Props.Create(() => new Capture(seen)));
        var live = new ConquestWorld(_ => { });

        try {
            live.Enter(session, wizard, encounter);
            WaitFor(seen, 1);
            var enter = Assert.IsType<ZONE_102_PROTOCOL.MSG_ZONETRANSFER>(seen[0]);
            Assert.Equal(spec.Zone, enter.DestinationZone);
            Assert.Equal(encounter.ArrivalLoc, enter.DestinationLocation);
            Assert.True(enter.ForceReload);
            Assert.True(enter.IsPrivate);
            Assert.True(enter.ResetInstance);
            Assert.Equal(wizard.CharId, enter.OwnerCharId);
            Assert.Equal(spec.Variant, enter.Variant);
            Assert.Equal(spec.Variant, ZoneVariants.Of(wizard.CharId)?.Variant);

            live.Leave(session, wizard, encounter, TimeSpan.Zero);
            WaitFor(seen, 2);
            var exit = Assert.IsType<ZONE_102_PROTOCOL.MSG_ZONETRANSFER>(seen[1]);
            Assert.Equal(Ravenwood, exit.DestinationZone);
            Assert.Equal(encounter.ExitLoc, exit.DestinationLocation);
            // A copy of Ravenwood is left for Ravenwood: the same zone, so the client has to reload or the wizard would
            // only be moved inside the copy.
            Assert.Equal(spec.Zone == Ravenwood, exit.ForceReload);
            Assert.False(exit.IsPrivate);
            Assert.Equal(wizard.CharId, exit.OwnerCharId);
            Assert.Null(ZoneVariants.Of(wizard.CharId));
        }
        finally {
            s_system.Stop(session);
        }
    }

    // ------------------------------------------------------------------ The battle profiles

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void TheProfileIsACustomWithOnlyVerifiedSpellsAndNoLoot(string id) {
        var spec = SpecOf(id);
        var profile = LoadProfile(spec);

        Assert.Equal(spec.Custom, profile.CustomID);
        Assert.Equal(spec.Combat, profile.BaseTemplateID);
        Assert.Equal(spec.School, profile.School);
        Assert.InRange(profile.Level!.Value, 11, 16);
        Assert.InRange(profile.Health!.Value, 800, 1500);
        Assert.Equal(0, profile.StartingPips);
        Assert.Equal(0f, profile.PowerPipChance);
        Assert.Equal(0f, profile.FlatDamage);
        Assert.Equal(0, profile.XP);
        Assert.Equal(0, profile.GoldMax);
        Assert.Equal(0, profile.GoldMin);
        Assert.Empty(profile.Drops!);
        Assert.Empty(BossOverrides.DropTablesFor(profile, ["base-creature-drop"]));

        // Every spell exists in the installed data, is of his own school, and the deck has no filler.
        Assert.NotEmpty(profile.Deck!);
        Assert.Equal(profile.Deck.Count, profile.Deck.Select(c => c.SpellID).Distinct().Count());
        foreach (var card in profile.Deck) {
            Assert.True(s_verifiedSpells.TryGetValue(card.SpellID, out var spell), $"unverified spell {card.SpellID}");
            Assert.Equal(spec.School, spell.School);
            Assert.Equal(spell.Name, card.Name);
        }
    }

    [Fact]
    public void BalestromIsAHighDamageLowAccuracyFragileStormFight() {
        var profile = LoadProfile(SpecOf("Balestrom"));
        var greyrose = LoadProfile(new Spec("Greyrose", "Ice", FrostSchool, FrostVariant, GreyroseTrainer, GreyroseCombat, GreyroseCustom, (0, 0), "", ""));

        // Fragile: less health than the Ice instructor before him, no shield or heal in the deck.
        Assert.True(profile.Health < greyrose.Health);
        Assert.DoesNotContain(profile.Deck!, c => s_verifiedSpells[c.SpellID].Name is "Storm Shield" or "Cleanse Charm");
        Assert.All(profile.Deck!.Where(c => s_verifiedSpells[c.SpellID].Rank > 0), c => Assert.Equal(70, s_verifiedSpells[c.SpellID].Accuracy));

        // The weapons: Stormblade and Storm Trap, and a dangerous multi-pip attack (Kraken costs four).
        Assert.Contains(profile.Deck!, c => c.SpellID == 2131701592);
        Assert.Contains(profile.Deck!, c => c.SpellID == 82998574);
        var kraken = Assert.Single(profile.Deck!, c => c.SpellID == 1737191829);
        Assert.Equal(1, kraken.Copies);
        Assert.Equal(4, s_verifiedSpells[kraken.SpellID].Rank);

        // The exploitable weakness is against a Shadow wizard specifically.
        Assert.Equal(15f, RowValue(profile.IncomingBoostBySchool, "Shadow"));
        Assert.Equal(0f, RowValue(profile.IncomingBoostBySchool, "Fire"));
        Assert.Equal(0f, RowValue(profile.ResistBySchool, "Shadow"));
    }

    [Fact]
    public void DrakeBreaksWardsAndStandsApartFromTheOtherThree() {
        var profile = LoadProfile(SpecOf("Drake"));

        var pierce = Assert.Single(profile.Deck!, c => c.SpellID == 1911022320);
        Assert.Equal(2, pierce.Copies);
        Assert.Equal("Pierce", s_verifiedSpells[pierce.SpellID].Name);
        Assert.Equal(10f, RowValue(profile.PierceBySchool, "Shadow"));
        Assert.Contains(profile.Deck!, c => c.SpellID == 949570680 && c.Copies == 1);
        Assert.InRange(profile.Health!.Value, 1000, 1300);

        // Distinct: no spell is shared between any two of the four instructors' decks.
        var decks = new Dictionary<string, HashSet<uint>> {
            ["Falmea"] = LoadFalmeaDeck(1880001), ["Greyrose"] = LoadFalmeaDeck(1880002),
            ["Balestrom"] = LoadProfile(SpecOf("Balestrom")).Deck!.Select(c => c.SpellID).ToHashSet(),
            ["Drake"] = profile.Deck!.Select(c => c.SpellID).ToHashSet(),
        };
        foreach (var (a, left) in decks) {
            foreach (var (b, right) in decks.Where(d => string.CompareOrdinal(d.Key, a) > 0)) {
                Assert.Empty(left.Intersect(right));
            }
        }
    }

    // A school's value in a profile row, as the battle reads it: the school's own entry, else "All".
    private static float RowValue(Dictionary<string, float>? row, string school)
        => row is not null && row.TryGetValue(school, out var own) ? own : row is not null && row.TryGetValue("All", out var all) ? all : 0f;

    private static HashSet<uint> LoadFalmeaDeck(int custom) {
        InitializeIsolatedTestConfiguration();

        return BossOverrides.Parse(File.ReadAllText(Path.Combine(ModFolder(), "spiraldb", "Customs", $"{custom}.json")),
            Customs.Key(custom))!.Deck!.Select(c => c.SpellID).ToHashSet();
    }

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void TheDuelFindsTheProfileThroughTheCustomTheSpawnerMarks(string id) {
        var spec = SpecOf(id);
        InitializeIsolatedTestConfiguration();
        var profile = LoadProfile(spec);
        Assert.True(Customs.IsCustom(Customs.Key(spec.Custom)));
        Assert.Equal(spec.Custom, Customs.IdOf(Customs.Key(spec.Custom)));
        Assert.Equal(spec.Combat, profile.BaseTemplateID);
    }

    // ------------------------------------------------------------------ Outcomes

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void AWinInTheOwnersCopyDefeatsTheInstructorOnceReturnsThemAndCompletesTheChapter(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = ReadyBefore(spec, mod);
        var campaign = CampaignBefore(wizard, spec, mod);
        var chapter = Accept(wizard, LoadQuest(encounter.QuestName), mod);
        EnterEncounter(world, mod, wizard, spec);

        BattleOutcomeEvents.Report(Win(world, wizard, spec), [mod]);

        // Saved before the event is queued: the registry already says Defeated at the moment of the post.
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, encounter));
        var defeat = Assert.Single(world.Events, e => e.Event == encounter.DefeatEvent);
        Assert.Equal(wizard.CharId, defeat.CharId);
        Assert.Contains(world.Shown, s => s.CharId == wizard.CharId && s.Chat.Contains(encounter.WonKey));
        Assert.Equal((wizard.CharId, id, ConquestEncounters.ReturnDelay), world.Left[^1]);

        // The same victory reported again (an echoed outcome) posts nothing more.
        var events = world.Events.Count;
        BattleOutcomeEvents.Report(Win(world, wizard, spec), [mod]);
        Assert.Equal(events, world.Events.Count);

        CompleteEvent(wizard, chapter, encounter.DefeatEvent, QuestProgressSource.Play, mod);
        CompleteEvent(wizard, campaign, encounter.DefeatEvent, QuestProgressSource.Play, mod);
        Assert.True(ConquestEncounters.IsMilestoneComplete(wizard, id));
        CompleteGoal(wizard, chapter, encounter.ReturnGoal, QuestProgressSource.Play, mod);
        CompleteQuest(wizard, chapter, QuestProgressSource.Play, mod);
        Assert.Equal(ConquestStage.Complete, ConquestEncounters.StageOf(wizard, encounter));

        // Farming: completing the finished encounter's goals again advances nothing and posts nothing.
        QuestProgressEvents.GoalCompleted(wizard, chapter, Goal(chapter, encounter.DuelGoal), QuestProgressSource.Play, [mod]);
        Assert.Equal(ConquestStage.Complete, ConquestEncounters.StageOf(wizard, encounter));
        Assert.Single(world.Events, e => e.Event == encounter.DefeatEvent);
    }

    [Theory]
    [InlineData("Balestrom", "lost")]
    [InlineData("Balestrom", "fled")]
    [InlineData("Balestrom", "left")]
    [InlineData("Drake", "lost")]
    [InlineData("Drake", "fled")]
    [InlineData("Drake", "left")]
    [InlineData("Wu", "lost")]
    [InlineData("Wu", "fled")]
    [InlineData("Wu", "left")]
    [InlineData("Ashthorn", "lost")]
    [InlineData("Ashthorn", "fled")]
    [InlineData("Ashthorn", "left")]
    [InlineData("Wethersfield", "lost")]
    [InlineData("Wethersfield", "fled")]
    [InlineData("Wethersfield", "left")]
    public void ALossFleeOrDisconnectGivesNothingAndARetryOpensAFreshCopy(string id, string how) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        var result = how switch { "lost" => BattleResult.Lost, "fled" => BattleResult.Fled, _ => BattleResult.Left };
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = ReadyBefore(spec, mod);
        CampaignBefore(wizard, spec, mod);
        var chapter = Accept(wizard, LoadQuest(encounter.QuestName), mod);
        EnterEncounter(world, mod, wizard, spec);
        var events = world.Events.Count;

        BattleOutcomeEvents.Report(Win(world, wizard, spec, result), [mod]);

        Assert.Equal(events, world.Events.Count);
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, encounter));
        Assert.False(ConquestEncounters.IsMilestoneComplete(wizard, id));
        Assert.True(ConquestEncounters.ActiveQuest(wizard, encounter)!.IsGoalActive(encounter.DuelGoal));
        if (result != BattleResult.Left) {
            Assert.Contains(world.Shown, s => s.CharId == wizard.CharId && s.Chat.Contains(encounter.LostKey));
        }

        // The wizard leaves (or is sent out) and later tries again at the anchor: a new session, a new copy.
        mod.OnDetached(wizard.CharId);
        Assert.Null(mod.Encounters.SessionOf(wizard.CharId));
        world.Noted.Remove(wizard.CharId);
        var entered = world.Entered.Count;
        Arrive(mod, world, wizard, spec.Zone);
        Look(mod, wizard, spec.Anchor);
        Look(mod, wizard, (spec.Anchor.X + 1, spec.Anchor.Y));
        Assert.Equal(entered + 1, world.Entered.Count);
        Arrive(mod, world, wizard, spec.Zone);
        Assert.Equal(ConquestEncounters.Phase.InCopy, mod.Encounters.SessionOf(wizard.CharId)!.Phase);

        BattleOutcomeEvents.Report(Win(world, wizard, spec), [mod]);
        Assert.Single(world.Events.Skip(events), e => e.Event == encounter.DefeatEvent);
        Assert.NotNull(chapter);
    }

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void OnlyTheOwnersBattleInTheirOwnCopyCounts(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var owner = ReadyBefore(spec, mod);
        EnterEncounter(world, mod, owner, spec);
        var other = ReadyBefore(spec, mod);
        var events = world.Events.Count;

        // Someone else in the zone, a battle with no session, a public-zone battle, the wrong variant, the wrong creature.
        mod.OnBattleEnded(Win(world, other, spec));
        Assert.Equal(events, world.Events.Count);
        BattleOutcomeEvents.Report(Outcome(world, owner, BattleResult.Won, spec.Zone, "", spec.Combat, spec.Custom, false), [mod]);
        BattleOutcomeEvents.Report(Outcome(world, owner, BattleResult.Won, spec.Zone, "RavenwoodMalistaire", spec.Combat, spec.Custom), [mod]);
        BattleOutcomeEvents.Report(Outcome(world, owner, BattleResult.Won, spec.Zone, spec.Variant, spec.Combat, 0), [mod]);
        mod.OnDetached(owner.CharId);
        BattleOutcomeEvents.Report(Win(world, owner, spec), [mod]);

        Assert.Equal(events, world.Events.Count);
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(owner, encounter));
        Assert.Equal(ConquestStage.None, ConquestEncounters.StageOf(other, encounter));
    }

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void ADisconnectAfterTheWinKeepsTheVictoryAndReplaysItAfterReconnect(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = ReadyBefore(spec, mod);
        var campaign = CampaignBefore(wizard, spec, mod);
        var chapter = Accept(wizard, LoadQuest(encounter.QuestName), mod);
        EnterEncounter(world, mod, wizard, spec);
        var savesBefore = world.Saves;

        BattleOutcomeEvents.Report(Win(world, wizard, spec), [mod]);
        Assert.True(world.Saves > savesBefore);
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, encounter));

        // The session dies before the journal event is consumed; the next attach replays it for chapter and campaign.
        mod.OnDetached(wizard.CharId);
        var events = world.Events.Count;
        wizard.Zone = Ravenwood;
        mod.OnAttached(world.Session(wizard), wizard);
        Assert.Single(world.Events.Skip(events), e => e.Event == encounter.DefeatEvent);
        Assert.Contains(encounter.DefeatEvent, QuestProgressEvents.GetZoneEventsToReplay(wizard, campaign, [mod]));
        Assert.Contains(encounter.DefeatEvent, QuestProgressEvents.GetZoneEventsToReplay(wizard, chapter, [mod]));
        Assert.True(QuestProgressEvents.CanCompleteGoalFromZoneEvent(wizard, campaign,
            campaign.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal(id)), encounter.DefeatEvent, [mod]));

        // A dropped and re-accepted chapter still replays the anchor then the victory, without a second defeat.
        wizard.QuestBehavior.CurrentQuestInstances.RemoveAll(q => q.QuestName == chapter.m_questName);
        Accept(wizard, LoadQuest(encounter.QuestName), mod);
        Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, encounter));
        Assert.Equal([encounter.AnchorEvent, encounter.DefeatEvent], QuestProgressEvents.GetZoneEventsToReplay(wizard, chapter, [mod]));
    }

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void ReconnectingInsideACopyWithNoSessionSendsTheWizardToTheExit(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = ReadyBefore(spec, mod);
        EnterEncounter(world, mod, wizard, spec);

        mod.OnDetached(wizard.CharId);
        world.Noted[wizard.CharId] = (spec.Zone, spec.Variant);
        Arrive(mod, world, wizard, spec.Zone);

        Assert.Equal((wizard.CharId, id, TimeSpan.Zero), world.Left[^1]);
        Assert.Null(mod.Encounters.SessionOf(wizard.CharId));
        Assert.Equal(ConquestStage.AnchorFound, ConquestEncounters.StageOf(wizard, encounter));
    }

    // ------------------------------------------------------------------ Exclusion

    [Theory]
    [InlineData("Balestrom")]
    [InlineData("Drake")]
    [InlineData("Wu")]
    [InlineData("Ashthorn")]
    [InlineData("Wethersfield")]
    public void AnOriginalSchoolWizardCannotTakePartEvenWithCopiedEntries(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        InitializeIsolatedTestConfiguration();
        foreach (var school in OriginalSchools) {
            using var world = new World();
            var mod = Mod(world);
            var wizard = NewWizard(school);
            wizard.QuestBehavior.SetRegistryValue(UmbralSight.STATE_KEY, UmbralSight.UNLOCKED_BY_WALK);
            wizard.QuestBehavior.SetQuestRegistryValue(UmbralSight.ORIGIN_QUEST, "Complete", 1);
            wizard.QuestBehavior.SetQuestRegistryValue(ConquestEncounters.LESSON_QUEST, "Complete", 1);
            foreach (var earlier in ConquestEncounters.Encounters.Where(e => e.Order < encounter.Order)) {
                wizard.QuestBehavior.SetRegistryValue(earlier.StateKey, (ulong) ConquestStage.Complete);
                wizard.QuestBehavior.SetQuestRegistryValue(earlier.QuestName, "Complete", 1);
            }

            Assert.False(ConquestEncounters.MeetsPrerequisites(wizard, encounter), school.ToString());
            var quest = Accept(wizard, LoadQuest(encounter.QuestName), mod);
            CompleteGoal(wizard, quest, encounter.AnchorGoal, QuestProgressSource.Play, mod);
            Assert.Equal(ConquestStage.None, ConquestEncounters.StageOf(wizard, encounter));

            wizard.Zone = spec.Zone;
            mod.OnAttached(world.Session(wizard), wizard);
            Look(mod, wizard, spec.Anchor);
            Look(mod, wizard, (spec.Anchor.X + 1, spec.Anchor.Y));
            Assert.Empty(world.Entered);
            Assert.Null(mod.Encounters.SessionOf(wizard.CharId));
        }
    }

    [Theory]
    [MemberData(nameof(StormMythIds))]
    public void ACommandOrAnUnverifiedEventNeverAdvancesTheEncounter(string id) {
        var spec = SpecOf(id);
        var encounter = EncounterOf(spec);
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = ReadyBefore(spec, mod);
        var quest = Accept(wizard, LoadQuest(encounter.QuestName), mod);

        CompleteGoal(wizard, quest, encounter.AnchorGoal, QuestProgressSource.Command, mod);
        Assert.Equal(ConquestStage.None, ConquestEncounters.StageOf(wizard, encounter));
        Assert.False(QuestProgressEvents.CanCompleteGoalFromCommand(wizard, quest, Goal(quest, encounter.DuelGoal), [mod]));
        Assert.False(QuestProgressEvents.CanCompleteQuestFromCommand(wizard, quest, [mod]));
        Assert.False(QuestProgressEvents.CanCompleteGoalFromZoneEvent(wizard, quest, Goal(quest, encounter.DuelGoal), encounter.DefeatEvent, [mod]));
        Assert.False(ConquestEncounters.IsVerifiedCampaignVictory(wizard, encounter.DefeatEvent));
        Assert.Empty(world.Entered);
    }

    [Fact]
    public void TheFinaleStaysAbsentAndNoCommandOrForgedEventCompletesAnyOfTheSevenMilestones() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);

        // Nothing is defeated: a command cannot complete any milestone, and an event for it finds no verified victory.
        foreach (var id in ConquestEncounters.CampaignMilestones) {
            var milestone = campaign.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal(id));
            Assert.False(QuestProgressEvents.CanCompleteGoalFromCommand(wizard, campaign, milestone, [mod]), id);
            Assert.False(QuestProgressEvents.CanCompleteGoalFromZoneEvent(wizard, campaign, milestone, $"ShadowSchool_Conquest_{id}_Defeated", [mod]), id);
            Assert.False(ConquestEncounters.IsVerifiedCampaignVictory(wizard, $"ShadowSchool_Conquest_{id}_Defeated"), id);
        }

        Assert.False(ConquestEncounters.AllInstructorsComplete(wizard));
        Assert.False(ConquestEncounters.AllInstructorsDefeated(wizard));
        Assert.DoesNotContain(campaign.m_goals, g => ((string) g.m_goalName).Contains("Ambrose"));
        Assert.False(ConquestEncounters.IsVerifiedCampaignVictory(wizard, "ShadowSchool_Conquest_Ambrose_Defeated"));
        Assert.False(wizard.HasCompletedQuest(Campaign));
    }

    [Fact]
    public void TheAllSevenPredicatesNeedEveryInstructorAndEveryChapterAndSurviveAMalformedEntry() {
        InitializeIsolatedTestConfiguration();
        var wizard = Ready(MagicSchool.Shadow);
        foreach (var encounter in ConquestEncounters.Encounters.OrderBy(e => e.Order)) {
            Assert.False(ConquestEncounters.AllInstructorsComplete(wizard));
            wizard.QuestBehavior.SetRegistryValue(encounter.StateKey, (ulong) ConquestStage.Defeated);
            Assert.Equal(encounter.Order == 7, ConquestEncounters.AllInstructorsDefeated(wizard));
            wizard.QuestBehavior.SetRegistryValue(encounter.StateKey, (ulong) ConquestStage.Complete);
            wizard.QuestBehavior.SetQuestRegistryValue(encounter.QuestName, "Complete", 1);
        }

        Assert.True(ConquestEncounters.AllInstructorsComplete(wizard));
        Assert.True(ConquestEncounters.AllInstructorsDefeated(wizard));

        // One chapter's quest flag removed, one stage malformed, one stage removed: each breaks it.
        var wu = ConquestEncounters.ById("Wu")!;
        wizard.QuestBehavior.SetRegistryValue(wu.StateKey, 9ul);
        Assert.False(ConquestEncounters.AllInstructorsComplete(wizard));
        Assert.False(ConquestEncounters.AllInstructorsDefeated(wizard));
        wizard.QuestBehavior.RemoveRegistryValue(wu.StateKey);
        Assert.False(ConquestEncounters.AllInstructorsDefeated(wizard));

        // Original-school wizards are never counted, whatever their registry holds.
        var deathWizard = NewWizard(MagicSchool.Death);
        foreach (var encounter in ConquestEncounters.Encounters) {
            deathWizard.QuestBehavior.SetRegistryValue(encounter.StateKey, (ulong) ConquestStage.Complete);
            deathWizard.QuestBehavior.SetQuestRegistryValue(encounter.QuestName, "Complete", 1);
        }

        Assert.False(ConquestEncounters.AllInstructorsComplete(deathWizard));
        Assert.False(ConquestEncounters.AllInstructorsDefeated(deathWizard));
    }

    [Fact]
    public void SevenVerifiedInstructorVictoriesCompleteEveryChapterAndEveryCampaignMilestone() {
        InitializeIsolatedTestConfiguration();
        using var world = new World();
        var mod = Mod(world);
        var wizard = Ready(MagicSchool.Shadow);
        var campaign = Accept(wizard, LoadQuest(Campaign), mod);
        Spec[] seven = [
            new("Falmea", "Fire", "WizardCity/Interiors/WC_SchoolFire", "ShadowConquest-Falmea", 38208, 403089, 1880001,
                (787, 9), "", ""),
            new("Greyrose", "Ice", "WizardCity/Interiors/WC_SchoolFrost", "ShadowConquest-Greyrose", 38172, 1475612, 1880002,
                (761, 21), "", ""),
            .. s_stormMyth,
        ];

        Assert.Equal(ConquestEncounters.CampaignMilestones, seven.Select(s => s.Id));
        foreach (var spec in seven) {
            var encounter = EncounterOf(spec);
            var chapter = Accept(wizard, LoadQuest(encounter.QuestName), mod);
            EnterEncounter(world, mod, wizard, spec);

            BattleOutcomeEvents.Report(Win(world, wizard, spec), [mod]);

            Assert.Equal(ConquestStage.Defeated, ConquestEncounters.StageOf(wizard, encounter));
            Assert.True(QuestProgressEvents.CanCompleteGoalFromZoneEvent(wizard, chapter,
                Goal(chapter, encounter.DuelGoal), encounter.DefeatEvent, [mod]));
            Assert.True(QuestProgressEvents.CanCompleteGoalFromZoneEvent(wizard, campaign,
                campaign.m_goals.Single(g => (string) g.m_goalName == ConquestEncounters.CampaignGoal(spec.Id)),
                encounter.DefeatEvent, [mod]));
            CompleteEvent(wizard, chapter, encounter.DefeatEvent, QuestProgressSource.Play, mod);
            CompleteEvent(wizard, campaign, encounter.DefeatEvent, QuestProgressSource.Play, mod);
            Assert.True(ConquestEncounters.IsMilestoneComplete(wizard, spec.Id));

            CompleteGoal(wizard, chapter, encounter.ReturnGoal, QuestProgressSource.Play, mod);
            CompleteQuest(wizard, chapter, QuestProgressSource.Play, mod);
            Assert.True(ConquestEncounters.IsComplete(wizard, encounter));
            Assert.Equal(spec.Id == "Wethersfield", ConquestEncounters.AllInstructorsDefeated(wizard));
            Assert.Equal(spec.Id == "Wethersfield", ConquestEncounters.AllInstructorsComplete(wizard));
        }

        Assert.True(campaign.m_goals.All(g => ConquestEncounters.ActiveCampaign(wizard)!.IsGoalCompleted(g.m_goalName)));
        CompleteQuest(wizard, campaign, QuestProgressSource.Play, mod);
        Assert.True(wizard.HasCompletedQuest(Campaign));
        Assert.True(ConquestEncounters.AllInstructorsComplete(wizard));
    }

    [Fact]
    public void TheLifeDeathAndBalanceDecksAreDistinctFromEachOtherAndFromTheFirstFour() {
        var decks = new Dictionary<string, HashSet<uint>>();
        foreach (var encounter in ConquestEncounters.Encounters) {
            decks[encounter.Id] = LoadFalmeaDeck(encounter.CustomId);
        }

        foreach (var (a, left) in decks) {
            foreach (var (b, right) in decks.Where(d => string.CompareOrdinal(d.Key, a) > 0)) {
                Assert.Empty(left.Intersect(right));
            }
        }
    }

}
