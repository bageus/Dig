using System.Linq;
using Dig.Application.Ecology;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class VukerBirthPlannerTests
{
    [Fact]
    public void RegionResolverJoinsWalkClimbAndDepthButKeepsDisconnectedCavesSeparate()
    {
        CellId floor = new CellId(0, 1, 0);
        CellId walk = new CellId(1, 1, 0);
        CellId climb = new CellId(1, 2, 0);
        CellId depth = new CellId(1, 2, 1);
        CellId separate = new CellId(5, 1, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 7,
            height: 4,
            depth: 2,
            openCells: new[] { floor, walk, climb, depth, separate },
            verticalCells: new[] { walk, climb },
            supportedCells: new[] { floor, walk, climb, depth, separate });

        VukerCaveRegionResolver resolver = new VukerCaveRegionResolver(volume);

        Assert.Equal(2, resolver.Regions.Count);
        Assert.True(resolver.TryResolveKey(floor, out VukerRegionKey connected));
        Assert.True(resolver.TryResolveKey(depth, out VukerRegionKey depthRegion));
        Assert.Equal(connected, depthRegion);
        Assert.True(resolver.TryResolveKey(separate, out VukerRegionKey other));
        Assert.NotEqual(connected, other);
    }

    [Fact]
    public void BirthUsesNearestStableFreeCellToLowestParent()
    {
        CellId first = new CellId(1, 1, 0);
        CellId second = new CellId(2, 1, 0);
        CellId nearest = new CellId(0, 1, 0);
        CellId farther = new CellId(3, 1, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 5,
            height: 3,
            depth: 1,
            openCells: new[] { nearest, first, second, farther },
            verticalCells: new CellId[0]);
        VukerCaveRegionResolver resolver = new VukerCaveRegionResolver(volume);
        Assert.True(resolver.TryResolveKey(first, out VukerRegionKey region));
        VukerEcologyState state = CreateDuePair(region, first, second);
        VukerPairSnapshot pair = Assert.Single(state.GetPairs());

        Result<VukerBirthPlan> result = new VukerBirthPlanner(resolver).Plan(
            state,
            pair,
            new[] { first, second },
            pair.NextBirthTick);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(nearest, result.Value.Position);
        Assert.Equal(state.CreateDeterministicChildId(pair.PairId, 0),
            result.Value.ChildId);
    }

    [Fact]
    public void NoFreeCellReturnsBlockedFailureWithoutMutatingPair()
    {
        CellId first = new CellId(0, 1, 0);
        CellId second = new CellId(1, 1, 0);
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 2,
            height: 3,
            depth: 1,
            openCells: new[] { first, second },
            verticalCells: new CellId[0]);
        VukerCaveRegionResolver resolver = new VukerCaveRegionResolver(volume);
        Assert.True(resolver.TryResolveKey(first, out VukerRegionKey region));
        VukerEcologyState state = CreateDuePair(region, first, second);
        VukerPairSnapshot pair = Assert.Single(state.GetPairs());

        Result<VukerBirthPlan> result = new VukerBirthPlanner(resolver).Plan(
            state,
            pair,
            new[] { first, second },
            pair.NextBirthTick);

        Assert.True(result.IsFailure);
        Assert.Equal("ecology.vuker.birth_cell_blocked", result.Error!.Code);
        Assert.Equal(0, state.GetPair(pair.PairId)!.SuccessfulCycles);
    }

    private static VukerEcologyState CreateDuePair(
        VukerRegionKey region,
        CellId firstCell,
        CellId secondCell)
    {
        VukerEcologyState state = new VukerEcologyState(88);
        Assert.True(state.RegisterAdult(
            Id(1), region, firstCell, VukerDisposition.Wild, 0).IsSuccess);
        Assert.True(state.RegisterAdult(
            Id(2), region, secondCell, VukerDisposition.Wild, 0).IsSuccess);
        state.Advance(0);
        VukerPairSnapshot pair = Assert.Single(state.GetPairs());
        Assert.Single(state.Advance(pair.NextBirthTick));
        return state;
    }

    private static EntityId Id(int suffix) => EntityId.Parse(
        "e300000000000000000000000000" + suffix.ToString("D4"));
}

}
