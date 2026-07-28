using System;
using System.Linq;
using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
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
    public void Generic_slot_lmb_starts_real_item_ghost_without_dropping_stack_immediately()
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
        ResidentInventoryLayoutViewModel before =
            terrain.LoadResidentInventoryLayout(resident.Id);
        ResidentInventoryLayoutSlotViewModel empty = before.Slots.First(value =>
            value.Compartment == ResidentInventoryCompartment.Main && value.IsEmpty);

        EntityId stackId = EntityId.Parse("99000000000000000000000000000001");
        InventoryState inventory = inventoryRepository.Get();
        Result added = inventory.AddUnit(
            stackId,
            new ItemId("material.mushroom_cap"),
            ItemLocation.InResidentSlot(
                EntityId.Parse(resident.Id),
                empty.Compartment,
                empty.SlotIndex),
            tick: 1);
        Assert.That(added.IsSuccess, Is.True, added.Error?.ToString());
        inventoryRepository.Save(inventory);

        ResidentInventoryLayoutSlotViewModel slot = terrain
            .LoadResidentInventoryLayout(resident.Id)
            .Slots.Single(value => string.Equals(
                value.StackId,
                stackId.ToString(),
                StringComparison.Ordinal));
        EventSystem eventSystem = EventSystem.current
            ?? new GameObject("Inventory test event system").AddComponent<EventSystem>();
        Invoke(
            hud,
            "HandleInventorySlotClick",
            slot,
            new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
            });

        Assert.That(GetProperty<bool>(interaction, "InventoryItemPlacementActive"), Is.True);
        Assert.That(
            GetField<string>(interaction, "_inventoryItemPlacementStackId"),
            Is.EqualTo(stackId.ToString()));
        Assert.That(_root.GetComponent<DigInventoryItemGhostRenderer>(), Is.Not.Null);

        ItemStackSnapshot? authoritative = inventoryRepository.Get().GetStack(stackId);
        Assert.That(authoritative, Is.Not.Null);
        Assert.That(authoritative!.Location.Kind, Is.EqualTo(ItemLocationKind.AgentInventory));
        Assert.That(authoritative.ReservedQuantity, Is.Zero);
    }

    private static void Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        method!.Invoke(target, arguments);
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
}

}