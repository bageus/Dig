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
    public void Marker_is_clickable_progress_is_collider_free_and_removal_is_rebuildable()
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
        Assert.That(marker.GetComponent<Collider>().enabled, Is.True);
        Assert.That(renderer.SelectById(initial.Id), Is.SameAs(marker));
        Assert.That(renderer.SelectedModel?.Id, Is.EqualTo(initial.Id));
        Collider[] enabled = _root.GetComponentsInChildren<Collider>(true)
            .Where(value => value.enabled)
            .ToArray();
        Assert.That(enabled.Length, Is.EqualTo(1));

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
