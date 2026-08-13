using System;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingPlacementTests
{
    private static readonly EntityId FirstBuildingId =
        EntityId.Parse("71000000000000000000000000000001");
    private static readonly EntityId SecondBuildingId =
        EntityId.Parse("71000000000000000000000000000002");

    [Fact]
    public void Placement_requires_empty_explored_unoccupied_footprint()
    {
        WorldState world = CreateEmptyWorld();
        BuildingDefinition definition = CreateDefinition();
        BuildingPlacementValidator validator = new BuildingPlacementValidator();
        BuildingsState buildings = new BuildingsState();
        CellId origin = new CellId(3, 3);
        CellId workPosition = new CellId(2, 3);
        BuildingPlacementResult valid = validator.Validate(
            definition,
            origin,
            BuildingOrientation.North,
            world.CreateSnapshot(),
            buildings.GetOccupiedCells(),
            new[] { workPosition });

        Assert.True(valid.Succeeded);
        Assert.True(buildings.Place(
            FirstBuildingId,
            definition,
            origin,
            BuildingOrientation.North,
            valid,
            tick: 1).IsSuccess);

        BuildingPlacementResult overlap = validator.Validate(
            definition,
            origin,
            BuildingOrientation.North,
            world.CreateSnapshot(),
            buildings.GetOccupiedCells(),
            new[] { workPosition });

        Assert.False(overlap.Succeeded);
        Assert.Equal(BuildingErrors.PlacementOccupied, overlap.Error);
    }

    [Fact]
    public void Permanent_mushroom_site_blocks_building_but_not_world_items()
    {
        WorldState world = CreateEmptyWorld();
        CellId origin = new CellId(3, 3);
        BuildingPlacementResult placement = new BuildingPlacementValidator().Validate(
            CreateDefinition(),
            origin,
            BuildingOrientation.North,
            world.CreateSnapshot(),
            Array.Empty<CellId>(),
            new[] { new CellId(2, 3) },
            ecologyBlockedCells: new[] { origin });

        Assert.False(placement.Succeeded);
        Assert.Equal(BuildingErrors.PlacementEcologyBlocked, placement.Error);

        ItemId cap = new ItemId("material.mushroom_cap");
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(cap, "Mushroom cap", 100, isTool: false),
        }));
        Assert.True(inventory.AddUnit(
            EntityId.Parse("71000000000000000000000000000003"),
            cap,
            ItemLocation.InWorld(origin),
            tick: 0).IsSuccess);
    }

    [Fact]
    public void Placement_never_substitutes_an_unreachable_side_work_position()
    {
        WorldState world = CreateEmptyWorld();
        CellId origin = new CellId(3, 3);
        CellId reachableConfigured = new CellId(3, 2);
        BuildingDefinition definition = new BuildingDefinition(
            new BuildingDefinitionId("campfire.reachable_work"),
            "Reachable work",
            new[] { new CellOffset(0, 0) },
            new[]
            {
                new CellOffset(0, -1),
                new CellOffset(-1, 0),
                new CellOffset(1, 0),
            },
            new[]
            {
                new BuildingMaterialRequirement(new ItemId("stone"), 1),
            },
            requiredWork: 3,
            maximumDurability: 100);

        BuildingPlacementResult placement = new BuildingPlacementValidator().Validate(
            definition,
            origin,
            BuildingOrientation.North,
            world.CreateSnapshot(),
            Array.Empty<CellId>(),
            new[] { reachableConfigured });

        Assert.True(placement.Succeeded, placement.Error?.ToString());
        Assert.Equal(reachableConfigured, placement.WorkPosition);
    }

    [Fact]
    public void Placement_reports_unreachable_work_position()
    {
        WorldState world = CreateEmptyWorld();
        BuildingPlacementResult placement = new BuildingPlacementValidator().Validate(
            CreateDefinition(),
            new CellId(3, 3),
            BuildingOrientation.North,
            world.CreateSnapshot(),
            Array.Empty<CellId>(),
            Array.Empty<CellId>());

        Assert.False(placement.Succeeded);
        Assert.Equal(BuildingErrors.NoReachableWorkPosition, placement.Error);
    }

    [Fact]
    public void Placement_reports_solid_and_out_of_bounds_footprints()
    {
        MaterialId rock = new MaterialId("rock");
        MaterialId air = new MaterialId("air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 100),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState solidWorld = Require(WorldState.CreateFilled(
            new WorldSize(4, 4),
            chunkSize: 2,
            materials,
            rock,
            explored: true));
        BuildingPlacementValidator validator = new BuildingPlacementValidator();
        BuildingDefinition definition = CreateDefinition();

        BuildingPlacementResult solid = validator.Validate(
            definition,
            new CellId(1, 1),
            BuildingOrientation.North,
            solidWorld.CreateSnapshot(),
            Array.Empty<CellId>(),
            new[] { new CellId(1, 0) });
        BuildingPlacementResult outside = validator.Validate(
            definition,
            new CellId(3, 3),
            BuildingOrientation.East,
            solidWorld.CreateSnapshot(),
            Array.Empty<CellId>(),
            new[] { new CellId(3, 2) });

        Assert.Equal(BuildingErrors.PlacementSolid, solid.Error);
        Assert.Equal(BuildingErrors.PlacementOutOfBounds, outside.Error);
    }

    [Fact]
    public void Wooden_ladder_fits_vertical_tunnel_between_two_and_eight_cells()
    {
        BuildingDefinition ladder = CreateLadderDefinition();
        WorldState shaft = CreateLadderShaftWorld();
        BuildingPlacementResult placement = new BuildingPlacementValidator().Validate(
            ladder,
            new CellId(3, 4, 1),
            BuildingOrientation.North,
            shaft.CreateSnapshot(),
            Array.Empty<CellId>(),
            new[] { new CellId(2, 7, 1) });

        Assert.True(placement.Succeeded, placement.Error?.ToString());
        Assert.InRange(placement.Footprint.Count, 2, 8);
        Assert.All(placement.Footprint, cell =>
        {
            Assert.Equal(3, cell.X);
            Assert.Equal(1, cell.Z);
        });
    }

    [Fact]
    public void Wooden_ladder_requires_the_front_face_of_z1()
    {
        BuildingPlacementResult placement = new BuildingPlacementValidator().Validate(
            CreateLadderDefinition(),
            new CellId(3, 4, 2),
            BuildingOrientation.North,
            CreateEmptyWorld().CreateSnapshot(),
            Array.Empty<CellId>(),
            new[] { new CellId(2, 4, 2) });

        Assert.Equal(BuildingErrors.LadderRequiresVerticalTunnel, placement.Error);
    }

    [Fact]
    public void Completed_ladder_grows_with_its_tunnel_but_never_above_eight_cells()
    {
        BuildingDefinition definition = CreateLadderDefinition();
        CellId origin = new CellId(3, 4, 1);
        CellId[] initial = Enumerable.Range(2, 4)
            .Select(y => new CellId(3, y, 1))
            .ToArray();
        BuildingSnapshot snapshot = new BuildingSnapshot(
            FirstBuildingId,
            definition,
            origin,
            BuildingOrientation.North,
            initial,
            new CellId(2, 4, 1),
            BuildingStatus.Completed,
            definition.RequiredWork,
            definition.MaximumDurability,
            version: 1,
            diagnosticReason: null,
            boxPlan: new BuildingBoxPlanSnapshot(
                SecondBuildingId,
                EntityId.Parse("71000000000000000000000000000004"),
                BuildingBoxCommitState.Consumed));
        BuildingsState buildings = BuildingsState.Restore(new[] { snapshot }).Value;
        WorldSnapshot shaft = CreateLadderShaftWorld().CreateSnapshot();

        int changed = buildings.ReconcileAdaptiveLadders(shaft);

        Assert.Equal(1, changed);
        BuildingSnapshot grown = buildings.Get(FirstBuildingId)!;
        Assert.Equal(8, grown.Footprint.Count);
        Assert.Contains(origin, grown.Footprint);
        Assert.Equal(0, buildings.ReconcileAdaptiveLadders(shaft));
    }

    internal static BuildingDefinition CreateDefinition()
    {
        return new BuildingDefinition(
            new BuildingDefinitionId("workshop.basic"),
            "Basic workshop",
            new[] { new CellOffset(0, 0), new CellOffset(1, 0) },
            new[] { new CellOffset(-1, 0), new CellOffset(2, 0) },
            new[]
            {
                new BuildingMaterialRequirement(new ItemId("resource.rock"), 4),
            },
            requiredWork: 10,
            maximumDurability: 100);
    }

    private static BuildingDefinition CreateLadderDefinition()
    {
        return new BuildingDefinition(
            new BuildingDefinitionId("building.ladder"),
            "Wooden ladder",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(-1, 0), new CellOffset(1, 0) },
            Array.Empty<BuildingMaterialRequirement>(),
            requiredWork: 2,
            maximumDurability: 100,
            boxPolicy: new BuildingBoxPolicy(
                new ItemId("building_box.ladder"),
                packingWork: 2));
    }

    internal static WorldState CreateEmptyWorld()
    {
        MaterialId air = new MaterialId("air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        return Require(WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            air,
            explored: true));
    }

    private static WorldState CreateLadderShaftWorld()
    {
        MaterialId rock = new MaterialId("ladder.rock");
        MaterialId air = new MaterialId("ladder.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 10),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = Require(WorldState.CreateFilled(
            new WorldSize(8, 8, 4),
            chunkSize: 4,
            materials,
            rock,
            explored: true));
        CellId[] openings = Enumerable.Range(0, 8)
            .Select(y => new CellId(3, y, 1))
            .Concat(new[] { new CellId(2, 7, 1), new CellId(4, 7, 1) })
            .ToArray();
        TerrainChange[] changes = openings
            .Select(cell => new TerrainChange(
                cell,
                world.GetCell(cell).Value.State.WithExcavatedTerrain(air)))
            .ToArray();
        Assert.True(world.ApplyTerrainChanges(changes, tick: 1).IsSuccess);
        return world;
    }

    private static T Require<T>(Result<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.ToString());
        return result.Value;
    }
}
}
