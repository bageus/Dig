using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Navigation;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Production;
using Xunit;

namespace Dig.Tests
{

public sealed class InternalStockRouteAndProgressCorrectionTests
{
    [Fact]
    public void Flat_route_is_preferred_over_shorter_vertical_climb_route()
    {
        CellId start = new CellId(0, 0, 0);
        CellId goal = new CellId(2, 0, 0);
        CellId[] flatRoute =
        {
            start,
            new CellId(0, 0, 1),
            new CellId(0, 0, 2),
            new CellId(1, 0, 2),
            new CellId(2, 0, 2),
            new CellId(2, 0, 1),
            goal,
        };
        CellId[] climbRoute =
        {
            start,
            new CellId(0, 1, 0),
            new CellId(1, 1, 0),
            new CellId(2, 1, 0),
            goal,
        };
        CellId[] open = flatRoute.Concat(climbRoute).Distinct().ToArray();
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 3,
            height: 2,
            depth: 3,
            openCells: open,
            verticalCells: new[]
            {
                new CellId(0, 1, 0),
                new CellId(2, 1, 0),
            },
            supportedCells: open);

        TunnelPathResult result = volume.FindPath(start, goal);

        Assert.True(result.Succeeded, result.Detail);
        Assert.Contains(new CellId(1, 0, 2), result.Path!.Cells);
        Assert.DoesNotContain(new CellId(1, 1, 0), result.Path.Cells);
        Assert.DoesNotContain(
            TunnelTraversalKind.VerticalClimb,
            result.Path.TraversalKinds);
    }

    [Fact]
    public void Straight_unburdened_movement_uses_run_animation()
    {
        ResidentVisualPresenter presenter = new ResidentVisualPresenter();
        ResidentActionVisualViewModel visual = presenter.PresentAction(
            Agent("Move"),
            isMoving: true,
            isCarrying: false,
            isRunning: true);

        Assert.Equal(ResidentActionVisualState.Run, visual.State);
        Assert.True(visual.IsLooping);
    }

    [Fact]
    public void Active_recipe_projects_completed_material_segments()
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
        EntityId capStackId = EntityId.Parse("a2000000000000000000000000000001");
        Assert.True(inventory.AddStack(
            capStackId,
            CampfireProductionContent.MushroomCapItemId,
            1,
            ItemLocation.InBuilding(buildingId),
            0).IsSuccess);
        BuildingSupplyState supply = new BuildingSupplyState();
        supply.Register(buildingId, content.GetWorkstation(
            CampfireBuildingBoxContent.CampfireBuildingId), 0);
        ProductionState production = new ProductionState();
        EntityId orderId = EntityId.Parse("a3000000000000000000000000000001");
        RecipeDefinition recipe = content.GetRecipe(
            CampfireProductionContent.GrilledMushroomRecipeId);
        production.Enqueue(orderId, recipe, buildingId, 0);
        Assert.True(production.ReserveInputs(
            orderId,
            new[]
            {
                new ItemReservationAllocation(
                    capStackId,
                    CampfireProductionContent.MushroomCapItemId,
                    quantity: 1),
            },
            tick: 1).IsSuccess);
        Assert.True(production.Start(
            orderId,
            tick: 2,
            resolvedStepDurations: new long[] { 1 }).IsSuccess);
        Assert.True(production.AddMaterialWork(
            orderId,
            elapsedTicks: 1,
            tick: 3).IsSuccess);

        ProductionIconViewModel grilled = new BuildingProductionPresenter(
            content,
            items).Present(
                buildingId,
                production,
                supply.Get(buildingId, inventory.CreateSnapshot())!)
            .Products.Single(value =>
                value.RecipeId == CampfireProductionContent.GrilledMushroomRecipeId);

        Assert.True(grilled.HasProgress);
        Assert.Equal(1, grilled.ProgressCurrent);
        Assert.Equal(1, grilled.ProgressTotal);
    }

    private static AgentViewModel Agent(string intent)
    {
        return new AgentViewModel(
            id: "resident.test",
            name: "Test",
            version: 12,
            isAlive: true,
            cellX: 1,
            cellY: 1,
            nutrition: 80,
            alertness: 75,
            mood: 70,
            health: 100,
            scheduledActivity: "Work",
            activeIntent: intent,
            actionElapsedTicks: 0,
            actionRequiredTicks: 0,
            decisionReason: "test",
            decisionExplanation: "test",
            utilityOptions: Array.Empty<AgentUtilityOptionViewModel>());
    }
}

}
