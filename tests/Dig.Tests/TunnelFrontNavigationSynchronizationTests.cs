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
            TunnelNavigationVolume.FromWorldSnapshot(
                world,
                new CellId[0],
                new CellId[0]);
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
            TunnelNavigationVolume.FromWorldSnapshot(
                world,
                new[] { upper, lower },
                new[] { upper, lower });
        TunnelPathResult route = synchronized.FindPath(
            new CellId(upper.X, upper.Y, 0),
            new CellId(lower.X, lower.Y, 0));

        Assert.True(route.Succeeded, route.Detail);
        Assert.All(route.Path!.Cells, cell => Assert.True(synchronized.IsVerticalTunnel(cell)));
    }

    [Fact]
    public void First_excavated_vertical_cell_connects_to_supported_horizontal_entry()
    {
        CellId entry = new CellId(2, 1, 0);
        CellId shaft = new CellId(2, 2, 0);
        WorldSnapshot world = CreateWorld(new[] { entry, shaft });
        TunnelNavigationVolume synchronized =
            TunnelNavigationVolume.FromWorldSnapshot(
                world,
                new[] { shaft },
                new[] { shaft });

        Assert.True(synchronized.IsOpen(entry));
        Assert.True(synchronized.IsVerticalTunnel(shaft));
        Assert.True(synchronized.CanTraverseStep(entry, shaft));
        Assert.True(synchronized.FindPath(entry, shaft).Succeeded);
    }

    [Fact]
    public void Completed_planned_tunnel_cells_remain_traversable_without_floor_support()
    {
        CellId first = new CellId(1, 2, 0);
        CellId second = new CellId(2, 2, 0);
        CellId third = new CellId(3, 2, 0);
        WorldSnapshot world = CreateWorld(new[]
        {
            first,
            second,
            third,
            new CellId(1, 3, 0),
            new CellId(2, 3, 0),
            new CellId(3, 3, 0),
        });
        TunnelNavigationVolume synchronized =
            TunnelNavigationVolume.FromWorldSnapshot(
                world,
                new[] { first, second, third },
                new CellId[0]);

        Assert.True(synchronized.FindPath(first, third).Succeeded);
        Assert.False(synchronized.IsVerticalTunnel(second));
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
            TunnelNavigationVolume.FromWorldSnapshot(
                world,
                new CellId[0],
                new CellId[0]);

        Assert.False(synchronized.IsOpen(new CellId(unsupported.X, unsupported.Y, 0)));
    }

    [Fact]
    public void Black_fog_is_closed_but_previously_explored_gray_tunnel_is_open()
    {
        CellId gray = new CellId(1, 2, 0);
        CellId black = new CellId(2, 2, 0);
        WorldSnapshot baseline = CreateWorld(new[] { gray, black });
        MaterialId rock = new MaterialId("sync.rock");
        MaterialId air = new MaterialId("sync.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 100),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.Restore(baseline, materials).Value;
        CellState blackState = world.GetCell(black).Value.State.WithExplored(false);
        world.ApplyTerrainChanges(
            new[] { new TerrainChange(black, blackState) },
            tick: 2);

        TunnelNavigationVolume synchronized =
            TunnelNavigationVolume.FromWorldSnapshot(
                world.CreateSnapshot(),
                new CellId[0],
                new CellId[0]);

        Assert.True(synchronized.IsOpen(gray));
        Assert.False(synchronized.IsOpen(black));
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
