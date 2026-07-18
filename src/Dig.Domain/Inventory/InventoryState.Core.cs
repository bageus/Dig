using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState : AggregateRoot
{
    private readonly Dictionary<EntityId, ItemStackState> _stacks =
        new Dictionary<EntityId, ItemStackState>();

    public InventoryState(ItemCatalog catalog)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public ItemCatalog Catalog { get; }

    public long Version { get; private set; }

    public Result AddStack(
        EntityId stackId,
        ItemId itemId,
        int quantity,
        ItemLocation location,
        long tick)
    {
        ValidateTick(tick);
        if (stackId.IsEmpty)
        {
            throw new ArgumentException("Stack id cannot be empty.", nameof(stackId));
        }

        if (_stacks.ContainsKey(stackId))
        {
            return Result.Failure(InventoryErrors.StackAlreadyExists);
        }

        ItemDefinition definition = Catalog.Get(itemId);
        if (quantity <= 0)
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        if (quantity > definition.MaximumStackSize)
        {
            return Result.Failure(InventoryErrors.StackSizeExceeded);
        }

        Result locationValidation = ValidateResidentLocationForNewStack(
            definition,
            location);
        if (locationValidation.IsFailure)
        {
            return locationValidation;
        }

        _stacks.Add(stackId, new ItemStackState(stackId, itemId, quantity, location));
        IncrementVersion();
        return Result.Success();
    }

    public Result ReserveQuantity(
        EntityId stackId,
        EntityId jobId,
        int quantity,
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

        if (quantity > stack.AvailableQuantity)
        {
            return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
        }

        stack.Reserve(jobId, quantity);
        IncrementVersion();
        Raise(new ItemQuantityReservationChanged(
            tick,
            stackId,
            jobId,
            stack.GetReservedQuantity(jobId)));
        return Result.Success();
    }

    public int ReleaseReservations(EntityId jobId, long tick)
    {
        ValidateTick(tick);
        ValidateJobId(jobId);
        int released = 0;

        foreach (ItemStackState stack in _stacks.Values)
        {
            int stackReleased = stack.Release(jobId);
            if (stackReleased == 0)
            {
                continue;
            }

            released = checked(released + stackReleased);
            Raise(new ItemQuantityReservationChanged(tick, stack.Id, jobId, 0));
        }

        if (released > 0)
        {
            IncrementVersion();
        }

        return released;
    }

    public ItemStackSnapshot? GetStack(EntityId stackId)
    {
        return Find(stackId)?.CreateSnapshot();
    }

    public IReadOnlyList<ItemStackSnapshot> FindAvailable(ItemId itemId)
    {
        ItemStackSnapshot[] stacks = _stacks.Values
            .Where(stack => stack.ItemId == itemId && stack.AvailableQuantity > 0)
            .Select(stack => stack.CreateSnapshot())
            .OrderBy(stack => stack.Location)
            .ThenBy(stack => stack.StackId.ToString(), StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<ItemStackSnapshot>(stacks);
    }

    public int GetTotal(ItemId itemId)
    {
        return _stacks.Values
            .Where(stack => stack.ItemId == itemId)
            .Sum(stack => stack.Quantity);
    }

    public int GetQuantityAt(ItemId itemId, ItemLocation location)
    {
        return _stacks.Values
            .Where(stack => stack.ItemId == itemId && stack.Location == location)
            .Sum(stack => stack.Quantity);
    }

    public int GetAvailableQuantityAt(ItemId itemId, ItemLocation location)
    {
        return _stacks.Values
            .Where(stack => stack.ItemId == itemId && stack.Location == location)
            .Sum(stack => stack.AvailableQuantity);
    }

    public InventorySnapshot CreateSnapshot()
    {
        return new InventorySnapshot(
            Version,
            _stacks.Values.Select(stack => stack.CreateSnapshot()).ToArray());
    }

    private ItemStackState? Find(EntityId stackId)
    {
        if (stackId.IsEmpty)
        {
            throw new ArgumentException("Stack id cannot be empty.", nameof(stackId));
        }

        return _stacks.TryGetValue(stackId, out ItemStackState? stack) ? stack : null;
    }

    private void IncrementVersion()
    {
        Version = checked(Version + 1);
    }

    private static void ValidateJobId(EntityId jobId)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}

}
