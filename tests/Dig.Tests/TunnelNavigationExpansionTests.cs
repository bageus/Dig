using System;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelNavigationExpansionTests
{
    [Fact]
    public void Additional_room_floor_cells_open_a_route_without_mutating_original_volume()
    {
        SpatialCellId start = new SpatialCellId(1, 2, 0);
        SpatialCellId entrance = new SpatialCellId(2, 2, 0);
        SpatialCellId deepEntrance = new SpatialCellId(2, 2, 1);
        SpatialCellId destination = new SpatialCellId(3, 2, 1);
        TunnelNavigationVolume original = new TunnelNavigationVolume(
            width: 5,
            height: 5,
            depth: 3,
            openCells: new[] { start, entrance },
            verticalCells: Array.Empty<SpatialCellId>());

        TunnelNavigationVolume expanded = original.WithAdditionalOpenCells(
            new[] { deepEntrance, destination });

        Assert.False(original.IsOpen(destination));
        Assert.True(expanded.IsOpen(destination));
        TunnelPathResult result = expanded.FindPath(start, destination);
        Assert.True(result.Succeeded, result.Detail);
        Assert.Equal(
            new[] { start, entrance, deepEntrance, destination },
            result.Path!.Cells);
    }

    [Fact]
    public void Additional_room_floor_rejects_cells_outside_the_volume()
    {
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 5,
            height: 5,
            depth: 3,
            openCells: new[] { new SpatialCellId(1, 2, 0) },
            verticalCells: Array.Empty<SpatialCellId>());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            volume.WithAdditionalOpenCells(new[] { new SpatialCellId(5, 2, 0) }));
    }
}

}
