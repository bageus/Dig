using System.Linq;

namespace Dig.Domain.World
{
public sealed partial class ExcavationWorkCoordinator
{
    public void SynchronizeCompleted(
        ExcavationWorkTarget target,
        ExcavationQuarter completed)
    {
        ExcavationQuarterState state = GetState(target);
        state.SynchronizeCompleted(completed);
        foreach (ExcavationWorkerAssignment assignment in _assignments.Values
        .Where(value => value.Target.Equals(target)))
        {
        assignment.ReservedQuarters &= ~completed;
        }

        if (state.IsComplete)
        {
        CancelAssignmentsFor(target);
        }
    }


}
}
