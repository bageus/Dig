using System;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class AtomicDigDesignationTests
{
    [Fact]
    public void Batch_designation_applies_all_cells_in_one_world_version()
    {
        WorldState world = CreateWorld();
        CellId first = new CellId(1, 1, 0);
        CellId second = new CellId(2, 1, 1);

        var result = world.SetDigDesignations(new[] { second, first }, tick: 7);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, world.Version);
        Assert.Equal(2, result.Value.ChangedCellCount);
        Assert.Equal(CellDesignation.Dig, world.GetCell(first).Value.Designation);
        Assert.Equal(CellDesignation.Dig, world.GetCell(second).Value.Designation);
    }

    [Fact]
    public void One_invalid_cell_rejects_the_entire_batch_without_mutation()
    {
        WorldState world = CreateWorld();
        CellId valid = new CellId(1, 1, 0);
        CellId invalid = new CellId(99, 1, 0);

        var result = world.SetDigDesignations(new[] { valid, invalid }, tick: 7);

        Assert.True(result.IsFailure);
        Assert.Equal(0, world.Version);
        Assert.Equal(CellDesignation.None, world.GetCell(valid).Value.Designation);
    }

    [Fact]
    public void Unmineable_cell_rejects_the_entire_batch()
    {
        MaterialId mineable = new MaterialId("terrain.mineable");
        MaterialId unmineable = new MaterialId("terrain.unmineable");
        MaterialCatalog catalog = new MaterialCatalog(new[]
        {
            new MaterialDefinition(mineable, "Mineable", true, 100, true, null),
            new MaterialDefinition(unmineable, "Unmineable", true, int.MaxValue, false, null),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(4, 4),
            2,
            catalog,
            mineable,
            explored: true).Value;
        CellId blocked = new CellId(2, 1, 0);
        CellState blockedState = world.GetCell(blocked).Value.State.WithTerrain(unmineable);
        world.ApplyTerrainChanges(
            new[] { new TerrainChange(blocked, blockedState) },
            tick: 1);
        long versionBefore = world.Version;
        CellId valid = new CellId(1, 1, 0);

        var result = world.SetDigDesignations(new[] { valid, blocked }, tick: 2);

        Assert.True(result.IsFailure);
        Assert.Equal(versionBefore, world.Version);
        Assert.Equal(CellDesignation.None, world.GetCell(valid).Value.Designation);
    }

    private static WorldState CreateWorld()
    {
        MaterialId rock = new MaterialId("terrain.rock");
        MaterialCatalog catalog = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, "Rock", true, 100, true, null),
        });
        return WorldState.CreateFilled(
            new WorldSize(4, 4),
            2,
            catalog,
            rock,
            explored: true).Value;
    }
}

}
