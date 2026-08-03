using Dig.Domain.Agents;
using Dig.Domain.Core;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentSleepRecoveryTests
{
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
        Assert.Equal(500, agent.CreateSnapshot(0).Needs.Health.Points);

        Result passive = agent.AdvanceNeeds(policy, tick: 1);
        AgentSnapshot sleeping = agent.CreateSnapshot(1);

        Assert.True(passive.IsSuccess);
        Assert.True(sleeping.IsAlive);
        Assert.Equal(550, sleeping.Needs.Health.Points);
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
        Assert.Equal(2_000, agent.CreateSnapshot(0).Needs.Health.Points);

        Assert.True(agent.AdvanceNeeds(policy, tick: 1).IsSuccess);
        AgentSnapshot sleeping = agent.CreateSnapshot(1);

        Assert.True(sleeping.IsAlive);
        Assert.Equal(1_500, sleeping.Needs.Health.Points);
        Assert.Equal(0, sleeping.Needs.Nutrition.Points);
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
            AgentTestFactory.CreateSleepSchedule());
    }
}

}
