using System;
using System.Linq;
using System.Reflection;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using Dig.Presentation.Production;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{

public sealed class BuildingSupplyRuntimePlayModeTests
{
    private GameObject? _root;
    private GameObject? _createdCamera;
    private GameObject? _createdCanvas;
    private GameObject? _createdEventSystem;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }

        if (_createdCanvas != null)
        {
            UnityEngine.Object.DestroyImmediate(_createdCanvas);
        }

        if (_createdCamera != null)
        {
            UnityEngine.Object.DestroyImmediate(_createdCamera);
        }

        if (_createdEventSystem != null)
        {
            UnityEngine.Object.DestroyImmediate(_createdEventSystem);
        }
    }

    [Test]
    public void Carried_mushroom_supply_is_deposited_into_internal_building_stock()
    {
        Camera? cameraBefore = Camera.main;
        UnityEngine.EventSystems.EventSystem? eventSystemBefore =
            UnityEngine.EventSystems.EventSystem.current;
        _root = new GameObject("Building supply runtime test");
        _root.AddComponent<DigUnityBootstrap>();

        DigWorldInteraction interaction = _root.GetComponent<DigWorldInteraction>();
        DigAgentRenderer agents = _root.GetComponent<DigAgentRenderer>();
        DigAgentSimulationDriver simulation = _root.GetComponent<DigAgentSimulationDriver>();
        Assert.That(interaction, Is.Not.Null);
        Assert.That(agents, Is.Not.Null);
        Assert.That(simulation, Is.Not.Null);

        Camera? cameraAfter = Camera.main;
        if (cameraBefore == null && cameraAfter != null)
        {
            _createdCamera = cameraAfter.gameObject;
        }

        DigGameHudCanvas hud = UnityEngine.Object.FindFirstObjectByType<DigGameHudCanvas>();
        Assert.That(hud, Is.Not.Null);
        _createdCanvas = hud.gameObject;
        if (eventSystemBefore == null
            && UnityEngine.EventSystems.EventSystem.current != null)
        {
            _createdEventSystem =
                UnityEngine.EventSystems.EventSystem.current.gameObject;
        }

        DigTerrainWorkSession terrain = GetField<DigTerrainWorkSession>(
            interaction,
            "_terrainSession");
        InMemoryInventoryRepository inventoryRepository =
            GetField<InMemoryInventoryRepository>(terrain, "_inventoryRepository");
        AgentViewModel resident = agents.GetHudModels().First();
        BuildingWorldViewModel campfire = terrain.LoadBuildings().Single(value =>
            string.Equals(
                value.DefinitionId,
                CampfireBuildingBoxContent.CampfireBuildingId.ToString(),
                StringComparison.Ordinal));
        EntityId buildingId = EntityId.Parse(campfire.Id);
        ItemId itemId = CampfireProductionContent.MushroomCapItemId;
        ItemLocation internalLocation = ItemLocation.InBuilding(buildingId);
        int before = inventoryRepository.Get().GetAvailableQuantityAt(
            itemId,
            internalLocation);

        InventoryState inventory = inventoryRepository.Get();
        Assert.That(
            inventory.Catalog.Get(itemId).MaximumStackSize,
            Is.GreaterThanOrEqualTo(4));
        Result added = inventory.AddStack(
            EntityId.Parse("99000000000000000000000000000002"),
            itemId,
            quantity: 4,
            ItemLocation.InWorld(new Dig.Domain.World.CellId(
                resident.CellX,
                resident.CellY,
                resident.CellZ)),
            tick: 1);
        Assert.That(added.IsSuccess, Is.True, added.Error?.ToString());
        inventoryRepository.Save(inventory);

        for (int tick = 0; tick < 160; tick++)
        {
            Invoke(simulation, "AdvanceOneTick");
        }

        int deposited = inventoryRepository.Get().GetAvailableQuantityAt(
            itemId,
            internalLocation);
        Assert.That(deposited, Is.EqualTo(before + 4));
        BuildingProductionViewModel production = terrain.LoadBuildingProduction(
            campfire.Id)!;
        Assert.That(
            production.Stocks.Single(value => value.ItemId == itemId).Current,
            Is.EqualTo(before + 4));
        Assert.That(
            inventoryRepository.Get().CreateSnapshot().Stacks.Any(value =>
                value.ItemId == itemId
                && value.Location.Kind == ItemLocationKind.AgentInventory
                && value.ReservedQuantity > 0),
            Is.False);
    }

    [Test]
    public void Queued_recipe_force_enables_required_internal_stock_delivery()
    {
        Camera? cameraBefore = Camera.main;
        UnityEngine.EventSystems.EventSystem? eventSystemBefore =
            UnityEngine.EventSystems.EventSystem.current;
        _root = new GameObject("Production input delivery runtime test");
        _root.AddComponent<DigUnityBootstrap>();

        DigWorldInteraction interaction = _root.GetComponent<DigWorldInteraction>();
        DigAgentRenderer agents = _root.GetComponent<DigAgentRenderer>();
        Assert.That(interaction, Is.Not.Null);
        Assert.That(agents, Is.Not.Null);

        Camera? cameraAfter = Camera.main;
        if (cameraBefore == null && cameraAfter != null)
        {
            _createdCamera = cameraAfter.gameObject;
        }

        DigGameHudCanvas hud = UnityEngine.Object.FindFirstObjectByType<DigGameHudCanvas>();
        Assert.That(hud, Is.Not.Null);
        _createdCanvas = hud.gameObject;
        if (eventSystemBefore == null
            && UnityEngine.EventSystems.EventSystem.current != null)
        {
            _createdEventSystem =
                UnityEngine.EventSystems.EventSystem.current.gameObject;
        }

        DigTerrainWorkSession terrain = GetField<DigTerrainWorkSession>(
            interaction,
            "_terrainSession");
        BuildingProductionViewModel campfire = terrain.LoadAllBuildingProduction()
            .Single();
        BuildingStockIconViewModel hamsterBefore = campfire.Stocks.Single(value =>
            value.ItemId == CampfireProductionContent.HamsterItemId);
        Assert.That(hamsterBefore.DeliveryEnabled, Is.False);

        Result queued = terrain.EnqueueBuildingProduction(
            campfire.BuildingId.ToString(),
            CampfireProductionContent.RoastedHamsterRecipeId.ToString(),
            tick: 1);
        Assert.That(queued.IsSuccess, Is.True, queued.Error?.ToString());
        terrain.SynchronizeBuildingProduction(
            tick: 2,
            agents.GetHudModels());

        BuildingStockIconViewModel hamsterAfter = terrain.LoadBuildingProduction(
                campfire.BuildingId.ToString())!
            .Stocks.Single(value =>
                value.ItemId == CampfireProductionContent.HamsterItemId);
        Assert.That(hamsterAfter.DeliveryEnabled, Is.True);
    }

    private static void Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo? method = FindMethod(target.GetType(), name, arguments.Length);
        Assert.That(method, Is.Not.Null, name);
        method!.Invoke(target, arguments);
    }

    private static T GetField<T>(object target, string name)
    {
        FieldInfo? field = FindField(target.GetType(), name);
        Assert.That(field, Is.Not.Null, name);
        return (T)field!.GetValue(target)!;
    }

    private static MethodInfo? FindMethod(Type type, string name, int parameterCount)
    {
        for (Type? current = type; current != null; current = current.BaseType)
        {
            MethodInfo? method = current.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SingleOrDefault(value => value.Name == name
                    && value.GetParameters().Length == parameterCount);
            if (method != null)
            {
                return method;
            }
        }

        return null;
    }

    private static FieldInfo? FindField(Type type, string name)
    {
        for (Type? current = type; current != null; current = current.BaseType)
        {
            FieldInfo? field = current.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field;
            }
        }

        return null;
    }
}

}
