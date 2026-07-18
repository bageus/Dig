using Dig.Domain.Navigation;
using Xunit;

namespace Dig.Tests
{

public sealed class CaveBackWallPolicyTests
{
    [Fact]
    public void Generated_cave_can_keep_its_back_wall()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(
            width: 20,
            height: 14,
            naturalCaveHasBackWall: true);

        Assert.True(volume.DemoLayout!.CaveHasBackWall);
    }

    [Fact]
    public void Generated_cave_can_be_open_backed()
    {
        TunnelNavigationVolume volume = TunnelNavigationVolume.CreateDemo(
            width: 20,
            height: 14,
            naturalCaveHasBackWall: false);

        Assert.False(volume.DemoLayout!.CaveHasBackWall);
    }
}

}
