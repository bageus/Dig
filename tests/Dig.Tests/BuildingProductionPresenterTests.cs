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

        BuildingStockIconViewModel hamster = model.Stocks.Single(value =>
            value.ItemId == CampfireProductionContent.HamsterItemId);
        Assert.False(hamster.DeliveryEnabled);
        Assert.All(
            model.Stocks.Where(value =>
                value.ItemId != CampfireProductionContent.HamsterItemId),
            stock => Assert.True(stock.DeliveryEnabled));
    }

    [Fact]
    public void Active_product_cell_projects_work_overlay_until_terminal_commit_or_cancel()
    {
        ItemCatalog items = CampfireProductionContentTests.CreateItems();
        BuildingCatalog buildings = new BuildingCatalog(
            CampfireProductionContent.CreateBuildings());
        ProductionContentCatalog content = ProductionContentCatalog.ValidateAndCreate(
            items,
            buildings,
            CampfireProductionContent.CreateRecipes(100),
            Array.Empty<TechnologyDefinition>(),
            new[] { CampfireProductionContent.CreateWorkstation() }).Catalog!;
        EntityId buildingId = EntityId.Parse("a4000000000000000000000000000001");
        EntityId orderId = EntityId.Parse("a5000000000000000000000000000001");
        EntityId capId = EntityId.Parse("a6000000000000000000000000000001");
        InventoryState inventory = new InventoryState(items);
        Assert.True(inventory.AddStack(
            capId,
            CampfireProductionContent.MushroomCapItemId,
            1,
            ItemLocation.InBuilding(buildingId),
            0).IsSuccess);
        BuildingSupplyState supply = new BuildingSupplyState();
        Assert.True(supply.Register(
            buildingId,
            content.GetWorkstation(CampfireBuildingBoxContent.CampfireBuildingId),
            0).IsSuccess);
        ProductionState production = new ProductionState();
        RecipeDefinition recipe = content.GetRecipe(
            CampfireProductionContent.GrilledMushroomRecipeId);
        Assert.True(production.Enqueue(orderId, recipe, buildingId, 0).IsSuccess);
        Assert.True(production.ReserveInputs(
            orderId,
            new[]
            {
                new ItemReservationAllocation(
                    capId,
                    CampfireProductionContent.MushroomCapItemId,
                    1),
            },
            1).IsSuccess);
        Assert.True(production.Start(orderId, 2, new[] { 100L }).IsSuccess);
        Assert.True(production.AddMaterialWork(orderId, 40, 3).IsSuccess);

        ProductionIconViewModel active = PresentGrilled(
            content,
            items,
            buildingId,
            production,
            supply,
            inventory);
        Assert.True(active.HasProductionOverlay);
        Assert.Equal(0.4d, active.ProductionProgress, precision: 6);

        Assert.True(production.AddMaterialWork(orderId, 60, 4).IsSuccess);
        ProductionIconViewModel finalizing = PresentGrilled(
            content,
            items,
            buildingId,
            production,
            supply,
            inventory);
        Assert.True(finalizing.HasProductionOverlay);
        Assert.Equal(1d, finalizing.ProductionProgress);

        Assert.True(production.Cancel(orderId, "cancelled", 5).IsSuccess);
        ProductionIconViewModel cancelled = PresentGrilled(
            content,
            items,
            buildingId,
            production,
            supply,
            inventory);
        Assert.False(cancelled.HasProductionOverlay);
        Assert.Equal(0d, cancelled.ProductionProgress);
        Assert.Equal(0, cancelled.QueuedCount);

        EntityId completedOrderId = EntityId.Parse(
            "a5000000000000000000000000000002");
        Assert.True(production.Enqueue(completedOrderId, recipe, buildingId, 6).IsSuccess);
        Assert.True(production.ReserveInputs(
            completedOrderId,
            new[]
            {
                new ItemReservationAllocation(
                    capId,
                    CampfireProductionContent.MushroomCapItemId,
                    1),
            },
            7).IsSuccess);
        Assert.True(production.Start(completedOrderId, 8, new[] { 100L }).IsSuccess);
        Assert.True(production.AddMaterialWork(completedOrderId, 100, 9).IsSuccess);
        ProductionIconViewModel awaitingDeposit = PresentGrilled(
            content,
            items,
            buildingId,
            production,
            supply,
            inventory);
        Assert.True(awaitingDeposit.HasProductionOverlay);
        Assert.Equal(1d, awaitingDeposit.ProductionProgress);
        Assert.Equal(1, awaitingDeposit.QueuedCount);

        Assert.True(production.Complete(completedOrderId, 10).IsSuccess);
        ProductionIconViewModel committed = PresentGrilled(
            content,
            items,
            buildingId,
            production,
            supply,
            inventory);
        Assert.False(committed.HasProductionOverlay);
        Assert.Equal(0d, committed.ProductionProgress);
        Assert.Equal(0, committed.QueuedCount);
    }

    private static ProductionIconViewModel PresentGrilled(
        ProductionContentCatalog content,
        ItemCatalog items,
        EntityId buildingId,
        ProductionState production,
        BuildingSupplyState supply,
        InventoryState inventory)
    {
        return new BuildingProductionPresenter(content, items).Present(
                buildingId,
                production,
                supply.Get(buildingId, inventory.CreateSnapshot())!)
            .Products.Single(value =>
                value.RecipeId == CampfireProductionContent.GrilledMushroomRecipeId);
    }
}

}
