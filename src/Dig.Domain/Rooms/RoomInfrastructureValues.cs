using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Rooms
{

public enum RoomTemplateKind
{
    Small = 0,
    Medium = 1,
    Large = 2,
    Tall = 3,
}

public enum RoomPurposeKind
{
    None = 0,
    Bedroom = 1,
    KitchenDining = 2,
    Workshop = 3,
    Farm = 4,
}

public enum RoomImprovementStatus
{
    Unimproved = 0,
    AwaitingMaterials = 1,
    ReadyForWork = 2,
    Improving = 3,
    Improved = 4,
}

public readonly struct RoomMaterialUnitId : IEquatable<RoomMaterialUnitId>, IComparable<RoomMaterialUnitId>
{
    public RoomMaterialUnitId(ItemId itemId, int ordinal)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Room material unit item id cannot be empty.", nameof(itemId));
        }

        if (ordinal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal));
        }

        ItemId = itemId;
        Ordinal = ordinal;
    }

    public ItemId ItemId { get; }

    public int Ordinal { get; }

    public int CompareTo(RoomMaterialUnitId other)
    {
        int itemComparison = ItemId.CompareTo(other.ItemId);
        return itemComparison != 0 ? itemComparison : Ordinal.CompareTo(other.Ordinal);
    }

    public bool Equals(RoomMaterialUnitId other)
    {
        return ItemId == other.ItemId && Ordinal == other.Ordinal;
    }

    public override bool Equals(object? obj)
    {
        return obj is RoomMaterialUnitId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ItemId, Ordinal);
    }

    public override string ToString()
    {
        return $"{ItemId}:{Ordinal}";
    }

    public static bool operator ==(RoomMaterialUnitId left, RoomMaterialUnitId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RoomMaterialUnitId left, RoomMaterialUnitId right)
    {
        return !left.Equals(right);
    }
}

public readonly struct RoomMaterialRequirement
{
    public RoomMaterialRequirement(ItemId itemId, int quantity)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Room material requirement item id cannot be empty.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        ItemId = itemId;
        Quantity = quantity;
    }

    public ItemId ItemId { get; }

    public int Quantity { get; }
}

public sealed class RoomMaterialCommitResult
{
    public RoomMaterialCommitResult(bool alreadyCommitted, bool improvementCompleted)
    {
        AlreadyCommitted = alreadyCommitted;
        ImprovementCompleted = improvementCompleted;
    }

    public bool AlreadyCommitted { get; }

    public bool ImprovementCompleted { get; }
}

}
