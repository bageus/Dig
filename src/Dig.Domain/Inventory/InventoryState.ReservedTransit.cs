using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result MoveFullyReservedPreservingReservation(
        EntityId stackId,
        EntityId jobId,
        ItemLocation destination,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(jobId);
        ItemStackState? stack = Find(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (stack.GetReservedQuantity(jobId) != stack.Quantity)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
        }

        if (stack.ReservedQuantity != stack.Quantity)
        {
            return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
        }

        ItemLocation source = stack.Location;
        if (source == destination)
        {
            return Result.Success();
        }

        stack.MoveFull(destination);
        IncrementVersion();
        Raise(new ItemStackMoved(
            tick,
            stack.Id,
            stack.Id,
            stack.ItemId,
            stack.Quantity,
            source,
            destination));
        return Result.Success();
    }
}
}
