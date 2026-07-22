using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelNavigationTests
{
    [Fact]
    public void Demo_has_two_four_depth_platforms_four_cells_apart()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = Assert.IsType<TunnelDemoLayout>(volume.DemoLayout);

        Assert.Equal(4, volume.Depth);
        Assert.Equal(4, layout.CaveFloorY - layout.SurfaceY);
        Assert.True(layout.CaveHeight >= 3);
        Assert.True(layout.CaveWidth >= 4);
        for (int z = 0; z < volume.Depth; z++)
        {
            Assert.True(volume.IsOpen(new CellId(
                layout.SurfaceMinX + 1,
                layout.SurfaceY,
                z)));
            Assert.True(volume.IsOpen(new CellId(
                layout.CaveMinX,
                layout.CaveFloorY,
                z)));
        }
    }

    [Fact]
    public void Demo_shaft_and_connector_use_only_the_nearest_depth_cell()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = volume.DemoLayout!;
        int shaftY = layout.SurfaceY + 1;

        Assert.Equal(0, layout.ShaftZ);
        Assert.True(volume.IsVerticalTunnel(new CellId(
            layout.ShaftX,
            shaftY,
            0)));
        for (int z = 1; z < volume.Depth; z++)
        {
            Assert.False(volume.IsOpen(new CellId(layout.ShaftX, shaftY, z)));
        }

        if (layout.CaveMinX - layout.ShaftX > 1)
        {
            int corridorX = layout.ShaftX + 1;
            Assert.True(volume.IsOpen(new CellId(
                corridorX,
                layout.CaveFloorY,
                0)));
            for (int z = 1; z < volume.Depth; z++)
            {
                Assert.False(volume.IsOpen(new CellId(
                    corridorX,
                    layout.CaveFloorY,
                    z)));
            }
        }
    }

    [Fact]
    public void Surface_route_walks_on_the_xz_plane_without_changing_y()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = volume.DemoLayout!;
        CellId start = new CellId(
            layout.SurfaceMinX,
            layout.SurfaceY,
            0);
        CellId goal = new CellId(
            layout.SurfaceMinX + 3,
            layout.SurfaceY,
            3);

        TunnelPathResult result = volume.FindPath(start, goal);

        Assert.True(result.Succeeded, result.Detail);
        Assert.All(result.Path!.Cells, cell => Assert.Equal(layout.SurfaceY, cell.Y));
        Assert.Contains(result.Path.Cells, cell => cell.Z == 3);
        Assert.Contains(result.Path.Cells, cell => cell.X == goal.X);
        AssertRouteUsesOnlyAuthoritativeSteps(volume, result.Path);
    }

    [Fact]
    public void Shaft_route_climbs_on_the_xy_plane_at_nearest_z()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = volume.DemoLayout!;
        CellId start = new CellId(
            layout.ShaftX,
            layout.SurfaceY,
            layout.ShaftZ);
        CellId goal = new CellId(
            layout.ShaftX,
            layout.CaveFloorY,
            layout.ShaftZ);

        TunnelPathResult result = volume.FindPath(start, goal);

        Assert.True(result.Succeeded, result.Detail);
        Assert.All(result.Path!.Cells, cell => Assert.Equal(layout.ShaftX, cell.X));
        Assert.All(result.Path.Cells, cell => Assert.Equal(0, cell.Z));
        Assert.Contains(result.Path.Cells, cell => cell.Y == layout.CaveFloorY);
        AssertRouteUsesOnlyAuthoritativeSteps(volume, result.Path);
    }

    [Fact]
    public void Demo_connects_surface_to_every_depth_of_the_lower_cave()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = volume.DemoLayout!;
        CellId start = new CellId(
            layout.SurfaceMinX,
            layout.SurfaceY,
            3);
        CellId goal = new CellId(
            layout.CaveMaxX,
            layout.CaveFloorY,
            3);

        TunnelPathResult result = volume.FindPath(start, goal);

        Assert.True(result.Succeeded, result.Detail);
        Assert.Equal(start, result.Path!.Cells[0]);
        Assert.Equal(goal, result.Path.Cells[result.Path.Cells.Count - 1]);
        Assert.Contains(result.Path.Cells, cell => volume.IsVerticalTunnel(cell));
        Assert.Contains(result.Path.Cells, cell => cell.Z == 0);
        Assert.Contains(result.Path.Cells, cell => cell.Z == 3);
        AssertRouteUsesOnlyAuthoritativeSteps(volume, result.Path);
    }

    [Fact]
    public void Horizontal_depth_step_requires_two_open_adjacent_cells()
    {
        CellId start = new CellId(1, 1, 0);
        CellId nextDepth = new CellId(1, 1, 1);
        CellId skippedDepth = new CellId(1, 1, 2);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 4,
            height: 4,
            depth: 4,
            openCells: new[] { start, nextDepth, skippedDepth },
            verticalCells: new CellId[0]);

        Assert.True(volume.CanTraverseStep(start, nextDepth));
        Assert.False(volume.CanTraverseStep(start, skippedDepth));
        Assert.False(volume.CanTraverseStep(start, new CellId(2, 1, 0)));
    }

    [Fact]
    public void Vertical_motion_is_rejected_outside_a_vertical_tunnel()
    {
        CellId lower = new CellId(1, 1, 0);
        CellId upper = new CellId(1, 2, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 4,
            height: 4,
            depth: 4,
            openCells: new[] { lower, upper },
            verticalCells: new CellId[0]);

        TunnelPathResult result = volume.FindPath(lower, upper);

        Assert.False(volume.CanTraverseStep(lower, upper));
        Assert.False(result.Succeeded);
        Assert.Equal(TunnelPathFailureReason.Unreachable, result.FailureReason);
    }

    [Fact]
    public void Vertical_motion_succeeds_when_both_cells_are_in_the_shaft()
    {
        CellId lower = new CellId(1, 1, 0);
        CellId upper = new CellId(1, 2, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 4,
            height: 4,
            depth: 4,
            openCells: new[] { lower, upper },
            verticalCells: new[] { lower, upper });

        TunnelPathResult result = volume.FindPath(lower, upper);

        Assert.True(volume.CanTraverseStep(lower, upper));
        Assert.True(result.Succeeded, result.Detail);
        Assert.Equal(new[] { lower, upper }, result.Path!.Cells);
    }

    private static void AssertRouteUsesOnlyAuthoritativeSteps(
        TunnelNavigationVolume volume,
        TunnelPath path)
    {
        Assert.True(path.Cells.Count > 0);
        for (int index = 1; index < path.Cells.Count; index++)
        {
            Assert.True(
                volume.CanTraverseStep(path.Cells[index - 1], path.Cells[index]),
                $"Invalid route step {path.Cells[index - 1]} -> {path.Cells[index]}.");
        }
    }
}

}
