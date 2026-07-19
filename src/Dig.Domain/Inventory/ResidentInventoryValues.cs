using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public enum ResidentInventoryCompartment
{
    Main = 0,
    Cargo = 1,
    Weapon = 2,
}

public enum InventoryExpansionGroup
{
    Cargo = 0,
    Weapon = 1,
}

public readonly struct ResidentInventorySlot
    : IEquatable<ResidentInventorySlot>, IComparable<ResidentInventorySlot>
{
    public ResidentInventorySlot(
        ResidentInventoryCompartment compartment,
        int index)
    {
        if (!Enum.IsDefined(typeof(ResidentInventoryCompartment), compartment))
        {
            throw new ArgumentOutOfRangeException(nameof(compartment));
        }

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Compartment = compartment;
        Index = index;
    }

    public ResidentInventoryCompartment Compartment { get; }

    public int Index { get; }

    public int CompareTo(ResidentInventorySlot other)
    {
        int compartment = Compartment.CompareTo(other.Compartment);
        return compartment != 0 ? compartment : Index.CompareTo(other.Index);
    }

    public bool Equals(ResidentInventorySlot other)
    {
        return Compartment == other.Compartment && Index == other.Index;
    }

    public override bool Equals(object? obj)
    {
        return obj is ResidentInventorySlot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Compartment, Index);
    }

    public override string ToString()
    {
        return $"{Compartment}:{Index}";
    }

    public static bool operator ==(ResidentInventorySlot left, ResidentInventorySlot right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ResidentInventorySlot left, ResidentInventorySlot right)
    {
        return !left.Equals(right);
    }
}

public sealed class InventoryExpansionDefinition
{
    private readonly ItemCategoryId[] _acceptedCategories;

    public InventoryExpansionDefinition(
        InventoryExpansionGroup group,
        int tier,
        int addedSlots,
        IEnumerable<ItemCategoryId> acceptedCategories,
        double moveSpeedMultiplierWhenOccupied,
        string visualAttachmentId)
    {
        if (!Enum.IsDefined(typeof(InventoryExpansionGroup), group))
        {
            throw new ArgumentOutOfRangeException(nameof(group));
        }

        if (tier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tier));
        }

        if (addedSlots <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(addedSlots));
        }

        if (acceptedCategories is null)
        {
            throw new ArgumentNullException(nameof(acceptedCategories));
        }

        if (moveSpeedMultiplierWhenOccupied <= 0d
            || moveSpeedMultiplierWhenOccupied > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(moveSpeedMultiplierWhenOccupied));
        }

        if (string.IsNullOrWhiteSpace(visualAttachmentId))
        {
            throw new ArgumentException(
                "Visual attachment id is required.",
                nameof(visualAttachmentId));
        }

        _acceptedCategories = acceptedCategories
            .Distinct()
            .OrderBy(category => category)
            .ToArray();
        if (_acceptedCategories.Length == 0)
        {
            throw new ArgumentException(
                "An inventory expansion must accept at least one item category.",
                nameof(acceptedCategories));
        }

        Group = group;
        Tier = tier;
        AddedSlots = addedSlots;
        MoveSpeedMultiplierWhenOccupied = moveSpeedMultiplierWhenOccupied;
        VisualAttachmentId = visualAttachmentId.Trim();
    }

    public InventoryExpansionGroup Group { get; }

    public int Tier { get; }

    public int AddedSlots { get; }

    public double MoveSpeedMultiplierWhenOccupied { get; }

    public string VisualAttachmentId { get; }

    public bool IsMainCompartmentOnly => true;

    public IReadOnlyList<ItemCategoryId> AcceptedCategories =>
        new ReadOnlyCollection<ItemCategoryId>(_acceptedCategories);

    public bool Accepts(ItemDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        for (int index = 0; index < _acceptedCategories.Length; index++)
        {
            if (definition.HasCategory(_acceptedCategories[index]))
            {
                return true;
            }
        }

        return false;
    }
}

public readonly struct ActiveInventoryExpansionSnapshot
{
    public ActiveInventoryExpansionSnapshot(
        EntityId stackId,
        ItemId itemId,
        InventoryExpansionDefinition definition)
    {
        if (stackId.IsEmpty || itemId.IsEmpty)
        {
            throw new ArgumentException("Expansion stack and item ids are required.");
        }

        StackId = stackId;
        ItemId = itemId;
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public EntityId StackId { get; }

    public ItemId ItemId { get; }

    public InventoryExpansionDefinition Definition { get; }
}

public readonly struct ResidentInventorySlotSnapshot
{
    public ResidentInventorySlotSnapshot(
        ResidentInventorySlot slot,
        EntityId? stackId,
        ItemId? itemId,
        int quantity,
        int reservedQuantity,
        bool isActiveExpansion)
    {
        if (quantity < 0 || reservedQuantity < 0 || reservedQuantity > quantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (stackId.HasValue != itemId.HasValue)
        {
            throw new ArgumentException("Stack and item ids must both be present or absent.");
        }

        if (!stackId.HasValue && (quantity != 0 || reservedQuantity != 0))
        {
            throw new ArgumentException("An empty slot cannot contain quantity.");
        }

        Slot = slot;
        StackId = stackId;
        ItemId = itemId;
        Quantity = quantity;
        ReservedQuantity = reservedQuantity;
        IsActiveExpansion = isActiveExpansion;
    }

    public ResidentInventorySlot Slot { get; }

    public EntityId? StackId { get; }

    public ItemId? ItemId { get; }

    public int Quantity { get; }

    public int ReservedQuantity { get; }

    public int AvailableQuantity => Quantity - ReservedQuantity;

    public bool IsEmpty => !StackId.HasValue;

    public bool IsActiveExpansion { get; }
}

public sealed class ResidentInventoryLayoutSnapshot
{
    public const int MainSlotCount = 6;

    public ResidentInventoryLayoutSnapshot(
        EntityId residentId,
        int cargoCapacity,
        int weaponCapacity,
        ActiveInventoryExpansionSnapshot? activeCargoExpansion,
        ActiveInventoryExpansionSnapshot? activeWeaponExpansion,
        IEnumerable<ResidentInventorySlotSnapshot> slots)
    {
        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (cargoCapacity < 0 || weaponCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cargoCapacity));
        }

        if (slots is null)
        {
            throw new ArgumentNullException(nameof(slots));
        }

        ResidentId = residentId;
        CargoCapacity = cargoCapacity;
        WeaponCapacity = weaponCapacity;
        ActiveCargoExpansion = activeCargoExpansion;
        ActiveWeaponExpansion = activeWeaponExpansion;
        Slots = new ReadOnlyCollection<ResidentInventorySlotSnapshot>(
            slots.OrderBy(value => value.Slot).ToArray());
    }

    public EntityId ResidentId { get; }

    public int MainCapacity => MainSlotCount;

    public int CargoCapacity { get; }

    public int WeaponCapacity { get; }

    public ActiveInventoryExpansionSnapshot? ActiveCargoExpansion { get; }

    public ActiveInventoryExpansionSnapshot? ActiveWeaponExpansion { get; }

    public IReadOnlyList<ResidentInventorySlotSnapshot> Slots { get; }

    public int GetCapacity(ResidentInventoryCompartment compartment)
    {
        return compartment switch
        {
            ResidentInventoryCompartment.Main => MainCapacity,
            ResidentInventoryCompartment.Cargo => CargoCapacity,
            ResidentInventoryCompartment.Weapon => WeaponCapacity,
            _ => throw new ArgumentOutOfRangeException(nameof(compartment)),
        };
    }
}

}
