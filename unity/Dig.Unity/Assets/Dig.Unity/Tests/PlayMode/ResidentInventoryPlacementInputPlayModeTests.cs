using System;
using System.Linq;
using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dig.Unity.Tests
{

public sealed class ResidentInventoryPlacementInputPlayModeTests
{
    private GameObject? _root;
    private GameObject? _createdCamera;
    private GameObject? _createdCanvas;

    [TearDown]
    public void TearDown()
    {
        Cursor.visible = true;
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
    }

    [Test]
    public void Generic_slot_lmb_starts_building_style_ghost_and_reserved_slot_is_blue()
    {
        Harness harness = CreateHarness();
        EntityId stackId = AddMaterialToEmptyMainSlot(harness, suffix: 1);
        ResidentInventoryLayoutSlotViewModel slot = FindSlot(harness, stackId);
        EventSystem eventSystem = EventSystem.current
            ?? new GameObject("Inventory test event system").AddComponent<EventSystem>();

        Invoke(
            harness.Hud,
            "HandleInventorySlotClick",
            slot,
            new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
            });

        Assert.That(GetProperty<bool>(
            harness.Interaction,
            "InventoryItemPlacementActive"), Is.True);
        Assert.That(
            GetField<string>(harness.Interaction, "_inventoryItemPlacementStackId"),
            Is.EqualTo(stackId.ToString()));
        Assert.That(_root!.GetComponent<DigInventoryItemGhostRenderer>(), Is.Not.Null);
        Assert.That(Cursor.visible, Is.False);

        ItemStackSnapshot? authoritative =
            harness.InventoryRepository.Get().GetStack(stackId);
        Assert.That(authoritative, Is.Not.Null);
        Assert.That(authoritative!.Location.Kind, Is.EqualTo(ItemLocationKind.AgentInventory));
        Assert.That(authoritative.ReservedQuantity, Is.Zero);

        Invoke(harness.Interaction, "CancelInventoryItemPlacement");
        Assert.That(Cursor.visible, Is.True);

        CellId target = GetField<DigWorldSession>(harness.Terrain, "_worldSession")
            .LoadSnapshot()
            .Chunks
            .SelectMany(value => value.Cells)
            .Select(value => value.Id)
            .First(value => harness.Terrain.ValidateResidentInventoryPlacement(
                harness.Resident.Id,
                stackId.ToString(),
                value).IsSuccess);
        Result created = harness.Terrain.CreateResidentInventoryPlacement(
            harness.Resident.Id,
            stackId.ToString(),
            target,
            tick: 2);
        Assert.That(created.IsSuccess, Is.True, created.Error?.ToString());

        ResidentInventoryLayoutSlotViewModel reserved = FindSlot(harness, stackId);
        Assert.That(reserved.ReservedQuantity, Is.EqualTo(1));
        Assert.That(reserved.AvailableQuantity, Is.Zero);
        Color background = InvokeResult<Color>(
            harness.Hud,
            "ResolveSlotBackground",
            reserved);
        Assert.That(background.r, Is.EqualTo(0.10f).Within(0.001f));
        Assert.That(background.g, Is.EqualTo(0.34f).Within(0.001f));
        Assert.That(background.b, Is.EqualTo(0.72f).Within(0.001f));
    }

    [Test]
    public void Live_layout_quick_drop_commits_exact_stack_at_resident_cell()
    {
        Harness harness = CreateHarness();
        EntityId stackId = AddMaterialToEmptyMainSlot(harness, suffix: 2);
        ResidentInventoryLayoutSlotViewModel slot = FindSlot(harness, stackId);
        CellId residentCell = new CellId(
            harness.Resident.CellX,
            harness.Resident.CellY,
            harness.Resident.CellZ);

        harness.Interaction.DropResidentInventoryLayoutSlot(slot);

        ItemStackSnapshot? dropped = harness.InventoryRepository.Get().GetStack(stackId);
        Assert.That(dropped, Is.Not.Null);
        Assert.That(dropped!.Location, Is.EqualTo(ItemLocation.InWorld(residentCell)));
        Assert.That(dropped.Quantity, Is.EqualTo(1));
        Assert.That(dropped.ReservedQuantity, Is.Zero);
        Assert.That(harness.Terrain.LoadResidentInventoryLayout(harness.Resident.Id).Slots
            .Any(value => string.Equals(
                value.StackId,
                stackId.ToString(),
                StringComparison.Ordinal)), Is.False);
    }

    private Harness CreateHarness()
    {
        Camera? cameraBefore = Camera.main;
        _root = new GameObject("Resident inventory placement input test");
        _root.AddComponent<DigUnityBootstrap>();

        DigWorldInteraction interaction = _root.GetComponent<DigWorldInteraction>();
        DigAgentRenderer agents = _root.GetComponent<DigAgentRenderer>();
        Assert.That(interaction, Is.Not.Null);
        Assert.That(interaction.enabled, Is.True);
        Assert.That(agents, Is.Not.Null);

        Camera? cameraAfter = Camera.main;
        if (cameraBefore == null && cameraAfter != null)
        {
            _createdCamera = cameraAfter.gameObject;
        }

        DigGameHudCanvas hud = UnityEngine.Object.FindFirstObjectByType<DigGameHudCanvas>();
        Assert.That(hud, Is.Not.Null);
        _createdCanvas = hud.gameObject;

        AgentViewModel resident = agents.GetHudModels().First();
        Assert.That(agents.SelectById(resident.Id), Is.Not.Null);

        DigTerrainWorkSession terrain = GetField<DigTerrainWorkSession>(
            interaction,
            "_terrainSession");
        InMemoryInventoryRepository inventoryRepository =
            GetField<InMemoryInventoryRepository>(terrain, "_inventoryRepository");
        return new Harness(
            interaction,
            agents,
            hud,
            resident,
            terrain,
            inventoryRepository);
    }

    private static EntityId AddMaterialToEmptyMainSlot(Harness harness, int suffix)
    {
        ResidentInventoryLayoutViewModel before =
            harness.Terrain.LoadResidentInventoryLayout(harness.Resident.Id);
        ResidentInventoryLayoutSlotViewModel empty = before.Slots.First(value =>
            value.Compartment == ResidentInventoryCompartment.Main && value.IsEmpty);
        EntityId stackId = EntityId.Parse(
            "9900000000000000000000000000" + suffix.ToString("D4"));
        InventoryState inventory = harness.InventoryRepository.Get();
        Result added = inventory.AddUnit(
            stackId,
            new ItemId("material.mushroom_cap"),
            ItemLocation.InResidentSlot(
                EntityId.Parse(harness.Resident.Id),
                empty.Compartment,
                empty.SlotIndex),
            tick: 1);
        Assert.That(added.IsSuccess, Is.True, added.Error?.ToString());
        harness.InventoryRepository.Save(inventory);
        return stackId;
    }

    private static ResidentInventoryLayoutSlotViewModel FindSlot(
        Harness harness,
        EntityId stackId)
    {
        return harness.Terrain
            .LoadResidentInventoryLayout(harness.Resident.Id)
            .Slots.Single(value => string.Equals(
                value.StackId,
                stackId.ToString(),
                StringComparison.Ordinal));
    }

    private static void Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        method!.Invoke(target, arguments);
    }

    private static T InvokeResult<T>(object target, string name, params object[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        return (T)method!.Invoke(target, arguments)!;
    }

    private static T GetField<T>(object target, string name)
    {
        FieldInfo? field = FindField(target.GetType(), name);
        Assert.That(field, Is.Not.Null, name);
        return (T)field!.GetValue(target)!;
    }

    private static T GetProperty<T>(object target, string name)
    {
        PropertyInfo? property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return (T)property!.GetValue(target)!;
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

    private sealed class Harness
    {
        internal Harness(
            DigWorldInteraction interaction,
            DigAgentRenderer agents,
            DigGameHudCanvas hud,
            AgentViewModel resident,
            DigTerrainWorkSession terrain,
            InMemoryInventoryRepository inventoryRepository)
        {
            Interaction = interaction;
            Agents = agents;
            Hud = hud;
            Resident = resident;
            Terrain = terrain;
            InventoryRepository = inventoryRepository;
        }

        internal DigWorldInteraction Interaction { get; }
        internal DigAgentRenderer Agents { get; }
        internal DigGameHudCanvas Hud { get; }
        internal AgentViewModel Resident { get; }
        internal DigTerrainWorkSession Terrain { get; }
        internal InMemoryInventoryRepository InventoryRepository { get; }
    }
}

}