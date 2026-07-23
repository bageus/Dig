using Dig.Domain.Core;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{
    public sealed class ExcavationWorkCoordinatorTests
    {
        [Fact]
        public void Two_workers_reserve_different_quarters_when_possible()
        {
            ExcavationWorkCoordinator coordinator = new ExcavationWorkCoordinator();
            ExcavationWorkTarget target = new ExcavationWorkTarget(new CellId(4, 5), 2);
            EntityId first = EntityId.Parse("00000000-0000-0000-0000-000000000001");
            EntityId second = EntityId.Parse("00000000-0000-0000-0000-000000000002");

            ExcavationWorkerAssignment firstAssignment = coordinator.Assign(
                first,
                target,
                ExcavationApproachSide.Right,
                miningSkill: 21);
            ExcavationWorkerAssignment secondAssignment = coordinator.Assign(
                second,
                target,
                ExcavationApproachSide.Right,
                miningSkill: 21);

            Assert.NotEqual(
                ExcavationQuarter.None,
                firstAssignment.ReservedQuarters);
            Assert.NotEqual(
                ExcavationQuarter.None,
                secondAssignment.ReservedQuarters);
            Assert.Equal(
                ExcavationQuarter.None,
                firstAssignment.ReservedQuarters & secondAssignment.ReservedQuarters);
        }

        [Fact]
        public void High_skill_workers_reserve_one_distinct_current_quarter_each()
        {
            ExcavationWorkCoordinator coordinator = new ExcavationWorkCoordinator();
            ExcavationWorkTarget target = new ExcavationWorkTarget(new CellId(5, 5), 0);
            EntityId first = EntityId.Parse("00000000-0000-0000-0000-000000000009");
            EntityId second = EntityId.Parse("00000000-0000-0000-0000-00000000000a");

            ExcavationWorkerAssignment firstAssignment = coordinator.Assign(
                first,
                target,
                ExcavationApproachSide.Left,
                miningSkill: 100);
            ExcavationWorkerAssignment secondAssignment = coordinator.Assign(
                second,
                target,
                ExcavationApproachSide.Right,
                miningSkill: 100);

            Assert.True(IsSingleQuarter(firstAssignment.ReservedQuarters));
            Assert.True(IsSingleQuarter(secondAssignment.ReservedQuarters));
            Assert.Equal(
                ExcavationQuarter.None,
                firstAssignment.ReservedQuarters & secondAssignment.ReservedQuarters);
        }

        [Fact]
        public void Quarter_completion_is_attributed_to_finishing_worker()
        {
            ExcavationWorkCoordinator coordinator = new ExcavationWorkCoordinator();
            ExcavationWorkTarget target = new ExcavationWorkTarget(new CellId(1, 2), 0);
            EntityId worker = EntityId.Parse("00000000-0000-0000-0000-000000000003");
            coordinator.Assign(
                worker,
                target,
                ExcavationApproachSide.Left,
                miningSkill: 21);

            ExcavationQuarterCompletion completion = Assert.Single(
                coordinator.ApplySwing(worker, deterministicSeed: 10));

            Assert.Equal(worker, completion.WorkerId);
            Assert.Equal(target, completion.Target);
            Assert.NotEqual(ExcavationQuarter.None, completion.Quarter);
        }

        [Fact]
        public void Low_skill_worker_accumulates_swings_without_sharing_progress()
        {
            ExcavationWorkCoordinator coordinator = new ExcavationWorkCoordinator();
            ExcavationWorkTarget target = new ExcavationWorkTarget(new CellId(8, 9), 1);
            EntityId first = EntityId.Parse("00000000-0000-0000-0000-000000000004");
            EntityId second = EntityId.Parse("00000000-0000-0000-0000-000000000005");
            coordinator.Assign(first, target, ExcavationApproachSide.Right, miningSkill: 0);
            coordinator.Assign(second, target, ExcavationApproachSide.Right, miningSkill: 0);

            Assert.Empty(coordinator.ApplySwing(first, deterministicSeed: 1));
            Assert.Empty(coordinator.ApplySwing(second, deterministicSeed: 1));

            ExcavationWorkerAssignment firstAssignment = coordinator.GetAssignment(first)!;
            ExcavationWorkerAssignment secondAssignment = coordinator.GetAssignment(second)!;
            Assert.Equal(
                ExcavationQuarter.None,
                firstAssignment.ReservedQuarters & secondAssignment.ReservedQuarters);
        }

        [Fact]
        public void State_is_independent_for_each_z_layer()
        {
            ExcavationWorkCoordinator coordinator = new ExcavationWorkCoordinator();
            CellId cell = new CellId(3, 3);
            ExcavationWorkTarget upper = new ExcavationWorkTarget(cell, 0);
            ExcavationWorkTarget lower = new ExcavationWorkTarget(cell, 1);
            EntityId worker = EntityId.Parse("00000000-0000-0000-0000-000000000006");

            coordinator.Assign(worker, upper, ExcavationApproachSide.Above, miningSkill: 100);
            coordinator.ApplySwing(worker, deterministicSeed: 4);

            Assert.True(coordinator.GetState(upper).Completed != ExcavationQuarter.None);
            Assert.Equal(ExcavationQuarter.None, coordinator.GetState(lower).Completed);
        }

        [Fact]
        public void Completing_cell_cancels_all_assignments_for_that_layer()
        {
            ExcavationWorkCoordinator coordinator = new ExcavationWorkCoordinator();
            ExcavationWorkTarget target = new ExcavationWorkTarget(new CellId(7, 7), 3);
            EntityId first = EntityId.Parse("00000000-0000-0000-0000-000000000007");
            EntityId second = EntityId.Parse("00000000-0000-0000-0000-000000000008");
            coordinator.Assign(first, target, ExcavationApproachSide.Left, miningSkill: 100);
            coordinator.Assign(second, target, ExcavationApproachSide.Right, miningSkill: 100);

            coordinator.ApplySwing(first, deterministicSeed: 20);
            if (!coordinator.GetState(target).IsComplete)
            {
                coordinator.ApplySwing(second, deterministicSeed: 21);
            }

            Assert.True(coordinator.GetState(target).IsComplete);
            Assert.Null(coordinator.GetAssignment(first));
            Assert.Null(coordinator.GetAssignment(second));
        }

        private static bool IsSingleQuarter(ExcavationQuarter quarters)
        {
            int value = (int)quarters;
            return value != 0 && (value & (value - 1)) == 0;
        }
    }
}
