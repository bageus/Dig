using System.Collections.Generic;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelNavigationExpansionTests
{
    [Fact]
    public void Excavated_world_cells_rebuild_the_route_without_mutating_the_previous_projection()
    {
        CellId start = new CellId(1, 2, 0);
        CellId entrance = new CellId(2, 2, 0);
        CellId deepEntrance = new CellId(2, 2, 1);
        CellId destination = new CellId(3, 2, 1);
        WorldState world = CreateWorld(new[] { start, entrance });
        TunnelNavigationVolume original = TunnelNavigationVolume.FromWorldSnapshot(
            world.CreateSnapshot(),
            new CellId[0]);

        Assert.True(world.Excavate(deepEntrance, Air, tick: 2).IsSuccess);
        Assert.True(world.Excavate(destination, Air, tick: 3).IsSuccess);
        TunnelNavigationVolume rebuilt = TunnelNavigationVolume.FromWorldSnapshot(
            world.CreateSnapshot(),
            new CellId[0]);

        Assert.False(original.IsOpen(destination));
        Assert.True(rebuilt.IsOpen(destination));
        TunnelPathResult result = rebuilt.FindPath(start, destination);
        Assert.True(result.Succeeded, result.Detail);
        Assert.Equal(
            new[] { start, entrance, deepEntrance, destination },
            result.Path!.Cells);
    }

    [Fact]
    public void Solid_or_out_of_bounds_cells_cannot_be_injected_into_navigation()
    {
        CellId start = new CellId(1, 2, 0);
        WorldState world = CreateWorld(new[] { start });

        TunnelNavigationVolume volume = TunnelNavigationVolume.FromWorldSnapshot(
            world.CreateSnapshot(),
            new[] { new CellId(2, 2, 0), new CellId(99, 2, 0) });

        Assert.True(volume.IsOpen(start));
        Assert.False(volume.IsOpen(new CellId(2, 2, 0)));
        Assert.False(volume.IsOpen(new CellId(99, 2, 0)));
    }

    private static readonly MaterialId Rock = new MaterialId("expansion.rock");
    private static readonly MaterialId Air = new MaterialId("expansion.air");

    private static WorldState CreateWorld(IReadOnlyCollection<CellId> airCells)
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(5, 5),
            chunkSize: 3,
            materials,
            Rock,
            explored: true).Value;
        foreach (CellId cell in airCells)
        {
            Assert.True(world.Excavate(cell, Air, tick: 1).IsSuccess);
        }

        return world;
    }
}

}
