using Dig.Application.Colonies;
using Dig.Application.Messaging;
using Dig.Domain.Colonies;
using Dig.Domain.Core;

namespace Dig.Infrastructure.InMemory;

public sealed class InMemoryColonyRepository : IColonyRepository
{
    private readonly object _gate = new object();
    private readonly Dictionary<EntityId, ColonyState> _colonies = new Dictionary<EntityId, ColonyState>();

    public ColonyState? Get(EntityId colonyId)
    {
        lock (_gate)
        {
            return _colonies.TryGetValue(colonyId, out ColonyState? colony)
                ? colony
                : null;
        }
    }

    public void Save(ColonyState colony)
    {
        if (colony is null)
        {
            throw new ArgumentNullException(nameof(colony));
        }

        lock (_gate)
        {
            _colonies[colony.Id] = colony;
        }
    }
}

public sealed class InMemoryEventJournal : IEventSink
{
    private readonly object _gate = new object();
    private readonly List<IDomainEvent> _events = new List<IDomainEvent>();

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
