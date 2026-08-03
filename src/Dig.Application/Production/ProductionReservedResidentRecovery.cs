using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Production
{

public static class ProductionReservedResidentRecovery
{
    public static readonly DomainError RecoveryCellRequired = new DomainError(
        "production.reserved_resident_recovery.cell_required",
        "A world recovery cell is required while reserved production items are carried.");

    public static Result DropCarriedItems(
        InventoryState inventory,
        EntityId reservationOwnerId,
        EntityId residentId,
        CellId? recoveryCell,
        long tick)
    {
        if (inventory is null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        ItemStackSnapshot[] carried = inventory.CreateSnapshot().Stacks
            .Where(value => value.Location.Kind == ItemLocationKind.AgentInventory)
            .Where(value => value.Location.HasOwner
                && value.Location.OwnerId == residentId)
            .Where(value => value.Reservations.Any(reservation =>
                reservation.JobId == reservationOwnerId
                && reservation.Quantity > 0))
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (carried.Length == 0)
        {
            return Result.Success();
        }

        if (!recoveryCell.HasValue)
        {
            return Result.Failure(RecoveryCellRequired);
        }

        ItemLocation destination = ItemLocation.InWorld(recoveryCell.Value);
        for (int index = 0; index < carried.Length; index++)
        {
            Result dropped = inventory.DropReservedResidentStackWithSpill(
                carried[index].StackId,
                reservationOwnerId,
                destination,
                tick);
            if (dropped.IsFailure)
            {
                return dropped;
            }
        }

        return Result.Success();
    }
}

}
