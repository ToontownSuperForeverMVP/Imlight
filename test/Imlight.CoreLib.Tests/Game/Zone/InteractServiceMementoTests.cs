using Imcodec.ObjectProperty.TypeCache;
using Imlight.CoreLib.Game.Zone.Components;
using Xunit;

namespace Imlight.CoreLib.Tests.Game.Zone;

public class InteractServiceMementoTests {

    [Fact]
    public void PersonaGoalOptionSignatureTracksOnlySortedPersonaGoals() {
        ServiceOptionBase[] options = [
            new GoalEntry {
                m_serviceName = "QuestPersonaGoalService",
                m_questID = 42,
                m_goalID = 9,
            },
            new GoalEntry {
                m_serviceName = "QuestPersonaGoalService",
                m_questID = 42,
                m_goalID = 3,
            },
            new GoalEntry {
                m_serviceName = "QuestOfferService",
                m_questID = 99,
                m_goalID = 1,
            },
        ];

        var signature = InteractServiceMementoComponent.GetPersonaGoalOptionSignature(options);

        Assert.Equal("42:3|42:9", signature);
    }

    [Fact]
    public void PersonaGoalOptionSignatureChangesWhenActiveQuestOrGoalChanges() {
        var first = new GoalEntry {
            m_serviceName = "QuestPersonaGoalService",
            m_questID = 42,
            m_goalID = 3,
        };
        var nextGoal = first with { m_goalID = 4 };
        var nextQuest = first with { m_questID = 43 };

        var firstSignature = InteractServiceMementoComponent.GetPersonaGoalOptionSignature([first]);

        Assert.NotEqual(firstSignature, InteractServiceMementoComponent.GetPersonaGoalOptionSignature([nextGoal]));
        Assert.NotEqual(firstSignature, InteractServiceMementoComponent.GetPersonaGoalOptionSignature([nextQuest]));
        Assert.Equal(string.Empty, InteractServiceMementoComponent.GetPersonaGoalOptionSignature([]));
    }

}
