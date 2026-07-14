using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result MoveAvailable(
        EntityId stackId,
        int quantity,
        ItemLocation destination,
        EntityId splitStackId,
        long tick)
    {
        ValidateTick(tick);
        ItemStackState? stack = Find(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (quantity <= 0 || quantity > stack.AvailableQuantity)
        {
            return Result.Failure(
                quantity <= 0
                    ? InventoryErrors.InvalidQuantity
                    : InventoryErrors.InsufficientAvailableQuantity);
        }

        return MoveCore(stack, quantity, destination, splitStackId, tick);
    }

    public Result MoveReserved(
        EntityId stackId,
        EntityId jobId,
        int quantity,
        ItemLocation destination,
        EntityId splitStackId,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(jobId);
        ItemStackState? stack = Find(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (quantity <= 0)
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        if (stack.GetReservedQuantity(jobId) < quantity)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
        }

        if (quantity == stack.Quantity && stack.ReservedQuantity != quantity)
        {
            return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
        }

        bool partial = quantity < stack.Quantity;
        Result splitValidation = ValidateSplit(stack, quantity, splitStackId, partial);
        if (splitValidation.IsFailure)
        {
            return splitValidation;
        }

        ItemLocation source = stack.Location;
        stack.ConsumeReservation(jobId, quantity);
        EntityId destinationStackId;
        if (partial)
        {
            ItemStackState moved = stack.Split(splitStackId, quantity, destination);
            _stacks.Add(moved.Id, moved);
            destinationStackId = moved.Id;
        }
        else
        {
            stack.MoveFull(destination);
            destinationStackId = stack.Id;
        }

        IncrementVersion();
        Raise(new ItemQuantityReservationChanged(
            tick,
            stackId,
            jobId,
            stack.GetReservedQuantity(jobId)));
        Raise(new ItemStackMoved(
            tick,
            stackId,
            destinationStackId,
            stack.ItemId,
            quantity,
            source,
            destination));
        return Result.Success();
    }

    private Result MoveCore(
        ItemStackState stack,
        int quantity,
        ItemLocation destination,
        EntityId splitStackId,
        long tick)
    {
        bool partial = quantity < stack.Quantity;
        Result splitValidation = ValidateSplit(stack, quantity, splitStackId, partial);
        if (splitValidation.IsFailure)
        {
            return splitValidation;
        }

        ItemLocation source = stack.Location;
        EntityId destinationStackId;
        if (partial)
        {
            ItemStackState moved = stack.Split(splitStackId, quantity, destination);
            _stacks.Add(moved.Id, moved);
            destinationStackId = moved.Id;
        }
        else
        {
            stack.MoveFull(destination);
            destinationStackId = stack.Id;
        }

        IncrementVersion();
        Raise(new ItemStackMoved(
            tick,
            stack.Id,
            destinationStackId,
            stack.ItemId,
            quantity,
            source,
            destination));
        return Result.Success();
    }

    private Result ValidateSplit(
        ItemStackState stack,
        int quantity,
        EntityId splitStackId,
        bool partial)
    {
        ItemDefinition definition = Catalog.Get(stack.ItemId);
        if (quantity > definition.MaximumStackSize)
        {
            return Result.Failure(InventoryErrors.StackSizeExceeded);
        }

        if (!partial)
        {
            return Result.Success();
        }

        if (splitStackId.IsEmpty)
        {
            return Result.Failure(InventoryErrors.SplitIdRequired);
        }

        return _stacks.ContainsKey(splitStackId)
            ? Result.Failure(InventoryErrors.StackAlreadyExists)
            : Result.Success();
    }
}
}
