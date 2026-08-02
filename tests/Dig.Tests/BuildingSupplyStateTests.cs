using System;
using System.Linq;
using System.Collections.Generic;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Production;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingSupplyStateTests
{
    private static readonly EntityId BuildingId = Id(1);

    [Fact]
    public void Planner_builds_deterministic_mixed_partial_load()
    {
        ItemCatalog items = CampfireProductionContentTests.CreateItems();
        InventoryState inventory = new InventoryState(items);
        CellId capCell = new CellId(1, 1, 0);
        CellId legCell = new CellId(2, 1, 0);
        CellId stoneCell = new CellId(3, 1, 0);
        Assert.True(inventory.AddStack(
            Id(10),
            CampfireProductionContent.MushroomCapItemId,
            4,
            ItemLocation.InWorld(capCell),
            0).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(11),
            CampfireProductionContent.MushroomLegItemId,
            4,
            ItemLocation.InWorld(legCell),
            0).IsSuccess);
        Assert.True(inventory.AddStack(
            Id(12),
            CampfireProductionContent.StoneItemId,
            4,
            ItemLocation.InWorld(stoneCell),
            0).IsSuccess);
        BuildingSupplyState state = new BuildingSupplyState();
        Assert.True(state.Register(
            BuildingId,
            CampfireProductionContent.CreateWorkstation(),
            0).IsSuccess);
        BuildingSupplySnapshot supply = state.Get(
            BuildingId,
            inventory.CreateSnapshot())!;

        BuildingSupplyPlan plan = BuildingSupplyPlanner.Plan(
            supply,
            inventory.GetAvailableWorldStacks(),
            new[] { capCell, legCell, stoneCell },
            new[] { capCell, legCell, stoneCell },
            new CellId(4, 1, 0),
            freeSlotCount: 6);

        Assert.Equal(2, plan.Allocations.Count);
        Assert.Equal(6, plan.SlotCount);
        Assert.Equal(CampfireProductionContent.MushroomCapItemId, plan.Allocations[0].ItemId);
        Assert.Equal(4, plan.Allocations[0].Quantity);
        Assert.Equal(CampfireProductionContent.MushroomLegItemId, plan.Allocations[1].ItemId);
        Assert.Equal(2, plan.Allocations[1].Quantity);
    }

    [Fact]
    public void Hidden_or_disabled_stock_suppresses_supply_without_a_production_gate()
    {
        ItemCatalog items = CampfireProductionContentTests.CreateItems();
        InventoryState inventory = new InventoryState(items);
        CellId cell = new CellId(1, 1, 0);
        Assert.True(inventory.AddStack(
            Id(20),
            CampfireProductionContent.MushroomCapItemId,
            4,
            ItemLocation.InWorld(cell),
            0).IsSuccess);
        BuildingSupplyState state = new BuildingSupplyState();
        state.Register(BuildingId, CampfireProductionContent.CreateWorkstation(), 0);
        state.SetDeliveryEnabled(
            BuildingId,
            CampfireProductionContent.MushroomCapItemId,
            enabled: false,
            tick: 1);
        BuildingSupplySnapshot supply = state.Get(BuildingId, inventory.CreateSnapshot())!;

        Assert.Empty(BuildingSupplyPlanner.Plan(
            supply,
            inventory.GetAvailableWorldStacks(),
            new[] { cell },
            new[] { cell },
            cell,
            4).Allocations);

        state.SetDeliveryEnabled(
            BuildingId,
            CampfireProductionContent.MushroomCapItemId,
            enabled: true,
            tick: 2);
        supply = state.Get(BuildingId, inventory.CreateSnapshot())!;
        Assert.Empty(BuildingSupplyPlanner.Plan(
            supply,
            inventory.GetAvailableWorldStacks(),
            Array.Empty<CellId>(),
            new[] { cell },
            cell,
            4).Allocations);
        BuildingSupplyPlan enabled = BuildingSupplyPlanner.Plan(
            supply,
            inventory.GetAvailableWorldStacks(),
            new[] { cell },
            new[] { cell },
            cell,
            4);
        Assert.Single(enabled.Allocations);
        Assert.Equal(4, enabled.TotalQuantity);
    }

    [Fact]
    public void Production_inputs_force_delivery_on_without_changing_stock_priority()
    {
        ItemCatalog items = CampfireProductionContentTests.CreateItems();
        InventoryState inventory = new InventoryState(items);
        BuildingSupplyState state = new BuildingSupplyState();
        Assert.True(state.Register(
            BuildingId,
            CampfireProductionContent.CreateWorkstation(),
            0).IsSuccess);
        Assert.True(state.SetDeliveryEnabled(
            BuildingId,
            CampfireProductionContent.MushroomCapItemId,
            enabled: false,
            tick: 1).IsSuccess);

        BuildingSupplySnapshot before = state.Get(
            BuildingId,
            inventory.CreateSnapshot())!;
        int capPriority = before.Stocks.Single(value =>
            value.ItemId == CampfireProductionContent.MushroomCapItemId).Priority;
        int hamsterPriority = before.Stocks.Single(value =>
            value.ItemId == CampfireProductionContent.HamsterItemId).Priority;

        Result enabled = state.EnableProductionInputDelivery(
            BuildingId,
            new[]
            {
                new ItemConsumptionRequest(
                    CampfireProductionContent.MushroomCapItemId,
                    1),
                new ItemConsumptionRequest(
                    CampfireProductionContent.HamsterItemId,
                    1),
            },
            tick: 2);

        Assert.True(enabled.IsSuccess, enabled.Error?.ToString());
        BuildingSupplySnapshot after = state.Get(
            BuildingId,
            inventory.CreateSnapshot())!;
        BuildingStockSnapshot cap = after.Stocks.Single(value =>
            value.ItemId == CampfireProductionContent.MushroomCapItemId);
        BuildingStockSnapshot hamster = after.Stocks.Single(value =>
            value.ItemId == CampfireProductionContent.HamsterItemId);
        Assert.True(cap.DeliveryEnabled);
        Assert.True(hamster.DeliveryEnabled);
        Assert.Equal(capPriority, cap.Priority);
        Assert.Equal(hamsterPriority, hamster.Priority);
    }


    [Fact]
    public void Operation_turn_starts_with_production_and_supply_completion_yields_back()
    {
        ItemCatalog items = CampfireProductionContentTests.CreateItems();
        InventoryState inventory = new InventoryState(items);
        BuildingSupplyState state = new BuildingSupplyState();
        Assert.True(state.Register(
            BuildingId,
            CampfireProductionContent.CreateWorkstation(),
            0).IsSuccess);
        Assert.Equal(
            BuildingOperationTurn.Production,
            state.Get(BuildingId, inventory.CreateSnapshot())!.OperationTurn);

        Assert.True(state.SetOperationTurn(
            BuildingId,
            BuildingOperationTurn.Supply,
            1).IsSuccess);
        EntityId jobId = Id(90);
        Assert.True(state.ReserveIncoming(
            BuildingId,
            jobId,
            new[]
            {
                new ItemConsumptionRequest(
                    CampfireProductionContent.MushroomCapItemId,
                    1),
            },
            new Dictionary<ItemId, int>(),
            2).IsSuccess);
        Assert.True(state.CompleteSupply(BuildingId, jobId, 3).IsSuccess);

        BuildingSupplySnapshot snapshot = state.Get(
            BuildingId,
            inventory.CreateSnapshot())!;
        Assert.Equal(BuildingOperationTurn.Production, snapshot.OperationTurn);
        Assert.False(snapshot.HasActiveSupply);
    }

    [Fact]
    public void Registered_workstation_inventory_is_protected_automatic_source()
    {
        BuildingSupplyState state = new BuildingSupplyState();
        state.Register(BuildingId, CampfireProductionContent.CreateWorkstation(), 0);

        Assert.True(state.IsProtectedAutomaticSource(ItemLocation.InBuilding(BuildingId)));
        Assert.False(state.IsProtectedAutomaticSource(ItemLocation.InWorld(new CellId(0, 0))));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
