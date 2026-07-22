using Dig.Application.World;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class TunnelDepthExcavationPolicyTests
{
    [Fact]
    public void Open_tunnel_designates_exactly_one_next_depth_cell()
    {
        CellId source = new CellId(2, 3, 0);
        TunnelNavigationVolume volume = CreateVolume(
            open: new[] { source },
            vertical: new CellId[0]);

        TunnelDepthExcavationPlanResult result =
            new TunnelDepthExcavationPolicy().Plan(volume, source);

        Assert.True(result.Succeeded, result.Detail);
        Assert.Equal(source, result.Plan!.Source);
        Assert.Equal(new CellId(2, 3, 1), result.Plan.Target);
    }

    [Fact]
    public void Open_vertical_tunnel_can_designate_the_next_depth_cell()
    {
        CellId source = new CellId(2, 3, 0);
        TunnelNavigationVolume volume = CreateVolume(
            open: new[] { source },
            vertical: new[] { source });

        TunnelDepthExcavationPlanResult result =
            new TunnelDepthExcavationPolicy().Plan(volume, source);

        Assert.True(result.Succeeded, result.Detail);
        Assert.Equal(new CellId(2, 3, 1), result.Plan!.Target);
    }

    [Fact]
    public void Already_open_next_layer_requires_selecting_that_layer()
    {
        CellId source = new CellId(2, 3, 0);
        CellId next = new CellId(2, 3, 1);
        TunnelNavigationVolume volume = CreateVolume(
            open: new[] { source, next },
            vertical: new CellId[0]);

        TunnelDepthExcavationPlanResult result =
            new TunnelDepthExcavationPolicy().Plan(volume, source);

        Assert.False(result.Succeeded);
        Assert.Equal(
            TunnelDepthExcavationFailureReason.NextDepthAlreadyOpen,
            result.FailureReason);
    }

    [Fact]
    public void Deepest_layer_cannot_exceed_the_four_cell_limit()
    {
        CellId source = new CellId(2, 3, 3);
        TunnelNavigationVolume volume = CreateVolume(
            open: new[] { source },
            vertical: new CellId[0]);

        TunnelDepthExcavationPlanResult result =
            new TunnelDepthExcavationPolicy().Plan(volume, source);

        Assert.False(result.Succeeded);
        Assert.Equal(
            TunnelDepthExcavationFailureReason.MaximumDepthReached,
            result.FailureReason);
    }

    [Fact]
    public void Solid_cell_cannot_start_depth_excavation()
    {
        CellId source = new CellId(2, 3, 0);
        TunnelNavigationVolume volume = CreateVolume(
            open: new CellId[0],
            vertical: new CellId[0]);

        TunnelDepthExcavationPlanResult result =
            new TunnelDepthExcavationPolicy().Plan(volume, source);

        Assert.False(result.Succeeded);
        Assert.Equal(
            TunnelDepthExcavationFailureReason.SourceNotOpen,
            result.FailureReason);
    }

    private static TunnelNavigationVolume CreateVolume(
        CellId[] open,
        CellId[] vertical)
    {
        return new TunnelNavigationVolume(
            width: 6,
            height: 6,
            depth: 4,
            openCells: open,
            verticalCells: vertical);
    }
}

}