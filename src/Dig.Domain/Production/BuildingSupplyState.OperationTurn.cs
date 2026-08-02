using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Production
{

public enum BuildingOperationTurn
{
    Production = 0,
    Supply = 1,
}


public static class BuildingSupplyQueuePolicy
{
    public static int GetRefillThreshold(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        return (capacity / 2) + (capacity % 2);
    }

    public static bool ShouldAttemptSupplyBeforeProduction(
        BuildingSupplySnapshot supply,
        RecipeDefinition recipe,
        IReadOnlyDictionary<ItemId, int> availableQuantities)
    {
        if (supply is null)
        {
            throw new ArgumentNullException(nameof(supply));
        }

        if (recipe is null)
        {
            throw new ArgumentNullException(nameof(recipe));
        }

        if (availableQuantities is null)
        {
            throw new ArgumentNullException(nameof(availableQuantities));
        }

        bool runnable = recipe.Inputs.All(input =>
            availableQuantities.TryGetValue(input.ItemId, out int available)
                && available >= input.Quantity);
        if (!runnable)
        {
            return true;
        }

        if (supply.OperationTurn == BuildingOperationTurn.Production)
        {
            return false;
        }

        Dictionary<ItemId, BuildingStockSnapshot> stocks = supply.Stocks
            .ToDictionary(value => value.ItemId);
        foreach (ItemId itemId in recipe.Inputs
            .Select(value => value.ItemId)
            .Distinct())
        {
            if (!stocks.TryGetValue(itemId, out BuildingStockSnapshot stock))
            {
                throw new InvalidOperationException(
                    $"Recipe input '{itemId}' has no internal-stock rule.");
            }

            if (stock.IsBelowRefillThreshold)
            {
                return true;
            }
        }

        return false;
    }
}

public sealed partial class BuildingSupplyState
{
    public Result SetOperationTurn(
        EntityId buildingId,
        BuildingOperationTurn operationTurn,
        long tick)
    {
        ValidateTick(tick);
        if (!Enum.IsDefined(typeof(BuildingOperationTurn), operationTurn))
        {
            throw new ArgumentOutOfRangeException(nameof(operationTurn));
        }

        WorkstationSupplyEntry? entry = Find(buildingId);
        if (entry is null)
        {
            return Result.Failure(BuildingSupplyErrors.WorkstationNotFound);
        }

        entry.SetOperationTurn(operationTurn);
        return Result.Success();
    }
}

}
