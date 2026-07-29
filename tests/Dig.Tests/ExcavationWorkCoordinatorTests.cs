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
        EntityId first = Id(1);
        EntityId second = Id(2);

        ExcavationWorkerAssignment firstAssignment = coordinator.Assign(
            first, target, ExcavationApproachSide.Right, miningSkill: 0);
        ExcavationWorkerAssignment secondAssignment = coordinator.Assign(
            second, target, ExcavationApproachSide.Right, miningSkill: 100);

        Assert.True(IsSingleQuarter(firstAssignment.ReservedQuarters));
        Assert.True(IsSingleQuarter(secondAssignment.ReservedQuarters));
        Assert.Equal(
            ExcavationQuarter.None,
            firstAssignment.ReservedQuarters & secondAssignment.ReservedQuarters);
    }

    [Fact]
    public void One_due_work_step_completes_exactly_one_reserved_quarter()
    {
        ExcavationWorkCoordinator coordinator = new ExcavationWorkCoordinator();
        ExcavationWorkTarget target = new ExcavationWorkTarget(new CellId(1, 2), 0);
        EntityId worker = Id(3);
        ExcavationWorkerAssignment assignment = coordinator.Assign(
            worker, target, ExcavationApproachSide.Left, miningSkill: 0);
        ExcavationQuarter reserved = assignment.ReservedQuarters;

        ExcavationQuarterCompletion completion = Assert.Single(
            coordinator.ApplyWork(worker));

        Assert.Equal(worker, completion.WorkerId);
        Assert.Equal(target, completion.Target);
        Assert.Equal(reserved, completion.Quarter);
        Assert.Equal(reserved, coordinator.GetState(target).Completed);
    }

    [Fact]
    public void Skill_changes_cadence_outside_coordinator_not_quarter_count()
    {
        ExcavationWorkCoordinator low = new ExcavationWorkCoordinator();
        ExcavationWorkCoordinator high = new ExcavationWorkCoordinator();
        ExcavationWorkTarget target = new ExcavationWorkTarget(new CellId(8, 9), 1);
        EntityId worker = Id(4);
        low.Assign(worker, target, ExcavationApproachSide.Right, miningSkill: 0);
        high.Assign(worker, target, ExcavationApproachSide.Right, miningSkill: 100);

        Assert.Single(low.ApplyWork(worker));
        Assert.Single(high.ApplyWork(worker));
        Assert.Equal(low.GetState(target).Completed, high.GetState(target).Completed);
    }

    [Fact]
    public void State_is_independent_for_each_z_layer()
    {
        ExcavationWorkCoordinator coordinator = new ExcavationWorkCoordinator();
        CellId cell = new CellId(3, 3);
        ExcavationWorkTarget upper = new ExcavationWorkTarget(cell, 0);
        ExcavationWorkTarget lower = new ExcavationWorkTarget(cell, 1);
        EntityId worker = Id(5);

        coordinator.Assign(worker, upper, ExcavationApproachSide.Above, miningSkill: 50);
        coordinator.ApplyWork(worker);

        Assert.NotEqual(ExcavationQuarter.None, coordinator.GetState(upper).Completed);
        Assert.Equal(ExcavationQuarter.None, coordinator.GetState(lower).Completed);
    }

    [Fact]
    public void Cancel_and_reassignment_preserve_completed_quarters()
    {
        ExcavationWorkCoordinator coordinator = new ExcavationWorkCoordinator();
        ExcavationWorkTarget target = new ExcavationWorkTarget(new CellId(6, 4), 0);
        EntityId first = Id(6);
        EntityId second = Id(7);
        coordinator.Assign(first, target, ExcavationApproachSide.Left, miningSkill: 21);
        coordinator.ApplyWork(first);
        ExcavationQuarter completed = coordinator.GetState(target).Completed;

        Assert.True(coordinator.Cancel(first));
        coordinator.Assign(second, target, ExcavationApproachSide.Right, miningSkill: 21);

        Assert.Equal(completed, coordinator.GetState(target).Completed);
    }

    [Fact]
    public void Completing_cell_cancels_all_assignments_for_target()
    {
        ExcavationWorkCoordinator coordinator = new ExcavationWorkCoordinator();
        ExcavationWorkTarget target = new ExcavationWorkTarget(new CellId(7, 7), 3);
        EntityId first = Id(8);
        EntityId second = Id(9);
        coordinator.Assign(first, target, ExcavationApproachSide.Left, miningSkill: 20);
        coordinator.Assign(second, target, ExcavationApproachSide.Right, miningSkill: 80);

        while (!coordinator.GetState(target).IsComplete)
        {
            if (coordinator.GetAssignment(first) != null)
            {
                coordinator.ApplyWork(first);
            }

            if (coordinator.GetAssignment(second) != null)
            {
                coordinator.ApplyWork(second);
            }
        }

        Assert.Null(coordinator.GetAssignment(first));
        Assert.Null(coordinator.GetAssignment(second));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse($"00000000-0000-0000-0000-{value:000000000000}");
    }

    private static bool IsSingleQuarter(ExcavationQuarter quarters)
    {
        int value = (int)quarters;
        return value != 0 && (value & (value - 1)) == 0;
    }
}

}
