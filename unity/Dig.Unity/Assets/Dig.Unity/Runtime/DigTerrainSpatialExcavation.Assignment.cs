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
        private const int SpatialManualAssignmentRadius = 4;

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
            EntityId[] residents = residentIds
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(EntityId.Parse)
                .Distinct()
                .OrderBy(value => value.ToString(), StringComparer.Ordinal)
                .ToArray();
            if (residents.Length == 0 || residents.Length != residentIds.Count)
            {
                throw new ArgumentException(
                    "Resident ids must be non-empty and unique.",
                    nameof(residentIds));
            }

            HashSet<EntityId> selectedResidents = new HashSet<EntityId>(residents);
            JobSnapshot[] active = LoadActiveSpatialJobs().ToArray();
            JobSnapshot? target = active
                .Where(value => ((SpatialDigJobDefinition)value.Definition)
                    .Target.WorkCell == workCell)
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

            JobSnapshot[] jobs = active
                .Where(value => !IsSpatialJobOwnedByUnselectedResident(
                    value,
                    selectedResidents))
                .Where(value => SpatialDistance(
                    ((SpatialDigJobDefinition)value.Definition).Target.WorkCell,
                    workCell) <= SpatialManualAssignmentRadius)
                .OrderByDescending(value => value.Id == target.Id)
                .ThenBy(value => SpatialDistance(
                    ((SpatialDigJobDefinition)value.Definition).Target.WorkCell,
                    workCell))
                .ThenBy(value => value.Id.ToString(), StringComparer.Ordinal)
                .Take(residents.Length)
                .ToArray();
            if (jobs.Length == 0)
            {
                result = Result.Failure(JobErrors.NotFound);
                return true;
            }

            for (int index = 0; index < residents.Length; index++)
            {
                Result released = ReleaseSpatialResidentAssignment(residents[index], tick);
                if (released.IsFailure)
                {
                    result = released;
                    return true;
                }
            }

            List<(EntityId JobId, EntityId ResidentId)> assigned =
                new List<(EntityId JobId, EntityId ResidentId)>();
            for (int index = 0; index < jobs.Length; index++)
            {
                JobSnapshot? refreshed = _jobRepository.Get().Get(jobs[index].Id);
                if (refreshed == null || refreshed.IsTerminal)
                {
                    continue;
                }

                _candidateProvider!.SetCandidates(refreshed.Id, NoCandidates);
                Result claimed = _specificAssignment!.Handle(
                    new AssignSpecificJobCommand(
                        refreshed.Id,
                        residents[index],
                        tick));
                if (claimed.IsFailure)
                {
                    RollbackSpatialAssignments(assigned, tick);
                    result = claimed;
                    return true;
                }

                assigned.Add((refreshed.Id, residents[index]));
            }

            result = assigned.Count == 0
                ? Result.Failure(JobErrors.NotFound)
                : Result.Success();
            return true;
        }

        private Result ReleaseSpatialResidentAssignment(EntityId residentId, long tick)
        {
            JobSnapshot? current = _jobRepository.Get().GetAll()
                .FirstOrDefault(value => !value.IsTerminal
                    && value.AssignedAgentId == residentId);
            return current == null
                ? Result.Success()
                : _releaseAssignment!.Handle(
                    new ReleaseJobAssignmentCommand(current.Id, tick));
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

        private static int SpatialDistance(CellId left, CellId right)
        {
            return Math.Abs(left.X - right.X)
                + Math.Abs(left.Y - right.Y)
                + Math.Abs(left.Z - right.Z);
        }
    }
}