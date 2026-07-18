using System;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public HeldItemReferenceSnapshot? GetHeldItem(EntityId residentId)
    {
        ValidateResidentId(residentId);
        return _heldItems.TryGetValue(
            residentId,
            out HeldItemReferenceSnapshot held)
                ? held
                : (HeldItemReferenceSnapshot?)null;
    }

    public Result HoldItem(
        EntityId residentId,
        EntityId stackId,
        int quantity,
        HeldItemPurpose purpose,
        long tick)
    {
        ValidateTick(tick);
        ValidateResidentId(residentId);
        if (_heldItems.ContainsKey(residentId))
        {
            return Result.Failure(InventoryErrors.HeldItemAlreadyExists);
        }

        ItemStackState? stack = Find(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (!IsResidentInventoryLocation(stack.Location, residentId))
        {
            return Result.Failure(InventoryErrors.HeldItemStackNotCarried);
        }

        if (quantity <= 0 || quantity > stack.AvailableQuantity)
        {
            return Result.Failure(quantity <= 0
                ? InventoryErrors.InvalidQuantity
                : InventoryErrors.InsufficientAvailableQuantity);
        }

        ItemDefinition definition = Catalog.Get(stack.ItemId);
        if (purpose == HeldItemPurpose.ToolUse
            && (!definition.IsTool || quantity != 1))
        {
            return Result.Failure(InventoryErrors.ToolRequired);
        }

        stack.Hold(quantity);
        HeldItemReferenceSnapshot held = new HeldItemReferenceSnapshot(
            residentId,
            stackId,
            quantity,
            purpose);
        _heldItems.Add(residentId, held);
        IncrementVersion();
        Raise(new HeldItemReferenceChanged(
            tick,
            residentId,
            stackId,
            quantity,
            purpose,
            isActive: true));
        return Result.Success();
    }

    public Result ReleaseHeldItem(EntityId residentId, long tick)
    {
        ValidateTick(tick);
        ValidateResidentId(residentId);
        if (!_heldItems.TryGetValue(
                residentId,
                out HeldItemReferenceSnapshot held))
        {
            return Result.Failure(InventoryErrors.HeldItemNotFound);
        }

        ItemStackState? stack = Find(held.StackId);
        if (stack is null || stack.HeldQuantity < held.Quantity)
        {
            return Result.Failure(InventoryErrors.HeldItemReferenceInvalid);
        }

        stack.ReleaseHeld(held.Quantity);
        _heldItems.Remove(residentId);
        IncrementVersion();
        Raise(new HeldItemReferenceChanged(
            tick,
            residentId,
            held.StackId,
            held.Quantity,
            held.Purpose,
            isActive: false));
        return Result.Success();
    }

    public Result SwitchHeldItem(
        EntityId residentId,
        EntityId stackId,
        HeldItemPurpose purpose,
        long tick)
    {
        ValidateTick(tick);
        ValidateResidentId(residentId);
        ItemStackState? target = Find(stackId);
        if (target is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (!IsResidentInventoryLocation(target.Location, residentId))
        {
            return Result.Failure(InventoryErrors.HeldItemStackNotCarried);
        }

        ItemDefinition definition = Catalog.Get(target.ItemId);
        if (purpose == HeldItemPurpose.ToolUse && !definition.IsTool)
        {
            return Result.Failure(InventoryErrors.ToolRequired);
        }

        HeldItemReferenceSnapshot? current = GetHeldItem(residentId);
        int currentQuantityOnTarget = current.HasValue
            && current.Value.StackId == target.Id
                ? current.Value.Quantity
                : 0;
        if (target.AvailableQuantity + currentQuantityOnTarget < 1)
        {
            return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
        }

        if (current.HasValue && current.Value.StackId == target.Id)
        {
            return current.Value.Purpose == purpose
                ? Result.Success()
                : ReplaceHeldPurpose(current.Value, purpose, tick);
        }

        HeldItemReferenceSnapshot? old = current;
        if (old.HasValue)
        {
            Result released = ReleaseHeldItem(residentId, tick);
            if (released.IsFailure)
            {
                return released;
            }
        }

        Result held = HoldItem(residentId, stackId, 1, purpose, tick);
        if (held.IsSuccess || !old.HasValue)
        {
            return held;
        }

        Result rollback = HoldItem(
            residentId,
            old.Value.StackId,
            old.Value.Quantity,
            old.Value.Purpose,
            tick);
        if (rollback.IsFailure)
        {
            throw new InvalidOperationException(
                "Held item switch rollback failed after preflight validation.");
        }

        return held;
    }

    public int ClearInvalidHeldItems(long tick)
    {
        ValidateTick(tick);
        EntityId[] invalid = _heldItems.Values
            .Where(held => !IsHeldReferenceValid(held))
            .Select(held => held.ResidentId)
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();
        for (int index = 0; index < invalid.Length; index++)
        {
            EntityId residentId = invalid[index];
            HeldItemReferenceSnapshot held = _heldItems[residentId];
            ItemStackState? stack = Find(held.StackId);
            if (stack != null && stack.HeldQuantity >= held.Quantity)
            {
                stack.ReleaseHeld(held.Quantity);
            }

            _heldItems.Remove(residentId);
            IncrementVersion();
            Raise(new HeldItemReferenceChanged(
                tick,
                residentId,
                held.StackId,
                held.Quantity,
                held.Purpose,
                isActive: false));
        }

        return invalid.Length;
    }

    private Result ReplaceHeldPurpose(
        HeldItemReferenceSnapshot current,
        HeldItemPurpose purpose,
        long tick)
    {
        HeldItemReferenceSnapshot replacement = new HeldItemReferenceSnapshot(
            current.ResidentId,
            current.StackId,
            current.Quantity,
            purpose);
        _heldItems[current.ResidentId] = replacement;
        IncrementVersion();
        Raise(new HeldItemReferenceChanged(
            tick,
            current.ResidentId,
            current.StackId,
            current.Quantity,
            purpose,
            isActive: true));
        return Result.Success();
    }

    private bool IsHeldReferenceValid(HeldItemReferenceSnapshot held)
    {
        ItemStackState? stack = Find(held.StackId);
        return stack != null
            && stack.HeldQuantity >= held.Quantity
            && IsResidentInventoryLocation(stack.Location, held.ResidentId);
    }

    private static bool IsResidentInventoryLocation(
        ItemLocation location,
        EntityId residentId)
    {
        return location.Kind == ItemLocationKind.AgentInventory
            && location.HasOwner
            && location.OwnerId == residentId;
    }
}

}
