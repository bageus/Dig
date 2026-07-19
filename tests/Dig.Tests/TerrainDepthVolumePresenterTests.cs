using System;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainDepthVolumePresenterTests
{
    [Fact]
    public void Demo_projection_matches_the_existing_deep_rock_mask()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = volume.DemoLayout!;
        TerrainDepthVolumePresenter presenter = new TerrainDepthVolumePresenter();

        TerrainDepthVolumeViewModel result = presenter.Present(
            volume,
            "demo.rock",
            hardness: 120,
            Array.Empty<SpatialCellId>());

        Assert.Equal(4, result.Depth);
        Assert.DoesNotContain(
            new SpatialCellId(0, layout.SurfaceY - 1, 1),
            result.SolidCells);
        Assert.DoesNotContain(
            new SpatialCellId(layout.SurfaceMinX, layout.SurfaceY, 1),
            result.SolidCells);
        Assert.DoesNotContain(
            new SpatialCellId(layout.CaveMinX, layout.CaveFloorY - 1, 2),
            result.SolidCells);
        Assert.Contains(
            new SpatialCellId(layout.ShaftX, layout.SurfaceY + 1, 2),
            result.SolidCells);
        Assert.All(result.SolidCells, cell => Assert.InRange(cell.Z, 1, 3));
    }

    [Fact]
    public void Explicit_excavation_removes_only_the_requested_deep_cell()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = volume.DemoLayout!;
        SpatialCellId target = new SpatialCellId(
            layout.ShaftX + 1,
            layout.SurfaceY + 1,
            2);
        TerrainDepthVolumePresenter presenter = new TerrainDepthVolumePresenter();

        TerrainDepthVolumeViewModel before = presenter.Present(
            volume,
            "demo.rock",
            hardness: 120,
            Array.Empty<SpatialCellId>());
        TerrainDepthVolumeViewModel after = presenter.Present(
            volume,
            "demo.rock",
            hardness: 120,
            new[] { target });

        Assert.Contains(target, before.SolidCells);
        Assert.DoesNotContain(target, after.SolidCells);
        Assert.Equal(before.SolidCells.Count - 1, after.SolidCells.Count);
        Assert.NotEqual(before.Version, after.Version);
    }

    [Fact]
    public void Projection_is_deterministic_for_the_same_inputs()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TerrainDepthVolumePresenter presenter = new TerrainDepthVolumePresenter();
        SpatialCellId first = new SpatialCellId(2, 5, 2);
        SpatialCellId second = new SpatialCellId(3, 5, 2);

        TerrainDepthVolumeViewModel left = presenter.Present(
            volume,
            "demo.rock",
            hardness: 120,
            new[] { first, second });
        TerrainDepthVolumeViewModel right = presenter.Present(
            volume,
            "demo.rock",
            hardness: 120,
            new[] { second, first });

        Assert.Equal(left.Version, right.Version);
        Assert.Equal(left.SolidCells, right.SolidCells);
    }

    [Fact]
    public void Non_demo_volume_treats_every_non_open_deep_cell_as_solid()
    {
        SpatialCellId open = new SpatialCellId(1, 1, 2);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 3,
            height: 3,
            depth: 4,
            openCells: new[] { open },
            verticalCells: Array.Empty<SpatialCellId>());
        TerrainDepthVolumePresenter presenter = new TerrainDepthVolumePresenter();

        TerrainDepthVolumeViewModel result = presenter.Present(
            volume,
            "stone",
            hardness: 80,
            Array.Empty<SpatialCellId>());

        Assert.Equal((3 * 3 * 3) - 1, result.SolidCells.Count);
        Assert.DoesNotContain(open, result.SolidCells);
        Assert.Contains(new SpatialCellId(0, 0, 1), result.SolidCells);
        Assert.Contains(new SpatialCellId(2, 2, 3), result.SolidCells);
    }
}

}
