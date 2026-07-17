using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{
public sealed class EquipmentRates
{
    private readonly Dictionary<ItemId, EquipmentProfile> _profiles;

    public EquipmentRates(IEnumerable<EquipmentProfile> profiles)
    {
        if (profiles == null)
        {
            throw new ArgumentNullException(nameof(profiles));
        }

        _profiles = profiles.ToDictionary(profile => profile.ItemId);
    }

    public EquipmentAppearanceKind ResolveAppearance(ItemId itemId)
    {
        EquipmentProfile? profile = Find(itemId);
        return profile?.AppearanceKind ?? EquipmentAppearanceKind.Generic;
    }

    public EquipmentWorkKind? ResolveWorkKind(ItemId itemId)
    {
        return Find(itemId)?.WorkKind;
    }

    public int ResolveIntervalTicks(
        EntityId residentId,
        EquipmentWorkKind workKind,
        int baseIntervalTicks,
        params InventorySnapshot[] snapshots)
    {
        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (!Enum.IsDefined(typeof(EquipmentWorkKind), workKind))
        {
            throw new ArgumentOutOfRangeException(nameof(workKind));
        }

        if (baseIntervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseIntervalTicks));
        }

        if (snapshots == null || snapshots.Any(snapshot => snapshot is null))
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        ItemStackSnapshot[] equipped = snapshots
            .SelectMany(snapshot => snapshot.Stacks)
            .Where(stack => stack.Location == ItemLocation.EquippedBy(residentId))
            .ToArray();
        if (equipped.Length > 1)
        {
            throw new InvalidOperationException(
                "A resident cannot have more than one equipped item.");
        }

        if (equipped.Length == 0)
        {
            return baseIntervalTicks;
        }

        EquipmentProfile? profile = Find(equipped[0].ItemId);
        return profile != null && profile.WorkKind == workKind
            ? Math.Min(baseIntervalTicks, profile.WorkIntervalTicks)
            : baseIntervalTicks;
    }

    private EquipmentProfile? Find(ItemId itemId)
    {
        return _profiles.TryGetValue(itemId, out EquipmentProfile? profile)
            ? profile
            : null;
    }
}
}
