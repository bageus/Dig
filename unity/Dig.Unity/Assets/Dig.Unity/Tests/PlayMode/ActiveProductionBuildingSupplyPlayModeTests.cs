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
    public void Active_production_allows_remote_internal_stock_refill()
    {
        Camera? cameraBefore = Camera.main;
        UnityEngine.EventSystems.EventSystem? eventSystemBefore =
            UnityEngine.EventSystems.EventSystem.current;
        _root = new GameObject("Active production supply concurrency test");
        _root.AddComponent<DigUnityBootstrap>();

        DigWorldInteraction interaction = _root.GetComponent<DigWorldInteraction>();
        DigAgentRenderer agentRenderer = _root.GetComponent<DigAgentRenderer>();
        DigAgentSimulationDriver simulation =
            _root.GetComponent<DigAgentSimulationDriver>();
        Assert.That(interaction, Is.Not.Null);
        Assert.That(agentRenderer, Is.Not.Null);
        Assert.That(simulation, Is.Not.Null);
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
            .Where(value => value.CellX != workPosition.X
                || value.CellY != workPosition.Y
                || value.CellZ != workPosition.Z)
            .OrderByDescending(value => Distance(value, workPosition))
            .ThenBy(value => value.Id, StringComparer.Ordinal)
            .First();
        CellId sourceCell = new CellId(
            sourceResident.CellX,
            sourceResident.CellY,
            sourceResident.CellZ);
        Assert.That(Distance(sourceResident, workPosition), Is.GreaterThan(0));

        ItemId cap = CampfireProductionContent.MushroomCapItemId;
        ItemLocation internalLocation = ItemLocation.InBuilding(buildingId);
        InventoryState inventory = inventoryRepository.Get();
        Assert.That(
            inventory.GetAvailableQuantityAt(cap, internalLocation),
            Is.EqualTo(0));
        AssertSuccess(inventory.AddStack(
            EntityId.Parse("ab000000000000000000000000000001"),
            cap,
            quantity: 1,
            internalLocation,
            tick: 1));
        EntityId remoteSourceId =
            EntityId.Parse("ab000000000000000000000000000002");
        AssertSuccess(inventory.AddStack(
            remoteSourceId,
            cap,
            quantity: 1,
            ItemLocation.InWorld(sourceCell),
            tick: 1));
        inventoryRepository.Save(inventory);

        AssertSuccess(terrain.EnqueueBuildingProduction(
            campfire.Id,
            CampfireProductionContent.GrilledMushroomRecipeId.ToString(),
            tick: 1));

        bool simultaneousJobs = false;
        bool distinctWorkers = false;
        bool supplyCompleted = false;
        for (int index = 0; index < 240; index++)
        {
            Invoke(simulation, "AdvanceOneTick");
            JobSnapshot[] jobs = jobRepository.Get().GetAll().ToArray();
            JobSnapshot? production = jobs.FirstOrDefault(value =>
                !value.IsTerminal
                && value.Definition is ProductionWorkJobDefinition definition
                && definition.BuildingId == buildingId);
            JobSnapshot? supply = jobs.FirstOrDefault(value =>
                !value.IsTerminal
                && value.Definition is BuildingSupplyJobDefinition definition
                && definition.BuildingId == buildingId
                && definition.IsSourceResolved);
            if (production?.AssignedAgentId.HasValue == true
                && supply?.AssignedAgentId.HasValue == true)
            {
                simultaneousJobs = true;
                distinctWorkers |= production.AssignedAgentId.Value
                    != supply.AssignedAgentId.Value;
            }

            supplyCompleted |= jobs.Any(value =>
                value.Status == JobStatus.Completed
                && value.Definition is BuildingSupplyJobDefinition definition
                && definition.BuildingId == buildingId
                && definition.IsSourceResolved);
            ItemStackSnapshot? source = inventoryRepository.Get().GetStack(remoteSourceId);
            bool sourceLeftWorld = source == null
                || source.Location.Kind != ItemLocationKind.World;
            if (simultaneousJobs
                && distinctWorkers
                && supplyCompleted
                && sourceLeftWorld
                && inventoryRepository.Get().GetAvailableQuantityAt(
                    cap,
                    internalLocation) > 0)
            {
                break;
            }
        }

        Assert.That(simultaneousJobs, Is.True);
        Assert.That(distinctWorkers, Is.True);
        Assert.That(supplyCompleted, Is.True);
        ItemStackSnapshot? remainingSource =
            inventoryRepository.Get().GetStack(remoteSourceId);
        Assert.That(
            remainingSource == null
                || remainingSource.Location.Kind != ItemLocationKind.World,
            Is.True);
        Assert.That(
            inventoryRepository.Get().GetAvailableQuantityAt(cap, internalLocation),
            Is.GreaterThan(0));
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
