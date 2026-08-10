using System;
using System.Linq;
using Dig.Application.Rooms;
using Dig.Domain.Rooms;
using Dig.Presentation.Rooms;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{

public sealed class RoomInfrastructurePresentationPlayModeTests
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
    public void Planning_visibility_hides_marker_but_keeps_physical_progress()
    {
        _root = new GameObject("Room presentation test");
        DigRoomInfrastructureRenderer renderer =
            _root.AddComponent<DigRoomInfrastructureRenderer>();
        RoomInfrastructureViewModel initial = CreateModel(
            version: 3,
            new[]
            {
                new RoomMaterialUnitProgressViewModel("material.stone", 1),
                new RoomMaterialUnitProgressViewModel("material.mushroom_leg", 1),
            });

        renderer.Render(new[] { initial });

        Assert.That(renderer.MarkerCount, Is.EqualTo(1));
        Assert.That(renderer.ProgressPieceCount, Is.EqualTo(2));
        DigRoomInfrastructureMarkerVisual marker =
            _root.GetComponentInChildren<DigRoomInfrastructureMarkerVisual>();
        Assert.That(marker, Is.Not.Null);
        Assert.That(marker.gameObject.activeSelf, Is.True);
        Assert.That(marker.GetComponent<Collider>().enabled, Is.True);
        Assert.That(renderer.SelectById(initial.Id), Is.SameAs(marker));
        Assert.That(renderer.SelectedModel?.Id, Is.EqualTo(initial.Id));
        Collider[] enabled = _root.GetComponentsInChildren<Collider>(true)
            .Where(value => value.enabled && value.gameObject.activeInHierarchy)
            .ToArray();
        Assert.That(enabled.Length, Is.EqualTo(1));

        renderer.SetPlanningOverlayVisibility(visible: false);

        Assert.That(renderer.PlanningOverlaysVisible, Is.False);
        Assert.That(marker.gameObject.activeSelf, Is.False);
        Transform[] physicalProgress = _root.GetComponentsInChildren<Transform>(true)
            .Where(value => value.name.StartsWith(
                "Room Progress ",
                StringComparison.Ordinal))
            .ToArray();
        Assert.That(physicalProgress.Length, Is.EqualTo(2));
        Assert.That(physicalProgress.All(value => value.gameObject.activeSelf), Is.True);
        Assert.That(renderer.SelectedModel?.Id, Is.EqualTo(initial.Id));

        renderer.SetPlanningOverlayVisibility(visible: true);

        Assert.That(renderer.PlanningOverlaysVisible, Is.True);
        Assert.That(marker.gameObject.activeSelf, Is.True);

        renderer.Render(new[] { CreateModel(
            version: 4,
            Array.Empty<RoomMaterialUnitProgressViewModel>()) });

        Assert.That(renderer.MarkerCount, Is.EqualTo(1));
        Assert.That(renderer.ProgressPieceCount, Is.EqualTo(0));
        Assert.That(renderer.SelectedModel?.Version, Is.EqualTo(4));
    }

    private static RoomInfrastructureViewModel CreateModel(
        long version,
        RoomMaterialUnitProgressViewModel[] completed)
    {
        return new RoomInfrastructureViewModel(
            "11111111111111111111111111111111",
            "template-room-1",
            RoomTemplateKind.Small,
            upgradeOrderCount: 1,
            RoomImprovementStatus.Improving,
            RoomPurposeKind.Workshop,
            RoomPurposeKind.None,
            cancellationAllowed: false,
            RoomInfrastructureBlockReason.None,
            markerX: 3f,
            markerY: 2,
            markerZ: 0,
            minX: 1,
            maxX: 5,
            minY: 2,
            maxY: 4,
            new[]
            {
                new RoomMaterialProgressViewModel("material.stone", 4, 4, 1),
                new RoomMaterialProgressViewModel("material.mushroom_leg", 4, 4, 1),
            },
            completed,
            version);
    }
}

}
