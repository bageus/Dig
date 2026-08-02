using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class DemoSurfaceNavigationTests
{
    [Fact]
    public void Demo_surface_is_open_supported_and_connected_through_both_world_edges()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = Assert.IsType<TunnelDemoLayout>(volume.DemoLayout);

        Assert.Equal(0, layout.SurfaceMinX);
        Assert.Equal(volume.Width - 1, layout.SurfaceMaxX);
        for (int z = 0; z < volume.Depth; z++)
        {
            CellId left = new CellId(0, layout.SurfaceY, z);
            CellId right = new CellId(volume.Width - 1, layout.SurfaceY, z);
            Assert.True(volume.IsOpen(left));
            Assert.True(volume.HasFullActorSupport(left));
            Assert.True(volume.IsOpen(right));
            Assert.True(volume.HasFullActorSupport(right));

            TunnelPathResult route = volume.FindPath(left, right);
            Assert.True(route.Succeeded, route.Detail);
            Assert.All(route.Path!.Cells, cell => Assert.Equal(layout.SurfaceY, cell.Y));
            Assert.All(
                route.Path.TraversalKinds,
                kind => Assert.True(
                    kind == TunnelTraversalKind.SupportedWalk
                    || kind == TunnelTraversalKind.DepthTraverse,
                    $"Surface route used {kind}."));
        }
    }

    [Fact]
    public void Longer_supported_route_wins_over_shorter_vertical_climb()
    {
        CellId start = new CellId(0, 1, 0);
        CellId goal = new CellId(4, 1, 0);
        CellId climbTopLeft = new CellId(1, 0, 0);
        CellId climbTopRight = new CellId(3, 0, 0);
        CellId[] flatDetour =
        {
            new CellId(0, 1, 1),
            new CellId(1, 1, 1),
            new CellId(2, 1, 1),
            new CellId(3, 1, 1),
            new CellId(4, 1, 1),
        };
        CellId[] open =
        {
            start,
            goal,
            climbTopLeft,
            new CellId(2, 0, 0),
            climbTopRight,
            new CellId(1, 1, 0),
            new CellId(3, 1, 0),
            flatDetour[0],
            flatDetour[1],
            flatDetour[2],
            flatDetour[3],
            flatDetour[4],
        };
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 5,
            height: 3,
            depth: 2,
            openCells: open,
            verticalCells: new[]
            {
                climbTopLeft,
                new CellId(1, 1, 0),
                climbTopRight,
                new CellId(3, 1, 0),
            },
            supportedCells: open);

        TunnelPathResult result = volume.FindPath(start, goal);

        Assert.True(result.Succeeded, result.Detail);
        Assert.DoesNotContain(
            TunnelTraversalKind.VerticalClimb,
            result.Path!.TraversalKinds);
        Assert.Contains(result.Path.Cells, cell => cell.Z == 1);
    }
}

}
