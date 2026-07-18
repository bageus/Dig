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
        ValidateResidentId(agentId);
        if (_heldItems.ContainsKey(agentId)
            || HasLegacyEquippedItem(agentId))
        {
            return Result.Failure(InventoryErrors.ToolSlotOccupied);
        }

        Result validation = ValidateToolTarget(stackId, agentId);
        return validation.IsFailure
            ? validation
            : HoldItem(agentId, stackId, 1, HeldItemPurpose.ToolUse, tick);
    }

    public Result CanSwitchTool(EntityId stackId, EntityId agentId, long tick)
    {
        ValidateTick(tick);
        ValidateResidentId(agentId);
        ItemStackState? target = Find(stackId);
        if (target is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (target.Location == ItemLocation.EquippedBy(agentId))
        {
            return Result.Success();
        }

        Result targetValidation = ValidateToolTarget(stackId, agentId);
        if (targetValidation.IsFailure)
        {
            HeldItemReferenceSnapshot? current = GetHeldItem(agentId);
            if (!current.HasValue || current.Value.StackId != stackId)
            {
                return targetValidation;
            }
        }

        HeldItemReferenceSnapshot? held = GetHeldItem(agentId);
        if (held.HasValue && !IsHeldReferenceValid(held.Value))
        {
            return Result.Failure(InventoryErrors.ToolSwitchUnsafe);
        }

        ItemStackState[] legacy = GetLegacyEquippedItems(agentId);
        if (legacy.Length > 1)
        {
            return Result.Failure(InventoryErrors.ToolSwitchUnsafe);
        }

        if (legacy.Length == 1)
        {
            ItemStackState current = legacy[0];
            ItemDefinition definition = Catalog.Get(current.ItemId);
            if (!definition.IsTool
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
        if (target.Location == ItemLocation.EquippedBy(agentId))
        {
            return Result.Success();
        }

        HeldItemReferenceSnapshot? held = GetHeldItem(agentId);
        if (held.HasValue)
        {
            return SwitchHeldItem(
                agentId,
                stackId,
                HeldItemPurpose.ToolUse,
                tick);
        }

        ItemStackState[] legacy = GetLegacyEquippedItems(agentId);
        if (legacy.Length == 1)
        {
            ItemStackState current = legacy[0];
            ItemLocation source = current.Location;
            ItemLocation destination = ItemLocation.InAgent(agentId);
            current.MoveFull(destination);
            IncrementVersion();
            Raise(new ItemStackMoved(
                tick,
                current.Id,
                current.Id,
                current.ItemId,
                current.Quantity,
                source,
                destination));
        }

        return HoldItem(agentId, stackId, 1, HeldItemPurpose.ToolUse, tick);
    }

    private Result ValidateToolTarget(EntityId stackId, EntityId agentId)
    {
        ItemStackState? stack = Find(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        ItemDefinition definition = Catalog.Get(stack.ItemId);
        if (!definition.IsTool
            || stack.Quantity != 1
            || stack.ReservedQuantity != 0)
        {
            return Result.Failure(InventoryErrors.ToolRequired);
        }

        if (!IsCarriedBy(stack.Location, agentId))
        {
            return Result.Failure(InventoryErrors.ToolNotCarried);
        }

        return stack.AvailableQuantity > 0 || stack.HeldQuantity > 0
            ? Result.Success()
            : Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
    }

    private bool HasLegacyEquippedItem(EntityId agentId)
    {
        return _stacks.Values.Any(candidate =>
            candidate.Location == ItemLocation.EquippedBy(agentId));
    }

    private ItemStackState[] GetLegacyEquippedItems(EntityId agentId)
    {
        ItemLocation location = ItemLocation.EquippedBy(agentId);
        return _stacks.Values.Where(candidate => candidate.Location == location).ToArray();
    }

    private static bool IsCarriedBy(ItemLocation location, EntityId agentId)
    {
        return location.Kind == ItemLocationKind.AgentInventory
            && location.HasOwner
            && location.OwnerId == agentId;
    }
}

}
