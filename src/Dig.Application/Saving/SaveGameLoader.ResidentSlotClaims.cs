using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static Result RestoreResidentSlotClaims(
        InventorySaveData data,
        InventoryState inventory)
    {
        if (data.ResidentSlotClaims == null || data.ResidentSlotClaims.Count == 0)
        {
            return Result.Success();
        }

        List<ResidentInventorySlotClaimSnapshot> claims = data.ResidentSlotClaims
            .OrderBy(value => value.JobId, StringComparer.Ordinal)
            .ThenBy(value => value.Compartment)
            .ThenBy(value => value.SlotIndex)
            .Select(ParseResidentSlotClaim)
            .ToList();
        return inventory.RestoreResidentSlotClaims(claims);
    }

    private static ResidentInventorySlotClaimSnapshot ParseResidentSlotClaim(
        ResidentSlotClaimSaveData data)
    {
        if (data == null
            || !Enum.IsDefined(
                typeof(ResidentInventoryCompartment),
                data.Compartment)
            || data.SlotIndex < 0
            || data.Quantity <= 0)
        {
            throw new InvalidOperationException("Resident slot claim save data is invalid.");
        }

        return new ResidentInventorySlotClaimSnapshot(
            EntityId.Parse(data.JobId),
            EntityId.Parse(data.ResidentId),
            new ItemId(data.ItemId),
            new ResidentInventorySlot(
                (ResidentInventoryCompartment)data.Compartment,
                data.SlotIndex),
            data.Quantity);
    }
}

}
