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

        BuildingInternalStockUnitViewModel[] units = production.Stocks
            .SelectMany((stock, stockIndex) => Enumerable.Range(0, stock.Current)
                .Select(unitIndex => new BuildingInternalStockUnitViewModel(
                    (100 + stockIndex).ToString("x32"),
                    buildingId,
                    stock.ItemId,
                    unitIndex,
                    isAvailable: !(stockIndex == 0 && unitIndex == 3))))
            .ToArray();

        Invoke(
            renderer,
            "Render",
            (object)new[] { production },
            (object)new[] { building },
            (object)units);

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
        Assert.That(unitColliders.Count(value => value.enabled), Is.EqualTo(13));
        Assert.That(visuals.Select(value => (string)GetProperty(value, "ItemId"))
            .Distinct().Count(), Is.EqualTo(4));
        Assert.That(visuals.Select(value => (string)GetProperty(value, "StackId"))
            .Distinct().Count(), Is.EqualTo(4));
        Assert.That(visuals.All(value =>
            value.GetComponent<DigWorldItemVisual>() != null), Is.True);

        float buildingX = DigTunnelProjection.ResidentWorldPosition(5, 5, 0).x;
        Assert.That(visuals.All(value => value.transform.position.x < buildingX), Is.True);
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
