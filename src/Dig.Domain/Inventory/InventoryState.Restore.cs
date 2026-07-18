using System;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public static Result<InventoryState> Restore(
        InventorySnapshot snapshot,
        ItemCatalog catalog)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        InventoryState inventory = new InventoryState(catalog);
        ItemStackSnapshot[] orderedStacks = snapshot.Stacks
            .OrderBy(stack => RestoreOrder(stack, catalog))
            .ThenBy(stack => stack.Location)
            .ThenBy(stack => stack.StackId.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (ItemStackSnapshot stack in orderedStacks)
        {
            if (!catalog.Contains(stack.ItemId))
            {
                return Result<InventoryState>.Failure(new DomainError(
                    "inventory.restore.unknown_item",
                    $"Item '{stack.ItemId}' is not present in the current catalog."));
            }

            Result added = inventory.AddStack(
                stack.StackId,
                stack.ItemId,
                stack.Quantity,
                stack.Location,
                tick: 0);
            if (added.IsFailure)
            {
                return Result<InventoryState>.Failure(added.Error!);
            }
        }

        foreach (ItemStackSnapshot stack in orderedStacks)
        {
            foreach (ItemQuantityReservationSnapshot reservation in stack.Reservations)
            {
                Result reserved = inventory.ReserveQuantity(
                    stack.StackId,
                    reservation.JobId,
                    reservation.Quantity,
                    tick: 0);
                if (reserved.IsFailure)
                {
                    return Result<InventoryState>.Failure(reserved.Error!);
                }
            }
        }

        foreach (HeldItemReferenceSnapshot held in snapshot.HeldItems
            .OrderBy(item => item.ResidentId.ToString(), StringComparer.Ordinal))
        {
            Result restored = inventory.HoldItem(
                held.ResidentId,
                held.StackId,
                held.Quantity,
                held.Purpose,
                tick: 0);
            if (restored.IsFailure)
            {
                return Result<InventoryState>.Failure(restored.Error!);
            }
        }

        inventory.DequeueUncommittedEvents();
        inventory.Version = snapshot.Version;
        return Result<InventoryState>.Success(inventory);
    }

    private static int RestoreOrder(
        ItemStackSnapshot stack,
        ItemCatalog catalog)
    {
        ItemLocation location = stack.Location;
        if (location.Kind != ItemLocationKind.AgentInventory
            || !location.HasResidentSlot)
        {
            return 3;
        }

        if (location.ResidentCompartment == ResidentInventoryCompartment.Main)
        {
            return catalog.Get(stack.ItemId).IsInventoryExpansion ? 0 : 1;
        }

        return 2;
    }
}

}
