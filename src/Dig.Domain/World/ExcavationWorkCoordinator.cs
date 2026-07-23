using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.World
{
    public readonly struct ExcavationWorkTarget : IEquatable<ExcavationWorkTarget>
    {
        public ExcavationWorkTarget(CellId cellId, int z)
        {
            CellId = cellId;
            Z = z;
        }

        public CellId CellId { get; }

        public int Z { get; }

        public bool Equals(ExcavationWorkTarget other)
        {
            return CellId.Equals(other.CellId) && Z == other.Z;
        }

        public override bool Equals(object? obj)
        {
            return obj is ExcavationWorkTarget other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CellId, Z);
        }
    }

    public readonly struct ExcavationQuarterCompletion
    {
        public ExcavationQuarterCompletion(
            ExcavationWorkTarget target,
            ExcavationQuarter quarter,
            EntityId workerId,
            bool completedCell)
        {
            Target = target;
            Quarter = quarter;
            WorkerId = workerId;
            CompletedCell = completedCell;
        }

        public ExcavationWorkTarget Target { get; }

        public ExcavationQuarter Quarter { get; }

        public EntityId WorkerId { get; }

        public bool CompletedCell { get; }
    }

    public sealed class ExcavationWorkerAssignment
    {
        internal ExcavationWorkerAssignment(
            EntityId workerId,
            ExcavationWorkTarget target,
            ExcavationApproachSide approach,
            int miningSkill)
        {
            WorkerId = workerId;
            Target = target;
            Approach = approach;
            MiningSkill = miningSkill;
        }

        public EntityId WorkerId { get; }

        public ExcavationWorkTarget Target { get; }

        public ExcavationApproachSide Approach { get; }

        public int MiningSkill { get; }

        public ExcavationQuarter ReservedQuarters { get; internal set; }
    }

    public sealed class ExcavationWorkCoordinator
    {
        private readonly Dictionary<ExcavationWorkTarget, ExcavationQuarterState> _states =
            new Dictionary<ExcavationWorkTarget, ExcavationQuarterState>();
        private readonly Dictionary<EntityId, ExcavationWorkerAssignment> _assignments =
            new Dictionary<EntityId, ExcavationWorkerAssignment>();
        private readonly ExcavationQuarterPlanner _planner = new ExcavationQuarterPlanner();

        public ExcavationWorkerAssignment Assign(
            EntityId workerId,
            ExcavationWorkTarget target,
            ExcavationApproachSide approach,
            int miningSkill)
        {
            if (miningSkill < 0 || miningSkill > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(miningSkill));
            }

            Cancel(workerId);
            ExcavationWorkerAssignment assignment = new ExcavationWorkerAssignment(
                workerId,
                target,
                approach,
                miningSkill);
            _assignments.Add(workerId, assignment);
            EnsureReservation(assignment, deterministicSeed: 0);
            return assignment;
        }

        public bool Cancel(EntityId workerId)
        {
            return _assignments.Remove(workerId);
        }

        public ExcavationWorkerAssignment? GetAssignment(EntityId workerId)
        {
            return _assignments.TryGetValue(workerId, out ExcavationWorkerAssignment? value)
                ? value
                : null;
        }

        public ExcavationQuarterState GetState(ExcavationWorkTarget target)
        {
            if (!_states.TryGetValue(target, out ExcavationQuarterState? state))
            {
                state = new ExcavationQuarterState();
                _states.Add(target, state);
            }

            return state;
        }

        public IReadOnlyList<ExcavationQuarterCompletion> ApplySwing(
            EntityId workerId,
            ulong deterministicSeed)
        {
            if (!_assignments.TryGetValue(
                    workerId,
                    out ExcavationWorkerAssignment? assignment))
            {
                throw new InvalidOperationException("The worker has no excavation assignment.");
            }

            ExcavationQuarterState state = GetState(assignment.Target);
            if (state.IsComplete)
            {
                Cancel(workerId);
                return Array.Empty<ExcavationQuarterCompletion>();
            }

            EnsureReservation(assignment, deterministicSeed);
            ExcavationQuarter reserved = assignment.ReservedQuarters;
            if (reserved == ExcavationQuarter.None)
            {
                return Array.Empty<ExcavationQuarterCompletion>();
            }

            ExcavationSwingPlan plan = _planner.Plan(
                state,
                assignment.Approach,
                assignment.MiningSkill,
                deterministicSeed,
                reserved: ReservedByOthers(assignment));
            ExcavationQuarter selected = plan.Quarters == ExcavationQuarter.None
                ? reserved
                : plan.Quarters;
            assignment.ReservedQuarters = selected;

            List<ExcavationQuarterCompletion> completed =
                new List<ExcavationQuarterCompletion>();
            foreach (ExcavationQuarter quarter in Enumerate(selected))
            {
                if (state.ApplySwing(quarter, plan.RequiredSwingsPerQuarter))
                {
                    completed.Add(new ExcavationQuarterCompletion(
                        assignment.Target,
                        quarter,
                        workerId,
                        state.IsComplete));
                }
            }

            assignment.ReservedQuarters &= ~state.Completed;
            if (state.IsComplete)
            {
                CancelAssignmentsFor(assignment.Target);
            }
            else if (assignment.ReservedQuarters == ExcavationQuarter.None)
            {
                EnsureReservation(assignment, deterministicSeed + 1UL);
            }

            return completed;
        }

        private void EnsureReservation(
            ExcavationWorkerAssignment assignment,
            ulong deterministicSeed)
        {
            ExcavationQuarterState state = GetState(assignment.Target);
            assignment.ReservedQuarters &= ~state.Completed;
            if (assignment.ReservedQuarters != ExcavationQuarter.None)
            {
                return;
            }

            ExcavationSwingPlan plan = _planner.Plan(
                state,
                assignment.Approach,
                assignment.MiningSkill,
                deterministicSeed,
                ReservedByOthers(assignment));
            assignment.ReservedQuarters = FirstQuarter(plan.Quarters);
        }

        private ExcavationQuarter ReservedByOthers(
            ExcavationWorkerAssignment assignment)
        {
            ExcavationQuarter reserved = ExcavationQuarter.None;
            foreach (ExcavationWorkerAssignment other in _assignments.Values)
            {
                if (!other.WorkerId.Equals(assignment.WorkerId)
                    && other.Target.Equals(assignment.Target))
                {
                    reserved |= other.ReservedQuarters;
                }
            }

            return reserved;
        }

        private void CancelAssignmentsFor(ExcavationWorkTarget target)
        {
            EntityId[] workers = _assignments.Values
                .Where(value => value.Target.Equals(target))
                .Select(value => value.WorkerId)
                .ToArray();
            for (int index = 0; index < workers.Length; index++)
            {
                _assignments.Remove(workers[index]);
            }
        }

        private static ExcavationQuarter FirstQuarter(ExcavationQuarter quarters)
        {
            foreach (ExcavationQuarter quarter in Enumerate(quarters))
            {
                return quarter;
            }

            return ExcavationQuarter.None;
        }

        private static IEnumerable<ExcavationQuarter> Enumerate(
            ExcavationQuarter quarters)
        {
            ExcavationQuarter[] values =
            {
                ExcavationQuarter.UpperLeft,
                ExcavationQuarter.LowerLeft,
                ExcavationQuarter.UpperRight,
                ExcavationQuarter.LowerRight,
            };
            for (int index = 0; index < values.Length; index++)
            {
                if ((quarters & values[index]) != 0)
                {
                    yield return values[index];
                }
            }
        }
    }
}
