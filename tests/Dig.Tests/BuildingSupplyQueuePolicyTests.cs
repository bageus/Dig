using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Production;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingSupplyQueuePolicyTests
{
    private static readonly EntityId BuildingId = Id(1);

    [Theory]
    [InlineData(4, false)]
    [InlineData(3, false)]
    [InlineData(2, false)]
    [InlineData(1, true)]
    public void Queued_recipe_requests_supply_only_below_half_capacity(
        int capQuantity,
        bool expectedSupply)
    {
        ItemCatalog items = CampfireProductionContentTests.CreateItems();
        InventoryState inventory = new InventoryState(items);
        Assert.True(inventory.AddStack(
            Id(70 + capQuantity),
            CampfireProductionContent.MushroomCapItemId,
            capQuantity,
            ItemLocation.InBuilding(BuildingId),
            0).IsSuccess);
        BuildingSupplyState state = CreateSupplyState();
        Assert.True(state.SetOperationTurn(
            BuildingId,
            BuildingOperationTurn.Supply,
            1).IsSuccess);
        BuildingSupplySnapshot supply = state.Get(
            BuildingId,
            inventory.CreateSnapshot())!;
        RecipeDefinition recipe = GrilledMushroomRecipe();

        bool shouldSupply = BuildingSupplyQueuePolicy
            .ShouldAttemptSupplyBeforeProduction(
                supply,
                recipe,
                new Dictionary<ItemId, int>
                {
                    [CampfireProductionContent.MushroomCapItemId] = capQuantity,
                });

        Assert.Equal(expectedSupply, shouldSupply);
        BuildingStockSnapshot cap = supply.Stocks.Single(value =>
            value.ItemId == CampfireProductionContent.MushroomCapItemId);
        Assert.Equal(2, cap.RefillThreshold);
        Assert.Equal(capQuantity < 2, cap.IsBelowRefillThreshold);
    }

    [Fact]
    public void Completed_partial_supply_grants_one_runnable_production_turn()
    {
        ItemCatalog items = CampfireProductionContentTests.CreateItems();
        InventoryState inventory = new InventoryState(items);
        Assert.True(inventory.AddStack(
            Id(80),
            CampfireProductionContent.MushroomCapItemId,
            1,
            ItemLocation.InBuilding(BuildingId),
            0).IsSuccess);
        BuildingSupplyState state = CreateSupplyState();
        Assert.True(state.SetOperationTurn(
            BuildingId,
            BuildingOperationTurn.Supply,
            1).IsSuccess);
        EntityId supplyJobId = Id(81);
        Assert.True(state.ReserveIncoming(
            BuildingId,
            supplyJobId,
            new[]
            {
                new ItemConsumptionRequest(
                    CampfireProductionContent.MushroomCapItemId,
                    1),
            },
            new Dictionary<ItemId, int>
            {
                [CampfireProductionContent.MushroomCapItemId] = 1,
            },
            2).IsSuccess);
        Assert.True(state.CompleteSupply(
            BuildingId,
            supplyJobId,
            3).IsSuccess);
        BuildingSupplySnapshot supply = state.Get(
            BuildingId,
            inventory.CreateSnapshot())!;

        Assert.False(BuildingSupplyQueuePolicy.ShouldAttemptSupplyBeforeProduction(
            supply,
            GrilledMushroomRecipe(),
            new Dictionary<ItemId, int>
            {
                [CampfireProductionContent.MushroomCapItemId] = 1,
            }));
        Assert.Equal(BuildingOperationTurn.Production, supply.OperationTurn);
    }

    [Fact]
    public void Targeted_queue_supply_collects_available_recipe_inputs_only()
    {
        ItemCatalog items = CampfireProductionContentTests.CreateItems();
        InventoryState inventory = new InventoryState(items);
        CellId capCell = new CellId(1, 1, 0);
        CellId legCell = new CellId(2, 1, 0);
        Assert.True(inventory.AddStack(
            Id(91),
            CampfireProductionContent.MushroomCapItemId,
            4,
            ItemLocation.InWorld(capCell),
            0).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(92),
            CampfireProductionContent.MushroomLegItemId,
            2,
            ItemLocation.InWorld(legCell),
            0).IsSuccess);
        BuildingSupplySnapshot supply = CreateSupplyState().Get(
            BuildingId,
            inventory.CreateSnapshot())!;

        BuildingSupplyPlan plan = BuildingSupplyPlanner.PlanForItems(
            supply,
            inventory.GetAvailableWorldStacks(),
            new[] { capCell, legCell },
            new[] { capCell, legCell },
            destination: new CellId(3, 1, 0),
            freeSlotCount: 4,
            targetItemIds: new[]
            {
                CampfireProductionContent.MushroomLegItemId,
                CampfireProductionContent.StoneItemId,
            });

        BuildingSupplyAllocation allocation = Assert.Single(plan.Allocations);
        Assert.Equal(CampfireProductionContent.MushroomLegItemId, allocation.ItemId);
        Assert.Equal(2, allocation.Quantity);
    }

    private static BuildingSupplyState CreateSupplyState()
    {
        BuildingSupplyState state = new BuildingSupplyState();
        Assert.True(state.Register(
            BuildingId,
            CampfireProductionContent.CreateWorkstation(),
            0).IsSuccess);
        return state;
    }

    private static RecipeDefinition GrilledMushroomRecipe() =>
        CampfireProductionContent
            .CreateRecipes(CampfireProductionContent.TestProductionMaterialTicks)
            .Single(value =>
                value.Id == CampfireProductionContent.GrilledMushroomRecipeId);

    private static EntityId Id(int value) =>
        EntityId.Parse(value.ToString("x32"));
}

}
