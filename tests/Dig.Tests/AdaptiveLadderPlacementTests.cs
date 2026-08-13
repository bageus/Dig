using System;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class AdaptiveLadderPlacementTests
{
    [Fact]
    public void Ladder_uses_the_whole_shaft_from_its_flat_bottom_regardless_of_click_cell()
    {
        WorldState shaft = CreateShaftWorld(
            topY: 1,
            junctionY: 6,
            columnBottomY: 8);
        BuildingPlacementValidator validator = new BuildingPlacementValidator();
        BuildingDefinition ladder = CreateLadderDefinition();

        BuildingPlacementResult upperClick = Validate(validator, ladder, shaft, 2);
        BuildingPlacementResult lowerClick = Validate(validator, ladder, shaft, 5);

        Assert.True(upperClick.Succeeded, upperClick.Error?.ToString());
        Assert.True(lowerClick.Succeeded, lowerClick.Error?.ToString());
        Assert.Equal(upperClick.Footprint, lowerClick.Footprint);
        Assert.Equal(6, upperClick.Footprint.Count);
        Assert.Equal(1, upperClick.Footprint.Min(cell => cell.Y));
        Assert.Equal(6, upperClick.Footprint.Max(cell => cell.Y));
        Assert.False(shaft.GetCell(new CellId(3, 7, 1)).Value.IsSolid);
    }

    private static BuildingPlacementResult Validate(
        BuildingPlacementValidator validator,
        BuildingDefinition ladder,
        WorldState world,
        int y)
    {
        return validator.Validate(
            ladder,
            new CellId(3, y, 1),
            BuildingOrientation.North,
            world.CreateSnapshot(),
            Array.Empty<CellId>(),
            new[] { new CellId(2, 6, 1) });
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

    private static WorldState CreateShaftWorld(
        int topY,
        int junctionY,
        int columnBottomY)
    {
        MaterialId rock = new MaterialId("shaft.rock");
        MaterialId air = new MaterialId("shaft.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 10),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(8, 10, 4),
            chunkSize: 4,
            materials,
            rock,
            explored: true).Value;
        CellId[] shaft = Enumerable.Range(
                topY,
                columnBottomY - topY + 1)
            .Select(y => new CellId(3, y, 1))
            .Concat(new[]
            {
                new CellId(2, junctionY, 1),
                new CellId(4, junctionY, 1),
            })
            .ToArray();
        TerrainChange[] openings = shaft
            .Select(cell => new TerrainChange(
                cell,
                world.GetCell(cell).Value.State.WithExcavatedTerrain(air)))
            .ToArray();
        Assert.True(world.ApplyTerrainChanges(openings, tick: 1).IsSuccess);
        return world;
    }
}

}
