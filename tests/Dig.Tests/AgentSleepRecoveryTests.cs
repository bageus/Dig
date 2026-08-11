using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Runtime;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentSleepRecoveryTests
{
    [Fact]
    public void Sleep_restores_exactly_one_displayed_alertness_unit_per_game_minute()
    {
        AgentState agent = CreateSleepingAgent(
            nutrition: 9_000,
            alertness: 1_000,
            health: 9_000);
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();

        Assert.True(agent.ApplyDecision(
            AgentTestFactory.CreateForcedDecision(AgentIntentKind.Sleep, tick: 0),
            policy,
            new AgentActivityTarget(AgentActivityTargetKind.Bed, agent.Id),
            tick: 0).IsSuccess);

        int before = agent.CreateSnapshot(0).Needs.Alertness.Points;
        for (long tick = 0; tick < 5; tick++)
        {
            Assert.True(agent.AdvanceTargetedAction(policy, tick).IsSuccess);
        }

        int recovered = agent.CreateSnapshot(4).Needs.Alertness.Points - before;
        Assert.Equal(200, recovered);
        Assert.Equal(
            100,
            recovered * GameTimeCadence.GameSecondsPerMinute
                / (5 * GameTimeCadence.GameSecondsPerTick));
    }

    [Fact]
    public void Committed_sleep_interval_prevents_alertness_only_health_damage()
    {
        AgentState agent = CreateSleepingAgent(
            nutrition: 9_000,
            alertness: 100,
            health: 483);
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();

        Result started = agent.ApplyDecision(
            AgentTestFactory.CreateForcedDecision(AgentIntentKind.Sleep, tick: 0),
            policy,
            new AgentActivityTarget(AgentActivityTargetKind.FloorSleep, agent.Id),
            tick: 0);
        Result<bool> interval = agent.AdvanceTargetedAction(policy, tick: 0);

        Assert.True(started.IsSuccess);
        Assert.True(interval.IsSuccess);
        Assert.False(interval.Value);
        Assert.Equal(493, agent.CreateSnapshot(0).Needs.Health.Points);

        Result passive = agent.AdvanceNeeds(policy, tick: 1);
        AgentSnapshot sleeping = agent.CreateSnapshot(1);

        Assert.True(passive.IsSuccess);
        Assert.True(sleeping.IsAlive);
        Assert.Equal(543, sleeping.Needs.Health.Points);
        Assert.Equal(140, sleeping.Needs.Alertness.Points);
        Assert.True(sleeping.ActiveAction.HasValue);
        Assert.Equal(AgentIntentKind.Sleep, sleeping.ActiveAction.Value.IntentKind);
        Assert.Equal(1, sleeping.ActiveAction.Value.ElapsedTicks);
    }

    [Fact]
    public void Critical_nutrition_still_damages_health_during_committed_sleep()
    {
        AgentState agent = CreateSleepingAgent(
            nutrition: 100,
            alertness: 100,
            health: 1_983);
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();

        Assert.True(agent.ApplyDecision(
            AgentTestFactory.CreateForcedDecision(AgentIntentKind.Sleep, tick: 0),
            policy,
            new AgentActivityTarget(AgentActivityTargetKind.FloorSleep, agent.Id),
            tick: 0).IsSuccess);
        Assert.True(agent.AdvanceTargetedAction(policy, tick: 0).IsSuccess);
        Assert.Equal(1_993, agent.CreateSnapshot(0).Needs.Health.Points);

        Assert.True(agent.AdvanceNeeds(policy, tick: 1).IsSuccess);
        AgentSnapshot sleeping = agent.CreateSnapshot(1);

        Assert.True(sleeping.IsAlive);
        Assert.Equal(1_987, sleeping.Needs.Health.Points);
        Assert.Equal(99, sleeping.Needs.Nutrition.Points);
    }

    private static AgentState CreateSleepingAgent(
        int nutrition,
        int alertness,
        int health)
    {
        return new AgentState(
            AgentTestFactory.DefaultAgentId,
            "Sleeping Test",
            AgentTestFactory.CreateNeeds(
                nutrition,
                alertness,
                mood: 5_000,
                health: health),
            AgentTestFactory.CreateSleepSchedule(GameTimeCadence.TicksPerDay));
    }
}

}
