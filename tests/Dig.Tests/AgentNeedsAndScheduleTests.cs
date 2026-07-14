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
    public void Agent_state_reuses_sorted_capability_views_until_skill_change()
    {
        AgentState agent = new AgentState(
            AgentTestFactory.DefaultAgentId,
            "Capability Test",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            AgentTestFactory.CreateWorkSchedule(),
            new[]
            {
                new AgentSkillValue(new AgentSkillId("mining"), 3_000),
                new AgentSkillValue(new AgentSkillId("building"), 5_000),
            },
            new[]
            {
                new AgentTraitId("steady"),
                new AgentTraitId("careful"),
            });
        AgentSnapshot first = agent.CreateSnapshot(0);
        AgentSnapshot second = agent.CreateSnapshot(0);

        Assert.Same(first.Skills, second.Skills);
        Assert.Same(first.Traits, second.Traits);
        Assert.Equal(
            new[] { "building", "mining" },
            first.Skills.Select(value => value.Id.ToString()).ToArray());
        Assert.Equal(
            new[] { "careful", "steady" },
            first.Traits.Select(value => value.ToString()).ToArray());

        Assert.True(agent.SetSkillLevel(new AgentSkillId("mining"), 7_000).IsSuccess);
        AgentSnapshot changed = agent.CreateSnapshot(0);

        Assert.NotSame(first.Skills, changed.Skills);
        Assert.Same(first.Traits, changed.Traits);
        Assert.Equal(3_000, first.GetSkillLevel(new AgentSkillId("mining")));
        Assert.Equal(7_000, changed.GetSkillLevel(new AgentSkillId("mining")));
    }

    [Fact]
    public void Public_snapshot_constructor_defensively_copies_capabilities()
    {
        List<AgentSkillValue> skills = new List<AgentSkillValue>
        {
            new AgentSkillValue(new AgentSkillId("mining"), 3_000),
        };
        List<AgentTraitId> traits = new List<AgentTraitId>
        {
            new AgentTraitId("steady"),
        };
        AgentSnapshot snapshot = new AgentSnapshot(
            AgentTestFactory.DefaultAgentId,
            "Public Snapshot",
            version: 0,
            isAlive: true,
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            ScheduleActivity.Work,
            activeAction: null,
            playerOrder: null,
            lastActionSwitchTick: -1,
            lastDecision: null,
            skills,
            traits);

        skills[0] = new AgentSkillValue(new AgentSkillId("mining"), 9_000);
        traits.Clear();

        Assert.Equal(3_000, snapshot.GetSkillLevel(new AgentSkillId("mining")));
        Assert.True(snapshot.HasTrait(new AgentTraitId("steady")));
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
