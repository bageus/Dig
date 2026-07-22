using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class AuthoritativeXyzWorldTests
{
    private static readonly MaterialId Rock = new MaterialId("xyz.rock");
    private static readonly MaterialId Air = new MaterialId("xyz.air");

    [Fact]
    public void Same_xy_on_four_depth_layers_are_distinct_stable_cells_and_reservations()
    {
        CellId[] cells = Enumerable.Range(0, WorldSize.RequiredDepth)
            .Select(z => new CellId(3, 4, z))
            .ToArray();

        Assert.Equal(4, new HashSet<CellId>(cells).Count);
        Assert.Equal(4, cells.Select(ReservationKey.ForPosition).Distinct().Count());
        Assert.Equal(
            new[] { "3,4,0", "3,4,1", "3,4,2", "3,4,3" },
            cells.OrderBy(cell => cell).Select(cell => cell.ToString()).ToArray());
    }

    [Fact]
    public void World_mutation_changes_only_the_requested_depth_layer()
    {
        WorldState world = CreateWorld();
        CellId target = new CellId(2, 2, 2);

        var result = world.Excavate(target, Air, tick: 1);

        Assert.True(result.IsSuccess);
        Assert.False(world.GetCell(target).Value.IsSolid);
        Assert.True(world.GetCell(new CellId(2, 2, 0)).Value.IsSolid);
        Assert.True(world.GetCell(new CellId(2, 2, 1)).Value.IsSolid);
        Assert.True(world.GetCell(new CellId(2, 2, 3)).Value.IsSolid);
        Assert.All(result.Value.InvalidatedChunks, chunk => Assert.Equal(2, chunk.Z));
    }

    [Fact]
    public void World_snapshot_contains_every_cell_once_with_exact_depth()
    {
        WorldState world = CreateWorld(width: 5, height: 3, chunkSize: 2);

        WorldSnapshot snapshot = world.CreateSnapshot();
        CellSnapshot[] cells = snapshot.Chunks.SelectMany(chunk => chunk.Cells).ToArray();

        Assert.Equal(5 * 3 * 4, cells.Length);
        Assert.Equal(cells.Length, cells.Select(cell => cell.Id).Distinct().Count());
        Assert.Equal(new[] { 0, 1, 2, 3 }, cells.Select(cell => cell.Id.Z).Distinct().OrderBy(z => z));
        Assert.Equal(snapshot.Chunks.Count, snapshot.Chunks.Select(chunk => chunk.Id).Distinct().Count());
    }

    [Fact]
    public void Authoritative_world_rejects_depth_outside_zero_to_three()
    {
        WorldState world = CreateWorld();

        Assert.True(world.GetCell(new CellId(0, 0, -1)).IsFailure);
        Assert.True(world.GetCell(new CellId(0, 0, 4)).IsFailure);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new WorldSize(4, 4, 3));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new WorldSize(4, 4, 5));
    }

    private static WorldState CreateWorld(
        int width = 6,
        int height = 6,
        int chunkSize = 3)
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(width, height),
            chunkSize,
            materials,
            Rock,
            explored: true).Value;
    }
}

}
