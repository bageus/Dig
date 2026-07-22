using System;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static ItemLocation ParseLocation(ItemLocationSaveData data)
    {
        if (data is null || !Enum.IsDefined(typeof(ItemLocationKind), data.Kind))
        {
            throw new InvalidOperationException("Item location save data is invalid.");
        }

        ItemLocationKind kind = (ItemLocationKind)data.Kind;
        return kind switch
        {
            ItemLocationKind.World => ItemLocation.InWorld(ParseCell(data)),
            ItemLocationKind.AgentInventory => ParseAgentLocation(data),
            ItemLocationKind.BuildingInventory => ParseOwnedLocation(data, kind),
            ItemLocationKind.Storage => ParseOwnedLocation(data, kind),
            ItemLocationKind.Equipped => ParseOwnedLocation(data, kind),
            _ => throw new InvalidOperationException("Unsupported item location kind."),
        };
    }

    private static ItemLocation ParseAgentLocation(ItemLocationSaveData data)
    {
        EntityId ownerId = EntityId.Parse(RequireOwner(data));
        if (!data.ResidentCompartment.HasValue
            && !data.ResidentSlotIndex.HasValue)
        {
            return ItemLocation.InAgent(ownerId);
        }

        if (!data.ResidentCompartment.HasValue
            || !data.ResidentSlotIndex.HasValue
            || data.ResidentSlotIndex.Value < 0
            || !Enum.IsDefined(
                typeof(ResidentInventoryCompartment),
                data.ResidentCompartment.Value))
        {
            throw new InvalidOperationException("Resident inventory location is malformed.");
        }

        return ItemLocation.InResidentSlot(
            ownerId,
            (ResidentInventoryCompartment)data.ResidentCompartment.Value,
            data.ResidentSlotIndex.Value);
    }

    private static ItemLocation ParseOwnedLocation(
        ItemLocationSaveData data,
        ItemLocationKind kind)
    {
        EnsureNoResidentSlot(data);
        EntityId ownerId = EntityId.Parse(RequireOwner(data));
        return kind switch
        {
            ItemLocationKind.BuildingInventory => ItemLocation.InBuilding(ownerId),
            ItemLocationKind.Storage => ItemLocation.InStorage(ownerId),
            ItemLocationKind.Equipped => ItemLocation.EquippedBy(ownerId),
            _ => throw new InvalidOperationException("Unsupported owned item location."),
        };
    }

    private static CellId ParseCell(ItemLocationSaveData data)
    {
        EnsureNoResidentSlot(data);
        if (!data.CellX.HasValue
            || !data.CellY.HasValue
            || !data.CellZ.HasValue
            || data.OwnerId is not null)
        {
            throw new InvalidOperationException("World item location is malformed.");
        }

        return new CellId(data.CellX.Value, data.CellY.Value, data.CellZ.Value);
    }

    private static string RequireOwner(ItemLocationSaveData data)
    {
        if (string.IsNullOrWhiteSpace(data.OwnerId)
            || data.CellX.HasValue
            || data.CellY.HasValue
            || data.CellZ.HasValue)
        {
            throw new InvalidOperationException("Owned item location is malformed.");
        }

        return data.OwnerId;
    }

    private static void EnsureNoResidentSlot(ItemLocationSaveData data)
    {
        if (data.ResidentCompartment.HasValue || data.ResidentSlotIndex.HasValue)
        {
            throw new InvalidOperationException(
                "Only resident inventory locations may contain a resident slot.");
        }
    }
}

}
