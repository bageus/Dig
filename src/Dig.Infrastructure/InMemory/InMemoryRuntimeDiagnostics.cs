using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Application.Runtime;
using Dig.Domain.Core;

namespace Dig.Infrastructure.InMemory
{

public readonly struct JournalEventEntry
{
    public JournalEventEntry(long sequence, IDomainEvent domainEvent)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        Sequence = sequence;
        DomainEvent = domainEvent
            ?? throw new ArgumentNullException(nameof(domainEvent));
    }

    public long Sequence { get; }
    public IDomainEvent DomainEvent { get; }
}

public sealed class InMemoryExecutionJournal
    : IExecutionJournal,
      IJobAssignmentReportSink,
      IJobAssignmentReportSource
{
    private readonly object _gate = new object();
    private readonly List<CommandJournalEntry> _commands = new List<CommandJournalEntry>();
    private readonly List<IDomainEvent> _events = new List<IDomainEvent>();
    private readonly List<long> _eventSequences = new List<long>();
    private readonly Dictionary<EntityId, JobAssignmentReport> _jobAssignmentReports =
        new Dictionary<EntityId, JobAssignmentReport>();
    private readonly int? _maximumCommands;
    private readonly int? _maximumEvents;
    private long _nextEventSequence = 1;

    public InMemoryExecutionJournal(
        int? maximumCommands = null,
        int? maximumEvents = null)
    {
        ValidateCapacity(maximumCommands, nameof(maximumCommands));
        ValidateCapacity(maximumEvents, nameof(maximumEvents));
        _maximumCommands = maximumCommands;
        _maximumEvents = maximumEvents;
    }

    public long DroppedCommandCount { get; private set; }

    public long DroppedEventCount { get; private set; }

    public IReadOnlyList<CommandJournalEntry> Commands
    {
        get
        {
            lock (_gate)
            {
                return _commands.ToArray();
            }
        }
    }

    public IReadOnlyList<IDomainEvent> Events
    {
        get
        {
            lock (_gate)
            {
                return _events.ToArray();
            }
        }
    }

    public long LatestEventSequence
    {
        get
        {
            lock (_gate)
            {
                return _nextEventSequence - 1;
            }
        }
    }

    public IReadOnlyDictionary<EntityId, JobAssignmentReport> JobAssignmentReports
    {
        get
        {
            lock (_gate)
            {
                return new ReadOnlyDictionary<EntityId, JobAssignmentReport>(
                    new Dictionary<EntityId, JobAssignmentReport>(_jobAssignmentReports));
            }
        }
    }

    public void RecordCommand(CommandJournalEntry entry)
    {
        lock (_gate)
        {
            _commands.Add(entry);
            DroppedCommandCount = checked(DroppedCommandCount
                + TrimOldest(_commands, _maximumCommands));
        }
    }

    public void Append(IReadOnlyCollection<IDomainEvent> events)
    {
        if (events is null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        lock (_gate)
        {
            foreach (IDomainEvent domainEvent in events)
            {
                if (domainEvent is null)
                {
                    throw new ArgumentException(
                        "Journal events cannot contain null.",
                        nameof(events));
                }

                _events.Add(domainEvent);
                _eventSequences.Add(_nextEventSequence);
                _nextEventSequence = checked(_nextEventSequence + 1);
            }

            int removed = TrimOldest(_events, _maximumEvents);
            if (removed > 0)
            {
                _eventSequences.RemoveRange(0, removed);
            }

            DroppedEventCount = checked(DroppedEventCount + removed);
        }
    }

    public IReadOnlyList<JournalEventEntry> ReadEventsAfter(
        long sequenceExclusive,
        int maximumCount = 256)
    {
        if (sequenceExclusive < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequenceExclusive));
        }

        if (maximumCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        lock (_gate)
        {
            int start = _eventSequences.FindIndex(value => value > sequenceExclusive);
            if (start < 0)
            {
                return Array.Empty<JournalEventEntry>();
            }

            int count = Math.Min(maximumCount, _events.Count - start);
            JournalEventEntry[] entries = new JournalEventEntry[count];
            for (int index = 0; index < count; index++)
            {
                entries[index] = new JournalEventEntry(
                    _eventSequences[start + index],
                    _events[start + index]);
            }

            return entries;
        }
    }

    public void Record(JobAssignmentReport report)
    {
        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        lock (_gate)
        {
            foreach (JobAssignment assignment in report.Assignments)
            {
                _jobAssignmentReports[assignment.JobId] = new JobAssignmentReport(
                    report.Tick,
                    new[] { assignment },
                    Array.Empty<JobAssignmentFailure>());
            }
        }
    }

    public JobAssignmentReport? Find(EntityId jobId)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        lock (_gate)
        {
            return _jobAssignmentReports.TryGetValue(jobId, out JobAssignmentReport? report)
                ? report
                : null;
        }
    }

    private static int TrimOldest<T>(List<T> values, int? maximum)
    {
        if (!maximum.HasValue || values.Count <= maximum.Value)
        {
            return 0;
        }

        int removeCount = values.Count - maximum.Value;
        values.RemoveRange(0, removeCount);
        return removeCount;
    }

    private static void ValidateCapacity(int? capacity, string parameterName)
    {
        if (capacity.HasValue && capacity.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public sealed class InMemorySimulationTrace : ISimulationTrace
{
    private readonly object _gate = new object();
    private readonly List<SystemExecution> _executions = new List<SystemExecution>();

    public IReadOnlyList<SystemExecution> Executions
    {
        get
        {
            lock (_gate)
            {
                return _executions.ToArray();
            }
        }
    }

    public void Record(SystemExecution execution)
    {
        lock (_gate)
        {
            _executions.Add(execution);
        }
    }
}
}
