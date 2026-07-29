using System;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Production;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ProductionOutputPlacementTests
{
    private static readonly MaterialId Air = new MaterialId("terrain.air");
    private static readonly MaterialId Rock = new MaterialId("terrain.rock");

    [Fact]
    public void Output_uses_next_free_cell_in_right_side_zone()
    {
        WorldState world = CreateWorld();
        BuildingSnapshot building = CreateBuilding(BuildingOrientation.North);
        CellId first = ProductionOutputPlacement.CreateCandidates(building, 2)[0];
        InventoryState inventory = new InventoryState(
            CampfireProductionContentTests.CreateItems());
        Assert.True(inventory.AddStack(
            EntityId.Parse("b1000000000000000000000000000001"),
            CampfireProductionContent.MushroomCapItemId,
            1,
            ItemLocation.InWorld(first),
            0).IsSuccess);

        Result<CellId> result = ProductionOutputPlacement.Resolve(
            building,
            world.CreateSnapshot(),
            building.Footprint,
            inventory.CreateSnapshot().Stacks,
            2);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(new CellId(first.X + 1, first.Y, first.Z), result.Value);
    }

    [Fact]
    public void Unsupported_right_candidate_is_skipped()
    {
        WorldState world = CreateWorld();
        BuildingSnapshot building = CreateBuilding(BuildingOrientation.North);
        CellId first = ProductionOutputPlacement.CreateCandidates(building, 1)[0];
        SetTerrain(world, new CellId(first.X, first.Y + 1, first.Z), Air, tick: 3);

        Result<CellId> result = ProductionOutputPlacement.Resolve(
            building,
            world.CreateSnapshot(),
            building.Footprint,
            Array.Empty<ItemStackSnapshot>(),
            maximumLateralDistance: 1);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(new CellId(first.X + 1, first.Y, first.Z), result.Value);
    }

    [Fact]
    public void Occupied_right_zone_does_not_fall_back_to_other_sides()
    {
        WorldState world = CreateWorld();
        BuildingSnapshot building = CreateBuilding(BuildingOrientation.North);
        InventoryState inventory = new InventoryState(
            CampfireProductionContentTests.CreateItems());
        CellId[] zone = ProductionOutputPlacement.CreateCandidates(building, 1)
            .ToArray();
        for (int index = 0; index < zone.Length; index++)
        {
            Assert.True(inventory.AddStack(
                EntityId.Parse((index + 20).ToString("x32")),
                CampfireProductionContent.MushroomCapItemId,
                1,
                ItemLocation.InWorld(zone[index]),
                0).IsSuccess);
        }

        Result<CellId> result = ProductionOutputPlacement.Resolve(
            building,
            world.CreateSnapshot(),
            building.Footprint,
            inventory.CreateSnapshot().Stacks,
            maximumLateralDistance: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductionErrors.OutputSpaceUnavailable, result.Error);
    }

    [Fact]
    public void Every_orientation_uses_same_screen_right_zone()
    {
        Assert.Equal(new CellId(5, 4, 0), First(BuildingOrientation.North));
        Assert.Equal(new CellId(5, 4, 0), First(BuildingOrientation.East));
        Assert.Equal(new CellId(5, 4, 0), First(BuildingOrientation.South));
        Assert.Equal(new CellId(5, 4, 0), First(BuildingOrientation.West));
    }

    private static CellId First(BuildingOrientation orientation)
    {
        return ProductionOutputPlacement.CreateCandidates(
            CreateBuilding(orientation),
            0).First();
    }

    private static BuildingSnapshot CreateBuilding(BuildingOrientation orientation)
    {
        BuildingDefinition definition = CampfireBuildingBoxContent.Definition.Building;
        CellId origin = new CellId(4, 4, 0);
        return new BuildingSnapshot(
            EntityId.Parse("b2000000000000000000000000000001"),
            definition,
            origin,
            orientation,
            definition.ResolveFootprint(origin, orientation),
            definition.ResolveWorkPositions(origin, orientation).First(),
            BuildingStatus.Completed,
            definition.RequiredWork,
            definition.MaximumDurability,
            1,
            null);
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(10, 10),
            5,
            materials,
            Air,
            explored: true).Value;
        TerrainChange[] floor = Enumerable.Range(0, 10)
            .Select(x => new TerrainChange(
                new CellId(x, 5, 0),
                new CellState(
                    Rock,
                    CellDesignation.None,
                    isExplored: true,
                    damage: 0,
                    temperature: 20)))
            .ToArray();
        Assert.True(world.ApplyTerrainChanges(floor, tick: 2).IsSuccess);
        world.DequeueUncommittedEvents();
        return world;
    }

    private static void SetTerrain(
        WorldState world,
        CellId cell,
        MaterialId material,
        long tick)
    {
        Result<CellSnapshot> current = world.GetCell(cell);
        Assert.True(current.IsSuccess, current.Error?.ToString());
        Result<WorldMutationResult> changed = world.ApplyTerrainChanges(
            new[]
            {
                new TerrainChange(
                    cell,
                    current.Value.State.WithTerrain(material)),
            },
            tick);
        Assert.True(changed.IsSuccess, changed.Error?.ToString());
    }
}

}
