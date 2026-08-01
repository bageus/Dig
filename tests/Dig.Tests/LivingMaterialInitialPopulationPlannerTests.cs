using System;
using System.Linq;
using Dig.Application.Ecology;
using Dig.Domain.Ecology;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialInitialPopulationPlannerTests
{
    [Fact]
    public void TwoHamstersShareAPlaneAndGrubUsesAnotherPlaneWhenAvailable()
    {
        NavigationSnapshot navigation = BuildCorridor(blockedX: 6);

        var planned = new LivingMaterialInitialPopulationPlanner().Plan(
            navigation,
            Array.Empty<CellId>());

        Assert.True(planned.IsSuccess, planned.Error?.ToString());
        LivingMaterialInitialPlacement[] hamsters = planned.Value.Placements
            .Where(value => value.Species == LivingMaterialSpecies.Hamster)
            .ToArray();
        LivingMaterialInitialPlacement grub = planned.Value.Placements.Single(
            value => value.Species == LivingMaterialSpecies.Grub);
        Assert.Equal(2, hamsters.Length);
        Assert.Equal(hamsters[0].PlaneKey, hamsters[1].PlaneKey);
        Assert.NotEqual(hamsters[0].PlaneKey, grub.PlaneKey);
        Assert.Equal(3, planned.Value.Placements.Select(value => value.Cell)
            .Distinct().Count());
    }

    [Fact]
    public void SinglePlaneUsesAThirdDistinctCellForGrub()
    {
        NavigationSnapshot navigation = BuildCorridor();

        var planned = new LivingMaterialInitialPopulationPlanner().Plan(
            navigation,
            Array.Empty<CellId>());

        Assert.True(planned.IsSuccess, planned.Error?.ToString());
        Assert.Single(planned.Value.Placements
            .Select(value => value.PlaneKey)
            .Distinct());
        Assert.Equal(3, planned.Value.Placements
            .Select(value => value.Cell)
            .Distinct().Count());
    }

    [Fact]
    public void OccupiedWorldCellsAreSkippedAndPlanningIsDeterministic()
    {
        NavigationSnapshot navigation = BuildCorridor(blockedX: 6);
        LivingMaterialInitialPopulationPlanner planner =
            new LivingMaterialInitialPopulationPlanner();
        var baseline = planner.Plan(navigation, Array.Empty<CellId>());
        Assert.True(baseline.IsSuccess, baseline.Error?.ToString());
        CellId occupied = baseline.Value.Placements[0].Cell;

        var first = planner.Plan(navigation, new[] { occupied });
        var second = planner.Plan(navigation, new[] { occupied });

        Assert.True(first.IsSuccess, first.Error?.ToString());
        Assert.True(second.IsSuccess, second.Error?.ToString());
        Assert.DoesNotContain(occupied, first.Value.Placements.Select(value => value.Cell));
        Assert.Equal(
            first.Value.Placements.Select(value => value.Species),
            second.Value.Placements.Select(value => value.Species));
        Assert.Equal(
            first.Value.Placements.Select(value => value.Cell),
            second.Value.Placements.Select(value => value.Cell));
        Assert.Equal(
            first.Value.Placements.Select(value => value.PlaneKey),
            second.Value.Placements.Select(value => value.PlaneKey));
    }

    private static NavigationSnapshot BuildCorridor(params int[] blockedX)
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 12,
            height: 6,
            chunkSize: 4,
            corridorY: 2,
            blockedX);
        NavigationMap map = NavigationTestFactory.BuildMap(
            world,
            TraversalProfile.CreateFreeMover());
        return NavigationTestFactory.GetSnapshot(map);
    }
}

}
