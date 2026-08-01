using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{

public sealed class FinishedProductionOutputInteractionPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void Pickup_output_keeps_root_collider_when_art_profile_disables_colliders()
    {
        _root = new GameObject("Finished production output interaction");
        DigWorldItemVisual visual = _root.AddComponent<DigWorldItemVisual>();
        WorldItemViewModel model = new WorldItemViewModel(
            "99000000000000000000000000000001",
            "food.grilled_mushroom",
            quantity: 1,
            reservedQuantity: 0,
            cellX: 7,
            cellY: 5,
            cellZ: 0,
            interactionProfile: ItemInteractionProfiles.Food);
        ItemStackVisualLayoutViewModel layout =
            new ItemStackVisualLayoutPresenter().Present(model);
        DigItemVisualResolution resolution = new DigItemVisualResolution(
            DigVisualAsset.CreateRuntimeFallback(
                "test.finished.output",
                Color.white),
            icon: null,
            carrySocket: DigItemCarrySocketPolicy.None,
            worldScale: new Vector3(0.3f, 0.3f, 0.3f),
            carryScale: new Vector3(0.3f, 0.3f, 0.3f),
            rotationPolicy: DigItemRotationPolicy.Fixed,
            colliderPolicy: DigItemColliderPolicy.None,
            maxVisibleInstances: 1,
            hasProfile: true);

        visual.Configure(model, layout, resolution);

        BoxCollider? collider = visual.GetComponent<BoxCollider>();
        Assert.That(model.IsInteractive, Is.True);
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider!.enabled, Is.True);
        Assert.That(collider.isTrigger, Is.True);
        Assert.That(visual.gameObject.layer, Is.EqualTo(0));
    }
}

}
