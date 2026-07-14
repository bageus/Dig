using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests;

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
        CellId workPosition = new CellId(3, 2);
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

    internal static BuildingDefinition CreateDefinition()
    {
        return new BuildingDefinition(
            new BuildingDefinitionId("workshop.basic"),
            "Basic workshop",
            new[] { new CellOffset(0, 0), new CellOffset(1, 0) },
            new[] { new CellOffset(0, -1), new CellOffset(1, -1) },
            new[]
            {
                new BuildingMaterialRequirement(new ItemId("resource.rock"), 4),
            },
            requiredWork: 10,
            maximumDurability: 100);
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

    private static T Require<T>(Result<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.ToString());
        return result.Value;
    }
}
