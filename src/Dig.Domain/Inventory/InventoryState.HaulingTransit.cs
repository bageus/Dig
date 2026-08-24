using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result AcquireReservedIntoResidentSlots(
        EntityId sourceStackId,
        EntityId jobId,
        EntityId residentId,
        EntityId destinationStackId,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(jobId);
        ValidateResidentId(residentId);
        ItemStackState? source = Find(sourceStackId);
        if (source is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        // A resident can finish another inventory-changing task while travelling
        // to this source. Reflow the previously reserved slots against the current
        // layout before validating them, otherwise a valid hauling reservation can
        // remain stuck in AcquireItem forever as ResidentSlotClaimStale.
        Result normalized = NormalizeResidentInventory(residentId, tick);
        if (normalized.IsFailure)
        {
            return normalized;
        }

        ResidentInventorySlotClaimSnapshot[] claims = GetResidentSlotClaims(jobId)
            .ToArray();
        int quantity = claims.Sum(claim => claim.Quantity);
        if (claims.Length == 0
            || claims.Any(claim => claim.ResidentId != residentId)
            || claims.Any(claim => claim.ItemId != source.ItemId)
            || source.GetReservedQuantity(jobId) != quantity)
        {
            return Result.Failure(InventoryErrors.ResidentSlotClaimStale);
        }

        Result claimValidation = ValidateResidentSlotClaims();
        if (claimValidation.IsFailure)
        {
            return claimValidation;
        }

        Result<HaulingAcquirePlan> planned = PlanHaulingAcquire(
            source,
            claims,
            destinationStackId);
        if (planned.IsFailure)
        {
            return Result.Failure(planned.Error!);
        }

        ExecuteHaulingAcquire(source, jobId, planned.Value, tick);
        ReleaseResidentSlotClaims(jobId, tick);
        return NormalizeResidentInventory(residentId, tick);
    }

    public Result DepositReservedResidentItems(
        EntityId jobId,
        EntityId residentId,
        ItemId itemId,
        int quantity,
        ItemLocation destination,
        EntityId destinationStackId,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(jobId);
        ValidateResidentId(residentId);
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        ItemStackState[] sources = _stacks.Values
            .Where(stack => stack.Location.Kind == ItemLocationKind.AgentInventory)
            .Where(stack => stack.Location.HasOwner
                && stack.Location.OwnerId == residentId)
            .Where(stack => stack.GetReservedQuantity(jobId) > 0)
            .Where(stack => stack.ItemId == itemId)
            .OrderBy(stack => stack.Location)
            .ThenBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (sources.Length == 0
            || sources.Sum(stack => stack.GetReservedQuantity(jobId)) != quantity)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
        }

        bool unitIdentityTransfer = sources.Length == 1
            && sources[0].Quantity == 1
            && sources[0].GetReservedQuantity(jobId) == 1;
        if (unitIdentityTransfer)
        {
            for (int index = 0; index < sources.Length; index++)
            {
                ItemStackState source = sources[index];
                ItemLocation sourceLocation = source.Location;
                source.ConsumeReservation(jobId, quantity: 1);
                source.MoveFull(destination);
                Raise(new ItemQuantityReservationChanged(
                    tick,
                    source.Id,
                    jobId,
                    source.GetReservedQuantity(jobId)));
                Raise(new ItemStackMoved(
                    tick,
                    source.Id,
                    source.Id,
                    itemId,
                    quantity: 1,
                    sourceLocation,
                    destination));
            }

            IncrementVersion();
            return Result.Success();
        }

        if (quantity > Catalog.Get(itemId).MaximumStackSize)
        {
            return Result.Failure(InventoryErrors.StackSizeExceeded);
        }

        if (destinationStackId.IsEmpty)
        {
            return Result.Failure(InventoryErrors.SplitIdRequired);
        }

        if (_stacks.ContainsKey(destinationStackId))
        {
            return Result.Failure(InventoryErrors.StackAlreadyExists);
        }

        ItemStackState deposited = new ItemStackState(
            destinationStackId,
            itemId,
            quantity,
            destination);
        for (int index = 0; index < sources.Length; index++)
        {
            ItemStackState source = sources[index];
            int moved = source.GetReservedQuantity(jobId);
            ItemLocation sourceLocation = source.Location;
            source.ConsumeReservation(jobId, moved);
            if (source.Quantity == 0)
            {
                _stacks.Remove(source.Id);
            }

            Raise(new ItemQuantityReservationChanged(
                tick,
                source.Id,
                jobId,
                source.GetReservedQuantity(jobId)));
            Raise(new ItemStackMoved(
                tick,
                source.Id,
                destinationStackId,
                itemId,
                moved,
                sourceLocation,
                destination));
        }

        _stacks.Add(destinationStackId, deposited);
        IncrementVersion();
        return Result.Success();
    }

    private Result<HaulingAcquirePlan> PlanHaulingAcquire(
        ItemStackState source,
        IReadOnlyList<ResidentInventorySlotClaimSnapshot> claims,
        EntityId destinationStackId)
    {
        if (claims.Any(claim => claim.Quantity != 1))
        {
            return Result<HaulingAcquirePlan>.Failure(
                InventoryErrors.ResidentSlotClaimStale);
        }

        bool singleUnitFullMove = claims.Count == 1
            && source.Quantity == 1
            && source.ReservedQuantity == 1;
        bool preserveSourceIdentity = claims.Count > 1
            && source.Quantity == claims.Count
            && source.ReservedQuantity == claims.Count;
        List<HaulingAcquireStep> steps = new List<HaulingAcquireStep>();
        for (int index = 0; index < claims.Count; index++)
        {
            ResidentInventorySlotClaimSnapshot claim = claims[index];
            ItemLocation location = ItemLocation.InResidentSlot(
                claim.ResidentId,
                claim.Slot.Compartment,
                claim.Slot.Index);
            if (FindStackAt(location, source.Id) != null)
            {
                return Result<HaulingAcquirePlan>.Failure(
                    InventoryErrors.ResidentSlotClaimStale);
            }

            bool usesSourceIdentity = singleUnitFullMove
                || (preserveSourceIdentity && index == claims.Count - 1);
            EntityId unitId;
            if (usesSourceIdentity)
            {
                unitId = source.Id;
            }
            else if (index == 0 && !destinationStackId.IsEmpty)
            {
                unitId = destinationStackId;
            }
            else
            {
                Result<EntityId> generated = CreateResidentUnitId(
                    source.Id,
                    ordinal: 10_000 + index);
                if (generated.IsFailure)
                {
                    return Result<HaulingAcquirePlan>.Failure(generated.Error!);
                }

                unitId = generated.Value;
            }

            if (!usesSourceIdentity && _stacks.ContainsKey(unitId))
            {
                return Result<HaulingAcquirePlan>.Failure(
                    InventoryErrors.StackAlreadyExists);
            }

            steps.Add(new HaulingAcquireStep(
                claim,
                location,
                unitId,
                usesSourceIdentity));
        }

        return Result<HaulingAcquirePlan>.Success(new HaulingAcquirePlan(steps));
    }

    private void ExecuteHaulingAcquire(
        ItemStackState source,
        EntityId jobId,
        HaulingAcquirePlan plan,
        long tick)
    {
        for (int index = 0; index < plan.Steps.Count; index++)
        {
            HaulingAcquireStep step = plan.Steps[index];
            ItemLocation sourceLocation = source.Location;
            if (step.UsesSourceIdentity)
            {
                source.MoveFull(step.Destination);
                Raise(new ItemStackMoved(
                    tick,
                    source.Id,
                    source.Id,
                    source.ItemId,
                    quantity: 1,
                    sourceLocation,
                    step.Destination));
                continue;
            }

            source.ConsumeReservedQuantity(jobId, quantity: 1);
            ItemStackState moved = new ItemStackState(
                step.DestinationStackId,
                source.ItemId,
                quantity: 1,
                step.Destination);
            moved.Reserve(jobId, quantity: 1);
            _stacks.Add(moved.Id, moved);
            Raise(new ItemQuantityReservationChanged(
                tick,
                moved.Id,
                jobId,
                moved.GetReservedQuantity(jobId)));
            Raise(new ItemQuantityReservationChanged(
                tick,
                source.Id,
                jobId,
                source.GetReservedQuantity(jobId)));
            Raise(new ItemStackMoved(
                tick,
                source.Id,
                moved.Id,
                source.ItemId,
                quantity: 1,
                sourceLocation,
                step.Destination));
        }

        if (source.Quantity == 0)
        {
            _stacks.Remove(source.Id);
        }

        IncrementVersion();
    }

    private sealed class HaulingAcquirePlan
    {
        public HaulingAcquirePlan(IReadOnlyList<HaulingAcquireStep> steps)
        {
            Steps = steps;
        }

        public IReadOnlyList<HaulingAcquireStep> Steps { get; }
    }

    private sealed class HaulingAcquireStep
    {
        public HaulingAcquireStep(
            ResidentInventorySlotClaimSnapshot claim,
            ItemLocation destination,
            EntityId destinationStackId,
            bool usesSourceIdentity)
        {
            Claim = claim;
            Destination = destination;
            DestinationStackId = destinationStackId;
            UsesSourceIdentity = usesSourceIdentity;
        }

        public ResidentInventorySlotClaimSnapshot Claim { get; }
        public ItemLocation Destination { get; }
        public EntityId DestinationStackId { get; }
        public bool UsesSourceIdentity { get; }
    }

}

}
