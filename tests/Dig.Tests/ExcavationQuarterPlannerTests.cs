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

        [Theory]
        [InlineData(0, 2, 3)]
        [InlineData(10, 2, 3)]
        [InlineData(11, 1, 2)]
        [InlineData(20, 1, 2)]
        [InlineData(21, 1, 1)]
        [InlineData(40, 1, 1)]
        public void Low_skill_bands_produce_expected_swing_ranges(
            int skill,
            int minimum,
            int maximum)
        {
            ExcavationQuarterPlanner planner = new ExcavationQuarterPlanner();

            for (ulong seed = 0; seed < 64; seed++)
            {
                ExcavationSwingPlan plan = planner.Plan(
                    new ExcavationQuarterState(),
                    ExcavationApproachSide.Right,
                    skill,
                    seed);

                Assert.InRange(plan.RequiredSwingsPerQuarter, minimum, maximum);
                Assert.Equal(1, Count(plan.Quarters));
            }
        }

        [Theory]
        [InlineData(41, 1, 2)]
        [InlineData(50, 1, 2)]
        [InlineData(51, 2, 3)]
        [InlineData(70, 2, 3)]
        [InlineData(71, 3, 4)]
        [InlineData(100, 3, 4)]
        public void High_skill_bands_produce_expected_quarter_ranges(
            int skill,
            int minimum,
            int maximum)
        {
            ExcavationQuarterPlanner planner = new ExcavationQuarterPlanner();

            for (ulong seed = 0; seed < 64; seed++)
            {
                ExcavationSwingPlan plan = planner.Plan(
                    new ExcavationQuarterState(),
                    ExcavationApproachSide.Right,
                    skill,
                    seed);

                Assert.InRange(Count(plan.Quarters), 1, maximum);
                Assert.True(Count(plan.Quarters) <= maximum);
                Assert.Equal(1, plan.RequiredSwingsPerQuarter);
            }
        }

        [Fact]
        public void Reserved_quarter_is_not_selected_when_another_is_available()
        {
            ExcavationQuarterPlanner planner = new ExcavationQuarterPlanner();

            ExcavationSwingPlan plan = planner.Plan(
                new ExcavationQuarterState(),
                ExcavationApproachSide.Right,
                miningSkill: 21,
                deterministicSeed: 4,
                reserved: ExcavationQuarter.UpperRight);

            Assert.Equal(ExcavationQuarter.LowerRight, plan.Quarters);
        }

        [Fact]
        public void Finishing_worker_can_start_another_quarter_without_helping_reserved_work()
        {
            ExcavationQuarterState state = new ExcavationQuarterState();
            state.Complete(ExcavationQuarter.UpperRight);
            ExcavationQuarterPlanner planner = new ExcavationQuarterPlanner();

            ExcavationSwingPlan plan = planner.Plan(
                state,
                ExcavationApproachSide.Right,
                miningSkill: 21,
                deterministicSeed: 7,
                reserved: ExcavationQuarter.LowerRight);

            Assert.NotEqual(ExcavationQuarter.LowerRight, plan.Quarters);
            Assert.False((plan.Quarters & state.Completed) != 0);
        }

        [Fact]
        public void Same_seed_produces_same_plan()
        {
            ExcavationQuarterPlanner planner = new ExcavationQuarterPlanner();

            ExcavationSwingPlan first = planner.Plan(
                new ExcavationQuarterState(),
                ExcavationApproachSide.Above,
                miningSkill: 50,
                deterministicSeed: 12345);
            ExcavationSwingPlan second = planner.Plan(
                new ExcavationQuarterState(),
                ExcavationApproachSide.Above,
                miningSkill: 50,
                deterministicSeed: 12345);

            Assert.Equal(first.Quarters, second.Quarters);
            Assert.Equal(
                first.RequiredSwingsPerQuarter,
                second.RequiredSwingsPerQuarter);
        }

        [Fact]
        public void Quarter_requires_configured_number_of_swings()
        {
            ExcavationQuarterState state = new ExcavationQuarterState();

            Assert.False(state.ApplySwing(ExcavationQuarter.UpperLeft, 3));
            Assert.False(state.ApplySwing(ExcavationQuarter.UpperLeft, 3));
            Assert.True(state.ApplySwing(ExcavationQuarter.UpperLeft, 3));
            Assert.True(state.IsCompleted(ExcavationQuarter.UpperLeft));
        }

        private static int Count(ExcavationQuarter quarters)
        {
            int value = (int)quarters;
            int count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }

            return count;
        }
    }
}
