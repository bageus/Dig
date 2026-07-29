using System;
using System.IO;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class DemoCampfirePlacementTests
{
    [Fact]
    public void Demo_layout_exposes_open_surface_anchor_two_cells_left_of_shaft()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(
            width: 16,
            height: 12);
        TunnelDemoLayout layout = Assert.IsType<TunnelDemoLayout>(volume.DemoLayout);
        CellId anchor = new CellId(
            layout.ShaftX - 2,
            layout.SurfaceY,
            layout.ShaftZ);

        Assert.True(volume.IsOpen(anchor));
        Assert.False(volume.IsVerticalTunnel(anchor));
        Assert.Equal(2, layout.ShaftX - anchor.X);
    }

    [Fact]
    public void Unity_bootstrap_uses_exact_surface_anchor_without_cave_fallback()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigTerrainWorkSession.Buildings.cs"));

        Assert.Contains("FindSurfaceCampfirePlacement", source);
        Assert.Contains("layout.ShaftX - 2", source);
        Assert.Contains("layout.SurfaceY", source);
        Assert.Contains("layout.ShaftZ", source);
        Assert.Contains("surface campfire anchor two cells left", source);
        Assert.DoesNotContain("FindLowerCavePlacement(campfireDefinition", source);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src"))
                && Directory.Exists(Path.Combine(current.FullName, "unity")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
