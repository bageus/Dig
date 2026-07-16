using System;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result EquipTool(EntityId stackId, EntityId agentId, long tick)
    {
        ValidateTick(tick);
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        ItemStackState? stack = Find(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        ItemDefinition definition = Catalog.Get(stack.ItemId);
        if (!definition.IsTool || stack.Quantity != 1 || stack.ReservedQuantity != 0)
        {
            return Result.Failure(InventoryErrors.ToolRequired);
        }

        if (stack.Location != ItemLocation.InAgent(agentId))
        {
            return Result.Failure(InventoryErrors.ToolNotCarried);
        }

        bool occupied = _stacks.Values.Any(candidate =>
            candidate.Location == ItemLocation.EquippedBy(agentId));
        if (occupied)
        {
            return Result.Failure(InventoryErrors.ToolSlotOccupied);
        }

        return MoveAvailable(
            stackId,
            quantity: 1,
            ItemLocation.EquippedBy(agentId),
            splitStackId: default,
            tick);
    }
}

}