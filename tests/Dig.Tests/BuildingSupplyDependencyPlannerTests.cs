using System;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Production;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingSupplyDependencyPlannerTests
{
    [Fact]
    public void Missing_enabled_mushroom_stock_creates_one_extraction_request()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(1);
        BuildingSupplySnapshot supply = harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!;
        CellId reachable = new CellId(1, 1, 0);

        ItemConsumptionRequest? request =
            BuildingSupplyDependencyPlanner.PlanSingleExtractionRequest(
                supply,
                Array.Empty<ItemStackSnapshot>(),
                new[] { reachable },
                new[] { reachable },
                MushroomItems());

        Assert.True(request.HasValue);
        Assert.Equal(
            CampfireProductionContent.MushroomCapItemId,
            request.Value.ItemId);
        Assert.Equal(1, request.Value.Quantity);
    }

    [Fact]
    public void Existing_eligible_cap_falls_through_to_missing_leg_dependency()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(1);
        CellId source = new CellId(1, 1, 0);
        Assert.True(harness.Inventory.AddStack(
            CampfireProductionTestHarness.Id(800),
            CampfireProductionContent.MushroomCapItemId,
            1,
            ItemLocation.InWorld(source),
            1).IsSuccess);
        BuildingSupplySnapshot supply = harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!;

        ItemConsumptionRequest? request =
            BuildingSupplyDependencyPlanner.PlanSingleExtractionRequest(
                supply,
                harness.Inventory.CreateSnapshot().Stacks,
                new[] { source },
                new[] { source },
                MushroomItems());

        Assert.True(request.HasValue);
        Assert.Equal(
            CampfireProductionContent.MushroomLegItemId,
            request.Value.ItemId);
    }

    [Fact]
    public void Disabled_delivery_does_not_create_extraction_request()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(1);
        Assert.True(harness.Supply.SetDeliveryEnabled(
            CampfireProductionTestHarness.BuildingId,
            CampfireProductionContent.MushroomCapItemId,
            false,
            1).IsSuccess);
        Assert.True(harness.Supply.SetDeliveryEnabled(
            CampfireProductionTestHarness.BuildingId,
            CampfireProductionContent.MushroomLegItemId,
            false,
            1).IsSuccess);
        BuildingSupplySnapshot supply = harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!;
        CellId reachable = new CellId(1, 1, 0);

        ItemConsumptionRequest? request =
            BuildingSupplyDependencyPlanner.PlanSingleExtractionRequest(
                supply,
                Array.Empty<ItemStackSnapshot>(),
                new[] { reachable },
                new[] { reachable },
                MushroomItems());

        Assert.False(request.HasValue);
    }

    [Fact]
    public void Completed_dependency_without_remaining_world_output_is_stale()
    {
        ItemConsumptionRequest[] requested =
        {
            new ItemConsumptionRequest(
                CampfireProductionContent.MushroomCapItemId,
                1),
        };

        Assert.False(BuildingSupplyDependencyPlanner.HasRequestedWorldQuantity(
            requested,
            Array.Empty<ItemStackSnapshot>()));
    }

    private static ItemId[] MushroomItems()
    {
        return new[]
        {
            CampfireProductionContent.MushroomCapItemId,
            CampfireProductionContent.MushroomLegItemId,
        };
    }
}

}
