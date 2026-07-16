using System;

namespace Dig.Domain.Inventory
{
public sealed class EquipmentProfile
{
    public EquipmentProfile(
        ItemId itemId,
        EquipmentAppearanceKind appearanceKind,
        EquipmentWorkKind workKind,
        int workIntervalTicks)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        if (workIntervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workIntervalTicks));
        }

        ItemId = itemId;
        AppearanceKind = appearanceKind;
        WorkKind = workKind;
        WorkIntervalTicks = workIntervalTicks;
    }

    public ItemId ItemId { get; }
    public EquipmentAppearanceKind AppearanceKind { get; }
    public EquipmentWorkKind WorkKind { get; }
    public int WorkIntervalTicks { get; }
}
}
