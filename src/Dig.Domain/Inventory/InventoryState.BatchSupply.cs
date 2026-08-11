using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>
        ReserveResidentBatchSlotCapacity(
            EntityId jobId,
            EntityId residentId,
            IReadOnlyCollection<ItemConsumptionRequest> requests,
            long tick)
    {
        ValidateTick(tick);
        ValidateJobId(jobId);
        ValidateResidentId(residentId);
        if (requests is null)
        {
            throw new ArgumentNullException(nameof(requests));
        }

        if (GetResidentSlotClaims(jobId).Count > 0)
        {
            return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Failure(
                InventoryErrors.ResidentSlotClaimConflict);
        }

        Result normalized = NormalizeResidentInventory(residentId, tick);
        if (normalized.IsFailure)
        {
            return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Failure(
                normalized.Error!);
        }

        ItemConsumptionRequest[] values = requests
            .GroupBy(value => value.ItemId)
            .Select(group => new ItemConsumptionRequest(
                group.Key,
                group.Sum(value => value.Quantity)))
            .ToArray();
        ResidentInventoryLayoutSnapshot layout = GetResidentInventoryLayout(residentId);
        Dictionary<ResidentInventorySlot, ItemStackState> occupied =
            CreateSlottedOccupancy(residentId);
        List<ResidentInventorySlotClaimSnapshot> planned =
            new List<ResidentInventorySlotClaimSnapshot>();
        foreach (ItemConsumptionRequest request in values)
        {
            ItemDefinition definition = Catalog.Get(request.ItemId);
            List<SlotCapacity> capacities = BuildClaimCapacities(
                residentId,
                definition,
                layout,
                occupied);
            int remaining = request.Quantity;
            foreach (SlotCapacity capacity in capacities)
            {
                int claimed = Math.Min(remaining, capacity.AvailableQuantity);
                if (claimed <= 0)
                {
                    continue;
                }

                ResidentInventorySlotClaimSnapshot claim =
                    new ResidentInventorySlotClaimSnapshot(
                        jobId,
                        residentId,
                        request.ItemId,
                        capacity.Slot,
                        claimed);
                planned.Add(claim);
                _residentSlotClaims.Add(claim);
                remaining -= claimed;
                if (remaining == 0)
                {
                    break;
                }
            }

            if (remaining > 0)
            {
                _residentSlotClaims.RemoveAll(claim => claim.JobId == jobId);
                return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Failure(
                    InventoryErrors.ResidentInventoryCapacityExceeded);
            }
        }

        IncrementVersion();
        foreach (ResidentInventorySlotClaimSnapshot claim in planned)
        {
            Raise(new ResidentInventorySlotClaimChanged(
                tick,
                claim.JobId,
                claim.ResidentId,
                claim.ItemId,
                claim.Slot,
                claim.Quantity));
        }

        return Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>>.Success(
            new ReadOnlyCollection<ResidentInventorySlotClaimSnapshot>(planned));
    }

    public Result AcquireReservedBatchIntoResidentSlots(
        EntityId jobId,
        EntityId residentId,
        IReadOnlyCollection<ItemReservationAllocation> allocations,
        IReadOnlyCollection<EntityId> newStackIds,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(jobId);
        ValidateResidentId(residentId);
        ItemReservationAllocation[] sources = allocations.ToArray();
        ResidentInventorySlotClaimSnapshot[] claims = GetResidentSlotClaims(jobId)
            .ToArray();
        if (sources.Length == 0 || claims.Length == 0
            || claims.Any(claim => claim.ResidentId != residentId))
        {
            return Result.Failure(InventoryErrors.ResidentSlotClaimStale);
        }

        foreach (IGrouping<ItemId, ItemReservationAllocation> group in sources
            .GroupBy(value => value.ItemId))
        {
            int sourceQuantity = group.Sum(value => value.Quantity);
            int claimQuantity = claims
                .Where(value => value.ItemId == group.Key)
                .Sum(value => value.Quantity);
            if (sourceQuantity != claimQuantity)
            {
                return Result.Failure(InventoryErrors.ResidentSlotClaimStale);
            }
        }

        Queue<EntityId> ids = new Queue<EntityId>(newStackIds);
        foreach (IGrouping<ItemId, ResidentInventorySlotClaimSnapshot> group in claims
            .GroupBy(value => value.ItemId))
        {
            Result moved = AcquireItemGroup(
                jobId,
                residentId,
                group.Key,
                group.OrderBy(value => value.Slot.Compartment)
                    .ThenBy(value => value.Slot.Index)
                    .ToArray(),
                sources.Where(value => value.ItemId == group.Key)
                    .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
                    .ToArray(),
                ids,
                tick);
            if (moved.IsFailure)
            {
                return moved;
            }
        }

        ReleaseResidentSlotClaims(jobId, tick);
        return Result.Success();
    }

    private Result AcquireItemGroup(
        EntityId jobId,
        EntityId residentId,
        ItemId itemId,
        IReadOnlyList<ResidentInventorySlotClaimSnapshot> claims,
        IReadOnlyList<ItemReservationAllocation> allocations,
        Queue<EntityId> newStackIds,
        long tick)
    {
        Queue<(ItemStackState Stack, int Remaining)> sources =
            new Queue<(ItemStackState, int)>();
        foreach (ItemReservationAllocation allocation in allocations)
        {
            ItemStackState? source = Find(allocation.StackId);
            if (source is null
                || source.ItemId != itemId
                || source.GetReservedQuantity(jobId) < allocation.Quantity)
            {
                return Result.Failure(InventoryErrors.ReservationNotFound);
            }

            sources.Enqueue((source, allocation.Quantity));
        }

        foreach (ResidentInventorySlotClaimSnapshot claim in claims)
        {
            if (claim.Quantity != 1)
            {
                return Result.Failure(InventoryErrors.ResidentSlotClaimStale);
            }

            ItemLocation destination = ItemLocation.InResidentSlot(
                residentId,
                claim.Slot.Compartment,
                claim.Slot.Index);
            if (FindStackAt(destination, default) != null)
            {
                return Result.Failure(InventoryErrors.ResidentSlotClaimStale);
            }

            if (newStackIds.Count == 0)
            {
                return Result.Failure(InventoryErrors.SplitIdRequired);
            }

            EntityId newId = newStackIds.Dequeue();
            if (newId.IsEmpty || _stacks.ContainsKey(newId))
            {
                return Result.Failure(InventoryErrors.StackAlreadyExists);
            }

            ItemStackState target = new ItemStackState(newId, itemId, 0, destination);
            _stacks.Add(newId, target);
            int need = claim.Quantity;
            while (need > 0)
            {
                (ItemStackState source, int remaining) = sources.Dequeue();
                int moved = Math.Min(need, remaining);
                ItemLocation sourceLocation = source.Location;
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
                    itemId,
                    moved,
                    sourceLocation,
                    destination));
                if (source.Quantity == 0)
                {
                    _stacks.Remove(source.Id);
                }

                remaining -= moved;
                need -= moved;
                if (remaining > 0)
                {
                    sources.Enqueue((source, remaining));
                }
            }

            Raise(new ItemQuantityReservationChanged(
                tick,
                target.Id,
                jobId,
                target.GetReservedQuantity(jobId)));
        }

        IncrementVersion();
        return Result.Success();
    }
}

}
