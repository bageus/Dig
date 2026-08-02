using System;
using System.Linq;
using System.Reflection;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using Dig.Presentation.Production;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{

public sealed class ActiveProductionBuildingSupplyPlayModeTests
{
    private GameObject? _root;
    private GameObject? _createdCamera;
    private GameObject? _createdCanvas;
    private GameObject? _createdEventSystem;

    [TearDown]
    public void TearDown()
    {
        Destroy(_root);
        Destroy(_createdCanvas);
        Destroy(_createdCamera);
        Destroy(_createdEventSystem);
    }

    [Test]
    public void Production_returns_then_remote_refill_runs_without_concurrency()
    {
        Camera? cameraBefore = Camera.main;
        UnityEngine.EventSystems.EventSystem? eventSystemBefore =
            UnityEngine.EventSystems.EventSystem.current;
        _root = new GameObject("Serialized production and supply test");
        _root.AddComponent<DigUnityBootstrap>();

        DigWorldInteraction interaction = _root.GetComponent<DigWorldInteraction>();
        DigAgentRenderer agentRenderer = _root.GetComponent<DigAgentRenderer>();
        DigAgentSimulationDriver simulation =
            _root.GetComponent<DigAgentSimulationDriver>();
        DigBuildingInternalStockRenderer stockRenderer =
            _root.GetComponent<DigBuildingInternalStockRenderer>();
        Assert.That(interaction, Is.Not.Null);
        Assert.That(agentRenderer, Is.Not.Null);
        Assert.That(simulation, Is.Not.Null);
        Assert.That(stockRenderer, Is.Not.Null);
        CaptureBootstrapObjects(cameraBefore, eventSystemBefore);

        DigTerrainWorkSession terrain = GetField<DigTerrainWorkSession>(
            interaction,
            "_terrainSession");
        InMemoryInventoryRepository inventoryRepository =
            GetField<InMemoryInventoryRepository>(terrain, "_inventoryRepository");
        InMemoryJobRepository jobRepository =
            GetField<InMemoryJobRepository>(terrain, "_jobRepository");
        AgentViewModel[] residents = agentRenderer.GetHudModels()
            .Where(value => value.IsAlive)
            .ToArray();
        Assert.That(residents.Length, Is.GreaterThanOrEqualTo(2));

        BuildingWorldViewModel campfire = terrain.LoadBuildings().Single(value =>
            string.Equals(
                value.DefinitionId,
                CampfireBuildingBoxContent.CampfireBuildingId.ToString(),
                StringComparison.Ordinal));
        EntityId buildingId = EntityId.Parse(campfire.Id);
        CellId workPosition = new CellId(
            campfire.WorkPositionX,
            campfire.WorkPositionY,
            campfire.WorkPositionZ);
        AgentViewModel sourceResident = residents
            .OrderByDescending(value => Distance(value, workPosition))
            .ThenBy(value => value.Id, StringComparer.Ordinal)
            .First();
        CellId sourceCell = new CellId(
            sourceResident.CellX,
            sourceResident.CellY,
            sourceResident.CellZ);

        InventoryState inventory = inventoryRepository.Get();
        BuildingProductionViewModel productionView =
            terrain.LoadBuildingProduction(campfire.Id)!;
        int idOrdinal = 1;
        foreach (BuildingStockIconViewModel stock in productionView.Stocks
            .Where(value => value.DeliveryEnabled))
        {
            int current = inventory.GetAvailableQuantityAt(
                stock.ItemId,
                ItemLocation.InBuilding(buildingId));
            int missing = stock.Capacity - current;
            if (missing > 0)
            {
                AssertSuccess(inventory.AddStack(
                    EntityId.Parse($"ac000000000000000000000000{idOrdinal++:x6}"),
                    stock.ItemId,
                    missing,
                    ItemLocation.InBuilding(buildingId),
                    tick: 1));
            }
        }

        EntityId remoteSourceId =
            EntityId.Parse("ac000000000000000000000000000099");
        AssertSuccess(inventory.AddStack(
            remoteSourceId,
            CampfireProductionContent.MushroomCapItemId,
            quantity: 1,
            ItemLocation.InWorld(sourceCell),
            tick: 1));
        inventoryRepository.Save(inventory);

        AssertSuccess(terrain.EnqueueBuildingProduction(
            campfire.Id,
            CampfireProductionContent.GrilledMushroomRecipeId.ToString(),
            tick: 1));

        int productionCompletedTick = -1;
        int supplyStartedTick = -1;
        string? productionWorkerId = null;
        bool workbenchVisible = false;
        bool packageClosed = false;
        bool supplyCompleted = false;
        for (int index = 0; index < 320; index++)
        {
            Invoke(simulation, "AdvanceOneTick");
            JobSnapshot[] jobs = jobRepository.Get().GetAll().ToArray();
            JobSnapshot? activeProduction = jobs.FirstOrDefault(value =>
                !value.IsTerminal
                && value.Definition is ProductionWorkJobDefinition definition
                && definition.BuildingId == buildingId);
            JobSnapshot? activeSupply = jobs.FirstOrDefault(value =>
                !value.IsTerminal
                && value.Definition is BuildingSupplyJobDefinition definition
                && definition.BuildingId == buildingId);
            Assert.That(
                activeProduction != null && activeSupply != null,
                Is.False,
                "One building must never have production and supply active together.");

            if (activeProduction?.AssignedAgentId is EntityId worker)
            {
                productionWorkerId ??= worker.ToString();
                workbenchVisible |= stockRenderer.ActiveWorkbenchCount > 0;
            }

            JobSnapshot? completedProduction = jobs.FirstOrDefault(value =>
                value.Status == JobStatus.Completed
                && value.Definition is ProductionWorkJobDefinition definition
                && definition.BuildingId == buildingId);
            if (completedProduction != null && productionCompletedTick < 0)
            {
                productionCompletedTick = index;
                productionWorkerId ??= completedProduction.AssignedAgentId?.ToString();
                AgentViewModel returned = agentRenderer.GetHudModels().Single(value =>
                    value.Id == productionWorkerId);
                Assert.That(
                    new CellId(returned.CellX, returned.CellY, returned.CellZ),
                    Is.EqualTo(workPosition));
            }

            packageClosed |= inventoryRepository.Get().CreateSnapshot().Stacks.Any(value =>
                value.ItemId == ProductionPackageContent.FoodPackageItemId);
            if (activeSupply != null && supplyStartedTick < 0)
            {
                supplyStartedTick = index;
            }

            supplyCompleted |= jobs.Any(value =>
                value.Status == JobStatus.Completed
                && value.Definition is BuildingSupplyJobDefinition definition
                && definition.BuildingId == buildingId);
            if (productionCompletedTick >= 0
                && supplyCompleted
                && inventoryRepository.Get().GetStack(remoteSourceId)?.Location.Kind
                    != ItemLocationKind.World)
            {
                break;
            }
        }

        Assert.That(workbenchVisible, Is.True);
        Assert.That(packageClosed, Is.True);
        Assert.That(productionCompletedTick, Is.GreaterThanOrEqualTo(0));
        Assert.That(supplyStartedTick, Is.GreaterThan(productionCompletedTick));
        Assert.That(supplyCompleted, Is.True);
        Assert.That(stockRenderer.ActiveWorkbenchCount, Is.EqualTo(0));
        Assert.That(
            inventoryRepository.Get().GetAvailableQuantityAt(
                CampfireProductionContent.MushroomCapItemId,
                ItemLocation.InBuilding(buildingId)),
            Is.EqualTo(productionView.Stocks.Single(value =>
                value.ItemId == CampfireProductionContent.MushroomCapItemId).Capacity));
    }

    private void CaptureBootstrapObjects(
        Camera? cameraBefore,
        UnityEngine.EventSystems.EventSystem? eventSystemBefore)
    {
        Camera? cameraAfter = Camera.main;
        if (cameraBefore == null && cameraAfter != null)
        {
            _createdCamera = cameraAfter.gameObject;
        }

        DigGameHudCanvas hud =
            UnityEngine.Object.FindFirstObjectByType<DigGameHudCanvas>();
        Assert.That(hud, Is.Not.Null);
        _createdCanvas = hud.gameObject;
        if (eventSystemBefore == null
            && UnityEngine.EventSystems.EventSystem.current != null)
        {
            _createdEventSystem =
                UnityEngine.EventSystems.EventSystem.current.gameObject;
        }
    }

    private static int Distance(AgentViewModel agent, CellId target)
    {
        return Math.Abs(agent.CellX - target.X)
            + Math.Abs(agent.CellY - target.Y)
            + Math.Abs(agent.CellZ - target.Z);
    }

    private static void AssertSuccess(Result result)
    {
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
    }

    private static void Invoke(object target, string name)
    {
        MethodInfo? method = target.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        method!.Invoke(target, Array.Empty<object>());
    }

    private static T GetField<T>(object target, string name)
    {
        FieldInfo? field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        return (T)field!.GetValue(target)!;
    }

    private static void Destroy(GameObject? value)
    {
        if (value != null)
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }
}

}
