using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Jobs
{

public sealed partial class JobSystem
{
    public static Result<JobSystem> Restore(
        IEnumerable<JobSnapshot> jobs,
        IEnumerable<ReservationSnapshot> reservations)
    {
        if (jobs is null)
        {
            throw new ArgumentNullException(nameof(jobs));
        }

        if (reservations is null)
        {
            throw new ArgumentNullException(nameof(reservations));
        }

        JobSnapshot[] orderedJobs = jobs
            .OrderBy(item => item.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        ReservationSnapshot[] orderedReservations = reservations
            .OrderBy(item => item.JobId.ToString(), StringComparer.Ordinal)
            .ThenBy(item => item.Key)
            .ToArray();
        JobSystem system = new JobSystem();
        foreach (JobSnapshot snapshot in orderedJobs)
        {
            if (system._jobs.ContainsKey(snapshot.Id))
            {
                return Invalid("The job snapshot contains duplicate job ids.");
            }

            Result<JobState> restored = JobState.Restore(snapshot);
            if (restored.IsFailure)
            {
                return Result<JobSystem>.Failure(restored.Error!);
            }

            system._jobs.Add(snapshot.Id, restored.Value);
        }

        foreach (IGrouping<EntityId, ReservationSnapshot> group in
            orderedReservations.GroupBy(item => item.JobId))
        {
            if (!system._jobs.TryGetValue(group.Key, out JobState? job))
            {
                return Invalid("A reservation references an unknown job.");
            }

            if (!job.AssignedAgentId.HasValue
                || job.CreateSnapshot().IsTerminal)
            {
                return Invalid("Only assigned non-terminal jobs may own reservations.");
            }

            ReservationSnapshot[] values = group.ToArray();
            if (values.Any(item => item.AgentId != job.AssignedAgentId.Value))
            {
                return Invalid("Reservation agent does not match the assigned worker.");
            }

            ReservationKey jobKey = ReservationKey.ForJob(job.Id);
            ReservationKey agentKey = ReservationKey.ForAgent(job.AssignedAgentId.Value);
            if (!values.Any(item => item.Key == jobKey)
                || !values.Any(item => item.Key == agentKey))
            {
                return Invalid("Assigned job reservations must contain job and agent keys.");
            }

            Result reserved = system._reservations.ReserveAll(
                job.Id,
                job.AssignedAgentId.Value,
                values.Select(item => item.Key),
                values.Min(item => item.CreatedTick));
            if (reserved.IsFailure)
            {
                return Result<JobSystem>.Failure(reserved.Error!);
            }
        }

        foreach (JobState job in system._jobs.Values)
        {
            bool assigned = job.AssignedAgentId.HasValue;
            bool hasReservations = orderedReservations.Any(item => item.JobId == job.Id);
            if (assigned != hasReservations)
            {
                return Invalid("Assigned job and reservation state are inconsistent.");
            }
        }

        return Result<JobSystem>.Success(system);
    }

    private static Result<JobSystem> Invalid(string message)
    {
        return Result<JobSystem>.Failure(new DomainError(
            "jobs.restore.invalid_system",
            message));
    }
}
}
