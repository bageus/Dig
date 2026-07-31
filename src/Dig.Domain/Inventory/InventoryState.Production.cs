using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public readonly struct ItemReservationAllocation
{
    public ItemReservationAllocation(EntityId stackId, ItemId itemId, int quantity)
    {
        if (stackId.IsEmpty || itemId.IsEmpty)
        {
            throw new ArgumentException("Allocation ids cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        StackId = stackId;
        ItemId = itemId;
        Quantity = quantity;
    }

    public EntityId StackId { get; }

    public ItemId ItemId { get; }

    public int Quantity { get; }
}

public readonly struct ItemStackCreation
{
    public ItemStackCreation(EntityId stackId, ItemId itemId, int quantity)
    {
        if (stackId.IsEmpty || itemId.IsEmpty)
        {
            throw new ArgumentException("Output stack ids cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        StackId = stackId;
        ItemId = itemId;
        Quantity = quantity;
    }

    public EntityId StackId { get; }

    public ItemId ItemId { get; }

    public int Quantity { get; }
}

public readonly struct ItemStackPlacement
{
    public ItemStackPlacement(ItemStackCreation stack, ItemLocation location)
    {
        Stack = stack;
        Location = location;
    }

    public ItemStackCreation Stack { get; }

    public ItemLocation Location { get; }
}

public sealed class ProductionInventoryCommitted : IDomainEvent
{
    public ProductionInventoryCommitted(
        long tick,
        EntityId orderId,
        IReadOnlyCollection<ItemReservationAllocation> inputs,
        IReadOnlyCollection<ItemStackCreation> outputs,
        ItemLocation outputLocation)
        : this(
            tick,
            orderId,
            inputs,
            outputs.Select(value => new ItemStackPlacement(value, outputLocation)).ToArray())
    {
    }

    public ProductionInventoryCommitted(
        long tick,
        EntityId orderId,
        IReadOnlyCollection<ItemReservationAllocation> inputs,
        IReadOnlyCollection<ItemStackPlacement> outputPlacements)
    {
        if (outputPlacements is null || outputPlacements.Count == 0)
        {
            throw new ArgumentException("Production output placements are required.");
        }

        Tick = tick;
        OrderId = orderId;
        Inputs = new ReadOnlyCollection<ItemReservationAllocation>(
            inputs.OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal).ToArray());
        OutputPlacements = new ReadOnlyCollection<ItemStackPlacement>(
            outputPlacements.OrderBy(value => value.Stack.StackId.ToString(), StringComparer.Ordinal)
                .ToArray());
        Outputs = new ReadOnlyCollection<ItemStackCreation>(
            OutputPlacements.Select(value => value.Stack).ToArray());
        OutputLocation = OutputPlacements[0].Location;
    }

    public long Tick { get; }

    public EntityId OrderId { get; }

    public IReadOnlyList<ItemReservationAllocation> Inputs { get; }

    public IReadOnlyList<ItemStackCreation> Outputs { get; }

    public IReadOnlyList<ItemStackPlacement> OutputPlacements { get; }

    public ItemLocation OutputLocation { get; }
}

public sealed partial class InventoryState
{
    public Result<IReadOnlyList<ItemReservationAllocation>> ReserveProductionInputs(
        EntityId orderId,
        ItemLocation location,
        IReadOnlyCollection<ItemConsumptionRequest> requirements,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(orderId);
        if (requirements is null)
        {
            throw new ArgumentNullException(nameof(requirements));
        }

        ItemConsumptionRequest[] normalized = NormalizeRequirements(requirements);
        List<ItemReservationAllocation> allocations = new List<ItemReservationAllocation>();
        foreach (ItemConsumptionRequest requirement in normalized)
        {
            ItemStackState[] candidates = _stacks.Values
                .Where(stack => stack.Location == location
                    && stack.ItemId == requirement.ItemId
                    && stack.AvailableQuantity > 0)
                .OrderBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
                .ToArray();
            int available = candidates.Sum(stack => stack.AvailableQuantity);
            if (available < requirement.Quantity)
            {
                return Result<IReadOnlyList<ItemReservationAllocation>>.Failure(
                    InventoryErrors.InsufficientAvailableQuantity);
            }

            int remaining = requirement.Quantity;
            foreach (ItemStackState stack in candidates)
            {
                int quantity = Math.Min(remaining, stack.AvailableQuantity);
                allocations.Add(new ItemReservationAllocation(
                    stack.Id,
                    stack.ItemId,
                    quantity));
                remaining -= quantity;
                if (remaining == 0)
                {
                    break;
                }
            }
        }

        foreach (ItemReservationAllocation allocation in allocations)
        {
            ItemStackState stack = _stacks[allocation.StackId];
            stack.Reserve(orderId, allocation.Quantity);
            Raise(new ItemQuantityReservationChanged(
                tick,
                stack.Id,
                orderId,
                stack.GetReservedQuantity(orderId)));
        }

        IncrementVersion();
        IReadOnlyList<ItemReservationAllocation> result =
            new ReadOnlyCollection<ItemReservationAllocation>(allocations.ToArray());
        return Result<IReadOnlyList<ItemReservationAllocation>>.Success(result);
    }

    public Result CompleteProductionTransaction(
        EntityId orderId,
        IReadOnlyCollection<ItemReservationAllocation> inputs,
        IReadOnlyCollection<ItemStackCreation> outputs,
        ItemLocation outputLocation,
        long tick)
    {
        return CompleteProductionTransaction(
            orderId,
            inputs,
            outputs.Select(value => new ItemStackPlacement(value, outputLocation)).ToArray(),
            tick);
    }

    public Result CompleteProductionTransaction(
        EntityId orderId,
        IReadOnlyCollection<ItemReservationAllocation> inputs,
        IReadOnlyCollection<ItemStackPlacement> outputPlacements,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(orderId);
        if (inputs is null || outputPlacements is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        ItemReservationAllocation[] inputValues = inputs.ToArray();
        ItemStackPlacement[] placementValues = outputPlacements.ToArray();
        ItemStackCreation[] outputValues = placementValues
            .Select(value => value.Stack)
            .ToArray();
        if (inputValues.Length == 0 || outputValues.Length == 0)
        {
            throw new ArgumentException("Production needs inputs and outputs.");
        }

        Result validation = ValidateProductionCommit(orderId, inputValues, outputValues);
        if (validation.IsFailure)
        {
            return validation;
        }

        foreach (ItemReservationAllocation input in inputValues)
        {
            ItemStackState stack = _stacks[input.StackId];
            stack.ConsumeReservedQuantity(orderId, input.Quantity);
            Raise(new ItemQuantityReservationChanged(
                tick,
                stack.Id,
                orderId,
                stack.GetReservedQuantity(orderId)));
            if (stack.Quantity == 0)
            {
                _stacks.Remove(stack.Id);
            }
        }

        foreach (ItemStackPlacement placement in placementValues)
        {
            ItemStackCreation output = placement.Stack;
            _stacks.Add(
                output.StackId,
                new ItemStackState(
                    output.StackId,
                    output.ItemId,
                    output.Quantity,
                    placement.Location));
        }

        IncrementVersion();
        Raise(new ProductionInventoryCommitted(
            tick,
            orderId,
            inputValues,
            placementValues));
        return Result.Success();
    }

    private Result ValidateProductionCommit(
        EntityId orderId,
        IReadOnlyCollection<ItemReservationAllocation> inputs,
        IReadOnlyCollection<ItemStackCreation> outputs)
    {
        foreach (ItemReservationAllocation input in inputs)
        {
            if (!_stacks.TryGetValue(input.StackId, out ItemStackState? stack))
            {
                return Result.Failure(InventoryErrors.StackNotFound);
            }

            if (stack.ItemId != input.ItemId
                || stack.GetReservedQuantity(orderId) < input.Quantity)
            {
                return Result.Failure(InventoryErrors.ReservationNotFound);
            }
        }

        EntityId[] outputIds = outputs.Select(value => value.StackId).ToArray();
        if (outputIds.Distinct().Count() != outputIds.Length
            || outputIds.Any(_stacks.ContainsKey))
        {
            return Result.Failure(InventoryErrors.StackAlreadyExists);
        }

        foreach (ItemStackCreation output in outputs)
        {
            ItemDefinition definition = Catalog.Get(output.ItemId);
            if (output.Quantity > definition.MaximumStackSize)
            {
                return Result.Failure(InventoryErrors.StackSizeExceeded);
            }
        }

        return Result.Success();
    }

    private static ItemConsumptionRequest[] NormalizeRequirements(
        IEnumerable<ItemConsumptionRequest> requirements)
    {
        ItemConsumptionRequest[] normalized = requirements
            .GroupBy(value => value.ItemId)
            .Select(group => new ItemConsumptionRequest(
                group.Key,
                checked(group.Sum(value => value.Quantity))))
            .OrderBy(value => value.ItemId)
            .ToArray();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one production input is required.");
        }

        return normalized;
    }
}
}
