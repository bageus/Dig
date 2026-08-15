using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Farming;

namespace Dig.Application.Farming
{

public sealed class InMemoryFarmRepository : IFarmRepository
{
    private readonly Dictionary<EntityId, FarmSnapshot> _snapshots =
        new Dictionary<EntityId, FarmSnapshot>();

    public IReadOnlyCollection<EntityId> GetFarmIds()
    {
        return _snapshots.Keys.OrderBy(value => value.ToString()).ToArray();
    }

    public FarmState? Get(EntityId buildingId)
    {
        return _snapshots.TryGetValue(buildingId, out FarmSnapshot? snapshot)
            ? FarmState.Restore(snapshot)
            : null;
    }

    public void Save(EntityId buildingId, FarmState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        _snapshots[buildingId] = state.CreateSnapshot();
    }

    public void Remove(EntityId buildingId)
    {
        _snapshots.Remove(buildingId);
    }
}

}
