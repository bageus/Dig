using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.Jobs;

public sealed class JobSystem : AggregateRoot
{
    private readonly Dictionary<EntityId, JobState> _jobs =
        new Dictionary<EntityId, JobState>();
    private readonly ReservationLedger _reservations = new ReservationLedger();

    public Result Add(JobDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (_jobs.ContainsKey(definition.Id))
        {
            return Result.Failure(JobErrors.AlreadyExists);
        }

        _jobs.Add(definition.Id, new JobState(definition));
        return Result.Success();
    }

    public Result MakeAvailable(EntityId jobId, long tick)
    {
        ValidateTick(tick);
        JobState? job = FindState(jobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.Status != JobStatus.Created)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        if (!DependenciesCompleted(job.Definition))
        {
            return Result.Failure(JobErrors.DependenciesIncomplete);
        }

        JobStatus previous = job.Status;
        job.MakeAvailable();
        RaiseStatusChanged(tick, job, previous, null);
        return Result.Success();
    }

    public Result Claim(EntityId jobId, EntityId agentId, long tick)
    {
        ValidateTick(tick);
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        JobState? job = FindState(jobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.Status != JobStatus.Available)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        List<ReservationKey> keys = new List<ReservationKey>
        {
            ReservationKey.ForJob(jobId),
            ReservationKey.ForAgent(agentId),
        };
        keys.AddRange(job.Definition.CreateReservationKeys());

        Result reserved = _reservations.ReserveAll(jobId, agentId, keys, tick);
        if (reserved.IsFailure)
        {
            return reserved;
        }

        JobStatus previous = job.Status;
        job.Claim(agentId);
        RaiseStatusChanged(tick, job, previous, null);
        return Result.Success();
    }

    public Result Start(EntityId jobId, long tick)
    {
        ValidateTick(tick);
        JobState? job = FindState(jobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.Status != JobStatus.Claimed)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        JobStatus previous = job.Status;
        job.Start();
        RaiseStatusChanged(tick, job, previous, null);
        return Result.Success();
    }

    public Result AdvanceStage(EntityId jobId, long tick)
    {
        ValidateTick(tick);
        JobState? job = FindState(jobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.Status != JobStatus.InProgress)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        JobStatus previous = job.Status;
        bool completed = job.AdvanceStage();
        if (completed)
        {
            ReleaseReservations(tick, jobId);
            RaiseStatusChanged(tick, job, previous, null);
        }

        return Result.Success();
    }

    public Result Complete(EntityId jobId, long tick)
    {
        ValidateTick(tick);
        JobState? job = FindState(jobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.Status != JobStatus.Claimed && job.Status != JobStatus.InProgress)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        JobStatus previous = job.Status;
        job.Complete();
        ReleaseReservations(tick, jobId);
        RaiseStatusChanged(tick, job, previous, null);
        return Result.Success();
    }

    public Result Block(EntityId jobId, JobBlockReason reason, long tick)
    {
        ValidateTick(tick);
        if (reason is null)
        {
            throw new ArgumentNullException(nameof(reason));
        }

        JobState? job = FindState(jobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.Status != JobStatus.Claimed && job.Status != JobStatus.InProgress)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        JobStatus previous = job.Status;
        ReleaseReservations(tick, jobId);
        if (job.RetryCount >= job.Definition.RetryPolicy.MaximumRetries)
        {
            JobBlockReason exhausted = new JobBlockReason(
                "retry_exhausted",
                $"Retry limit reached after: {reason.Message}");
            job.Fail(exhausted);
            RaiseStatusChanged(tick, job, previous, exhausted.Code);
            return Result.Failure(JobErrors.RetryLimitReached);
        }

        long nextRetryTick = checked(tick + job.Definition.RetryPolicy.RetryDelayTicks);
        job.Block(reason, nextRetryTick);
        RaiseStatusChanged(tick, job, previous, reason.Code);
        return Result.Success();
    }

    public Result Retry(EntityId jobId, long tick)
    {
        ValidateTick(tick);
        JobState? job = FindState(jobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.Status != JobStatus.Blocked)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        if (tick < job.NextRetryTick)
        {
            return Result.Failure(JobErrors.RetryNotReady);
        }

        if (!DependenciesCompleted(job.Definition))
        {
            return Result.Failure(JobErrors.DependenciesIncomplete);
        }

        JobStatus previous = job.Status;
        job.MakeAvailable();
        RaiseStatusChanged(tick, job, previous, null);
        return Result.Success();
    }

    public Result Cancel(EntityId jobId, JobBlockReason reason, long tick)
    {
        return End(jobId, reason, tick, JobStatus.Cancelled);
    }

    public Result Fail(EntityId jobId, JobBlockReason reason, long tick)
    {
        return End(jobId, reason, tick, JobStatus.Failed);
    }

    public JobSnapshot? Get(EntityId jobId)
    {
        return FindState(jobId)?.CreateSnapshot();
    }

    public IReadOnlyList<JobSnapshot> GetAll()
    {
        JobSnapshot[] jobs = _jobs.Values
            .Select(job => job.CreateSnapshot())
            .OrderBy(job => job.Definition.CreatedTick)
            .ThenByDescending(job => job.Definition.Priority)
            .ThenBy(job => job.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<JobSnapshot>(jobs);
    }

    public IReadOnlyList<ReservationSnapshot> GetReservations()
    {
        return _reservations.CreateSnapshot();
    }

    private Result End(
        EntityId jobId,
        JobBlockReason reason,
        long tick,
        JobStatus terminalStatus)
    {
        ValidateTick(tick);
        if (reason is null)
        {
            throw new ArgumentNullException(nameof(reason));
        }

        JobState? job = FindState(jobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.CreateSnapshot().IsTerminal)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        JobStatus previous = job.Status;
        if (terminalStatus == JobStatus.Cancelled)
        {
            job.Cancel(reason);
        }
        else
        {
            job.Fail(reason);
        }

        ReleaseReservations(tick, jobId);
        RaiseStatusChanged(tick, job, previous, reason.Code);
        return Result.Success();
    }

    private bool DependenciesCompleted(JobDefinition definition)
    {
        return definition.Dependencies.All(
            dependency => _jobs.TryGetValue(dependency, out JobState? job)
                && job.Status == JobStatus.Completed);
    }

    private JobState? FindState(EntityId jobId)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        return _jobs.TryGetValue(jobId, out JobState? job) ? job : null;
    }

    private void ReleaseReservations(long tick, EntityId jobId)
    {
        int released = _reservations.ReleaseForJob(jobId);
        if (released > 0)
        {
            Raise(new JobReservationsReleased(tick, jobId, released));
        }
    }

    private void RaiseStatusChanged(
        long tick,
        JobState job,
        JobStatus previousStatus,
        string? reasonCode)
    {
        Raise(new JobStatusChanged(
            tick,
            job.Id,
            previousStatus,
            job.Status,
            job.AssignedAgentId,
            reasonCode));
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}
