using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Notifications;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentNeedNotificationTests
{
    [Fact]
    public void Hunger_event_is_raised_only_when_threshold_is_crossed_downward()
    {
        AgentState agent = AgentTestFactory.CreateAgent(nutrition: 1_600);

        Assert.True(agent.ApplyExternalNeedDelta(
            new NeedDelta(-100, 0, 0, 0),
            "test.hunger",
            tick: 1).IsSuccess);
        Assert.Empty(agent.DequeueUncommittedEvents().OfType<AgentNeedThresholdCrossed>());
        Assert.True(agent.ApplyExternalNeedDelta(
            new NeedDelta(-1, 0, 0, 0),
            "test.crossing",
            tick: 2).IsSuccess);

        AgentNeedThresholdCrossed crossed = Assert.Single(
            agent.DequeueUncommittedEvents().OfType<AgentNeedThresholdCrossed>());
        Assert.Equal(AgentNeedThresholdKind.Hunger, crossed.Kind);
        Assert.Equal(1_500, crossed.PreviousValue);
        Assert.Equal(1_499, crossed.CurrentValue);
        Assert.Equal(agent.Id, crossed.AgentId);

        Assert.True(agent.ApplyExternalNeedDelta(
            new NeedDelta(-100, 0, 0, 0),
            "test.still-hungry",
            tick: 3).IsSuccess);
        Assert.Empty(agent.DequeueUncommittedEvents().OfType<AgentNeedThresholdCrossed>());
    }

    [Fact]
    public void Recovery_allows_a_later_mood_crossing_to_notify_again()
    {
        AgentState agent = AgentTestFactory.CreateAgent(mood: 600);

        ApplyMood(agent, -101, "first", tick: 1);
        AgentNeedThresholdCrossed first = Assert.Single(
            agent.DequeueUncommittedEvents().OfType<AgentNeedThresholdCrossed>());
        ApplyMood(agent, 301, "recover", tick: 2);
        Assert.Empty(agent.DequeueUncommittedEvents().OfType<AgentNeedThresholdCrossed>());
        ApplyMood(agent, -301, "second", tick: 3);
        AgentNeedThresholdCrossed second = Assert.Single(
            agent.DequeueUncommittedEvents().OfType<AgentNeedThresholdCrossed>());

        Assert.Equal(AgentNeedThresholdKind.CriticalMood, first.Kind);
        Assert.Equal(AgentNeedThresholdKind.CriticalMood, second.Kind);
        Assert.NotEqual(first.EventId, second.EventId);
    }

    [Fact]
    public void Death_notification_navigates_to_authoritative_last_known_cell()
    {
        AgentState agent = AgentTestFactory.CreateAgent(health: 100);
        CellId position = new CellId(7, 4);
        Assert.True(agent.MoveTo(position, tick: 1).IsSuccess);
        agent.DequeueUncommittedEvents();

        Assert.True(agent.ApplyExternalNeedDelta(
            new NeedDelta(0, 0, 0, -100),
            "test.death",
            tick: 2).IsSuccess);
        AgentDied died = Assert.Single(
            agent.DequeueUncommittedEvents().OfType<AgentDied>());
        GameNotification notification = new GameNotificationProjector().Project(died)!;

        Assert.True(died.LastKnownPosition.HasValue);
        Assert.Equal(position, died.LastKnownPosition.Value);
        Assert.Equal(GameNotificationKind.ResidentDied, notification.Kind);
        Assert.Equal(
            GameNotificationNavigationKind.Cell,
            notification.NavigationTarget.Kind);
        Assert.True(notification.NavigationTarget.Cell.HasValue);
        Assert.Equal(position, notification.NavigationTarget.Cell.Value);
    }

    private static void ApplyMood(
        AgentState agent,
        int delta,
        string source,
        long tick)
    {
        Result result = agent.ApplyExternalNeedDelta(
            new NeedDelta(0, 0, delta, 0),
            source,
            tick);
        Assert.True(result.IsSuccess);
    }
}

}
