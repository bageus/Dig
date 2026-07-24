using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        public IReadOnlyDictionary<string, CellId> ApplyResidentMovementCadence(
            IReadOnlyDictionary<string, CellId> plannedMovement,
            long tick)
        {
            if (plannedMovement == null)
            {
                throw new ArgumentNullException(nameof(plannedMovement));
            }

            if (plannedMovement.Count == 0)
            {
                return plannedMovement;
            }

            Dictionary<string, CellId> due =
                new Dictionary<string, CellId>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, CellId> movement in plannedMovement)
            {
                EntityId residentId = EntityId.Parse(movement.Key);
                if (IsManualExcavationAgent(residentId)
                    || _inventoryRepository.Get().IsResidentMovementDue(residentId, tick))
                {
                    due.Add(movement.Key, movement.Value);
                }
            }

            return due;
        }
    }
}
