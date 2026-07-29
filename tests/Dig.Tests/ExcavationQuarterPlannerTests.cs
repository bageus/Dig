using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ExcavationQuarterPlannerTests
{
    [Theory]
    [InlineData(ExcavationApproachSide.Left,
        ExcavationQuarter.UpperLeft | ExcavationQuarter.LowerLeft)]
    [InlineData(ExcavationApproachSide.Right,
        ExcavationQuarter.UpperRight | ExcavationQuarter.LowerRight)]
    [InlineData(ExcavationApproachSide.Above,
        ExcavationQuarter.UpperLeft | ExcavationQuarter.UpperRight)]
    [InlineData(ExcavationApproachSide.Below,
        ExcavationQuarter.LowerLeft | ExcavationQuarter.LowerRight)]
    public void Approach_side_limits_initial_quarters(
        ExcavationApproachSide side,
        ExcavationQuarter expected)
    {
        Assert.Equal(expected, ExcavationQuarterPlanner.CandidatesFor(side));
    }

    [Fact]
    public void Planner_selects_one_stable_nearest_quarter()
    {
        ExcavationQuarterPlanner planner = new ExcavationQuarterPlanner();

        ExcavationSwingPlan plan = planner.Plan(
            new ExcavationQuarterState(),
            ExcavationApproachSide.Right);

        Assert.Equal(ExcavationQuarter.UpperRight, plan.Quarters);
        Assert.Equal(1, plan.RequiredSwingsPerQuarter);
    }

    [Theory]
    [InlineData(0, 0UL)]
    [InlineData(10, 1UL)]
    [InlineData(21, 2UL)]
    [InlineData(50, 99UL)]
    [InlineData(100, 999UL)]
    public void Compatibility_overload_does_not_make_quarter_selection_random(
        int skill,
        ulong seed)
    {
        ExcavationQuarterPlanner planner = new ExcavationQuarterPlanner();

        ExcavationSwingPlan plan = planner.Plan(
            new ExcavationQuarterState(),
            ExcavationApproachSide.Above,
            skill,
            seed);

        Assert.Equal(ExcavationQuarter.UpperLeft, plan.Quarters);
        Assert.Equal(1, plan.RequiredSwingsPerQuarter);
    }

    [Fact]
    public void Reserved_quarter_is_not_selected_when_another_is_available()
    {
        ExcavationQuarterPlanner planner = new ExcavationQuarterPlanner();

        ExcavationSwingPlan plan = planner.Plan(
            new ExcavationQuarterState(),
            ExcavationApproachSide.Right,
            reserved: ExcavationQuarter.UpperRight);

        Assert.Equal(ExcavationQuarter.LowerRight, plan.Quarters);
    }

    [Fact]
    public void Planner_falls_back_to_an_unfinished_quarter_after_preferred_side()
    {
        ExcavationQuarterState state = new ExcavationQuarterState();
        state.Complete(ExcavationQuarter.UpperRight);
        state.Complete(ExcavationQuarter.LowerRight);
        ExcavationQuarterPlanner planner = new ExcavationQuarterPlanner();

        ExcavationSwingPlan plan = planner.Plan(
            state,
            ExcavationApproachSide.Right);

        Assert.Equal(ExcavationQuarter.UpperLeft, plan.Quarters);
    }

    [Fact]
    public void Completed_and_reserved_quarters_return_no_plan()
    {
        ExcavationQuarterState state = new ExcavationQuarterState();
        state.Complete(ExcavationQuarter.UpperLeft);
        state.Complete(ExcavationQuarter.LowerLeft);

        ExcavationSwingPlan plan = new ExcavationQuarterPlanner().Plan(
            state,
            ExcavationApproachSide.Left,
            ExcavationQuarter.UpperRight | ExcavationQuarter.LowerRight);

        Assert.Equal(ExcavationQuarter.None, plan.Quarters);
    }

    [Fact]
    public void Legacy_swing_progress_state_remains_compatible_but_is_not_runtime_cadence()
    {
        ExcavationQuarterState state = new ExcavationQuarterState();

        Assert.False(state.ApplySwing(ExcavationQuarter.UpperLeft, 3));
        Assert.False(state.ApplySwing(ExcavationQuarter.UpperLeft, 3));
        Assert.True(state.ApplySwing(ExcavationQuarter.UpperLeft, 3));
        Assert.True(state.IsCompleted(ExcavationQuarter.UpperLeft));
    }
}

}
