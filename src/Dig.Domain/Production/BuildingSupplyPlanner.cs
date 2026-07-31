using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Production
{

public readonly struct BuildingSupplyAllocation
{
    public BuildingSupplyAllocation(
        EntityId sourceStackId,
        ItemId itemId,
        int quantity,
        CellId sourceCell)
    {
        SourceStackId = sourceStackId;
        ItemId = itemId;
        Quantity = quantity;
        SourceCell = sourceCell;
    }

    public EntityId SourceStackId { get; }
    public ItemId ItemId { get; }
    public int Quantity { get; }
    public CellId SourceCell { get; }
}

public sealed class BuildingSupplyPlan
{
    public BuildingSupplyPlan(IReadOnlyCollection<BuildingSupplyAllocation> allocations)
    {
        Allocations = new ReadOnlyCollection<BuildingSupplyAllocation>(allocations.ToArray());
    }

    public IReadOnlyList<BuildingSupplyAllocation> Allocations { get; }
    public int SlotCount => TotalQuantity;
    public int TotalQuantity => Allocations.Sum(value => value.Quantity);
}

public static class BuildingSupplyPlanner
{
    public static BuildingSupplyPlan Plan(
        BuildingSupplySnapshot supply,
        IReadOnlyCollection<ItemStackSnapshot> worldStacks,
        IReadOnlyCollection<CellId> revealedCells,
        IReadOnlyCollection<CellId> reachableCells,
        CellId destination,
        int freeSlotCount,
        bool productionActive)
    {
        if (supply is null || worldStacks is null
            || revealedCells is null || reachableCells is null)
        {
            throw new ArgumentNullException(nameof(supply));
        }

        if (freeSlotCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(freeSlotCount));
        }

        if (productionActive || supply.HasActiveSupply || freeSlotCount == 0)
        {
            return new BuildingSupplyPlan(Array.Empty<BuildingSupplyAllocation>());
        }

        HashSet<CellId> revealed = revealedCells.ToHashSet();
        HashSet<CellId> reachable = reachableCells.ToHashSet();
        List<BuildingSupplyAllocation> allocations = new List<BuildingSupplyAllocation>();
        HashSet<EntityId> used = new HashSet<EntityId>();
        // Resident inventory is unit-per-slot, so each allocated unit consumes one slot.
        int availableUnitSlots = freeSlotCount;
        foreach (BuildingStockSnapshot stock in supply.Stocks)
        {
            if (!stock.DeliveryEnabled || stock.Missing == 0)
            {
                continue;
            }

            int remaining = stock.Missing;
            ItemStackSnapshot[] candidates = worldStacks
                .Where(stack => stack.ItemId == stock.ItemId
                    && stack.Location.Kind == ItemLocationKind.World
                    && stack.Location.HasCell
                    && stack.AvailableQuantity > 0
                    && revealed.Contains(stack.Location.CellId)
                    && reachable.Contains(stack.Location.CellId)
                    && !used.Contains(stack.StackId))
                .OrderBy(stack => Distance(stack.Location.CellId, destination))
                .ThenBy(stack => stack.StackId.ToString(), StringComparer.Ordinal)
                .ToArray();
            foreach (ItemStackSnapshot stack in candidates)
            {
                if (availableUnitSlots == 0 || remaining == 0)
                {
                    break;
                }

                int quantity = Math.Min(
                    availableUnitSlots,
                    Math.Min(remaining, stack.AvailableQuantity));
                allocations.Add(new BuildingSupplyAllocation(
                    stack.StackId,
                    stack.ItemId,
                    quantity,
                    stack.Location.CellId));
                used.Add(stack.StackId);
                remaining -= quantity;
                availableUnitSlots -= quantity;
            }

            if (availableUnitSlots == 0)
            {
                break;
            }
        }

        return new BuildingSupplyPlan(allocations);
    }

    public static BuildingSupplyPlan PlanRequests(
        BuildingSupplySnapshot supply,
        IReadOnlyCollection<ItemStackSnapshot> worldStacks,
        IReadOnlyCollection<CellId> revealedCells,
        IReadOnlyCollection<CellId> reachableCells,
        CellId destination,
        int freeSlotCount,
        IReadOnlyCollection<ItemConsumptionRequest> requests)
    {
        if (supply is null || worldStacks is null || revealedCells is null
            || reachableCells is null || requests is null)
        {
            throw new ArgumentNullException(nameof(supply));
        }

        if (freeSlotCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(freeSlotCount));
        }

        if (supply.HasActiveSupply || freeSlotCount == 0)
        {
            return new BuildingSupplyPlan(Array.Empty<BuildingSupplyAllocation>());
        }

        Dictionary<ItemId, int> requested = requests
            .GroupBy(value => value.ItemId)
            .ToDictionary(group => group.Key, group => group.Sum(value => value.Quantity));
        HashSet<CellId> revealed = revealedCells.ToHashSet();
        HashSet<CellId> reachable = reachableCells.ToHashSet();
        List<BuildingSupplyAllocation> allocations = new List<BuildingSupplyAllocation>();
        HashSet<EntityId> used = new HashSet<EntityId>();
        int availableUnitSlots = freeSlotCount;
        foreach (BuildingStockSnapshot stock in supply.Stocks)
        {
            if (!stock.DeliveryEnabled
                || !requested.TryGetValue(stock.ItemId, out int requestedQuantity)
                || requestedQuantity <= 0
                || stock.Missing == 0)
            {
                continue;
            }

            int remaining = Math.Min(stock.Missing, requestedQuantity);
            ItemStackSnapshot[] candidates = worldStacks
                .Where(stack => stack.ItemId == stock.ItemId
                    && stack.Location.Kind == ItemLocationKind.World
                    && stack.Location.HasCell
                    && stack.AvailableQuantity > 0
                    && revealed.Contains(stack.Location.CellId)
                    && reachable.Contains(stack.Location.CellId)
                    && !used.Contains(stack.StackId))
                .OrderBy(stack => Distance(stack.Location.CellId, destination))
                .ThenBy(stack => stack.StackId.ToString(), StringComparer.Ordinal)
                .ToArray();
            foreach (ItemStackSnapshot stack in candidates)
            {
                if (availableUnitSlots == 0 || remaining == 0)
                {
                    break;
                }

                int quantity = Math.Min(
                    availableUnitSlots,
                    Math.Min(remaining, stack.AvailableQuantity));
                allocations.Add(new BuildingSupplyAllocation(
                    stack.StackId,
                    stack.ItemId,
                    quantity,
                    stack.Location.CellId));
                used.Add(stack.StackId);
                remaining -= quantity;
                availableUnitSlots -= quantity;
            }

            if (availableUnitSlots == 0)
            {
                break;
            }
        }

        bool complete = requested.All(pair => allocations
            .Where(value => value.ItemId == pair.Key)
            .Sum(value => value.Quantity) >= pair.Value);
        return complete
            ? new BuildingSupplyPlan(allocations)
            : new BuildingSupplyPlan(Array.Empty<BuildingSupplyAllocation>());
    }

    private static int Distance(CellId left, CellId right)
    {
        return Math.Abs(left.X - right.X)
            + Math.Abs(left.Y - right.Y)
            + Math.Abs(left.Z - right.Z);
    }
}

}
