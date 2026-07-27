using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Presentation.Production
{

public sealed class ProductionIngredientViewModel
{
    public ProductionIngredientViewModel(
        ItemId itemId,
        string displayName,
        int required,
        int current)
    {
        if (itemId.IsEmpty || string.IsNullOrWhiteSpace(displayName)
            || required <= 0 || current < 0)
        {
            throw new ArgumentException("Production ingredient values are invalid.");
        }

        ItemId = itemId;
        DisplayName = displayName.Trim();
        Required = required;
        Current = current;
    }

    public ItemId ItemId { get; }
    public string DisplayName { get; }
    public int Required { get; }
    public int Current { get; }
    public int Missing => Math.Max(0, Required - Current);
}

public sealed class ProductionIconViewModel
{
    public ProductionIconViewModel(
        RecipeId recipeId,
        ItemId outputItemId,
        string displayName,
        int outputQuantity,
        int queuedCount,
        IReadOnlyCollection<ProductionIngredientViewModel> ingredients)
    {
        if (recipeId.IsEmpty || outputItemId.IsEmpty
            || string.IsNullOrWhiteSpace(displayName)
            || outputQuantity <= 0 || queuedCount < 0 || ingredients is null)
        {
            throw new ArgumentException("Production icon values are invalid.");
        }

        RecipeId = recipeId;
        OutputItemId = outputItemId;
        DisplayName = displayName.Trim();
        OutputQuantity = outputQuantity;
        QueuedCount = queuedCount;
        Ingredients = new ReadOnlyCollection<ProductionIngredientViewModel>(
            ingredients.OrderBy(value => value.ItemId).ToArray());
    }

    public RecipeId RecipeId { get; }
    public ItemId OutputItemId { get; }
    public string DisplayName { get; }
    public int OutputQuantity { get; }
    public int QueuedCount { get; }
    public IReadOnlyList<ProductionIngredientViewModel> Ingredients { get; }
    public bool HasInputs => Ingredients.All(value => value.Missing == 0);
    public bool IsOrange => !HasInputs;
    public string Tooltip => string.Join(
        " · ",
        Ingredients.Select(value =>
            $"{value.DisplayName} {value.Current}/{value.Required}"));
}

public sealed class BuildingStockIconViewModel
{
    public BuildingStockIconViewModel(
        ItemId itemId,
        string displayName,
        int current,
        int incoming,
        int capacity,
        bool deliveryEnabled)
    {
        if (itemId.IsEmpty || string.IsNullOrWhiteSpace(displayName)
            || current < 0 || incoming < 0 || capacity <= 0
            || current + incoming > capacity)
        {
            throw new ArgumentException("Building stock values are invalid.");
        }

        ItemId = itemId;
        DisplayName = displayName.Trim();
        Current = current;
        Incoming = incoming;
        Capacity = capacity;
        DeliveryEnabled = deliveryEnabled;
    }

    public ItemId ItemId { get; }
    public string DisplayName { get; }
    public int Current { get; }
    public int Incoming { get; }
    public int Capacity { get; }
    public bool DeliveryEnabled { get; }
}

public sealed class BuildingProductionViewModel
{
    public BuildingProductionViewModel(
        EntityId buildingId,
        string animationProfileId,
        IReadOnlyCollection<ProductionIconViewModel> products,
        IReadOnlyCollection<BuildingStockIconViewModel> stocks)
    {
        if (buildingId.IsEmpty || string.IsNullOrWhiteSpace(animationProfileId)
            || products is null || stocks is null)
        {
            throw new ArgumentException("Building production values are invalid.");
        }

        BuildingId = buildingId;
        AnimationProfileId = animationProfileId.Trim();
        Products = new ReadOnlyCollection<ProductionIconViewModel>(products.ToArray());
        Stocks = new ReadOnlyCollection<BuildingStockIconViewModel>(stocks.ToArray());
    }

    public EntityId BuildingId { get; }
    public string AnimationProfileId { get; }
    public IReadOnlyList<ProductionIconViewModel> Products { get; }
    public IReadOnlyList<BuildingStockIconViewModel> Stocks { get; }
}

}
