using Dig.Domain.Navigation;
using Dig.Domain.Production;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingSupplyReachabilityTests
{
    [Fact]
    public void Connected_cells_exclude_explored_open_sources_in_another_region()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 8,
            height: 4,
            chunkSize: 4,
            corridorY: 1,
            blockedX: 4);
        NavigationSnapshot navigation = NavigationTestFactory.GetSnapshot(
            NavigationTestFactory.BuildMap(
                world,
                TraversalProfile.CreateGroundedDwarf()));
        CellId destination = new CellId(1, 1, 0);
        CellId connected = new CellId(3, 1, 0);
        CellId disconnected = new CellId(6, 1, 0);

        var reachable = BuildingSupplyReachability.ResolveConnectedCells(
            navigation,
            destination);

        Assert.Contains(destination, reachable);
        Assert.Contains(connected, reachable);
        Assert.DoesNotContain(disconnected, reachable);
        Assert.True(BuildingSupplyReachability.IsConnected(
            navigation,
            destination,
            connected));
        Assert.False(BuildingSupplyReachability.IsConnected(
            navigation,
            destination,
            disconnected));
    }
}

}
