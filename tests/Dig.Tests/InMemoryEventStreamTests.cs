using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class InMemoryEventStreamTests
{
    [Fact]
    public void Sequence_cursor_remains_monotonic_when_old_events_are_trimmed()
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal(
            maximumEvents: 2);
        journal.Append(new IDomainEvent[] { Completed(1, 1), Completed(2, 2) });
        long cursor = Assert.Single(journal.ReadEventsAfter(0, maximumCount: 1)).Sequence;
        journal.Append(new IDomainEvent[] { Completed(3, 3) });

        var remaining = journal.ReadEventsAfter(cursor, maximumCount: 10);

        Assert.Equal(2, remaining.Count);
        Assert.Equal(2, remaining[0].Sequence);
        Assert.Equal(3, remaining[1].Sequence);
        Assert.Equal(3, journal.LatestEventSequence);
        Assert.Equal(1, journal.DroppedEventCount);
    }

    private static JobStatusChanged Completed(int id, long tick)
    {
        return new JobStatusChanged(
            tick,
            EntityId.Parse(id.ToString("x32")),
            JobStatus.InProgress,
            JobStatus.Completed,
            agentId: null,
            reasonCode: null);
    }
}

}
