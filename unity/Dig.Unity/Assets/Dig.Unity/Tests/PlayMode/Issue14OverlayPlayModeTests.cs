using System.Reflection;
using Dig.Presentation.Overlays;
using Dig.Presentation.World;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class Issue14OverlayPlayModeTests
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
    public void Designation_overlay_reuses_marker_and_visibility_never_changes_model()
    {
        _root = new GameObject("Issue 14 Overlay Test");
        DigOverlayManager manager = _root.AddComponent<DigOverlayManager>();
        DigWorldOverlayRenderer overlays = _root.AddComponent<DigWorldOverlayRenderer>();
        Invoke(
            overlays,
            "Initialize",
            manager,
            _root.AddComponent<DigAgentRenderer>(),
            _root.AddComponent<DigBuildingRenderer>(),
            _root.AddComponent<DigWorldRenderer>());
        TerrainDepositVolumeViewModel deposits =
            TerrainDepositVolumeViewModel.Empty(2, 1, 4);
        WorldViewModel designated = World(designated: true, version: 1);

        Invoke(overlays, "RenderWorld", designated, deposits);
        Transform root = _root.transform.Find("Designation Overlay");
        Assert.That(root, Is.Not.Null);
        Assert.That(root.childCount, Is.EqualTo(1));
        Transform marker = root.GetChild(0);
        DigOverlayMetadata metadata = marker.GetComponent<DigOverlayMetadata>();
        Assert.That(metadata.Layer, Is.EqualTo(OverlayLayerKind.Designation));
        Assert.That(metadata.Shape, Is.Not.EqualTo(OverlayShapeKind.Ring));

        Invoke(overlays, "RenderWorld", World(designated: false, version: 2), deposits);
        Assert.That(marker.gameObject.activeSelf, Is.False);
        Invoke(overlays, "RenderWorld", World(designated: true, version: 3), deposits);
        Assert.That(root.childCount, Is.EqualTo(1));
        Assert.That(root.GetChild(0), Is.SameAs(marker));

        manager.SetVisibilityProfile(OverlayVisibilityProfile.Release);
        Assert.That(root.gameObject.activeSelf, Is.True);
        Assert.That(designated.Chunks[0].Cells[0].IsDesignated, Is.True);
        manager.SetVisibilityProfile(OverlayVisibilityProfile.All);
        Assert.That(designated.Chunks[0].Cells[0].IsDesignated, Is.True);
    }

    private static WorldViewModel World(bool designated, long version)
    {
        WorldCellViewModel first = new WorldCellViewModel(
            0,
            0,
            0,
            "test.rock",
            isSolid: true,
            isExplored: true,
            isDesignated: designated,
            hardness: 100,
            damage: 0,
            temperature: 20,
            worldVersion: version);
        WorldCellViewModel second = new WorldCellViewModel(
            1,
            0,
            0,
            "test.rock",
            isSolid: true,
            isExplored: true,
            isDesignated: false,
            hardness: 100,
            damage: 0,
            temperature: 20,
            worldVersion: version);
        return new WorldViewModel(
            width: 2,
            height: 1,
            depth: Dig.Domain.World.WorldSize.RequiredDepth,
            chunkSize: 2,
            version: version,
            chunks: new[]
            {
                new WorldChunkViewModel(0, 0, 0, version, new[] { first, second }),
            });
    }

    private static void Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, name);
        method!.Invoke(target, arguments);
    }
}
}