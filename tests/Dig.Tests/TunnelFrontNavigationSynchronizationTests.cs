using System.Collections.Generic;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelFrontNavigationSynchronizationTests
{
    [Fact]
    public void Supported_excavated_cells_join_the_horizontal_route()
    {
        WorldSnapshot world = CreateWorld(new[]
        {
            new CellId(1, 2),
            new CellId(2, 2),
            new CellId(3, 2),
        });
        CellId start = new CellId(1, 2, 0);
        TunnelNavigationVolume synchronized =
            TunnelNavigationVolume.FromWorldSnapshot(world, new CellId[0]);
        TunnelPathResult route = synchronized.FindPath(
            start,
            new CellId(3, 2, 0));

        Assert.True(route.Succeeded, route.Detail);
        Assert.Equal(3, route.Path!.Cells.Count);
    }

    [Fact]
    public void Planned_vertical_cells_join_the_climbing_route_without_support()
    {
        CellId upper = new CellId(2, 2);
        CellId lower = new CellId(2, 3);
        WorldSnapshot world = CreateWorld(new[] { upper, lower });
        TunnelNavigationVolume synchronized =
            TunnelNavigationVolume.FromWorldSnapshot(world, new[] { upper, lower });
        TunnelPathResult route = synchronized.FindPath(
            new CellId(upper.X, upper.Y, 0),
            new CellId(lower.X, lower.Y, 0));

        Assert.True(route.Succeeded, route.Detail);
        Assert.All(route.Path!.Cells, cell => Assert.True(synchronized.IsVerticalTunnel(cell)));
    }

    [Fact]
    public void Unsupported_room_air_does_not_become_a_walkable_wall()
    {
        CellId unsupported = new CellId(3, 2);
        WorldSnapshot world = CreateWorld(new[]
        {
            unsupported,
            new CellId(3, 3),
        });
        TunnelNavigationVolume synchronized =
            TunnelNavigationVolume.FromWorldSnapshot(world, new CellId[0]);

        Assert.False(synchronized.IsOpen(new CellId(unsupported.X, unsupported.Y, 0)));
    }

    private static WorldSnapshot CreateWorld(IReadOnlyCollection<CellId> airCells)
    {
        MaterialId rock = new MaterialId("sync.rock");
        MaterialId air = new MaterialId("sync.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 100),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(6, 6),
            chunkSize: 3,
            materials,
            rock,
            explored: true).Value;
        CellState empty = new CellState(
            air,
            CellDesignation.None,
            isExplored: true,
            damage: 0,
            temperature: 20);
        List<TerrainChange> changes = new List<TerrainChange>();
        foreach (CellId cell in airCells)
        {
            changes.Add(new TerrainChange(cell, empty));
        }

        world.ApplyTerrainChanges(changes, tick: 1);
        return world.CreateSnapshot();
    }
}

}
