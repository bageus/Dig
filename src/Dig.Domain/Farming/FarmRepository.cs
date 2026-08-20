using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Farming;

namespace Dig.Application.Farming
{
    public sealed class InMemoryFarmRepository : IFarmRepository
    {
        private readonly Dictionary<EntityId, FarmState> _farms = new();

        public IReadOnlyCollection<EntityId> GetFarmIds()
        {
            return _farms.Keys;
        }

        public FarmState? Get(EntityId buildingId)
        {
            _farms.TryGetValue(buildingId, out var state);
            return state;
        }

        public void Save(EntityId buildingId, FarmState state)
        {
            _farms[buildingId] = state;
        }

        public void Remove(EntityId buildingId)
        {
            _farms.Remove(buildingId);
        }
    }
}
