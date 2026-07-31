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
        Assert.True(world.Excavate(
            new CellId(first.X, first.Y + 1, first.Z),
            Air,
            tick: 3).IsSuccess);

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
    public void Default_output_search_skips_more_than_six_occupied_cells()
    {
        WorldState world = CreateWorld(width: 20);
        BuildingSnapshot building = CreateBuilding(BuildingOrientation.North);
        InventoryState inventory = new InventoryState(
            CampfireProductionContentTests.CreateItems());
        CellId[] occupied = ProductionOutputPlacement.CreateCandidates(building, 6)
            .Take(7)
            .ToArray();
        for (int index = 0; index < occupied.Length; index++)
        {
            Assert.True(inventory.AddStack(
                EntityId.Parse((index + 100).ToString("x32")),
                CampfireProductionContent.MushroomCapItemId,
                1,
                ItemLocation.InWorld(occupied[index]),
                tick: 0).IsSuccess);
        }

        Result<CellId> result = ProductionOutputPlacement.Resolve(
            building,
            world.CreateSnapshot(),
            building.Footprint,
            inventory.CreateSnapshot().Stacks);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(occupied[^1].X + 1, result.Value.X);
        Assert.Equal(occupied[^1].Y, result.Value.Y);
        Assert.Equal(occupied[^1].Z, result.Value.Z);
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

    private static WorldState CreateWorld(int width = 10)
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(width, 10),
            5,
            materials,
            Rock,
            explored: true).Value;
        long tick = 1;
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Result<WorldMutationResult> excavated = world.Excavate(
                    new CellId(x, y, 0),
                    Air,
                    tick++);
                Assert.True(excavated.IsSuccess, excavated.Error?.ToString());
            }
        }

        world.DequeueUncommittedEvents();
        return world;
    }
}

}
