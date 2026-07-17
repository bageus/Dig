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

public sealed class InMemoryExecutionJournal
    : IExecutionJournal,
      IJobAssignmentReportSink,
      IJobAssignmentReportSource
{
    private readonly object _gate = new object();
    private readonly List<CommandJournalEntry> _commands = new List<CommandJournalEntry>();
    private readonly List<IDomainEvent> _events = new List<IDomainEvent>();
    private readonly Dictionary<EntityId, JobAssignmentReport> _jobAssignmentReports =
        new Dictionary<EntityId, JobAssignmentReport>();
    private readonly int? _maximumCommands;
    private readonly int? _maximumEvents;

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
            _events.AddRange(events);
            DroppedEventCount = checked(DroppedEventCount
                + TrimOldest(_events, _maximumEvents));
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
