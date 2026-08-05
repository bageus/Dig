using System;
using System.Linq;
using System.Reflection;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using Dig.Presentation.Navigation;
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
            UnityEngine.Object.DestroyImmediate(_root);
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

    [Test]
    public void Non_completed_building_never_creates_service_footprint_platform()
    {
        _root = new GameObject("Obsolete building footprint platform test");
        DigOverlayManager manager = _root.AddComponent<DigOverlayManager>();
        DigWorldOverlayRenderer overlays = _root.AddComponent<DigWorldOverlayRenderer>();
        Invoke(
            overlays,
            "Initialize",
            manager,
            _root.AddComponent<DigAgentRenderer>(),
            _root.AddComponent<DigBuildingRenderer>(),
            _root.AddComponent<DigWorldRenderer>());

        EntityId buildingId = EntityId.Parse("98000000000000000000000000000041");
        BuildingDefinitionId definitionId = new BuildingDefinitionId(
            "building.test.project");
        BuildingFunctionsViewModel functions = new BuildingFunctionsViewModel(
            buildingId,
            definitionId,
            BuildingStatus.UnderConstruction,
            durability: 100,
            maximumDurability: 100,
            isPacking: false,
            packingCompletedWork: 0,
            packingRequiredWork: 0,
            actions: Array.Empty<BuildingFunctionActionViewModel>());
        BuildingWorldViewModel project = new BuildingWorldViewModel(
            buildingId.ToString(),
            definitionId.ToString(),
            "Test project",
            originX: 3,
            originY: 2,
            originZ: 0,
            orientation: BuildingOrientation.North,
            workPositionX: 4,
            workPositionY: 2,
            workPositionZ: 0,
            status: BuildingStatus.UnderConstruction,
            completedWork: 1,
            requiredWork: 4,
            version: 1,
            footprint: new[] { new BuildingFootprintCellViewModel(3, 2, 0) },
            functions: functions);

        Invoke(
            overlays,
            "RenderDynamic",
            (object)new[] { project },
            new DigStorageStatus(new CellId(0, 0, 0), 0, 0, 10),
            (object)Array.Empty<RouteViewModel>());

        Assert.That(_root.transform.Find("Building Footprint Overlay"), Is.Null);
        Assert.That(_root.GetComponentsInChildren<Transform>(includeInactive: true)
            .Any(value => value.name.StartsWith(
                "Building Footprint",
                StringComparison.Ordinal)), Is.False);
        Assert.That(_root.transform.Find("Reservation Overlay"), Is.Not.Null);
        Assert.That(_root.transform.Find("World Diagnostic Overlay"), Is.Not.Null);
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