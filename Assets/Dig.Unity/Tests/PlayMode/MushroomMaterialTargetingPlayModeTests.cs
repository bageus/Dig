using System;
using System.Linq;
using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Ecology;
using Dig.Domain.World;
using Dig.Presentation.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class MushroomMaterialTargetingPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void Foreground_drop_is_pickup_only_and_blocks_regrown_mushroom_axe_target()
    {
        _root = new GameObject("Mushroom foreground target test");
        DigMushroomRenderer mushroomRenderer = _root.AddComponent<DigMushroomRenderer>();
        DigWorldItemRenderer itemRenderer = _root.AddComponent<DigWorldItemRenderer>();
        DigWorldInteraction interaction = _root.AddComponent<DigWorldInteraction>();
        SetField(interaction, "_mushroomRenderer", mushroomRenderer);
        SetField(interaction, "_itemRenderer", itemRenderer);

        EntityId siteId = EntityId.Parse("80000000000000000000000000000002");
        MushroomSiteSnapshot mushroom = Snapshot(siteId);
        Invoke(mushroomRenderer, "Render", (object)new[] { mushroom });
        WorldItemViewModel cap = new WorldItemViewModel(
            "8f000000000000000000000000000011",
            "material.mushroom_cap",
            quantity: 1,
            reservedQuantity: 0,
            cellX: mushroom.Cell.X,
            cellY: mushroom.Cell.Y,
            cellZ: mushroom.Cell.Z,
            interactionProfile: ItemInteractionProfiles.Generic);
        itemRenderer.Render(new[] { cap });
        Physics.SyncTransforms();

        DigMushroomVisual mushroomVisual =
            _root.GetComponentInChildren<DigMushroomVisual>();
        DigWorldItemVisual itemVisual =
            _root.GetComponentInChildren<DigWorldItemVisual>();
        Assert.That(itemVisual.Model.CanPickup, Is.True);
        Assert.That(itemVisual.GetComponentInParent<DigMushroomVisual>(), Is.Null);

        Collider itemCollider = itemVisual.GetComponent<Collider>();
        Collider mushroomCollider = mushroomVisual.GetComponent<Collider>();
        Vector3 itemCenter = itemCollider.bounds.center;
        Vector3 mushroomCenter = mushroomCollider.bounds.center;
        Vector3 direction = (mushroomCenter - itemCenter).normalized;
        RaycastHit[] hits = Physics.RaycastAll(
            itemCenter - (direction * 1f),
            direction,
            10f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        Assert.That(hits.Length, Is.GreaterThanOrEqualTo(2));

        object?[] mushroomArguments = { hits, null };
        bool resolvesMushroom = InvokeResolver(
            interaction,
            "TryResolveMushroomHit",
            mushroomArguments);
        Assert.That(resolvesMushroom, Is.False);
        Assert.That(mushroomArguments[1], Is.Null);

        object?[] itemArguments = { hits, false, null };
        bool resolvesItem = InvokeResolver(
            interaction,
            "TryResolveWorldItemPointerTarget",
            itemArguments);
        Assert.That(resolvesItem, Is.True);
        object resolved = itemArguments[2]!;
        PropertyInfo? itemProperty = resolved.GetType().GetProperty(
            "Item",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.That(itemProperty, Is.Not.Null);
        Assert.That(itemProperty!.GetValue(resolved), Is.SameAs(itemVisual));
    }

    private static MushroomSiteSnapshot Snapshot(EntityId siteId)
    {
        return new MushroomSiteSnapshot(
            siteId,
            new MushroomDefinitionId("ecology.mushroom.common"),
            new CellId(3, 3, 0),
            MushroomStage.Tiny,
            stageStartedTick: 0,
            nextStageTick: 1,
            growthGeneration: 0,
            activeChopJobId: null,
            activeWorkerId: null,
            requiredSwings: 0,
            completedSwings: 0,
            growthPausedAtTick: null,
            version: 0);
    }

    private static bool InvokeResolver(
        DigWorldInteraction interaction,
        string name,
        object?[] arguments)
    {
        MethodInfo? method = typeof(DigWorldInteraction).GetMethods(
                BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(value => value.Name == name
                && value.GetParameters().Length == arguments.Length);
        Assert.That(method, Is.Not.Null, name);
        return (bool)method!.Invoke(interaction, arguments)!;
    }

    private static object Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(value => value.Name == name
                && value.GetParameters().Length == arguments.Length);
        Assert.That(method, Is.Not.Null, name);
        return method!.Invoke(target, arguments)!;
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo? field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        field!.SetValue(target, value);
    }
}
}
