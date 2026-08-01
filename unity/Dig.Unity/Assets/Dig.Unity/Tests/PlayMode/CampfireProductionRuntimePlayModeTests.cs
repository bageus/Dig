using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class CampfireProductionRuntimePlayModeTests
{
    [Test]
    public void Acquire_progress_and_package_close_complete_in_one_runtime_cycle()
    {
        DigWorldSession world = DigWorldSession.CreateDemo(20, 14, 5);
        DigAgentSession residents = DigAgentSession.CreateDemo(
            world.LoadView(),
            world.CreateTunnelNavigationVolume(),
            world.Journal);
        AgentViewModel[] agents = residents.LoadView().ToArray();
        DigTerrainWorkSession terrain = DigTerrainWorkSession.CreateDemo(
            world,
            agents,
            world.Journal,
            residents.SkillGrants);
        terrain.InitializeBuildingDemo(world.Journal);
        terrain.InitializeBuildingProductionDemo(
            residents.Repository,
            world.Journal,
            materialDurationTicks: 1);

        BuildingIdAndRepositories runtime = PrepareCampfireStock(terrain);
        Result queued = terrain.EnqueueBuildingProduction(
            runtime.BuildingId.ToString(),
            CampfireProductionContent.GrilledMushroomRecipeId.ToString(),
            tick: 1);
        Assert.That(queued.IsSuccess, Is.True, queued.Error?.ToString());

        bool sawAcquire = false;
        bool sawProgress = false;
        ProductionOrderSnapshot? order = null;
        for (int index = 0; index < 200; index++)
        {
            AgentViewModel[] before = residents.LoadView().ToArray();
            long nextTick = residents.Tick + 1;
            terrain.SynchronizeBuildingProduction(nextTick, before);
            IReadOnlyDictionary<string, CellId> movement =
                terrain.PlanMovement(before, nextTick);
            Assert.That(residents.Advance(movement).IsSuccess, Is.True);
            AgentViewModel[] after = residents.LoadView().ToArray();
            Result advanced = terrain.AdvanceBuildingProduction(
                residents.Tick,
                after);
            Assert.That(advanced.IsSuccess, Is.True, advanced.Error?.ToString());

            InventorySnapshot inventory = runtime.Inventory.Get().CreateSnapshot();
            sawAcquire |= inventory.Stacks.Any(value =>
                value.ItemId == CampfireProductionContent.MushroomCapItemId
                && value.Location.Kind == ItemLocationKind.AgentInventory
                && value.Reservations.Any(reservation => reservation.Quantity > 0));
            order = runtime.Production.Get().GetAll().SingleOrDefault();
            sawProgress |= order?.MaterialSteps.Any(value =>
                value.CompletedTicks > 0) == true;
            if (order?.Status == ProductionOrderStatus.Completed)
            {
                break;
            }
        }

        Assert.That(sawAcquire, Is.True);
        Assert.That(sawProgress, Is.True);
        Assert.That(order, Is.Not.Null);
        Assert.That(order!.Status, Is.EqualTo(ProductionOrderStatus.Completed));
        JobSnapshot productionJob = terrain.LoadJobSnapshots().Single(value =>
            value.Definition is ProductionWorkJobDefinition);
        Assert.That(productionJob.Status, Is.EqualTo(JobStatus.Completed));
        ProductionOutputPackageSnapshot package = runtime.Production.Get()
            .GetOutputPackageForOrder(order.Id)!;
        Assert.That(package, Is.Not.Null);
        Assert.That(package.IsClosed, Is.True);
        Assert.That(package.Manifest, Has.Count.EqualTo(1));
        Assert.That(package.Manifest[0].ItemId,
            Is.EqualTo(CampfireProductionContent.GrilledMushroomItemId));
        Assert.That(package.Manifest[0].Quantity, Is.EqualTo(2));
        Assert.That(runtime.Inventory.Get().CreateSnapshot().Stacks.Any(value =>
            value.ItemId == CampfireProductionContent.MushroomCapItemId
            && value.Location.Kind == ItemLocationKind.AgentInventory
            && value.ReservedQuantity > 0), Is.False);
    }

    private static BuildingIdAndRepositories PrepareCampfireStock(
        DigTerrainWorkSession terrain)
    {
        InMemoryInventoryRepository inventory = GetField<InMemoryInventoryRepository>(
            terrain,
            "_inventoryRepository");
        InMemoryProductionRepository production =
            GetField<InMemoryProductionRepository>(terrain, "_productionRepository");
        EntityId buildingId = terrain.LoadBuildings().Single(value =>
            string.Equals(
                value.DefinitionId,
                CampfireBuildingBoxContent.CampfireBuildingId.ToString(),
                StringComparison.Ordinal)).Functions.BuildingId;
        InventoryState state = inventory.Get();
        AddStock(state, buildingId, CampfireProductionContent.MushroomCapItemId, 4, 1);
        AddStock(state, buildingId, CampfireProductionContent.MushroomLegItemId, 4, 2);
        AddStock(state, buildingId, CampfireProductionContent.StoneItemId, 4, 3);
        inventory.Save(state);
        return new BuildingIdAndRepositories(buildingId, inventory, production);
    }

    private static void AddStock(
        InventoryState inventory,
        EntityId buildingId,
        ItemId itemId,
        int quantity,
        int suffix)
    {
        Result added = inventory.AddStack(
            EntityId.Parse((9500 + suffix).ToString("x32")),
            itemId,
            quantity,
            ItemLocation.InBuilding(buildingId),
            tick: 0);
        Assert.That(added.IsSuccess, Is.True, added.Error?.ToString());
    }

    private static T GetField<T>(object target, string name)
    {
        FieldInfo? field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field!.GetValue(target)!;
    }

    private readonly struct BuildingIdAndRepositories
    {
        internal BuildingIdAndRepositories(
            EntityId buildingId,
            InMemoryInventoryRepository inventory,
            InMemoryProductionRepository production)
        {
            BuildingId = buildingId;
            Inventory = inventory;
            Production = production;
        }

        internal EntityId BuildingId { get; }
        internal InMemoryInventoryRepository Inventory { get; }
        internal InMemoryProductionRepository Production { get; }
    }
}

}
