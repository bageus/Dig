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
        private bool TryAssignSpatialExcavationGroup(
            CellId workCell,
            IReadOnlyList<string> residentIds,
            long tick,
            out Result result)
        {
            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
            }

            RequireSpatialExcavationInitialized();
            RequireManualExcavationInitialized();
            EntityId[] residents = ParseDirectExcavationAgents(residentIds);
            HashSet<EntityId> selectedResidents = new HashSet<EntityId>(residents);
            JobSnapshot[] active = LoadActiveSpatialJobs().ToArray();
            JobSnapshot? target = active
                .Where(value => IsSpatialSourceCell(
                    ((SpatialDigJobDefinition)value.Definition).Target,
                    workCell))
                .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
                .FirstOrDefault();
            if (target == null)
            {
                result = Result.Success();
                return false;
            }

            if (IsSpatialJobOwnedByUnselectedResident(target, selectedResidents))
            {
                result = Result.Failure(JobErrors.ReservationConflict);
                return true;
            }

            CellId[] designated = active
                .Select(value => ((SpatialDigJobDefinition)value.Definition)
                    .Target.TargetCell)
                .Distinct()
                .OrderBy(cell => cell)
                .ToArray();
            CellId seed = ((SpatialDigJobDefinition)target.Definition)
                .Target.TargetCell;
            HashSet<CellId> cluster = new HashSet<CellId>(_clusterPlanner!.Select(
                seed,
                designated,
                CollectTemplateRoomGroups(designated)));
            JobSnapshot[] jobs = active
                .Where(value => cluster.Contains(
                    ((SpatialDigJobDefinition)value.Definition).Target.TargetCell))
                .Where(value => !IsSpatialJobOwnedByUnselectedResident(
                    value,
                    selectedResidents))
                .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
                .ToArray();
            if (jobs.Length == 0)
            {
                result = Result.Failure(JobErrors.NotFound);
                return true;
            }

            Result released = ReleaseAssignmentsForAgents(selectedResidents, tick);
            if (released.IsFailure)
            {
                result = released;
                return true;
            }

            for (int index = 0; index < residents.Length; index++)
            {
                CancelManualQuarterExcavation(residents[index].ToString());
            }

            Result<NavigationSnapshot> navigation = LoadDirectAssignmentNavigation();
            if (navigation.IsFailure)
            {
                result = Result.Failure(navigation.Error!);
                return true;
            }

            DirectJobWorker[] workers = residents
                .Select((residentId, index) => new DirectJobWorker(
                    residentId,
                    ResolveDirectResidentCell(residentId, workCell, index)))
                .ToArray();
            Result<DirectJobAssignmentPlan> planned =
                _directSpatialAssignmentPlanner!.Plan(
                    workers,
                    jobs,
                    navigation.Value);
            if (planned.IsFailure)
            {
                result = Result.Failure(planned.Error!);
                return true;
            }

            if (planned.Value.Assignments.Count == 0)
            {
                result = Result.Failure(NoExcavationFront);
                return true;
            }

            List<(EntityId JobId, EntityId ResidentId)> assigned =
                new List<(EntityId JobId, EntityId ResidentId)>();
            for (int index = 0; index < planned.Value.Assignments.Count; index++)
            {
                DirectJobAssignment assignment = planned.Value.Assignments[index];
                Result claimed = _specificAssignment!.Handle(
                    new AssignSpecificJobCommand(
                        assignment.JobId,
                        assignment.AgentId,
                        tick));
                if (claimed.IsFailure)
                {
                    RollbackSpatialAssignments(assigned, tick);
                    result = claimed;
                    return true;
                }

                assigned.Add((assignment.JobId, assignment.AgentId));
            }

            result = Result.Success();
            return true;
        }


        private static bool IsSpatialSourceCell(
            SpatialDigJobTarget target,
            CellId source)
        {
            return target.TargetCell.X == source.X
                && target.TargetCell.Y == source.Y
                && target.TargetCell.Z == source.Z + 1;
        }

        private void RollbackSpatialAssignments(
            IReadOnlyList<(EntityId JobId, EntityId ResidentId)> assignments,
            long tick)
        {
            for (int index = 0; index < assignments.Count; index++)
            {
                JobSnapshot? job = _jobRepository.Get().Get(assignments[index].JobId);
                if (job != null
                    && job.AssignedAgentId == assignments[index].ResidentId
                    && (job.Status == JobStatus.Claimed
                        || job.Status == JobStatus.InProgress))
                {
                    _releaseAssignment!.Handle(
                        new ReleaseJobAssignmentCommand(job.Id, tick));
                }
            }
        }

        private static bool IsSpatialJobOwnedByUnselectedResident(
            JobSnapshot job,
            ISet<EntityId> selectedResidents)
        {
            return (job.Status == JobStatus.Claimed || job.Status == JobStatus.InProgress)
                && job.AssignedAgentId.HasValue
                && !selectedResidents.Contains(job.AssignedAgentId.Value);
        }
    }
}
