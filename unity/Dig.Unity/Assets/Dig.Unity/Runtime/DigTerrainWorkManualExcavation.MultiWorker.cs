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
                    && seenJobs.Add(job.Id))
                {
                    orderedJobs.Add(job);
                }
            }

            if (orderedJobs.Count == 0)
            {
                return Result.Failure(JobErrors.NotFound);
            }

            List<JobSnapshot> assignableJobs = new List<JobSnapshot>();
            for (int index = 0; index < orderedJobs.Count; index++)
            {
                JobSnapshot job = orderedJobs[index];
                bool active = job.Status == JobStatus.Claimed
                    || job.Status == JobStatus.InProgress;
                bool ownedBySelected = active
                    && job.AssignedAgentId.HasValue
                    && selectedAgents.Contains(job.AssignedAgentId.Value);
                if (active && !ownedBySelected)
                {
                    continue;
                }

                RemoveJobFromExistingManualGroup(job.Id);
                if (ownedBySelected)
                {
                    Result released = _releaseAssignment!.Handle(
                        new ReleaseJobAssignmentCommand(job.Id, tick));
                    if (released.IsFailure)
                    {
                        return released;
                    }
                }

                _candidateProvider!.SetCandidates(job.Id, NoCandidates);
                assignableJobs.Add(job);
            }

            Dictionary<CellId, CellSnapshot> cells = CollectWorldCells();
            JobSnapshot[] frontiers = assignableJobs
                .Where(job => IsExcavationFrontier(
                    ((DigJobDefinition)job.Definition).Target.CellId,
                    cells))
                .ToArray();
            int workerCount = Math.Min(agents.Length, frontiers.Length);
            if (workerCount == 0)
            {
                return Result.Failure(NoExcavationFront);
            }

            List<EntityId>[] buckets = new List<EntityId>[workerCount];
            CellId[] anchors = new CellId[workerCount];
            HashSet<EntityId> distributed = new HashSet<EntityId>();
            for (int index = 0; index < workerCount; index++)
            {
                buckets[index] = new List<EntityId> { frontiers[index].Id };
                anchors[index] = ((DigJobDefinition)frontiers[index].Definition)
                    .Target.CellId;
                distributed.Add(frontiers[index].Id);
            }

            for (int index = 0; index < assignableJobs.Count; index++)
            {
                JobSnapshot job = assignableJobs[index];
                if (!distributed.Add(job.Id))
                {
                    continue;
                }

                CellId cell = ((DigJobDefinition)job.Definition).Target.CellId;
                buckets[SelectNearestBucket(cell, anchors)].Add(job.Id);
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
                    anchors[index],
                    tick);
                if (assigned.IsFailure)
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