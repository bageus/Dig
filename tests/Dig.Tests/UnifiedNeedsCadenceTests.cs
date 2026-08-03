using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Runtime;
using Xunit;

namespace Dig.Tests
{

public sealed class UnifiedNeedsCadenceTests
{
    [Fact]
    public void Passive_need_periods_reach_exact_calendar_endpoints()
    {
        AgentNeedPolicy policy = AgentBehaviorPolicy.CreateDefault().Needs;
        int nutrition = 0;
        int alertness = 0;
        for (long tick = 0; tick < GameTimeCadence.TicksFromDays(3); tick++)
        {
            NeedDelta delta = policy.ResolvePassiveDelta(
                tick,
                GameTimeCadence.TicksPerDay);
            if (tick < GameTimeCadence.TicksFromDays(2))
            {
                nutrition += delta.Nutrition;
            }

            alertness += delta.Alertness;
        }

        Assert.Equal(-NeedValue.Maximum, nutrition);
        Assert.Equal(-NeedValue.Maximum, alertness);
        Assert.Equal(GameTimeCadence.TicksPerDay * 4, policy.MoodFullDepletionTicks);
    }

    [Fact]
    public void Continuous_critical_hunger_depletes_health_over_half_a_day()
    {
        AgentState agent = new AgentState(
            EntityId.Parse("00000000000000000000000000000001"),
            "Hungry",
            new AgentNeedsSnapshot(
                new NeedValue(0),
                new NeedValue(10_000),
                new NeedValue(10_000),
                new NeedValue(10_000)),
            new DailySchedule(
                GameTimeCadence.TicksPerDay,
                new[]
                {
                    new ScheduleSegment(
                        0,
                        GameTimeCadence.TicksPerDay,
                        ScheduleActivity.Work),
                }));
        AgentBehaviorPolicy policy = AgentBehaviorPolicy.CreateDefault();
        long starvationTicks = GameTimeCadence.TicksFromHours(12);

        for (long tick = 0; tick < starvationTicks - 1; tick++)
        {
            Assert.True(agent.AdvanceNeeds(policy, tick).IsSuccess);
        }

        Assert.True(agent.IsAlive);
        Assert.True(agent.CreateSnapshot(starvationTicks - 2).Needs.Health.Points > 0);
        Assert.True(agent.AdvanceNeeds(policy, starvationTicks - 1).IsSuccess);
        Assert.False(agent.IsAlive);
        Assert.Equal(
            0,
            agent.CreateSnapshot(starvationTicks - 1).Needs.Health.Points);
    }
}

}
