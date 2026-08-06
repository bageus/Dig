using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result ReserveAvailableAt(
        ItemLocation location,
        ItemId itemId,
        EntityId jobId,
        int quantity,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(jobId);
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        ItemStackState[] candidates = _stacks.Values
            .Where(stack => stack.Location == location
                && stack.ItemId == itemId
                && stack.AvailableQuantity > 0)
            .OrderBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (candidates.Sum(stack => stack.AvailableQuantity) < quantity)
        {
            return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
        }

        int remaining = quantity;
        for (int index = 0; index < candidates.Length && remaining > 0; index++)
        {
            ItemStackState stack = candidates[index];
            int reserved = Math.Min(remaining, stack.AvailableQuantity);
            stack.Reserve(jobId, reserved);
            remaining -= reserved;
            Raise(new ItemQuantityReservationChanged(
                tick,
                stack.Id,
                jobId,
                stack.GetReservedQuantity(jobId)));
        }

        IncrementVersion();
        return Result.Success();
    }

    public IReadOnlyList<ItemStackSnapshot> GetReservedStacksAt(
        EntityId jobId,
        ItemId itemId,
        ItemLocation location)
    {
        ValidateJobId(jobId);
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        ItemStackSnapshot[] stacks = _stacks.Values
            .Where(stack => stack.Location == location
                && stack.ItemId == itemId
                && stack.GetReservedQuantity(jobId) > 0)
            .OrderBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .Select(stack => stack.CreateSnapshot())
            .ToArray();
        return new ReadOnlyCollection<ItemStackSnapshot>(stacks);
    }

    public int GetReservedQuantityAt(
        EntityId jobId,
        ItemId itemId,
        ItemLocation location)
    {
        ValidateJobId(jobId);
        return _stacks.Values
            .Where(stack => stack.Location == location && stack.ItemId == itemId)
            .Sum(stack => stack.GetReservedQuantity(jobId));
    }
}

}
