using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Storage;

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

        StorageZoneState? selected = null;
        int selectedOccupied = 0;
        foreach (StorageZoneState zone in _zones.Values)
        {
            if (!zone.Definition.Filter.Accepts(item))
            {
                continue;
            }

            int occupied = occupiedQuantityProvider(zone.Definition.Id);
            int available = zone.Definition.Capacity - occupied - zone.ReservedIncoming;
            if (available < minimumQuantity)
            {
                continue;
            }

            if (selected is null || CompareZones(zone, selected) < 0)
            {
                selected = zone;
                selectedOccupied = occupied;
            }
        }

        return selected is null
            ? null
            : CreateSnapshot(selected, selectedOccupied);
    }

    private static int CompareZones(StorageZoneState left, StorageZoneState right)
    {
        int priorityComparison = right.Definition.Priority.CompareTo(
            left.Definition.Priority);
        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        return string.Compare(
            left.Definition.Id.ToString(),
            right.Definition.Id.ToString(),
            StringComparison.Ordinal);
    }
}
