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

    [Fact]
    public void Output_uses_front_then_deterministic_lateral_free_cell()
    {
        WorldState world = CreateWorld();
        BuildingSnapshot building = CreateBuilding(BuildingOrientation.North);
        CellId front = Assert.IsType<CellId>(
            ProductionOutputPlacement.CreateCandidates(building, 2)[0]);
        InventoryState inventory = new InventoryState(
            CampfireProductionContentTests.CreateItems());
        Assert.True(inventory.AddStack(
            EntityId.Parse("b1000000000000000000000000000001"),
            CampfireProductionContent.MushroomCapItemId,
            1,
            ItemLocation.InWorld(front),
            0).IsSuccess);

        Result<CellId> result = ProductionOutputPlacement.Resolve(
            building,
            world.CreateSnapshot(),
            building.Footprint,
            inventory.CreateSnapshot().Stacks,
            2);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(new CellId(front.X - 1, front.Y, front.Z), result.Value);
    }

    [Fact]
    public void Every_orientation_places_output_on_its_facing_side()
    {
        Assert.Equal(new CellId(4, 3, 0), First(BuildingOrientation.North));
        Assert.Equal(new CellId(5, 4, 0), First(BuildingOrientation.East));
        Assert.Equal(new CellId(4, 5, 0), First(BuildingOrientation.South));
        Assert.Equal(new CellId(3, 4, 0), First(BuildingOrientation.West));
    }

    private static CellId First(BuildingOrientation orientation)
    {
        return ProductionOutputPlacement.CreateCandidates(
            CreateBuilding(orientation),
            0).Single();
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
        });
        return WorldState.CreateFilled(
            new WorldSize(10, 10),
            5,
            materials,
            Air,
            explored: true).Value;
    }
}

}
