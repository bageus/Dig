using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result RestoreResidentSlotClaims(
        IReadOnlyCollection<ResidentInventorySlotClaimSnapshot> claims)
    {
        if (claims is null)
        {
            throw new ArgumentNullException(nameof(claims));
        }

        if (_residentSlotClaims.Count != 0)
        {
            return Result.Failure(InventoryErrors.ResidentSlotClaimConflict);
        }

        ResidentInventorySlotClaimSnapshot[] ordered = claims
            .OrderBy(claim => claim.JobId.ToString(), StringComparer.Ordinal)
            .ThenBy(claim => claim.Slot.Compartment)
            .ThenBy(claim => claim.Slot.Index)
            .ToArray();
        _residentSlotClaims.AddRange(ordered);
        Result validation = ValidateResidentSlotClaims();
        if (validation.IsFailure)
        {
            _residentSlotClaims.Clear();
            return validation;
        }

        return Result.Success();
    }

    public Result ValidateResidentSlotClaims()
    {
        foreach (IGrouping<EntityId, ResidentInventorySlotClaimSnapshot> jobClaims
            in _residentSlotClaims.GroupBy(claim => claim.JobId))
        {
            if (jobClaims.Select(claim => claim.ResidentId).Distinct().Count() != 1
                || jobClaims.Select(claim => claim.ItemId).Distinct().Count() != 1)
            {
                return Result.Failure(InventoryErrors.ResidentSlotClaimConflict);
            }
        }

        foreach (IGrouping<(EntityId ResidentId, ResidentInventorySlot Slot),
            ResidentInventorySlotClaimSnapshot> slotClaims in _residentSlotClaims
            .GroupBy(claim => (claim.ResidentId, claim.Slot)))
        {
            Result validated = ValidateClaimedSlot(slotClaims.Key, slotClaims.ToArray());
            if (validated.IsFailure)
            {
                return validated;
            }
        }

        return Result.Success();
    }

    private Result ValidateClaimedSlot(
        (EntityId ResidentId, ResidentInventorySlot Slot) key,
        IReadOnlyCollection<ResidentInventorySlotClaimSnapshot> claims)
    {
        ResidentInventoryLayoutSnapshot layout =
            GetResidentInventoryLayout(key.ResidentId);
        ResidentInventorySlotSnapshot? slotSnapshot = layout.Slots
            .SingleOrDefault(value => value.Slot == key.Slot);
        if (!slotSnapshot.HasValue)
        {
            return Result.Failure(InventoryErrors.ResidentSlotClaimStale);
        }

        ItemId[] itemIds = claims.Select(claim => claim.ItemId).Distinct().ToArray();
        if (itemIds.Length != 1)
        {
            return Result.Failure(InventoryErrors.ResidentSlotClaimConflict);
        }

        ItemDefinition definition = Catalog.Get(itemIds[0]);
        if (!CanClaimSlot(definition, key.Slot, layout))
        {
            return Result.Failure(InventoryErrors.ResidentSlotClaimStale);
        }

        int existingQuantity = slotSnapshot.Value.IsEmpty
            ? 0
            : slotSnapshot.Value.ItemId == definition.Id
                ? slotSnapshot.Value.Quantity
                : definition.MaximumStackSize;
        int claimedQuantity = claims.Sum(claim => claim.Quantity);
        return existingQuantity + claimedQuantity <= definition.MaximumStackSize
            ? Result.Success()
            : Result.Failure(InventoryErrors.ResidentInventoryCapacityExceeded);
    }
}

}