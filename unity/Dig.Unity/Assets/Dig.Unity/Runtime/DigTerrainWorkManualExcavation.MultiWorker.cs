using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
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

            HashSet<EntityId> selectedAgents = new HashSet<EntityId>(agents);
            for (int index = 0; index < agents.Length; index++)
            {
                ClearManualGroupForAgent(agents[index]);
            }

            IReadOnlyList<CellId> cluster = _clusterPlanner!.Select(
                seed,
                CollectDesignatedCells(),
                radius: 4);
            Dictionary<CellId, JobSnapshot> jobsByCell = CollectActiveDigJobs();
            List<JobSnapshot> orderedJobs = new List<JobSnapshot>();
            HashSet<EntityId> seenJobs = new HashSet<EntityId>();
            for (int index = 0; index < cluster.Count; index++)
            {
                if (jobsByCell.TryGetValue(cluster[index], out JobSnapshot? job)
                    && seenJobs.Add(job.Id)
                    && !IsOwnedByUnselectedResident(job, selectedAgents))
                {
                    orderedJobs.Add(job);
                }
            }

            if (orderedJobs.Count == 0)
            {
                return Result.Failure(JobErrors.NotFound);
            }

            for (int index = 0; index < orderedJobs.Count; index++)
            {
                JobSnapshot job = orderedJobs[index];
                RemoveJobFromExistingManualGroup(job.Id);
                if ((job.Status == JobStatus.Claimed || job.Status == JobStatus.InProgress)
                    && job.AssignedAgentId.HasValue
                    && selectedAgents.Contains(job.AssignedAgentId.Value))
                {
                    Result released = _releaseAssignment!.Handle(
                        new ReleaseJobAssignmentCommand(job.Id, tick));
                    if (released.IsFailure)
                    {
                        return released;
                    }
                }

                _candidateProvider!.SetCandidates(job.Id, NoCandidates);
            }

            Dictionary<CellId, CellSnapshot> cells = CollectWorldCells();
            JobSnapshot[] frontiers = orderedJobs
                .Where(job => IsExcavationFrontier(
                    ((DigJobDefinition)job.Definition).Target.CellId,
                    cells))
                .ToArray();
            int workerCount = Math.Min(agents.Length, orderedJobs.Count);
            JobSnapshot[] anchors = frontiers
                .Concat(orderedJobs.Where(job => !frontiers.Contains(job)))
                .Take(workerCount)
                .ToArray();

            List<EntityId>[] buckets = new List<EntityId>[workerCount];
            CellId[] anchorCells = new CellId[workerCount];
            HashSet<EntityId> distributed = new HashSet<EntityId>();
            for (int index = 0; index < workerCount; index++)
            {
                buckets[index] = new List<EntityId> { anchors[index].Id };
                anchorCells[index] = ((DigJobDefinition)anchors[index].Definition)
                    .Target.CellId;
                distributed.Add(anchors[index].Id);
            }

            for (int index = 0; index < orderedJobs.Count; index++)
            {
                JobSnapshot job = orderedJobs[index];
                if (!distributed.Add(job.Id))
                {
                    continue;
                }

                CellId cell = ((DigJobDefinition)job.Definition).Target.CellId;
                buckets[SelectNearestBucket(cell, anchorCells)].Add(job.Id);
            }

            List<ManualExcavationGroup> created = new List<ManualExcavationGroup>();
            for (int index = 0; index < workerCount; index++)
            {
                ManualExcavationGroup group = new ManualExcavationGroup(
                    buckets[index][0],
                    agents[index],
                    buckets[index]);
                RegisterManualGroup(group);
                created.Add(group);
                Result assigned = AssignNextManualExcavation(
                    group,
                    anchorCells[index],
                    tick);
                if (assigned.IsFailure && !IsWaitingForExcavationFront(assigned))
                {
                    RollbackManualGroups(created, tick);
                    return assigned;
                }
            }

            return Result.Success();
        }

        private void RegisterManualGroup(ManualExcavationGroup group)
        {
            _manualGroups[group.Id] = group;
            for (int index = 0; index < group.JobIds.Count; index++)
            {
                _manualGroupByJob[group.JobIds[index]] = group.Id;
            }
        }

        private void RollbackManualGroups(
            IReadOnlyList<ManualExcavationGroup> groups,
            long tick)
        {
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                ManualExcavationGroup group = groups[groupIndex];
                for (int jobIndex = 0; jobIndex < group.JobIds.Count; jobIndex++)
                {
                    JobSnapshot? job = _jobRepository.Get().Get(group.JobIds[jobIndex]);
                    if (job != null
                        && job.AssignedAgentId == group.AgentId
                        && (job.Status == JobStatus.Claimed
                            || job.Status == JobStatus.InProgress))
                    {
                        _releaseAssignment!.Handle(
                            new ReleaseJobAssignmentCommand(job.Id, tick));
                    }
                }

                ClearManualGroup(group);
            }
        }

        private static bool IsOwnedByUnselectedResident(
            JobSnapshot job,
            ISet<EntityId> selectedAgents)
        {
            return (job.Status == JobStatus.Claimed || job.Status == JobStatus.InProgress)
                && job.AssignedAgentId.HasValue
                && !selectedAgents.Contains(job.AssignedAgentId.Value);
        }

        private static int SelectNearestBucket(CellId cell, IReadOnlyList<CellId> anchors)
        {
            int selected = 0;
            int selectedDistance = int.MaxValue;
            for (int index = 0; index < anchors.Count; index++)
            {
                int distance = Math.Abs(cell.X - anchors[index].X)
                    + Math.Abs(cell.Y - anchors[index].Y);
                if (distance < selectedDistance)
                {
                    selected = index;
                    selectedDistance = distance;
                }
            }

            return selected;
        }
    }
}
