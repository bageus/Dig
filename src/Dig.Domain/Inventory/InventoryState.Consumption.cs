using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public readonly struct ItemConsumptionRequest
{
    public ItemConsumptionRequest(ItemId itemId, int quantity)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        ItemId = itemId;
        Quantity = quantity;
    }

    public ItemId ItemId { get; }

    public int Quantity { get; }
}

public sealed class ItemsConsumed : IDomainEvent
{
    public ItemsConsumed(
        long tick,
        ItemLocation location,
        IReadOnlyCollection<ItemConsumptionRequest> requests)
    {
        Tick = tick;
        Location = location;
        Requests = new ReadOnlyCollection<ItemConsumptionRequest>(
            requests.OrderBy(request => request.ItemId).ToArray());
    }

    public long Tick { get; }

    public ItemLocation Location { get; }

    public IReadOnlyList<ItemConsumptionRequest> Requests { get; }
}

public sealed partial class InventoryState
{
    public Result ConsumeAvailableAt(
        ItemLocation location,
        IReadOnlyCollection<ItemConsumptionRequest> requests,
        long tick)
    {
        ValidateTick(tick);
        if (requests is null)
        {
            throw new ArgumentNullException(nameof(requests));
        }

        ItemConsumptionRequest[] normalized = requests
            .GroupBy(request => request.ItemId)
            .Select(group => new ItemConsumptionRequest(
                group.Key,
                checked(group.Sum(request => request.Quantity))))
            .OrderBy(request => request.ItemId)
            .ToArray();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one consumption request is required.", nameof(requests));
        }

        foreach (ItemConsumptionRequest request in normalized)
        {
            int available = _stacks.Values
                .Where(stack => stack.Location == location && stack.ItemId == request.ItemId)
                .Sum(stack => stack.AvailableQuantity);
            if (available < request.Quantity)
            {
                return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
            }
        }

        foreach (ItemConsumptionRequest request in normalized)
        {
            int remaining = request.Quantity;
            ItemStackState[] candidates = _stacks.Values
                .Where(stack => stack.Location == location
                    && stack.ItemId == request.ItemId
                    && stack.AvailableQuantity > 0)
                .OrderBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
                .ToArray();
            foreach (ItemStackState stack in candidates)
            {
                int consumed = Math.Min(remaining, stack.AvailableQuantity);
                stack.ConsumeAvailable(consumed);
                remaining -= consumed;
                if (stack.Quantity == 0)
                {
                    _stacks.Remove(stack.Id);
                }

                if (remaining == 0)
                {
                    break;
                }
            }
        }

        IncrementVersion();
        Raise(new ItemsConsumed(tick, location, normalized));
        return Result.Success();
    }

    public int ReturnUnreservedStacks(
        ItemLocation source,
        ItemLocation destination,
        long tick)
    {
        ValidateTick(tick);
        ItemStackState[] stacks = _stacks.Values
            .Where(stack => stack.Location == source && stack.ReservedQuantity == 0)
            .OrderBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (ItemStackState stack in stacks)
        {
            Result moved = MoveAvailable(
                stack.Id,
                stack.Quantity,
                destination,
                splitStackId: default,
                tick);
            if (moved.IsFailure)
            {
                throw new InvalidOperationException(moved.Error!.ToString());
            }
        }

        return stacks.Length;
    }
}
}
