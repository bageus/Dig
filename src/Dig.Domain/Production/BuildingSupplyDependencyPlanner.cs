using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Production
{

public static class BuildingSupplyDependencyPlanner
{
    public static ItemConsumptionRequest? PlanSingleExtractionRequest(
        BuildingSupplySnapshot supply,
        IReadOnlyCollection<ItemStackSnapshot> worldStacks,
        IReadOnlyCollection<CellId> revealedCells,
        IReadOnlyCollection<CellId> reachableCells,
        IReadOnlyCollection<ItemId> supportedItems,
        IReadOnlyCollection<ItemId>? targetItemIds = null)
    {
        if (supply is null || worldStacks is null
            || revealedCells is null || reachableCells is null
            || supportedItems is null)
        {
            throw new ArgumentNullException(nameof(supply));
        }

        HashSet<ItemId> supported = supportedItems.ToHashSet();
        if (targetItemIds != null)
        {
            supported.IntersectWith(targetItemIds);
        }
        HashSet<CellId> revealed = revealedCells.ToHashSet();
        HashSet<CellId> reachable = reachableCells.ToHashSet();
        foreach (BuildingStockSnapshot stock in supply.Stocks)
        {
            if (!stock.DeliveryEnabled
                || stock.Missing == 0
                || !supported.Contains(stock.ItemId))
            {
                continue;
            }

            bool hasEligibleWorldSource = worldStacks.Any(stack =>
                stack.ItemId == stock.ItemId
                && stack.Location.Kind == ItemLocationKind.World
                && stack.Location.HasCell
                && stack.AvailableQuantity > 0
                && revealed.Contains(stack.Location.CellId)
                && reachable.Contains(stack.Location.CellId));
            if (!hasEligibleWorldSource)
            {
                return new ItemConsumptionRequest(stock.ItemId, quantity: 1);
            }
        }

        return null;
    }

    public static bool HasRequestedWorldQuantity(
        IReadOnlyCollection<ItemConsumptionRequest> requests,
        IReadOnlyCollection<ItemStackSnapshot> worldStacks)
    {
        if (requests is null || worldStacks is null)
        {
            throw new ArgumentNullException(nameof(requests));
        }

        return requests.All(request => worldStacks
            .Where(stack => stack.ItemId == request.ItemId
                && stack.Location.Kind == ItemLocationKind.World)
            .Sum(stack => stack.Quantity) >= request.Quantity);
    }
}

}
