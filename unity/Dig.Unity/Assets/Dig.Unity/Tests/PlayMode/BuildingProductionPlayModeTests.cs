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
    public void Internal_stock_is_rendered_as_four_separate_trigger_pickup_piles()
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
        Renderer[] units = _root.GetComponentsInChildren<Renderer>();
        Assert.That(units.Length, Is.EqualTo(14));
        Collider[] colliders = _root.GetComponentsInChildren<Collider>();
        Assert.That(colliders.Length, Is.EqualTo(14));
        Assert.That(colliders.All(value => value.isTrigger), Is.True);
        Component[] visuals = _root.GetComponentsInChildren<Component>()
            .Where(value => value.GetType().Name == "DigBuildingInternalStockVisual")
            .ToArray();
        Assert.That(visuals.Length, Is.EqualTo(14));
        Assert.That(visuals.All(value => (string)GetProperty(value, "BuildingId")
            == buildingId.ToString()), Is.True);
        Assert.That(visuals.Select(value => (string)GetProperty(value, "ItemId"))
            .Distinct().Count(), Is.EqualTo(4));
        Assert.That(units.Select(value => value.gameObject.name.Split(':')[1])
            .Distinct().Count(), Is.EqualTo(4));
        Assert.That(units.Select(value => Math.Round(value.transform.position.x, 2))
            .Distinct().Count(), Is.GreaterThanOrEqualTo(4));
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
            status: BuildingStatus.Completed,
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
