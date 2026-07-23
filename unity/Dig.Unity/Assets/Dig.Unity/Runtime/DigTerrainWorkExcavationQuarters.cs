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

            ExcavationApproachSide approach = ResolveExcavationApproach(
                residentCell,
                targetCell);
            return _excavationQuarterWork.Assign(
                EntityId.Parse(residentId),
                new ExcavationWorkTarget(targetCell, targetZ),
                approach,
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
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            List<ExcavationQuarterCompletion> completed =
                new List<ExcavationQuarterCompletion>();
            for (int index = 0; index < agents.Count; index++)
            {
                AgentViewModel agent = agents[index];
                ExcavationWorkerAssignment? assignment =
                    _excavationQuarterWork.GetAssignment(EntityId.Parse(agent.Id));
                if (assignment == null || !IsAtManualExcavationApproach(agent, assignment.Target))
                {
                    continue;
                }

                IReadOnlyList<ExcavationQuarterCompletion> swing =
                    AdvanceManualQuarterExcavation(agent.Id, tick, worldSeed: 0UL);
                for (int completionIndex = 0;
                    completionIndex < swing.Count;
                    completionIndex++)
                {
                    completed.Add(swing[completionIndex]);
                }
            }

            return completed;
        }

        internal ExcavationQuarterState LoadExcavationQuarterState(
            CellId targetCell,
            int targetZ)
        {
            return _excavationQuarterWork.GetState(
                new ExcavationWorkTarget(targetCell, targetZ));
        }

        private static bool IsAtManualExcavationApproach(
            AgentViewModel agent,
            ExcavationWorkTarget target)
        {
            if (agent.CellZ != target.Z)
            {
                return false;
            }

            int distance = Math.Abs(agent.CellX - target.CellId.X)
                + Math.Abs(agent.CellY - target.CellId.Y);
            return distance == 1;
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
