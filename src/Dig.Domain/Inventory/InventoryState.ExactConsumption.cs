using System.Collections.Generic;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result ConsumeAvailableStack(
        EntityId stackId,
        int quantity,
        long tick)
    {
        ValidateTick(tick);
        ItemStackState? stack = Find(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (quantity <= 0)
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        if (quantity > stack.AvailableQuantity)
        {
            return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
        }

        ItemLocation location = stack.Location;
        ItemId itemId = stack.ItemId;
        stack.ConsumeAvailable(quantity);
        if (stack.Quantity == 0)
        {
            _stacks.Remove(stack.Id);
        }

        IncrementVersion();
        Raise(new ItemsConsumed(
            tick,
            location,
            new List<ItemConsumptionRequest>
            {
                new ItemConsumptionRequest(itemId, quantity),
            }));
        return Result.Success();
    }
}
}