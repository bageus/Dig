using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result<bool> AcquireReservedSupplySourceIntoResidentSlots(
        EntityId jobId,
        EntityId residentId,
        ItemReservationAllocation allocation,
        IReadOnlyCollection<EntityId> newStackIds,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(jobId);
        ValidateResidentId(residentId);
        if (newStackIds is null)
        {
            throw new ArgumentNullException(nameof(newStackIds));
        }

        ItemStackState? source = Find(allocation.StackId);
        if (source is null
            || source.ItemId != allocation.ItemId
            || source.Location.Kind != ItemLocationKind.World
            || source.GetReservedQuantity(jobId) < allocation.Quantity)
        {
            return Result<bool>.Failure(InventoryErrors.ReservationNotFound);
        }

        ResidentInventorySlotClaimSnapshot[] claims = GetResidentSlotClaims(jobId)
            .Where(value => value.ResidentId == residentId
                && value.ItemId == allocation.ItemId)
            .OrderBy(value => value.Slot.Compartment)
            .ThenBy(value => value.Slot.Index)
            .ToArray();
        if (claims.Length == 0)
        {
            return Result<bool>.Failure(InventoryErrors.ResidentSlotClaimStale);
        }

        Queue<EntityId> ids = new Queue<EntityId>(newStackIds);
        int remaining = allocation.Quantity;
        ItemLocation sourceLocation = source.Location;
        foreach (ResidentInventorySlotClaimSnapshot claim in claims)
        {
            ItemLocation destination = ItemLocation.InResidentSlot(
                residentId,
                claim.Slot.Compartment,
                claim.Slot.Index);
            if (claim.Quantity <= 0
                || claim.Quantity > Catalog.Get(allocation.ItemId).MaximumStackSize)
            {
                return Result<bool>.Failure(InventoryErrors.ResidentSlotClaimStale);
            }

            ItemStackState? target = FindStackAt(destination, default);
            if (target != null)
            {
                if (target.ItemId != allocation.ItemId
                    || target.GetReservedQuantity(jobId) > claim.Quantity)
                {
                    return Result<bool>.Failure(InventoryErrors.ResidentSlotClaimStale);
                }
            }
            else if (ids.Count == 0)
            {
                return Result<bool>.Failure(InventoryErrors.SplitIdRequired);
            }
            else
            {
                EntityId newId = ids.Dequeue();
                if (newId.IsEmpty || _stacks.ContainsKey(newId))
                {
                    return Result<bool>.Failure(InventoryErrors.StackAlreadyExists);
                }

                target = new ItemStackState(
                    newId,
                    allocation.ItemId,
                    quantity: 0,
                    destination);
                _stacks.Add(newId, target);
            }

            int claimAvailable = claim.Quantity - target.GetReservedQuantity(jobId);
            int moved = Math.Min(remaining, claimAvailable);
            if (moved == 0)
            {
                continue;
            }
            source.ConsumeReservedQuantity(jobId, moved);
            target.AddQuantity(moved);
            target.Reserve(jobId, moved);
            Raise(new ItemQuantityReservationChanged(
                tick,
                source.Id,
                jobId,
                source.GetReservedQuantity(jobId)));
            Raise(new ItemStackMoved(
                tick,
                source.Id,
                target.Id,
                allocation.ItemId,
                moved,
                sourceLocation,
                destination));
            Raise(new ItemQuantityReservationChanged(
                tick,
                target.Id,
                jobId,
                target.GetReservedQuantity(jobId)));
            remaining -= moved;
            if (remaining == 0)
            {
                break;
            }
        }

        if (remaining != 0)
        {
            return Result<bool>.Failure(InventoryErrors.ResidentSlotClaimStale);
        }

        if (source.Quantity == 0)
        {
            _stacks.Remove(source.Id);
        }

        IncrementVersion();
        bool allSourcesAcquired = !_stacks.Values.Any(value =>
            value.Location.Kind == ItemLocationKind.World
            && value.GetReservedQuantity(jobId) > 0);
        if (allSourcesAcquired)
        {
            ReleaseResidentSlotClaims(jobId, tick);
        }

        return Result<bool>.Success(allSourcesAcquired);
    }
}

}
