using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public static class ResidentItemTransferService
{
    public static Result AcquireReservedIntoResidentSlots(
        InventoryState inventory,
        EntityId sourceStackId,
        EntityId jobId,
        EntityId residentId,
        EntityId destinationStackId,
        long tick)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        ItemStackSnapshot? source = inventory.GetStack(sourceStackId);
        if (source != null)
        {
            int reservedQuantity = source.Reservations
                .Where(reservation => reservation.JobId == jobId)
                .Sum(reservation => reservation.Quantity);
            ResidentInventorySlotClaimSnapshot[] claims = inventory
                .GetResidentSlotClaims(jobId)
                .ToArray();
            bool claimsMatch = claims.Length > 0
                && claims.All(claim => claim.ResidentId == residentId)
                && claims.All(claim => claim.ItemId == source.ItemId)
                && claims.Sum(claim => claim.Quantity) == reservedQuantity;
            if (reservedQuantity > 0 && !claimsMatch)
            {
                inventory.ReleaseResidentSlotClaims(jobId, tick);
                Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>> repaired =
                    inventory.ReserveResidentSlotCapacity(
                        jobId,
                        residentId,
                        source.ItemId,
                        reservedQuantity,
                        tick);
                if (repaired.IsFailure)
                {
                    return Result.Failure(repaired.Error!);
                }
            }
        }

        return inventory.AcquireReservedIntoResidentSlots(
            sourceStackId,
            jobId,
            residentId,
            destinationStackId,
            tick);
    }

    public static Result AcquireReservedStack(
        InventoryState inventory,
        EntityId stackId,
        EntityId reservationOwnerId,
        EntityId residentId,
        long tick)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        return inventory.MoveFullyReservedPreservingReservation(
            stackId,
            reservationOwnerId,
            ItemLocation.InAgent(residentId),
            tick);
    }

    public static Result AcquireReservedBatchIntoResidentSlots(
        InventoryState inventory,
        EntityId jobId,
        EntityId residentId,
        IReadOnlyCollection<ItemReservationAllocation> allocations,
        IReadOnlyCollection<EntityId> newStackIds,
        long tick)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        return inventory.AcquireReservedBatchIntoResidentSlots(
            jobId,
            residentId,
            allocations,
            newStackIds,
            tick);
    }

    public static Result<bool> AcquireReservedSupplySourceIntoResidentSlots(
        InventoryState inventory,
        EntityId jobId,
        EntityId residentId,
        ItemReservationAllocation allocation,
        IReadOnlyCollection<EntityId> newStackIds,
        long tick)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        return inventory.AcquireReservedSupplySourceIntoResidentSlots(
            jobId,
            residentId,
            allocation,
            newStackIds,
            tick);
    }

    public static Result<EntityId> AcquireReservedProductionUnit(
        InventoryState inventory,
        EntityId sourceStackId,
        EntityId reservationOwnerId,
        EntityId residentId,
        EntityId destinationStackId,
        long tick)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        return inventory.AcquireReservedProductionUnit(
            sourceStackId,
            reservationOwnerId,
            residentId,
            destinationStackId,
            tick);
    }

    public static Result MoveReserved(
        InventoryState inventory,
        EntityId stackId,
        EntityId reservationOwnerId,
        int quantity,
        ItemLocation destination,
        EntityId splitStackId,
        long tick)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        return inventory.MoveReserved(
            stackId,
            reservationOwnerId,
            quantity,
            destination,
            splitStackId,
            tick);
    }

    public static Result DepositReservedResidentItems(
        InventoryState inventory,
        EntityId sourceStackId,
        EntityId jobId,
        EntityId residentId,
        ItemId itemId,
        int quantity,
        ItemLocation destination,
        EntityId destinationStackId,
        long tick)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        Result moved = inventory.DepositReservedResidentItems(
            jobId,
            residentId,
            itemId,
            quantity,
            destination,
            destinationStackId,
            tick);
        if (moved.IsFailure && moved.Error == InventoryErrors.ReservationNotFound)
        {
            moved = MoveReserved(
                inventory,
                sourceStackId,
                jobId,
                quantity,
                destination,
                destinationStackId,
                tick);
        }

        return moved;
    }

    public static Result DropReserved(
        InventoryState inventory,
        EntityId stackId,
        EntityId reservationOwnerId,
        ItemLocation destination,
        long tick)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        ItemStackSnapshot? stack = inventory.GetStack(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        ItemDefinition definition = inventory.Catalog.Get(stack.ItemId);
        return definition.IsInventoryExpansion
            ? inventory.DropReservedResidentStackWithSpill(
                stackId,
                reservationOwnerId,
                destination,
                tick)
            : inventory.MoveReserved(
                stackId,
                reservationOwnerId,
                stack.Quantity,
                destination,
                splitStackId: default,
                tick);
    }

    public static Result Drop(
        InventoryState inventory,
        EntityId actorId,
        EntityId stackId,
        CellId destination,
        long tick)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        ItemStackSnapshot? stack = inventory.GetStack(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (!IsOwnedByResident(stack.Location, actorId))
        {
            return Result.Failure(ResidentInventoryActionErrors.StackNotCarriedByActor);
        }

        if (stack.ReservedQuantity != 0)
        {
            return Result.Failure(ResidentInventoryActionErrors.StackReserved);
        }

        return inventory.DropResidentStackWithSpill(
            stackId,
            ItemLocation.InWorld(destination),
            tick);
    }

    public static bool IsOwnedByResident(ItemLocation location, EntityId residentId)
    {
        return location.HasOwner
            && location.OwnerId == residentId
            && (location.Kind == ItemLocationKind.AgentInventory
                || location.Kind == ItemLocationKind.Equipped);
    }
}

}
