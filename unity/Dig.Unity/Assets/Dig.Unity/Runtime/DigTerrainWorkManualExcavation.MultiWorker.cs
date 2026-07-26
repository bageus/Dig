using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private Func<EntityId, CellId?>? _manualExcavationResidentCell;

        internal void BindManualExcavationResidentState(
            Func<EntityId, CellId?> residentCell,
            Func<EntityId, int> miningSkill)
        {
            _manualExcavationResidentCell = residentCell
                ?? throw new ArgumentNullException(nameof(residentCell));
            _ = miningSkill ?? throw new ArgumentNullException(nameof(miningSkill));
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
            EntityId[] agents = ParseDirectExcavationAgents(residentIds);
            IReadOnlyCollection<CellId> designated = CollectDesignatedCells();
            IReadOnlyList<CellId> cluster = _clusterPlanner!.Select(
                seed,
                designated,
                CollectTemplateRoomGroups(designated));
            if (cluster.Count == 0)
            {
                return Result.Failure(JobErrors.NotFound);
            }

            HashSet<EntityId> selectedAgents = new HashSet<EntityId>(agents);
            Dictionary<CellId, CellSnapshot> worldCells = CollectWorldCells();
            Dictionary<CellId, JobSnapshot> jobsByCell = CollectActiveDigJobs();
            JobSnapshot[] jobs = cluster
                .Where(jobsByCell.ContainsKey)
                .Select(cell => jobsByCell[cell])
                .Where(job => IsExcavationFrontier(
                    ((DigJobDefinition)job.Definition).Target.CellId,
                    worldCells))
                .Where(job => !IsOwnedByUnselectedResident(job, selectedAgents))
                .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
                .ToArray();
            if (jobs.Length == 0)
            {
                return Result.Failure(JobErrors.NotFound);
            }

            Result released = ReleaseAssignmentsForAgents(selectedAgents, tick);
            if (released.IsFailure)
            {
                return released;
            }

            for (int index = 0; index < agents.Length; index++)
            {
                CancelManualQuarterExcavation(agents[index].ToString());
            }

            Result<NavigationSnapshot> navigation = LoadDirectAssignmentNavigation();
            if (navigation.IsFailure)
            {
                return Result.Failure(navigation.Error!);
            }

            DirectJobWorker[] workers = agents
                .Select((agentId, index) => new DirectJobWorker(
                    agentId,
                    ResolveDirectResidentCell(agentId, seed, index)))
                .ToArray();
            Result<DirectJobAssignmentPlan> planned = _directAssignmentPlanner!.Plan(
                workers,
                jobs,
                navigation.Value);
            if (planned.IsFailure)
            {
                return Result.Failure(planned.Error!);
            }

            if (planned.Value.Assignments.Count == 0)
            {
                return Result.Failure(NoExcavationFront);
            }

            for (int index = 0; index < planned.Value.Assignments.Count; index++)
            {
                DirectJobAssignment assignment = planned.Value.Assignments[index];
                Result assigned = _specificAssignment!.Handle(
                    new AssignSpecificJobCommand(
                        assignment.JobId,
                        assignment.AgentId,
                        tick));
                if (assigned.IsFailure)
                {
                    return assigned;
                }
            }

            return Result.Success();
        }

        private Result<NavigationSnapshot> LoadDirectAssignmentNavigation()
        {
            NavigationMap? map = _navigationRepository.Get(_profile.Id);
            if (map == null)
            {
                return Result<NavigationSnapshot>.Failure(new DomainError(
                    "unity.excavation.navigation_missing",
                    "Navigation map is unavailable for direct excavation."));
            }

            return map.GetSnapshot();
        }

        private static EntityId[] ParseDirectExcavationAgents(
            IReadOnlyList<string> residentIds)
        {
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

            return agents;
        }

        private static bool IsOwnedByUnselectedResident(
            JobSnapshot job,
            ISet<EntityId> selected)
        {
            return (job.Status == JobStatus.Claimed || job.Status == JobStatus.InProgress)
                && job.AssignedAgentId.HasValue
                && !selected.Contains(job.AssignedAgentId.Value);
        }

        private CellId ResolveDirectResidentCell(
            EntityId agentId,
            CellId target,
            int workerIndex)
        {
            CellId? actual = _manualExcavationResidentCell?.Invoke(agentId);
            if (actual.HasValue)
            {
                return actual.Value;
            }

            return workerIndex % 2 == 0
                ? new CellId(Math.Max(0, target.X - 1), target.Y, target.Z)
                : new CellId(target.X + 1, target.Y, target.Z);
        }
    }
}
