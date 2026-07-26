using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private readonly ExcavationWorkCoordinator _excavationQuarterWork =
            new ExcavationWorkCoordinator();
        private Func<EntityId, int>? _excavationMiningSkill;

        internal void BindExcavationSkillSource(Func<EntityId, int> skillSource)
        {
            _excavationMiningSkill = skillSource
                ?? throw new ArgumentNullException(nameof(skillSource));
        }

        internal ExcavationWorkerAssignment AssignManualQuarterExcavation(
            string residentId,
            CellId targetCell,
            int targetZ,
            CellId residentCell,
            int miningSkill)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            return EnsureExcavationQuarterAssignment(
                EntityId.Parse(residentId),
                new ExcavationWorkTarget(targetCell, targetZ),
                residentCell,
                miningSkill);
        }

        internal ExcavationWorkerAssignment? LoadManualQuarterAssignment(
            string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                return null;
            }

            return _excavationQuarterWork.GetAssignment(EntityId.Parse(residentId));
        }

        internal bool CancelManualQuarterExcavation(string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                return false;
            }

            return _excavationQuarterWork.Cancel(EntityId.Parse(residentId));
        }

        internal IReadOnlyList<ExcavationQuarterCompletion> AdvanceManualQuarterExcavation(
            string residentId,
            long tick,
            ulong worldSeed)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            EntityId workerId = EntityId.Parse(residentId);
            ulong seed = BuildExcavationSeed(worldSeed, tick, workerId);
            return _excavationQuarterWork.ApplySwing(workerId, seed);
        }

        internal IReadOnlyList<ExcavationQuarterCompletion>
            AdvanceReadyManualQuarterExcavations(
                long tick,
                IReadOnlyList<AgentViewModel> agents)
        {
            // Quarter work is advanced by the owning Dig/SpatialDig job at its
            // authoritative PerformWork cadence. Keeping this compatibility hook as a
            // no-op prevents a second Presentation/runtime timer from double-applying
            // swings.
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            return Array.Empty<ExcavationQuarterCompletion>();
        }

        internal ExcavationQuarterState LoadExcavationQuarterState(
            CellId targetCell,
            int targetZ)
        {
            return _excavationQuarterWork.GetState(
                new ExcavationWorkTarget(targetCell, targetZ));
        }

        internal IReadOnlyList<ExcavationQuarterProgressSnapshot>
            LoadExcavationQuarterProgress()
        {
            return _excavationQuarterWork.GetProgress();
        }

        private bool AdvanceExcavationQuarterWork(
            EntityId workerId,
            ExcavationWorkTarget target,
            CellId residentCell,
            long tick)
        {
            int skill = ResolveExcavationMiningSkill(workerId);
            EnsureExcavationQuarterAssignment(
                workerId,
                target,
                residentCell,
                skill);
            ulong seed = BuildExcavationSeed(
                unchecked((ulong)(uint)_worldSession.MiningOutputWorldSeed),
                tick,
                workerId);
            _excavationQuarterWork.ApplySwing(workerId, seed);
            return _excavationQuarterWork.GetState(target).IsComplete;
        }

        private ExcavationWorkerAssignment EnsureExcavationQuarterAssignment(
            EntityId workerId,
            ExcavationWorkTarget target,
            CellId residentCell,
            int miningSkill)
        {
            _excavationQuarterWork.CancelAssignmentsExcept(target, workerId);
            ExcavationWorkerAssignment? existing =
                _excavationQuarterWork.GetAssignment(workerId);
            if (existing != null && existing.Target.Equals(target))
            {
                return existing;
            }

            ExcavationApproachSide approach = ResolveExcavationApproach(
                residentCell,
                target.CellId);
            return _excavationQuarterWork.Assign(
                workerId,
                target,
                approach,
                Math.Max(0, Math.Min(100, miningSkill)));
        }

        private int ResolveExcavationMiningSkill(EntityId workerId)
        {
            int value = _excavationMiningSkill?.Invoke(workerId) ?? 0;
            return Math.Max(0, Math.Min(100, value));
        }

        private void CompleteExcavationQuarterTarget(CellId target)
        {
            _excavationQuarterWork.Remove(
                new ExcavationWorkTarget(target, target.Z));
        }

        private static ExcavationApproachSide ResolveExcavationApproach(
            CellId residentCell,
            CellId targetCell)
        {
            int dx = residentCell.X - targetCell.X;
            int dy = residentCell.Y - targetCell.Y;
            if (Math.Abs(dx) >= Math.Abs(dy))
            {
                return dx < 0
                    ? ExcavationApproachSide.Left
                    : ExcavationApproachSide.Right;
            }

            return dy < 0
                ? ExcavationApproachSide.Below
                : ExcavationApproachSide.Above;
        }

        private static ulong BuildExcavationSeed(
            ulong worldSeed,
            long tick,
            EntityId workerId)
        {
            unchecked
            {
                ulong value = worldSeed ^ (ulong)tick;
                string text = workerId.ToString();
                for (int index = 0; index < text.Length; index++)
                {
                    value ^= text[index];
                    value *= 1099511628211UL;
                }

                return value;
            }
        }
    }
}
