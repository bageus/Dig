using System;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Inventory
{

public enum ItemLocationKind
{
    World = 0,
    AgentInventory = 1,
    BuildingInventory = 2,
    Storage = 3,
    Equipped = 4,
}

public readonly struct ItemLocation : IEquatable<ItemLocation>, IComparable<ItemLocation>
{
    private ItemLocation(
        ItemLocationKind kind,
        EntityId ownerId,
        bool hasOwner,
        CellId cellId,
        bool hasCell,
        ResidentInventoryCompartment residentCompartment,
        int residentSlotIndex,
        bool hasResidentSlot)
    {
        Kind = kind;
        OwnerId = ownerId;
        HasOwner = hasOwner;
        CellId = cellId;
        HasCell = hasCell;
        ResidentCompartment = residentCompartment;
        ResidentSlotIndex = residentSlotIndex;
        HasResidentSlot = hasResidentSlot;
    }

    public ItemLocationKind Kind { get; }

    public EntityId OwnerId { get; }

    public bool HasOwner { get; }

    public CellId CellId { get; }

    public bool HasCell { get; }

    public ResidentInventoryCompartment ResidentCompartment { get; }

    public int ResidentSlotIndex { get; }

    public bool HasResidentSlot { get; }

    public ResidentInventorySlot ResidentSlot
    {
        get
        {
            if (!HasResidentSlot)
            {
                throw new InvalidOperationException(
                    "The item location does not identify a resident inventory slot.");
            }

            return new ResidentInventorySlot(ResidentCompartment, ResidentSlotIndex);
        }
    }

    public static ItemLocation InWorld(CellId cellId)
    {
        return new ItemLocation(
            ItemLocationKind.World,
            default,
            false,
            cellId,
            true,
            default,
            0,
            false);
    }

    public static ItemLocation InAgent(EntityId agentId)
    {
        return ForOwner(ItemLocationKind.AgentInventory, agentId);
    }

    public static ItemLocation InResidentSlot(
        EntityId agentId,
        ResidentInventoryCompartment compartment,
        int slotIndex)
    {
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Location owner id cannot be empty.", nameof(agentId));
        }

        ResidentInventorySlot slot = new ResidentInventorySlot(compartment, slotIndex);
        return new ItemLocation(
            ItemLocationKind.AgentInventory,
            agentId,
            true,
            default,
            false,
            slot.Compartment,
            slot.Index,
            true);
    }

    public static ItemLocation InBuilding(EntityId buildingId)
    {
        return ForOwner(ItemLocationKind.BuildingInventory, buildingId);
    }

    public static ItemLocation InStorage(EntityId storageId)
    {
        return ForOwner(ItemLocationKind.Storage, storageId);
    }

    public static ItemLocation EquippedBy(EntityId agentId)
    {
        return ForOwner(ItemLocationKind.Equipped, agentId);
    }

    public int CompareTo(ItemLocation other)
    {
        int kindComparison = Kind.CompareTo(other.Kind);
        if (kindComparison != 0)
        {
            return kindComparison;
        }

        int ownerComparison = string.Compare(
            HasOwner ? OwnerId.ToString() : string.Empty,
            other.HasOwner ? other.OwnerId.ToString() : string.Empty,
            StringComparison.Ordinal);
        if (ownerComparison != 0)
        {
            return ownerComparison;
        }

        int slotComparison = CompareResidentSlot(other);
        if (slotComparison != 0)
        {
            return slotComparison;
        }

        if (!HasCell && !other.HasCell)
        {
            return 0;
        }

        if (!HasCell)
        {
            return -1;
        }

        if (!other.HasCell)
        {
            return 1;
        }

        return CellId.CompareTo(other.CellId);
    }

    public bool Equals(ItemLocation other)
    {
        return Kind == other.Kind
            && HasOwner == other.HasOwner
            && (!HasOwner || OwnerId == other.OwnerId)
            && HasCell == other.HasCell
            && (!HasCell || CellId == other.CellId)
            && HasResidentSlot == other.HasResidentSlot
            && (!HasResidentSlot
                || (ResidentCompartment == other.ResidentCompartment
                    && ResidentSlotIndex == other.ResidentSlotIndex));
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Kind,
            HasOwner,
            OwnerId,
            HasCell,
            CellId,
            HasResidentSlot,
            ResidentCompartment,
            ResidentSlotIndex);
    }

    public override string ToString()
    {
        if (HasOwner)
        {
            return HasResidentSlot
                ? $"{Kind}:{OwnerId}:{ResidentCompartment}:{ResidentSlotIndex}"
                : $"{Kind}:{OwnerId}";
        }

        return HasCell ? $"{Kind}:{CellId}" : Kind.ToString();
    }

    public static bool operator ==(ItemLocation left, ItemLocation right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemLocation left, ItemLocation right)
    {
        return !left.Equals(right);
    }

    private int CompareResidentSlot(ItemLocation other)
    {
        if (!HasResidentSlot && !other.HasResidentSlot)
        {
            return 0;
        }

        if (!HasResidentSlot)
        {
            return -1;
        }

        if (!other.HasResidentSlot)
        {
            return 1;
        }

        int compartment = ResidentCompartment.CompareTo(other.ResidentCompartment);
        return compartment != 0
            ? compartment
            : ResidentSlotIndex.CompareTo(other.ResidentSlotIndex);
    }

    private static ItemLocation ForOwner(ItemLocationKind kind, EntityId ownerId)
    {
        if (ownerId.IsEmpty)
        {
            throw new ArgumentException("Location owner id cannot be empty.", nameof(ownerId));
        }

        return new ItemLocation(
            kind,
            ownerId,
            true,
            default,
            false,
            default,
            0,
            false);
    }
}

}
