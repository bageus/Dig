using Dig.Domain.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class UtilityTieBreakTests
{
    [Fact]
    public void Equal_eat_and_sleep_scores_select_eat()
    {
        DailySchedule schedule = new DailySchedule(
            12,
            new[] { new ScheduleSegment(0, 12, ScheduleActivity.Rest) });
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 5_000,
            alertness: 5_000,
            mood: 10_000,
            schedule: schedule);

        AgentDecision decision = new AgentDecisionSystem().Decide(
            agent.CreateSnapshot(0),
            AgentDecisionContext.AllAvailable(),
            AgentBehaviorPolicy.CreateDefault(),
            0);

        Assert.Equal(AgentIntentKind.Eat, decision.SelectedIntent);
        Assert.Equal(
            decision.Options[1].FinalScore,
            decision.Options[2].FinalScore);
    }
}
}
