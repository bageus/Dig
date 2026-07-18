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

        if (!IsCarriedBy(stack.Location, agentId))
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

    public Result CanSwitchTool(EntityId stackId, EntityId agentId, long tick)
    {
        ValidateTick(tick);
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        ItemStackState? target = Find(stackId);
        if (target is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        ItemDefinition targetDefinition = Catalog.Get(target.ItemId);
        if (!targetDefinition.IsTool
            || target.Quantity != 1
            || target.ReservedQuantity != 0)
        {
            return Result.Failure(InventoryErrors.ToolRequired);
        }

        ItemLocation equippedLocation = ItemLocation.EquippedBy(agentId);
        if (target.Location == equippedLocation)
        {
            return Result.Success();
        }

        if (!IsCarriedBy(target.Location, agentId))
        {
            return Result.Failure(InventoryErrors.ToolNotCarried);
        }

        ItemStackState[] equipped = _stacks.Values
            .Where(candidate => candidate.Location == equippedLocation)
            .ToArray();
        if (equipped.Length > 1)
        {
            throw new InvalidOperationException(
                "A resident cannot have more than one equipped item.");
        }

        if (equipped.Length == 1)
        {
            ItemStackState current = equipped[0];
            ItemDefinition currentDefinition = Catalog.Get(current.ItemId);
            if (!currentDefinition.IsTool
                || current.Quantity != 1
                || current.ReservedQuantity != 0)
            {
                return Result.Failure(InventoryErrors.ToolSwitchUnsafe);
            }
        }

        return Result.Success();
    }

    public Result SwitchTool(EntityId stackId, EntityId agentId, long tick)
    {
        Result validation = CanSwitchTool(stackId, agentId, tick);
        if (validation.IsFailure)
        {
            return validation;
        }

        ItemStackState target = Find(stackId)!;
        ItemLocation equippedLocation = ItemLocation.EquippedBy(agentId);
        if (target.Location == equippedLocation)
        {
            return Result.Success();
        }

        ItemLocation targetCarriedLocation = target.Location;
        ItemStackState? current = _stacks.Values.SingleOrDefault(
            candidate => candidate.Location == equippedLocation);
        if (current is not null)
        {
            current.MoveFull(targetCarriedLocation);
        }

        target.MoveFull(equippedLocation);
        IncrementVersion();
        if (current is not null)
        {
            Raise(new ItemStackMoved(
                tick,
                current.Id,
                current.Id,
                current.ItemId,
                1,
                equippedLocation,
                targetCarriedLocation));
        }

        Raise(new ItemStackMoved(
            tick,
            target.Id,
            target.Id,
            target.ItemId,
            1,
            targetCarriedLocation,
            equippedLocation));
        return Result.Success();
    }

    private static bool IsCarriedBy(ItemLocation location, EntityId agentId)
    {
        return location.Kind == ItemLocationKind.AgentInventory
            && location.HasOwner
            && location.OwnerId == agentId;
    }
}

}
