using System;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Production;
using Dig.Presentation.Production;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingProductionPresenterTests
{
    [Fact]
    public void Campfire_panel_shows_six_products_four_stock_toggles_and_shortage()
    {
        ItemCatalog items = CampfireProductionContentTests.CreateItems();
        BuildingCatalog buildings = new BuildingCatalog(
            CampfireProductionContent.CreateBuildings());
        ProductionContentCatalog content = ProductionContentCatalog.ValidateAndCreate(
            items,
            buildings,
            CampfireProductionContent.CreateRecipes(1),
            Array.Empty<TechnologyDefinition>(),
            new[] { CampfireProductionContent.CreateWorkstation() }).Catalog!;
        EntityId buildingId = EntityId.Parse("a1000000000000000000000000000001");
        InventoryState inventory = new InventoryState(items);
        Assert.True(inventory.AddStack(
            EntityId.Parse("a2000000000000000000000000000001"),
            CampfireProductionContent.MushroomCapItemId,
            1,
            ItemLocation.InBuilding(buildingId),
            0).IsSuccess);
        BuildingSupplyState supply = new BuildingSupplyState();
        supply.Register(buildingId, content.GetWorkstation(
            CampfireBuildingBoxContent.CampfireBuildingId), 0);
        ProductionState production = new ProductionState();
        EntityId orderId = EntityId.Parse("a3000000000000000000000000000001");
        production.Enqueue(
            orderId,
            content.GetRecipe(CampfireProductionContent.GrilledMushroomRecipeId),
            buildingId,
            0);

        BuildingProductionViewModel model = new BuildingProductionPresenter(
            content,
            items).Present(
                buildingId,
                production,
                supply.Get(buildingId, inventory.CreateSnapshot())!);

        Assert.Equal(6, model.Products.Count);
        Assert.Equal(4, model.Stocks.Count);
        ProductionIconViewModel grilled = model.Products.Single(value =>
            value.RecipeId == CampfireProductionContent.GrilledMushroomRecipeId);
        Assert.Equal(1, grilled.QueuedCount);
        Assert.True(grilled.HasInputs);
        ProductionIconViewModel tent = model.Products.Single(value =>
            value.RecipeId == CampfireProductionContent.TentRecipeId);
        Assert.True(tent.IsOrange);
        Assert.Contains("Mushroom leg", tent.Tooltip, StringComparison.Ordinal);
        Assert.All(model.Stocks, stock => Assert.True(stock.DeliveryEnabled));
    }
}

}
