using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        internal Result UseResidentInventoryItemWithSlotGuard(
            string residentId,
            string stackId,
            long tick)
        {
            EntityId actor = ParseInventoryEntityId(residentId, nameof(residentId));
            bool occupied = LoadResidentEquipment().Any(model => string.Equals(
                model.ResidentId,
                actor.ToString(),
                StringComparison.Ordinal));
            return occupied
                ? Result.Failure(InventoryErrors.ToolSlotOccupied)
                : UseResidentInventoryItem(residentId, stackId, tick);
        }
    }
}