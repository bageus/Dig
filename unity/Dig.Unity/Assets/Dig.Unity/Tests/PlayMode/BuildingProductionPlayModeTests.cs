using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Presentation.Buildings;
using Dig.Presentation.Production;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dig.Unity.Tests
{

public sealed class BuildingProductionPlayModeTests
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
    public void Product_icon_right_click_invokes_one_decrement_and_left_click_does_not()
    {
        _root = new GameObject("Production icon pointer test");
        DigProductionIconPointer pointer = _root.AddComponent<DigProductionIconPointer>();
        int decrementCount = 0;
        pointer.RightClicked = () => decrementCount++;

        GameObject eventObject = new GameObject("Event System");
        eventObject.transform.SetParent(_root.transform);
        EventSystem eventSystem = eventObject.AddComponent<EventSystem>();

        pointer.OnPointerClick(new PointerEventData(eventSystem)
        {
            button = PointerEventData.InputButton.Left,
        });
        Assert.That(decrementCount, Is.Zero);

        pointer.OnPointerClick(new PointerEventData(eventSystem)
        {
            button = PointerEventData.InputButton.Right,
        });
        Assert.That(decrementCount, Is.EqualTo(1));

        pointer.RightClicked = null;
        pointer.OnPointerClick(new PointerEventData(eventSystem)
        {
            button = PointerEventData.InputButton.Right,
        });
        Assert.That(decrementCount, Is.EqualTo(1));
    }

    [Test]
    public void Product_progress_overlay_fills_the_whole_cell_without_intercepting_input()
    {
        _root = new GameObject(
            "Production progress cell",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        Button button = _root.GetComponent<Button>();
        MethodInfo? method = typeof(DigGameHudCanvas).GetMethod(
            "CreateProductionProgressOverlay",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);

        method!.Invoke(null, new object[] { button, 0.42d });

        Transform overlayTransform = _root.transform.Find("Production Progress Overlay");
        Assert.That(overlayTransform, Is.Not.Null);
        Image overlay = overlayTransform.GetComponent<Image>();
        RectTransform rect = (RectTransform)overlay.transform;
        Assert.That(overlay.type, Is.EqualTo(Image.Type.Filled));
        Assert.That(overlay.fillMethod, Is.EqualTo(Image.FillMethod.Vertical));
        Assert.That(overlay.fillOrigin, Is.EqualTo((int)Image.OriginVertical.Bottom));
        Assert.That(overlay.fillAmount, Is.EqualTo(0.42f).Within(0.001f));
        Assert.That(overlay.raycastTarget, Is.False);
        Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
    }

    [Test]
    public void Internal_stock_is_left_and_finished_output_zone_is_right()
    {
        _root = new GameObject("Building production renderer test");
        DigBuildingInternalStockRenderer renderer =
            _root.AddComponent<DigBuildingInternalStockRenderer>();
        EntityId buildingId = EntityId.Parse("98000000000000000000000000000001");
        BuildingDefinitionId definitionId = new BuildingDefinitionId(
            "building.campfire");
        BuildingWorldViewModel building = CreateBuilding(
            buildingId,
            definitionId);
        BuildingProductionViewModel production = new BuildingProductionViewModel(
            buildingId,
            "animation.workstation.campfire",
            Array.Empty<ProductionIconViewModel>(),
            new[]
            {
                Stock("material.mushroom_cap", 4, 4),
                Stock("material.mushroom_leg", 4, 4),
                Stock("material.stone", 4, 4),
                Stock("creature.hamster", 2, 2),
            });

        Invoke(renderer, "Render", (object)new[] { production }, (object)new[] { building });

        Assert.That((int)GetProperty(renderer, "ActiveUnitCount"), Is.EqualTo(14));
        Assert.That((int)GetProperty(renderer, "ActiveBayCount"), Is.EqualTo(2));
        Renderer[] renderers = _root.GetComponentsInChildren<Renderer>();
        Assert.That(renderers.Length, Is.EqualTo(16));
        Assert.That(_root.GetComponentsInChildren<Transform>()
            .Any(value => value.name == "Storage back rail"), Is.False);
        Component[] visuals = _root.GetComponentsInChildren<Component>()
            .Where(value => value.GetType().Name == "DigBuildingInternalStockVisual")
            .ToArray();
        Assert.That(visuals.Length, Is.EqualTo(14));
        Assert.That(visuals.All(value => (string)GetProperty(value, "BuildingId")
            == buildingId.ToString()), Is.True);
        Collider[] unitColliders = visuals
            .Select(value => value.GetComponent<Collider>())
            .ToArray();
        Assert.That(unitColliders.All(value => value != null && value.isTrigger), Is.True);
        Assert.That(visuals.Select(value => (string)GetProperty(value, "ItemId"))
            .Distinct().Count(), Is.EqualTo(4));

        float buildingX = DigTunnelProjection.ResidentWorldPosition(5, 5, 0).x;
        Renderer[] units = visuals.Select(value => value.GetComponent<Renderer>()).ToArray();
        Assert.That(units.All(value => value.transform.position.x < buildingX), Is.True);
        Transform inputZone = FindTransform(_root.transform, "Internal Storage Zone ");
        Transform outputZone = FindTransform(_root.transform, "Finished Output Zone ");
        Assert.That(inputZone.position.x, Is.LessThan(buildingX));
        Assert.That(outputZone.position.x, Is.GreaterThan(buildingX));
        Assert.That(_root.GetComponentsInChildren<Collider>()
            .Where(value => !unitColliders.Contains(value))
            .All(value => !value.enabled), Is.True);
    }

    private static Transform FindTransform(Transform root, string prefix)
    {
        Transform? found = root.GetComponentsInChildren<Transform>()
            .FirstOrDefault(value => value.name.StartsWith(
                prefix,
                StringComparison.Ordinal));
        Assert.That(found, Is.Not.Null, prefix);
        return found!;
    }

    private static BuildingStockIconViewModel Stock(
        string itemId,
        int current,
        int capacity)
    {
        return new BuildingStockIconViewModel(
            new ItemId(itemId),
            itemId,
            current,
            incoming: 0,
            capacity,
            deliveryEnabled: true);
    }

    private static BuildingWorldViewModel CreateBuilding(
        EntityId buildingId,
        BuildingDefinitionId definitionId)
    {
        BuildingFunctionsViewModel functions = new BuildingFunctionsViewModel(
            buildingId,
            definitionId,
            BuildingStatus.Completed,
            durability: 100,
            maximumDurability: 100,
            isPacking: false,
            packingCompletedWork: 0,
            packingRequiredWork: 1,
            actions: Array.Empty<BuildingFunctionActionViewModel>());
        return new BuildingWorldViewModel(
            buildingId.ToString(),
            definitionId.ToString(),
            "Campfire",
            originX: 5,
            originY: 5,
            originZ: 0,
            orientation: BuildingOrientation.North,
            workPositionX: 7,
            workPositionY: 5,
            workPositionZ: 0,
            status: BuildingStatus.Completed,
            completedWork: 1,
            requiredWork: 1,
            version: 0,
            footprint: new[] { new BuildingFootprintCellViewModel(5, 5, 0) },
            functions: functions);
    }

    private static object Invoke(
        object target,
        string name,
        params object[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(value => value.Name == name
                && value.GetParameters().Length == arguments.Length);
        Assert.That(method, Is.Not.Null, name);
        return method!.Invoke(target, arguments)!;
    }

    private static object GetProperty(object target, string name)
    {
        PropertyInfo? property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return property!.GetValue(target)!;
    }
}

}
