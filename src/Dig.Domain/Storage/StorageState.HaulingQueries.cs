using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Storage
{

public sealed partial class StorageState
{
    public StorageZoneSnapshot? FindFirstDestination(
        ItemDefinition item,
        int minimumQuantity,
        Func<EntityId, int> occupiedQuantityProvider)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (occupiedQuantityProvider is null)
        {
            throw new ArgumentNullException(nameof(occupiedQuantityProvider));
        }

        if (minimumQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumQuantity));
        }

        StorageZoneDefinition? selected = null;
        int selectedOccupied = 0;
        int selectedReserved = 0;
        foreach (StorageZoneDefinition zone in _zones.Values)
        {
            if (!zone.Filter.Accepts(item))
            {
                continue;
            }

            int occupied = occupiedQuantityProvider(zone.Id);
            int reserved = GetReservedIncoming(zone.Id);
            int available = zone.Capacity - occupied - reserved;
            if (available < minimumQuantity)
            {
                continue;
            }

            if (selected is null || CompareZones(zone, selected) < 0)
            {
                selected = zone;
                selectedOccupied = occupied;
                selectedReserved = reserved;
            }
        }

        return selected is null
            ? null
            : new StorageZoneSnapshot(selected, selectedOccupied, selectedReserved);
    }

    private static int CompareZones(
        StorageZoneDefinition left,
        StorageZoneDefinition right)
    {
        int priorityComparison = right.Priority.CompareTo(left.Priority);
        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        return string.Compare(
            left.Id.ToString(),
            right.Id.ToString(),
            StringComparison.Ordinal);
    }
}
}
