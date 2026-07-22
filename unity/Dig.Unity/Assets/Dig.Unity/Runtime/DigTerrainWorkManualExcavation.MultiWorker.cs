using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private Func<EntityId, CellId?>? _manualExcavationResidentCell;
        private Func<EntityId, int>? _manualExcavationMiningSkill;

        internal void BindManualExcavationResidentState(
            Func<EntityId, CellId?> residentCell,
            Func<EntityId, int> miningSkill)
        {
            _manualExcavationResidentCell = residentCell
                ?? throw new ArgumentNullException(nameof(residentCell));
            _manualExcavationMiningSkill = miningSkill
                ?? throw new ArgumentNullException(nameof(miningSkill));
        }

        internal Result AssignExcavationClusterToResidents(
            CellId seed,
            IReadOnlyList<string> residentIds,
            long tick)
        {
            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
            }

            RequireManualExcavationInitialized();
            EntityId[] agents = residentIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(EntityId.Parse)
                .Distinct()
                .OrderBy(id => id.ToString(), StringComparer.Ordinal)
                .ToArray();
            if (agents.Length == 0 || agents.Length != residentIds.Count)
            {
                throw new ArgumentException(
                    "Resident ids must be non-empty and unique.",
                    nameof(residentIds));
            }

            IReadOnlyList<CellId> cluster = _clusterPlanner!.Select(
                seed,
                CollectDesignatedCells(),
                radius: 4);
            Dictionary<CellId, JobSnapshot> jobsByCell = CollectActiveDigJobs();
            CellId[] targets = cluster
                .Where(jobsByCell.ContainsKey)
                .Distinct()
                .ToArray();
            if (targets.Length == 0)
            {
                return Result.Failure(JobErrors.NotFound);
            }

            // Forced excavation is deliberately independent from JobSystem ownership.
            // Existing automatic jobs keep their owner, status and candidate list.
            for (int index = 0; index < agents.Length; index++)
            {
                EntityId agentId = agents[index];
                CellId target = targets[index % targets.Length];
                CellId residentCell = ResolveManualResidentCell(agentId, target, index);
                int miningSkill = ResolveManualMiningSkill(agentId);
                AssignManualQuarterExcavation(
                    agentId.ToString(),
                    target,
                    target.Z,
                    residentCell,
                    miningSkill);
            }

            return Result.Success();
        }

        private CellId ResolveManualResidentCell(
            EntityId agentId,
            CellId target,
            int workerIndex)
        {
            CellId? actual = _manualExcavationResidentCell?.Invoke(agentId);
            if (actual.HasValue)
            {
                return actual.Value;
            }

            // The runtime normally supplies the authoritative resident cell. This
            // deterministic fallback keeps tests and headless sessions functional.
            switch (workerIndex % 4)
            {
                case 0:
                    return new CellId(target.X - 1, target.Y, target.Z);
                case 1:
                    return new CellId(target.X + 1, target.Y, target.Z);
                case 2:
                    return new CellId(target.X, target.Y + 1, target.Z);
                default:
                    return new CellId(target.X, target.Y - 1, target.Z);
            }
        }

        private int ResolveManualMiningSkill(EntityId agentId)
        {
            int skill = _manualExcavationMiningSkill?.Invoke(agentId) ?? 0;
            return Math.Max(0, Math.Min(100, skill));
        }
    }
}
