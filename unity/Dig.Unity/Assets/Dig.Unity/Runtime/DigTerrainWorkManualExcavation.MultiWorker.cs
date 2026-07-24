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
            HashSet<EntityId> selected = new HashSet<EntityId>(agents);
            JobSnapshot[] jobs = cluster
                .Where(jobsByCell.ContainsKey)
                .Select(cell => jobsByCell[cell])
                .Where(job => !IsOwnedByUnselectedResident(job, selected))
                .Distinct()
                .ToArray();
            if (jobs.Length == 0)
            {
                return Result.Failure(JobErrors.NotFound);
            }

            Result released = ReleaseAssignmentsForAgents(selected, tick);
            if (released.IsFailure)
            {
                return released;
            }

            for (int index = 0; index < agents.Length; index++)
            {
                ClearManualGroupForAgent(agents[index]);
                CancelManualQuarterExcavation(agents[index].ToString());
            }

            for (int index = 0; index < jobs.Length; index++)
            {
                RemoveJobFromExistingManualGroup(jobs[index].Id);
            }

            List<ManualExcavationGroup> created =
                CreateManualExcavationGroups(agents, jobs);
            for (int index = 0; index < created.Count; index++)
            {
                ManualExcavationGroup group = created[index];
                RegisterManualExcavationGroup(group);
                Result assigned = AssignNextManualExcavation(
                    group,
                    preferredCell: seed,
                    tick);
                if (assigned.IsFailure && !IsWaitingForExcavationFront(assigned))
                {
                    for (int rollback = 0; rollback < created.Count; rollback++)
                    {
                        ClearManualGroup(created[rollback]);
                    }

                    return assigned;
                }
            }

            return Result.Success();
        }

        private List<ManualExcavationGroup> CreateManualExcavationGroups(
            IReadOnlyList<EntityId> agents,
            IReadOnlyList<JobSnapshot> jobs)
        {
            List<EntityId>[] buckets = new List<EntityId>[agents.Count];
            for (int index = 0; index < buckets.Length; index++)
            {
                buckets[index] = new List<EntityId>();
            }

            for (int index = 0; index < jobs.Count; index++)
            {
                buckets[index % buckets.Length].Add(jobs[index].Id);
            }

            List<ManualExcavationGroup> groups =
                new List<ManualExcavationGroup>(agents.Count);
            for (int index = 0; index < agents.Count; index++)
            {
                if (buckets[index].Count == 0)
                {
                    continue;
                }

                groups.Add(new ManualExcavationGroup(
                    buckets[index][0],
                    agents[index],
                    buckets[index]));
            }

            return groups;
        }

        private void RegisterManualExcavationGroup(ManualExcavationGroup group)
        {
            _manualGroups[group.Id] = group;
            for (int index = 0; index < group.JobIds.Count; index++)
            {
                EntityId jobId = group.JobIds[index];
                _manualGroupByJob[jobId] = group.Id;
                _candidateProvider!.SetCandidates(jobId, NoCandidates);
            }
        }

        private static bool IsOwnedByUnselectedResident(
            JobSnapshot job,
            ISet<EntityId> selected)
        {
            return (job.Status == JobStatus.Claimed || job.Status == JobStatus.InProgress)
                && job.AssignedAgentId.HasValue
                && !selected.Contains(job.AssignedAgentId.Value);
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

            switch (workerIndex % 4)
            {
                case 0:
                    return new CellId(target.X - 1, target.Y);
                case 1:
                    return new CellId(target.X + 1, target.Y);
                case 2:
                    return new CellId(target.X, target.Y + 1);
                default:
                    return new CellId(target.X, target.Y - 1);
            }
        }

        private int ResolveManualMiningSkill(EntityId agentId)
        {
            int skill = _manualExcavationMiningSkill?.Invoke(agentId) ?? 0;
            return Math.Max(0, Math.Min(100, skill));
        }
    }
}
