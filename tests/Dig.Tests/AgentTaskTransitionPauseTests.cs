using System.Linq;
using Dig.Domain.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentTaskTransitionPauseTests
{
    private readonly AgentBehaviorPolicy _policy = AgentBehaviorPolicy.CreateDefault();
    private readonly AgentDecisionSystem _decisions = new AgentDecisionSystem();

    [Fact]
    public void Completed_task_waits_one_full_tick_before_next_ordinary_work()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 8_000,
            alertness: 8_000,
            mood: 8_000);
        Assert.True(agent.RecordTaskCompletion("job_completed", tick: 1).IsSuccess);

        AgentDecision duringPause = Decide(agent, tick: 2);
        AgentDecision afterPause = Decide(agent, tick: 3);

        Assert.Equal(AgentIntentKind.Idle, duringPause.SelectedIntent);
        UtilityOptionDiagnostic work = Assert.Single(
            duringPause.Options,
            option => option.IntentKind == AgentIntentKind.Work);
        Assert.Equal("rejected.cooldown", work.ReasonCode);
        Assert.Equal(AgentIntentKind.Work, afterPause.SelectedIntent);
    }

    [Fact]
    public void Direct_player_order_bypasses_task_transition_pause()
    {
        AgentState agent = AgentTestFactory.CreateAgent();
        Assert.True(agent.RecordTaskCompletion("test_task", tick: 1).IsSuccess);
        PlayerOrder order = new PlayerOrder(
            "order-after-task",
            "Move now",
            priority: 10_000,
            issuedTick: 2,
            expiresTick: 20);
        Assert.True(agent.SetPlayerOrder(order, tick: 2).IsSuccess);

        AgentDecision decision = Decide(agent, tick: 2);

        Assert.Equal(AgentIntentKind.PlayerOrder, decision.SelectedIntent);
        Assert.Equal(order.Id, decision.SelectedPlayerOrderId);
    }

    [Fact]
    public void Critical_survival_bypasses_task_transition_pause()
    {
        DailySchedule freeTime = new DailySchedule(
            12,
            new[] { new ScheduleSegment(0, 12, ScheduleActivity.Rest) });
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 8_000,
            alertness: 1_000,
            mood: 8_000,
            schedule: freeTime);
        Assert.True(agent.RecordTaskCompletion("test_task", tick: 1).IsSuccess);

        AgentDecision decision = Decide(agent, tick: 2);

        Assert.Equal(AgentIntentKind.Sleep, decision.SelectedIntent);
        Assert.True(decision.Critical);
    }

    [Fact]
    public void Repeated_completion_in_same_tick_does_not_extend_or_duplicate_pause()
    {
        AgentState agent = AgentTestFactory.CreateAgent();

        Assert.True(agent.RecordTaskCompletion("job_completed", tick: 4).IsSuccess);
        Assert.True(agent.RecordTaskCompletion("job_completed", tick: 4).IsSuccess);

        Assert.Equal(4, agent.LastTaskCompletionTick);
        Assert.Single(
            agent.DequeueUncommittedEvents()
                .OfType<AgentTaskTransitionPauseStarted>());
        Assert.True(agent.IsTaskTransitionPaused(_policy, tick: 5));
        Assert.False(agent.IsTaskTransitionPaused(_policy, tick: 6));
    }

    private AgentDecision Decide(AgentState agent, long tick)
    {
        return _decisions.Decide(
            agent.CreateSnapshot(tick),
            AgentDecisionContext.AllAvailable(),
            _policy,
            tick);
    }
}

}
