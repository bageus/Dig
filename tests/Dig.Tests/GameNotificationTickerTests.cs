using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Presentation.Notifications;
using Xunit;

namespace Dig.Tests
{

public sealed class GameNotificationTickerTests
{
    [Fact]
    public void Duplicate_source_event_is_not_enqueued_twice()
    {
        EntityId agentId = Id(1);
        AgentNeedThresholdCrossed need = new AgentNeedThresholdCrossed(
            tick: 10,
            agentId,
            AgentNeedThresholdKind.Hunger,
            threshold: 1_500,
            previousValue: 1_500,
            currentValue: 1_499);
        GameNotificationTicker ticker = new GameNotificationTicker();

        int added = ticker.Ingest(new IDomainEvent[] { need, need });

        Assert.Equal(1, added);
        Assert.Equal(1, ticker.ActiveCount);
        Assert.Equal(need.EventId, ticker.Current!.SourceEventKey);
    }

    [Fact]
    public void Higher_priority_notification_is_shown_first_without_merging()
    {
        EntityId agentId = Id(2);
        JobStatusChanged completed = new JobStatusChanged(
            tick: 4,
            jobId: Id(20),
            previousStatus: JobStatus.InProgress,
            currentStatus: JobStatus.Completed,
            agentId,
            reasonCode: null);
        AgentNeedThresholdCrossed hunger = new AgentNeedThresholdCrossed(
            tick: 5,
            agentId,
            AgentNeedThresholdKind.Hunger,
            threshold: 1_500,
            previousValue: 1_501,
            currentValue: 1_499);
        GameNotificationTicker ticker = new GameNotificationTicker();

        ticker.Ingest(new IDomainEvent[] { completed, hunger });

        Assert.Equal(2, ticker.ActiveCount);
        Assert.Equal(GameNotificationKind.ResidentHungry, ticker.Current!.Kind);
        GameNotification? dismissed = ticker.DismissCurrent();
        Assert.NotNull(dismissed);
        Assert.False(dismissed!.IsActive);
        Assert.Equal(GameNotificationKind.JobCompleted, ticker.Current!.Kind);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
