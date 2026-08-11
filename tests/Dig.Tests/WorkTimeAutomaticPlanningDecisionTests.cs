using System.Linq;
using Dig.Domain.Agents;
using Dig.Presentation.Agents;
using Xunit;

namespace Dig.Tests
{

public sealed class WorkTimeAutomaticPlanningDecisionTests
{
    [Theory]
    [InlineData("Eat")]
    [InlineData("Sleep")]
    public void Active_needs_action_blocks_automatic_job_planning(string activeIntent)
    {
        AgentViewModel agent = CreateAgent(activeIntent);

        Assert.False(agent.IsAvailableForAutomaticPlanning);
    }

    private readonly AgentBehaviorPolicy _policy = AgentBehaviorPolicy.CreateDefault();
    private readonly AgentDecisionSystem _decisions = new AgentDecisionSystem();

    [Fact]
    public void Work_time_auto_off_blocks_needs_and_selects_idle()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 500,
            alertness: 500,
            mood: 500);
        Assert.True(agent.SetAutomaticPlanningEnabled(false, tick: 0).IsSuccess);

        AgentDecision decision = Decide(agent, AgentDecisionContext.AllAvailable());

        Assert.Equal(AgentIntentKind.Idle, decision.SelectedIntent);
        Assert.All(
            decision.Options.Where(option => option.IntentKind is
                AgentIntentKind.Eat or AgentIntentKind.Sleep or AgentIntentKind.Rest
                    or AgentIntentKind.Work),
            option => Assert.False(option.Available));
    }

    [Fact]
    public void Work_time_auto_on_selects_work_only_when_available()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 9_000,
            alertness: 9_000,
            mood: 9_000);

        AgentDecision available = Decide(agent, AgentDecisionContext.AllAvailable());
        AgentDecision unavailable = Decide(agent, Context(workAvailable: false));

        Assert.Equal(AgentIntentKind.Work, available.SelectedIntent);
        Assert.True(Option(available, AgentIntentKind.Work).Available);
        Assert.Equal(AgentIntentKind.Idle, unavailable.SelectedIntent);
        Assert.False(Option(unavailable, AgentIntentKind.Work).Available);
    }

    [Fact]
    public void Free_time_never_exposes_new_automatic_work_for_either_auto_state()
    {
        AgentState autoOn = CreateFreeTimeAgent();
        AgentState autoOff = CreateFreeTimeAgent();
        Assert.True(autoOff.SetAutomaticPlanningEnabled(false, tick: 0).IsSuccess);

        AgentDecision onDecision = Decide(autoOn, AgentDecisionContext.AllAvailable());
        AgentDecision offDecision = Decide(autoOff, AgentDecisionContext.AllAvailable());

        Assert.False(Option(onDecision, AgentIntentKind.Work).Available);
        Assert.False(Option(offDecision, AgentIntentKind.Work).Available);
        Assert.NotEqual(AgentIntentKind.Work, onDecision.SelectedIntent);
        Assert.NotEqual(AgentIntentKind.Work, offDecision.SelectedIntent);
    }

    [Fact]
    public void Existing_work_continues_after_auto_off_and_free_time_transition()
    {
        AgentState agent = CreateFreeTimeAgent();
        Assert.True(agent.ApplyDecision(
            AgentTestFactory.CreateForcedDecision(AgentIntentKind.Work, tick: 0),
            _policy,
            tick: 0).IsSuccess);
        Assert.True(agent.SetAutomaticPlanningEnabled(false, tick: 1).IsSuccess);

        AgentDecision decision = _decisions.Decide(
            agent.CreateSnapshot(1),
            Context(workAvailable: false, needsAvailable: false),
            _policy,
            tick: 1);

        Assert.Equal(AgentIntentKind.Work, decision.SelectedIntent);
        Assert.True(Option(decision, AgentIntentKind.Work).Available);
    }

    private AgentDecision Decide(AgentState agent, AgentDecisionContext context)
    {
        return _decisions.Decide(agent.CreateSnapshot(0), context, _policy, tick: 0);
    }

    private static UtilityOptionDiagnostic Option(
        AgentDecision decision,
        AgentIntentKind intent)
    {
        return Assert.Single(
            decision.Options,
            option => option.IntentKind == intent);
    }

    private static AgentState CreateFreeTimeAgent()
    {
        DailySchedule freeTime = new DailySchedule(
            12,
            new[] { new ScheduleSegment(0, 12, ScheduleActivity.Rest) });
        return AgentTestFactory.CreateAgent(
            nutrition: 9_000,
            alertness: 9_000,
            mood: 9_000,
            schedule: freeTime);
    }

    private static AgentDecisionContext Context(
        bool workAvailable,
        bool needsAvailable = true)
    {
        return new AgentDecisionContext(
            foodAvailable: needsAvailable,
            bedAvailable: needsAvailable,
            workAvailable,
            restAvailable: needsAvailable,
            escapeRouteAvailable: true,
            threatLevel: 0);
    }

    private static AgentViewModel CreateAgent(string activeIntent)
    {
        return new AgentViewModel(
            "ae000000000000000000000000000001",
            "Planning Test",
            version: 1,
            isAlive: true,
            cellX: 1,
            cellY: 1,
            nutrition: 5_000,
            alertness: 5_000,
            mood: 5_000,
            health: 10_000,
            scheduledActivity: "Work",
            activeIntent: activeIntent,
            actionElapsedTicks: 0,
            actionRequiredTicks: 4,
            decisionReason: "test",
            decisionExplanation: "test",
            utilityOptions: System.Array.Empty<AgentUtilityOptionViewModel>());
    }
}

}
