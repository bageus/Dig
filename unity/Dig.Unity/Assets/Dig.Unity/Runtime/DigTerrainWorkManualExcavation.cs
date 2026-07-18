using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private static readonly DomainError NoExcavationFront = new DomainError(
            "unity.excavation.no_reachable_front",
            "The selected excavation group has no reachable front cell.");
        private AssignSpecificJobHandler? _specificAssignment;
        private ReleaseJobAssignmentHandler? _releaseAssignment;
        private ExcavationClusterPlanner? _clusterPlanner;

        internal void InitializeManualExcavation(InMemoryExecutionJournal journal)
        {
            _specificAssignment = new AssignSpecificJobHandler(_jobRepository, journal);
            _releaseAssignment = new ReleaseJobAssignmentHandler(_jobRepository, journal);
            _clusterPlanner = new ExcavationClusterPlanner();
        }

        internal Result AssignExcavationCluster(
            CellId seed,
            string residentId,
            long tick)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            RequireManualExcavationInitialized();
            EntityId agentId = EntityId.Parse(residentId);
            ClearManualGroupForAgent(agentId);
            IReadOnlyList<CellId> cluster = _clusterPlanner!.Select(
                seed,
                CollectDesignatedCells(),
                radius: 4);
            if (cluster.Count == 0)
            {
                return Result.Failure(JobErrors.NotFound);
            }

            Dictionary<CellId, JobSnapshot> jobsByCell = CollectActiveDigJobs();
            List<EntityId> jobIds = new List<EntityId>();
            for (int index = 0; index < cluster.Count; index++)
            {
                if (jobsByCell.TryGetValue(cluster[index], out JobSnapshot? job))
                {
                    jobIds.Add(job.Id);
                }
            }

            if (jobIds.Count == 0)
            {
                return Result.Failure(JobErrors.NotFound);
            }

            for (int index = 0; index < jobIds.Count; index++)
            {
                RemoveJobFromExistingManualGroup(jobIds[index]);
            }

            EntityId groupId = jobIds[0];
            ManualExcavationGroup group = new ManualExcavationGroup(
                groupId,
                agentId,
                jobIds);
            _manualGroups[groupId] = group;
            for (int index = 0; index < jobIds.Count; index++)
            {
                EntityId jobId = jobIds[index];
                _manualGroupByJob[jobId] = groupId;
                _candidateProvider!.SetCandidates(jobId, NoCandidates);
                JobSnapshot? job = _jobRepository.Get().Get(jobId);
                if (job != null
                    && (job.Status == JobStatus.Claimed
                        || job.Status == JobStatus.InProgress))
                {
                    Result released = _releaseAssignment!.Handle(
                        new ReleaseJobAssignmentCommand(jobId, tick));
                    if (released.IsFailure)
                    {
                        ClearManualGroup(group);
                        return released;
                    }
                }
            }

            Result assigned = AssignNextManualExcavation(group, seed, tick);
            if (assigned.IsFailure)
            {
                ClearManualGroup(group);
            }

            return assigned;
        }

        internal Result ContinueManualExcavation(
            EntityId completedJobId,
            EntityId agentId,
            long tick)
        {
            if (!_manualGroupByJob.TryGetValue(completedJobId, out EntityId groupId)
                || !_manualGroups.TryGetValue(groupId, out ManualExcavationGroup? group)
                || group.AgentId != agentId)
            {
                return Result.Success();
            }

            group.Remove(completedJobId);
            _manualGroupByJob.Remove(completedJobId);
            if (group.JobIds.Count == 0)
            {
                _manualGroups.Remove(groupId);
                return Result.Success();
            }

            return AssignNextManualExcavation(group, preferredCell: null, tick);
        }

        private Result AssignNextManualExcavation(
            ManualExcavationGroup group,
            CellId? preferredCell,
            long tick)
        {
            Dictionary<CellId, CellSnapshot> cells = CollectWorldCells();
            JobSnapshot? next = group.JobIds
                .Select(jobId => _jobRepository.Get().Get(jobId))
                .Where(job => job != null
                    && !job.IsTerminal
                    && job.Definition is DigJobDefinition)
                .OrderByDescending(job => Preferred(job!, preferredCell))
                .ThenBy(job => Distance(
                    ((DigJobDefinition)job!.Definition).Target.CellId,
                    preferredCell))
                .ThenBy(job => job!.Id.ToString(), StringComparer.Ordinal)
                .FirstOrDefault(job => IsExcavationFrontier(
                    ((DigJobDefinition)job!.Definition).Target.CellId,
                    cells));
            if (next == null)
            {
                return Result.Failure(NoExcavationFront);
            }

            return _specificAssignment!.Handle(new AssignSpecificJobCommand(
                next.Id,
                group.AgentId,
                tick));
        }

        private void ClearManualGroup(ManualExcavationGroup group)
        {
            EntityId[] jobIds = group.JobIds.ToArray();
            for (int index = 0; index < jobIds.Length; index++)
            {
                _manualGroupByJob.Remove(jobIds[index]);
            }

            _manualGroups.Remove(group.Id);
        }

        private void RequireManualExcavationInitialized()
        {
            if (_specificAssignment == null
                || _releaseAssignment == null
                || _clusterPlanner == null
                || _candidateProvider == null)
            {
                throw new InvalidOperationException(
                    "Manual excavation is not initialized.");
            }
        }
    }
}
