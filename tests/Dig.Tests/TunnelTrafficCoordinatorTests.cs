using Dig.Application.Agents;
using Dig.Domain.Core;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelTrafficCoordinatorTests
{
    private static readonly EntityId ResidentA =
        EntityId.Parse("91000000000000000000000000000001");
    private static readonly EntityId ResidentB =
        EntityId.Parse("91000000000000000000000000000002");

    [Fact]
    public void Residents_may_enter_the_same_tunnel_cell_in_one_tick()
    {
        TunnelTrafficCoordinator traffic = new TunnelTrafficCoordinator();
        CellId shared = new CellId(2, 3, 0);

        Assert.True(traffic.CanMove(
            ResidentA,
            new CellId(1, 3, 0),
            shared,
            tick: 10));
        traffic.RecordMove(
            ResidentA,
            new CellId(1, 3, 0),
            shared,
            tick: 10);

        Assert.True(traffic.CanMove(
            ResidentB,
            new CellId(3, 3, 0),
            shared,
            tick: 10));
    }

    [Fact]
    public void Residents_do_not_exchange_cells_through_each_other_in_one_tick()
    {
        TunnelTrafficCoordinator traffic = new TunnelTrafficCoordinator();
        CellId left = new CellId(1, 3, 0);
        CellId right = new CellId(2, 3, 0);

        traffic.RecordMove(ResidentA, left, right, tick: 20);

        Assert.False(traffic.CanMove(ResidentB, right, left, tick: 20));
        Assert.True(traffic.CanMove(ResidentB, right, left, tick: 21));
    }
}

}
