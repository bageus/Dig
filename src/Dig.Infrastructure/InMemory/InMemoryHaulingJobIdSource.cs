using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Core;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryHaulingJobIdSource : IHaulingJobIdSource
{
    private readonly Queue<EntityId> _ids;

    public InMemoryHaulingJobIdSource(IEnumerable<EntityId> ids)
    {
        if (ids is null)
        {
            throw new ArgumentNullException(nameof(ids));
        }

        EntityId[] values = ids.ToArray();
        if (values.Any(value => value.IsEmpty))
        {
            throw new ArgumentException("Hauling job ids cannot be empty.", nameof(ids));
        }

        _ids = new Queue<EntityId>(values);
    }

    public int Remaining => _ids.Count;

    public EntityId Next()
    {
        if (_ids.Count == 0)
        {
            throw new InvalidOperationException("No hauling job ids remain.");
        }

        return _ids.Dequeue();
    }
}
}
