using System;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{

public sealed class LivingMaterialWorldItemLayoutPlayModeTests
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

    [TestCase("creature.hamster")]
    [TestCase("creature.grub")]
    [TestCase("creature.larva")]
    public void Hidden_living_material_proxy_does_not_shift_unfinished_package(
        string livingItemId)
    {
        _root = new GameObject("Living material item layout test");
        DigWorldItemRenderer renderer = _root.AddComponent<DigWorldItemRenderer>();
        WorldItemViewModel package = Item(
            "81000000000000000000000000000001",
            "package.unfinished",
            cellX: 5,
            cellY: 3,
            cellZ: 0,
            ItemInteractionProfiles.NonInteractive);
        WorldItemViewModel livingInPackageCell = Item(
            "81000000000000000000000000000002",
            livingItemId,
            cellX: 5,
            cellY: 3,
            cellZ: 0,
            ItemInteractionProfiles.Generic);
        WorldItemViewModel livingOutsidePackageCell = Item(
            livingInPackageCell.StackId,
            livingItemId,
            cellX: 8,
            cellY: 3,
            cellZ: 0,
            ItemInteractionProfiles.Generic);

        renderer.Render(new[] { package });
        DigWorldItemVisual packageVisual = Find(renderer, package.StackId);
        Vector3 expectedPosition = packageVisual.transform.position;
        Quaternion expectedRotation = packageVisual.transform.rotation;

        renderer.Render(new[] { livingInPackageCell, package });
        AssertStable(packageVisual, expectedPosition, expectedRotation);

        renderer.Render(new[] { package, livingOutsidePackageCell });
        AssertStable(packageVisual, expectedPosition, expectedRotation);

        renderer.Render(new[] { livingInPackageCell, package });
        AssertStable(packageVisual, expectedPosition, expectedRotation);
    }

    private static WorldItemViewModel Item(
        string stackId,
        string itemId,
        int cellX,
        int cellY,
        int cellZ,
        ItemInteractionProfile interactions)
    {
        return new WorldItemViewModel(
            stackId,
            itemId,
            quantity: 1,
            reservedQuantity: 0,
            cellX,
            cellY,
            cellZ,
            interactionProfile: interactions);
    }

    private static DigWorldItemVisual Find(
        DigWorldItemRenderer renderer,
        string stackId)
    {
        return renderer.GetComponentsInChildren<DigWorldItemVisual>(
                includeInactive: true)
            .Single(value => string.Equals(
                value.Model.StackId,
                stackId,
                StringComparison.Ordinal));
    }

    private static void AssertStable(
        DigWorldItemVisual visual,
        Vector3 expectedPosition,
        Quaternion expectedRotation)
    {
        Assert.That(
            Vector3.Distance(visual.transform.position, expectedPosition),
            Is.LessThan(0.0001f));
        Assert.That(
            Quaternion.Angle(visual.transform.rotation, expectedRotation),
            Is.LessThan(0.001f));
    }
}

}
