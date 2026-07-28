using Dig.Domain.Core;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{
public sealed class WorldOwnedExcavationTests
{
    private static readonly MaterialId Rock = new MaterialId("rock");
    private static readonly MaterialId Air = new MaterialId("air");

    [Fact]
    public void Fourth_world_quarter_opens_cell_and_preserves_source_material()
    {
        WorldState world = CreateWorld();
        CellId target = new CellId(2, 2);
        Assert.True(world.SetDigDesignation(target, designated: true, tick: 1).IsSuccess);

        AssertQuarter(world, target, ExcavationQuarter.UpperLeft, tick: 2, solid: true);
        AssertQuarter(world, target, ExcavationQuarter.UpperRight, tick: 3, solid: true);
        AssertQuarter(world, target, ExcavationQuarter.LowerLeft, tick: 4, solid: true);
        AssertQuarter(world, target, ExcavationQuarter.LowerRight, tick: 5, solid: false);

        CellSnapshot opened = world.GetCell(target).Value;
        Assert.Equal(Air, opened.State.MaterialId);
        Assert.Equal(Rock, opened.State.ExcavationSourceMaterialId);
        Assert.Equal(ExcavationQuarter.All, opened.State.CompletedExcavationQuarters);
        Assert.Equal(ExcavationCutPattern.HorizontalRows, opened.State.ExcavationCutPattern);
        Assert.Equal(CellDesignation.None, opened.State.Designation);
        Assert.True(opened.State.IsExcavationOpen);

        Result<WorldMutationResult> retry = world.CommitExcavationQuarter(
            target,
            ExcavationQuarter.LowerRight,
            ExcavationCutPattern.HorizontalRows,
            Air,
            tick: 6);
        Assert.True(retry.IsSuccess);
        Assert.Equal(0, retry.Value.ChangedCellCount);
    }

    [Fact]
    public void Vertical_target_uses_horizontal_near_band_even_from_side_work_cell()
    {
        CellId target = new CellId(4, 4);
        ExcavationApproachSide approach = ExcavationApproachResolver.Resolve(
            new CellId(3, 4),
            target,
            ExcavationCutPattern.HorizontalRows);
        ExcavationQuarterPlanner planner = new ExcavationQuarterPlanner();
        ExcavationSwingPlan plan = planner.Plan(
            new ExcavationQuarterState(),
            approach,
            miningSkill: 100,
            deterministicSeed: 7);

        Assert.Equal(ExcavationApproachSide.Above, approach);
        Assert.NotEqual(ExcavationQuarter.None, plan.Quarters);
        Assert.Equal(
            ExcavationQuarter.None,
            plan.Quarters & (ExcavationQuarter.LowerLeft | ExcavationQuarter.LowerRight));
    }

    private static void AssertQuarter(
        WorldState world,
        CellId target,
        ExcavationQuarter quarter,
        long tick,
        bool solid)
    {
        Result<WorldMutationResult> result = world.CommitExcavationQuarter(
            target,
            quarter,
            ExcavationCutPattern.HorizontalRows,
            Air,
            tick);
        Assert.True(result.IsSuccess);
        Assert.Equal(solid, world.GetCell(target).Value.IsSolid);
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(6, 6),
            chunkSize: 2,
            materials,
            Rock,
            explored: true).Value;
    }
}
}
