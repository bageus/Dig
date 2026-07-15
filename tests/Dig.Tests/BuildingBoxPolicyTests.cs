using System;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxPolicyTests
{
    private static readonly ItemId BoxItem = new ItemId("building_box.test");
    private static readonly EntityId BuildingId = Id(1);
    private static readonly EntityId StackId = Id(2);
    private static readonly EntityId JobId = Id(3);

    [Fact]
    public void Box_and_legacy_material_policies_are_mutually_exclusive()
    {
        Assert.Throws<ArgumentException>(() => new BuildingDefinition(
            new BuildingDefinitionId("mixed"),
            "Mixed",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(0, 1) },
            new[] { new BuildingMaterialRequirement(new ItemId("wood"), 1) },
            requiredWork: 1,
            maximumDurability: 10,
            boxPolicy: new BuildingBoxPolicy(BoxItem, packingWork: 1)));
    }

    [Fact]
    public void Legacy_place_rejects_box_definition_and_box_place_rejects_legacy()
    {
        BuildingsState buildings = new BuildingsState();
        BuildingPlacementResult placement = BuildingPlacementResult.Success(
            new[] { new CellId(2, 2) },
            new CellId(2, 1));
        BuildingDefinition box = BoxDefinition();
        BuildingDefinition legacy = LegacyDefinition();

        Result legacyPath = buildings.Place(
            BuildingId,
            box,
            new CellId(2, 2),
            BuildingOrientation.North,
            placement,
            tick: 0);
        Result boxPath = buildings.PlaceBoxPlan(
            BuildingId,
            StackId,
            JobId,
            legacy,
            new CellId(2, 2),
            BuildingOrientation.North,
            placement,
            tick: 0);

        Assert.Equal(BuildingErrors.WrongConstructionPolicy, legacyPath.Error);
        Assert.Equal(BuildingErrors.WrongConstructionPolicy, boxPath.Error);
        Assert.Null(buildings.Get(BuildingId));
    }

    [Fact]
    public void Site_commit_is_single_and_exposes_ready_to_build_state()
    {
        BuildingsState buildings = new BuildingsState();
        BuildingDefinition definition = BoxDefinition();
        BuildingPlacementResult placement = BuildingPlacementResult.Success(
            new[] { new CellId(2, 2) },
            new CellId(2, 1));
        Assert.True(buildings.PlaceBoxPlan(
            BuildingId,
            StackId,
            JobId,
            definition,
            new CellId(2, 2),
            BuildingOrientation.North,
            placement,
            tick: 0).IsSuccess);

        Result first = buildings.MarkBoxAtSite(BuildingId, tick: 1);
        Result replay = buildings.MarkBoxAtSite(BuildingId, tick: 1);

        Assert.True(first.IsSuccess);
        Assert.True(replay.IsFailure);
        BuildingSnapshot snapshot = buildings.Get(BuildingId)!;
        Assert.Equal(BuildingStatus.ReadyToBuild, snapshot.Status);
        Assert.Equal(BuildingBoxCommitState.AtSite, snapshot.BoxPlan!.CommitState);
        Assert.Single(buildings.PeekUncommittedEvents()
            .OfType<BuildingBoxCommitChanged>());
    }

    private static BuildingDefinition BoxDefinition()
    {
        return new BuildingDefinition(
            new BuildingDefinitionId("box"),
            "Box",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(0, -1) },
            Array.Empty<BuildingMaterialRequirement>(),
            requiredWork: 2,
            maximumDurability: 10,
            boxPolicy: new BuildingBoxPolicy(BoxItem, packingWork: 1));
    }

    private static BuildingDefinition LegacyDefinition()
    {
        return new BuildingDefinition(
            new BuildingDefinitionId("legacy"),
            "Legacy",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(0, -1) },
            new[] { new BuildingMaterialRequirement(new ItemId("wood"), 1) },
            requiredWork: 2,
            maximumDurability: 10);
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}