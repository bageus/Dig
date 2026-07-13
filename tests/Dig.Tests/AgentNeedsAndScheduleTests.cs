using Dig.Domain.Agents;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests;

public sealed class AgentNeedsAndScheduleTests
{
    [Fact]
    public void Need_values_clamp_deterministically()
    {
        NeedValue middle = new NeedValue(5_000);

        Assert.Equal(NeedValue.Maximum, middle.AddClamped(20_000).Points);
        Assert.Equal(NeedValue.Minimum, middle.AddClamped(-20_000).Points);
        Assert.Equal(5_000, middle.Deficit);
    }

    [Fact]
    public void Schedule_wraps_across_days_without_gaps()
    {
        DailySchedule schedule = new DailySchedule(
            ticksPerDay: 8,
            new[]
            {
                new ScheduleSegment(0, 4, ScheduleActivity.Work),
                new ScheduleSegment(4, 6, ScheduleActivity.Rest),
                new ScheduleSegment(6, 8, ScheduleActivity.Sleep),
            });

        Assert.Equal(ScheduleActivity.Work, schedule.GetActivity(0));
        Assert.Equal(ScheduleActivity.Rest, schedule.GetActivity(5));
        Assert.Equal(ScheduleActivity.Sleep, schedule.GetActivity(7));
        Assert.Equal(ScheduleActivity.Work, schedule.GetActivity(8));
        Assert.Equal(ScheduleActivity.Rest, schedule.GetActivity(13));
    }

    [Fact]
    public void Critical_survival_needs_damage_health()
    {
        AgentState agent = AgentTestFactory.CreateAgent(
            nutrition: 100,
            alertness: 100,
            mood: 5_000,
            health: 10_000);

        Result result = agent.AdvanceNeeds(AgentBehaviorPolicy.CreateDefault(), tick: 1);

        Assert.True(result.IsSuccess);
        AgentSnapshot snapshot = agent.CreateSnapshot(1);
        Assert.Equal(0, snapshot.Needs.Nutrition.Points);
        Assert.Equal(0, snapshot.Needs.Alertness.Points);
        Assert.Equal(9_500, snapshot.Needs.Health.Points);
        Assert.Equal(4_700, snapshot.Needs.Mood.Points);
    }

    [Fact]
    public void Earlier_snapshot_does_not_change_after_skill_update()
    {
        AgentState agent = AgentTestFactory.CreateAgent();
        AgentSkillId work = new AgentSkillId("general.work");
        AgentSnapshot before = agent.CreateSnapshot(0);

        Assert.True(agent.SetSkillLevel(work, 7_000).IsSuccess);
        AgentSnapshot after = agent.CreateSnapshot(0);

        Assert.Equal(4_000, before.GetSkillLevel(work));
        Assert.Equal(7_000, after.GetSkillLevel(work));
        Assert.True(after.HasTrait(new AgentTraitId("steady")));
    }

    [Fact]
    public void Switching_intent_keeps_exactly_one_active_action()
    {
        AgentState agent = AgentTestFactory.CreateAgent();
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();
        Assert.True(agent.ApplyDecision(
            AgentTestFactory.CreateForcedDecision(AgentIntentKind.Work, tick: 0),
            policy,
            tick: 0).IsSuccess);

        Assert.True(agent.ApplyDecision(
            AgentTestFactory.CreateForcedDecision(AgentIntentKind.Eat, tick: 1),
            policy,
            tick: 1).IsSuccess);

        AgentSnapshot snapshot = agent.CreateSnapshot(1);
        Assert.True(snapshot.ActiveAction.HasValue);
        Assert.Equal(AgentIntentKind.Eat, snapshot.ActiveAction.Value.IntentKind);
        Assert.Contains(
            agent.PeekUncommittedEvents(),
            domainEvent => domainEvent is AgentActionInterrupted interrupted
                && interrupted.PreviousIntent == AgentIntentKind.Work
                && interrupted.NextIntent == AgentIntentKind.Eat);
    }

    [Fact]
    public void Needs_cannot_advance_twice_on_same_tick()
    {
        AgentState agent = AgentTestFactory.CreateAgent();
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();

        Assert.True(agent.AdvanceNeeds(policy, tick: 1).IsSuccess);
        Result repeated = agent.AdvanceNeeds(policy, tick: 1);

        Assert.True(repeated.IsFailure);
        Assert.Equal(AgentErrors.TickNotIncreasing, repeated.Error);
    }
}
