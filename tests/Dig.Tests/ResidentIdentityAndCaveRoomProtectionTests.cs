using System.Linq;
using Dig.Application.World;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class NaturalCaveProtectionTests
{
    [Fact]
    public void Natural_cave_protects_complete_outer_perimeter_only()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(20, 14);
        TunnelDemoLayout layout = volume.DemoLayout!;

        CellId[] protectedCells = new NaturalCaveShellProtectionPolicy()
            .Resolve(layout, new WorldSize(volume.Width, volume.Height))
            .ToArray();

        int left = layout.CaveMinX - 1;
        int right = layout.CaveMaxX + 1;
        int top = layout.CaveCeilingY;
        int bottom = layout.CaveFloorY + 1;
        for (int x = left; x <= right; x++)
        {
            Assert.Contains(new CellId(x, top), protectedCells);
            Assert.Contains(new CellId(x, bottom), protectedCells);
        }

        for (int y = top + 1; y < bottom; y++)
        {
            Assert.Contains(new CellId(left, y), protectedCells);
            Assert.Contains(new CellId(right, y), protectedCells);
        }

        Assert.DoesNotContain(
            new CellId(layout.CaveMinX, layout.CaveFloorY),
            protectedCells);
        Assert.DoesNotContain(
            new CellId(layout.CaveMaxX, layout.CaveCeilingY + 1),
            protectedCells);
    }
}

}