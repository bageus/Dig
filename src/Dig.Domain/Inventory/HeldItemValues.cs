using System;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public enum HeldItemPurpose
{
    ToolUse = 0,
    WeaponUse = 1,
    ItemUse = 2,
}

public readonly struct HeldItemReferenceSnapshot
{
    public HeldItemReferenceSnapshot(
        EntityId residentId,
        EntityId stackId,
        int quantity,
        HeldItemPurpose purpose)
    {
        if (residentId.IsEmpty || stackId.IsEmpty)
        {
            throw new ArgumentException("Resident and stack ids are required.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (!Enum.IsDefined(typeof(HeldItemPurpose), purpose))
        {
            throw new ArgumentOutOfRangeException(nameof(purpose));
        }

        ResidentId = residentId;
        StackId = stackId;
        Quantity = quantity;
        Purpose = purpose;
    }

    public EntityId ResidentId { get; }

    public EntityId StackId { get; }

    public int Quantity { get; }

    public HeldItemPurpose Purpose { get; }
}

}
