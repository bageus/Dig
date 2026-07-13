using Dig.Application.Messaging;
using Dig.Application.Runtime;
using Dig.Domain.Core;

namespace Dig.Infrastructure.InMemory;

public sealed class InMemoryExecutionJournal : IExecutionJournal
{
    private readonly object _gate = new object();
    private readonly List<CommandJournalEntry> _commands = new List<CommandJournalEntry>();
    private readonly List<IDomainEvent> _events = new List<IDomainEvent>();

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

    public void RecordCommand(CommandJournalEntry entry)
    {
        lock (_gate)
        {
            _commands.Add(entry);
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
