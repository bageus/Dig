using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Production;

namespace Dig.Presentation.Production
{

public sealed class BuildingProductionPresenter
{
    private readonly ProductionContentCatalog _content;
    private readonly ItemCatalog _items;

    public BuildingProductionPresenter(
        ProductionContentCatalog content,
        ItemCatalog items)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public BuildingProductionViewModel Present(
        EntityId buildingId,
        ProductionState production,
        BuildingSupplySnapshot supply)
    {
        if (production is null || supply is null)
        {
            throw new ArgumentNullException(nameof(production));
        }

        if (supply.BuildingId != buildingId)
        {
            throw new ArgumentException("Supply snapshot belongs to another building.");
        }

        Dictionary<ItemId, int> stock = supply.Stocks.ToDictionary(
            value => value.ItemId,
            value => value.Current);
        ProductionIconViewModel[] products = supply.Definition.RecipeIds
            .Select(recipeId => PresentRecipe(buildingId, recipeId, production, stock))
            .ToArray();
        BuildingStockIconViewModel[] stocks = supply.Stocks
            .Select(value => new BuildingStockIconViewModel(
                value.ItemId,
                _items.Get(value.ItemId).DisplayName,
                value.Current,
                value.Incoming,
                value.Capacity,
                value.DeliveryEnabled))
            .ToArray();
        return new BuildingProductionViewModel(
            buildingId,
            supply.Definition.AnimationProfileId,
            products,
            stocks);
    }

    private ProductionIconViewModel PresentRecipe(
        EntityId buildingId,
        RecipeId recipeId,
        ProductionState production,
        IReadOnlyDictionary<ItemId, int> stock)
    {
        RecipeDefinition recipe = _content.GetRecipe(recipeId);
        ContentItemQuantity output = recipe.Outputs[0];
        ProductionIngredientViewModel[] ingredients = recipe.Inputs
            .Select(input => new ProductionIngredientViewModel(
                input.ItemId,
                _items.Get(input.ItemId).DisplayName,
                input.Quantity,
                stock.TryGetValue(input.ItemId, out int current) ? current : 0))
            .ToArray();
        return new ProductionIconViewModel(
            recipe.Id,
            output.ItemId,
            recipe.DisplayName,
            output.Quantity,
            production.GetQueuedCount(buildingId, recipe.Id),
            ingredients);
    }
}

}
