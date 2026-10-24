/*
 * Shadow School: an eighth starting school with its own Wizard City story (docs/shadow-school/).
 *
 * The school itself is the game's Shadow school (id 1429009101, MagicSchool.Shadow); this mod lets a new wizard
 * pick it, and gives it a curriculum, health, a tutor and its story. Everything here only applies to wizards whose
 * school of focus is Shadow, so the other seven schools play exactly as before.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Imcodec.ObjectProperty.TypeCache;
using Imcodec.Wad;
using Imlight.CoreLib.Game.Combat;
using Imlight.CoreLib.Game.Quests;
using Imlight.CoreLib.Game.World;
using Imlight.CoreLib.Shared.Behaviors;
using Imlight.CoreLib.WizardData.Models.Player;

namespace Imlight.CoreLib.Mods.ShadowSchool;

internal sealed class ShadowSchoolMod : IMod, IQuestDialogueProvider, IQuestProgressObserver, IPlayerPresenceObserver,
                                        IBattleOutcomeObserver, IQuestZoneEventValidator, IQuestCommandValidator {

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

    // How Umbral Sight's registry changes are saved: the character's record, or a test's recorder.
    private readonly Action<Wizard> _save;

    // The Ravenwood conquest (ConquestEncounters.cs): its sessions live as long as the mod.
    private readonly ConquestEncounters _conquest;

    public ShadowSchoolMod() : this(UmbralSight.SaveToDatabase) {
        s_live = this;
    }

    internal ShadowSchoolMod(Action<Wizard> save) : this(save, new ConquestWorld(save)) {
    }

    internal ShadowSchoolMod(Action<Wizard> save, IConquestWorld world) {
        _save = save;
        _conquest = new ConquestEncounters(world);
    }

    // The mod the server runs (ModRegistry's), for its staff commands.
    private static ShadowSchoolMod s_live;

    internal static ConquestEncounters Conquest => s_live?._conquest;

    internal ConquestEncounters Encounters => _conquest;

    public string Name => NAME;

    internal static bool Enabled => ModRegistry.IsEnabled(NAME);

    internal static bool IsShadowWizard(Wizard wizard)
        => Enabled && wizard?.MagicSchoolBehavior?.MagicSchool == MagicSchool.Shadow;

    public void BuildRootFiles(Archive root, IDictionary<string, byte[]> files)
        => ShadowSchoolContent.Build(root, files, ModRegistry.FolderOf(this));

    // What a Shadow wizard hears in the canonical Wizard City quests (ShadowMainlineDialogue.cs).
    public ActorDialog Resolve(QuestDialogueRequest request) => ShadowMainlineDialogue.Resolve(request);

    // Umbral Sight (UmbralSight.cs): the Nightside walk recorded step by step, and the unlock at its end.
    // The Ravenwood conquest (ConquestEncounters.cs) records each encounter's steps from the same events.
    public void OnQuestStarted(Wizard wizard, QuestTemplate quest, QuestProgressSource source) {
        UmbralSight.OnQuestStarted(wizard, quest, source, _save);
        _conquest.OnQuestStarted(wizard, quest);
    }

    public void OnGoalCompleted(Wizard wizard, QuestTemplate quest, GoalTemplate goal, QuestProgressSource source) {
        UmbralSight.OnGoalCompleted(wizard, quest, goal, source, _save);
        _conquest.OnGoalCompleted(wizard, quest, goal, source);
    }

    public void OnQuestCompleted(Wizard wizard, QuestTemplate quest, QuestProgressSource source) {
        UmbralSight.OnQuestCompleted(wizard, quest, source, _save);
        _conquest.OnQuestCompleted(wizard, quest, source);
    }

    public bool CanCompleteGoalFromZoneEvent(Wizard wizard, QuestTemplate quest, GoalTemplate goal, string eventName) {
        var questName = (string) (quest?.m_questName);
        var goalName = (string) (goal?.m_goalName);
        var campaignId = questName == ConquestEncounters.CAMPAIGN_QUEST
            ? ConquestEncounters.CampaignMilestones.FirstOrDefault(milestone => ConquestEncounters.CampaignGoal(milestone) == goalName)
            : null;
        var chapter = ConquestEncounters.ByQuest(questName);
        var isChapterDuel = chapter is not null && goalName == chapter.DuelGoal;
        if (campaignId is null && !isChapterDuel) {
            return true;
        }

        var encounter = campaignId is not null ? ConquestEncounters.ById(campaignId) : chapter;
        var allowed = encounter is not null && eventName == encounter.DefeatEvent
            && ConquestEncounters.IsVerifiedCampaignVictory(wizard, eventName);
        if (!allowed) {
            Imlight.Common.Logger.Warning("Shadow School: ignored unverified encounter event {0} for {1} goal {2}.",
                Imlight.Common.Logger.Args(eventName, wizard?.CharId, goalName));
        }

        return allowed;
    }

    public IEnumerable<string> GetZoneEventsToReplay(Wizard wizard, QuestTemplate quest) {
        var questName = (string) (quest?.m_questName);
        var chapter = ConquestEncounters.ByQuest(questName);
        if (chapter is not null && ConquestEncounters.IsVerifiedCampaignVictory(wizard, chapter.DefeatEvent)) {
            yield return chapter.AnchorEvent;
            yield return chapter.DefeatEvent;
        }

        if (questName == ConquestEncounters.CAMPAIGN_QUEST) {
            foreach (var encounter in ConquestEncounters.Encounters.Where(e =>
                         ConquestEncounters.IsVerifiedCampaignVictory(wizard, e.DefeatEvent))) {
                yield return encounter.DefeatEvent;
            }
        }
    }

    public bool CanCompleteGoalFromCommand(Wizard wizard, QuestTemplate quest, GoalTemplate goal) {
        if ((string) (quest?.m_questName) == ConquestEncounters.CAMPAIGN_QUEST) {
            return false;
        }

        var encounter = ConquestEncounters.ByQuest((string) (quest?.m_questName));
        return encounter is null || (string) (goal?.m_goalName) != encounter.DuelGoal;
    }

    public bool CanCompleteQuestFromCommand(Wizard wizard, QuestTemplate quest)
        => (string) (quest?.m_questName) != ConquestEncounters.CAMPAIGN_QUEST
            && ConquestEncounters.ByQuest((string) (quest?.m_questName)) is null;

    // What an unlocked wizard perceives as they move (UmbralPerception.cs), shown to that wizard only.
    // The conquest's anchors are bindings the sight perceives; looking at one also takes the encounter a step on.
    public void OnAttached(IActorRef session, Wizard wizard) {
        UmbralSight.Reconcile(wizard, _save);
        UmbralPerception.Attach(wizard);
        _conquest.OnAttached(session, wizard);
    }

    public void OnMoved(IActorRef session, Wizard wizard, float x, float y, float z) {
        UmbralPerception.Send(session, UmbralPerception.Moved(wizard, x, y));
        _conquest.OnMoved(wizard);
    }

    public void OnStill(IActorRef session, Wizard wizard, TimeSpan still) {
        UmbralPerception.Send(session, UmbralPerception.Still(wizard, wizard.Location.X, wizard.Location.Y, still));
        _conquest.OnStill(session, wizard, wizard.Location.X, wizard.Location.Y, still);
    }

    public void OnDetached(ulong charId) {
        UmbralPerception.Detach(charId);
        _conquest.OnDetached(charId);
    }

    // How a battle ended (BattleOutcomeEvents): only a conquest encounter's own private battle counts for anything.
    public void OnBattleEnded(BattleOutcome outcome) => _conquest.OnBattleEnded(outcome);

}
