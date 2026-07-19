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
        return Result.Success();
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
            .OrderBy(stack => stack.Location)
            .ThenBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (sources.Length == 0
            || sources.Any(stack => stack.ItemId != itemId)
            || sources.Sum(stack => stack.GetReservedQuantity(jobId)) != quantity)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
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
            source.ConsumeReservedQuantity(jobId, moved);
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
        List<HaulingAcquireStep> steps = new List<HaulingAcquireStep>();
        int emptyCount = 0;
        for (int index = 0; index < claims.Count; index++)
        {
            ResidentInventorySlotClaimSnapshot claim = claims[index];
            ItemLocation location = ItemLocation.InResidentSlot(
                claim.ResidentId,
                claim.Slot.Compartment,
                claim.Slot.Index);
            ItemStackState? target = FindStackAt(location, source.Id);
            if (target != null)
            {
                if (target.ItemId != source.ItemId
                    || target.Quantity + claim.Quantity
                        > Catalog.Get(source.ItemId).MaximumStackSize)
                {
                    return Result<HaulingAcquirePlan>.Failure(
                        InventoryErrors.ResidentSlotClaimStale);
                }
            }
            else
            {
                emptyCount++;
            }

            steps.Add(new HaulingAcquireStep(claim, location, target));
        }

        bool fullMove = steps.Count == 1
            && steps[0].Target == null
            && source.Quantity == steps[0].Claim.Quantity
            && source.ReservedQuantity == source.Quantity;
        if (emptyCount > (fullMove ? 1 : 0))
        {
            if (destinationStackId.IsEmpty)
            {
                return Result<HaulingAcquirePlan>.Failure(
                    InventoryErrors.SplitIdRequired);
            }

            if (_stacks.ContainsKey(destinationStackId))
            {
                return Result<HaulingAcquirePlan>.Failure(
                    InventoryErrors.StackAlreadyExists);
            }
        }

        if (!fullMove && emptyCount > 1)
        {
            return Result<HaulingAcquirePlan>.Failure(
                InventoryErrors.ResidentInventoryCapacityExceeded);
        }

        return Result<HaulingAcquirePlan>.Success(new HaulingAcquirePlan(
            steps,
            destinationStackId,
            fullMove));
    }

    private void ExecuteHaulingAcquire(
        ItemStackState source,
        EntityId jobId,
        HaulingAcquirePlan plan,
        long tick)
    {
        if (plan.FullMove)
        {
            ItemLocation sourceLocation = source.Location;
            ItemLocation destination = plan.Steps[0].Destination;
            source.MoveFull(destination);
            IncrementVersion();
            Raise(new ItemStackMoved(
                tick,
                source.Id,
                source.Id,
                source.ItemId,
                source.Quantity,
                sourceLocation,
                destination));
            return;
        }

        for (int index = 0; index < plan.Steps.Count; index++)
        {
            HaulingAcquireStep step = plan.Steps[index];
            int quantity = step.Claim.Quantity;
            ItemLocation sourceLocation = source.Location;
            source.ConsumeReservedQuantity(jobId, quantity);
            EntityId destinationId;
            if (step.Target != null)
            {
                step.Target.AddQuantity(quantity);
                step.Target.Reserve(jobId, quantity);
                destinationId = step.Target.Id;
                Raise(new ItemQuantityReservationChanged(
                    tick,
                    step.Target.Id,
                    jobId,
                    step.Target.GetReservedQuantity(jobId)));
            }
            else
            {
                ItemStackState moved = new ItemStackState(
                    plan.DestinationStackId,
                    source.ItemId,
                    quantity,
                    step.Destination);
                moved.Reserve(jobId, quantity);
                _stacks.Add(moved.Id, moved);
                destinationId = moved.Id;
                Raise(new ItemQuantityReservationChanged(
                    tick,
                    moved.Id,
                    jobId,
                    moved.GetReservedQuantity(jobId)));
            }

            Raise(new ItemQuantityReservationChanged(
                tick,
                source.Id,
                jobId,
                source.GetReservedQuantity(jobId)));
            Raise(new ItemStackMoved(
                tick,
                source.Id,
                destinationId,
                source.ItemId,
                quantity,
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
        public HaulingAcquirePlan(
            IReadOnlyList<HaulingAcquireStep> steps,
            EntityId destinationStackId,
            bool fullMove)
        {
            Steps = steps;
            DestinationStackId = destinationStackId;
            FullMove = fullMove;
        }

        public IReadOnlyList<HaulingAcquireStep> Steps { get; }
        public EntityId DestinationStackId { get; }
        public bool FullMove { get; }
    }

    private sealed class HaulingAcquireStep
    {
        public HaulingAcquireStep(
            ResidentInventorySlotClaimSnapshot claim,
            ItemLocation destination,
            ItemStackState? target)
        {
            Claim = claim;
            Destination = destination;
            Target = target;
        }

        public ResidentInventorySlotClaimSnapshot Claim { get; }
        public ItemLocation Destination { get; }
        public ItemStackState? Target { get; }
    }
}

}