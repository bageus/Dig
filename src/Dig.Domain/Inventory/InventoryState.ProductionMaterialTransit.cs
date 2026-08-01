using System;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result<EntityId> AcquireReservedProductionUnit(
        EntityId sourceStackId,
        EntityId reservationOwnerId,
        EntityId residentId,
        EntityId destinationStackId,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(reservationOwnerId);
        ValidateResidentId(residentId);
        ItemStackState? source = Find(sourceStackId);
        if (source is null)
        {
            return Result<EntityId>.Failure(InventoryErrors.StackNotFound);
        }

        if (source.GetReservedQuantity(reservationOwnerId) < 1)
        {
            return Result<EntityId>.Failure(InventoryErrors.ReservationNotFound);
        }

        Result<System.Collections.Generic.IReadOnlyList<ResidentInventorySlotClaimSnapshot>>
            capacity = ReserveResidentSlotCapacity(
                reservationOwnerId,
                residentId,
                source.ItemId,
                quantity: 1,
                tick);
        if (capacity.IsFailure)
        {
            return Result<EntityId>.Failure(capacity.Error!);
        }

        ResidentInventorySlotClaimSnapshot claim = capacity.Value.Single();
        ItemLocation destination = ItemLocation.InResidentSlot(
            residentId,
            claim.Slot.Compartment,
            claim.Slot.Index);
        ItemLocation sourceLocation = source.Location;
        EntityId movedStackId;
        if (source.Quantity == 1)
        {
            source.MoveFull(destination);
            movedStackId = source.Id;
        }
        else
        {
            if (destinationStackId.IsEmpty)
            {
                ReleaseResidentSlotClaims(reservationOwnerId, tick);
                return Result<EntityId>.Failure(InventoryErrors.SplitIdRequired);
            }

            if (_stacks.ContainsKey(destinationStackId))
            {
                ReleaseResidentSlotClaims(reservationOwnerId, tick);
                return Result<EntityId>.Failure(InventoryErrors.StackAlreadyExists);
            }

            source.ConsumeReservedQuantity(reservationOwnerId, quantity: 1);
            ItemStackState moved = new ItemStackState(
                destinationStackId,
                source.ItemId,
                quantity: 1,
                destination);
            moved.Reserve(reservationOwnerId, quantity: 1);
            _stacks.Add(moved.Id, moved);
            movedStackId = moved.Id;
            Raise(new ItemQuantityReservationChanged(
                tick,
                moved.Id,
                reservationOwnerId,
                moved.GetReservedQuantity(reservationOwnerId)));
            Raise(new ItemQuantityReservationChanged(
                tick,
                source.Id,
                reservationOwnerId,
                source.GetReservedQuantity(reservationOwnerId)));
        }

        ReleaseResidentSlotClaims(reservationOwnerId, tick);
        IncrementVersion();
        Raise(new ItemStackMoved(
            tick,
            sourceStackId,
            movedStackId,
            source.ItemId,
            quantity: 1,
            sourceLocation,
            destination));
        return Result<EntityId>.Success(movedStackId);
    }

    public Result ConsumeReservedProductionUnit(
        EntityId reservationOwnerId,
        EntityId residentId,
        ItemId itemId,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(reservationOwnerId);
        ValidateResidentId(residentId);
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        ItemStackState? source = _stacks.Values
            .Where(stack => stack.ItemId == itemId
                && stack.Location.Kind == ItemLocationKind.AgentInventory
                && stack.Location.HasOwner
                && stack.Location.OwnerId == residentId
                && stack.GetReservedQuantity(reservationOwnerId) > 0)
            .OrderBy(stack => stack.Location)
            .ThenBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
        if (source is null)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
        }

        source.ConsumeReservedQuantity(reservationOwnerId, quantity: 1);
        Raise(new ItemQuantityReservationChanged(
            tick,
            source.Id,
            reservationOwnerId,
            source.GetReservedQuantity(reservationOwnerId)));
        Raise(new ReservedItemConsumed(
            tick,
            reservationOwnerId,
            source.Id,
            itemId,
            quantity: 1));
        if (source.Quantity == 0)
        {
            _stacks.Remove(source.Id);
        }

        IncrementVersion();
        return Result.Success();
    }
}

}
