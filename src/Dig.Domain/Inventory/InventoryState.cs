using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory;

public sealed class InventoryState : AggregateRoot
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

        _stacks.Add(stackId, new ItemStackState(stackId, itemId, quantity, location));
        IncrementVersion();
        return Result.Success();
    }

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

        return MoveAvailable(
            stackId,
            quantity: 1,
            ItemLocation.EquippedBy(agentId),
            splitStackId: default,
            tick);
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

    public InventorySnapshot CreateSnapshot()
    {
        return new InventorySnapshot(
            Version,
            _stacks.Values.Select(stack => stack.CreateSnapshot()).ToArray());
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
