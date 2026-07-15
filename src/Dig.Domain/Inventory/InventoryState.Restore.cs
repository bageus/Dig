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
        foreach (ItemStackSnapshot stack in snapshot.Stacks.OrderBy(item => item.StackId.ToString(), StringComparer.Ordinal))
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

            foreach (ItemReservationSnapshot reservation in stack.Reservations)
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

        inventory.DequeueUncommittedEvents();
        inventory.Version = snapshot.Version;
        return Result<InventoryState>.Success(inventory);
    }
}
}
