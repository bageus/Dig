using System;
using System.Collections.Generic;
using Dig.Domain.Core;

namespace Dig.Domain.Jobs
{

public sealed partial class JobSystem
{
    public Result CanClaim(
        EntityId jobId,
        EntityId agentId,
        EntityId? toolStackId,
        long tick)
    {
        ValidateTick(tick);
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        if (toolStackId.HasValue && toolStackId.Value.IsEmpty)
        {
            throw new ArgumentException("Tool stack id cannot be empty.", nameof(toolStackId));
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

        foreach (ReservationKey key in CreateClaimKeys(job, agentId, toolStackId))
        {
            ReservationSnapshot? existing = _reservations.Find(key);
            if (existing is null || existing.JobId == jobId)
            {
                continue;
            }

            return Result.Failure(
                key.Kind == ReservationKind.Agent
                    ? JobErrors.AgentUnavailable
                    : JobErrors.ReservationConflict);
        }

        return Result.Success();
    }

    public Result Claim(
        EntityId jobId,
        EntityId agentId,
        EntityId? toolStackId,
        long tick)
    {
        Result canClaim = CanClaim(jobId, agentId, toolStackId, tick);
        if (canClaim.IsFailure)
        {
            return canClaim;
        }

        JobState job = FindState(jobId)!;
        Result reserved = _reservations.ReserveAll(
            jobId,
            agentId,
            CreateClaimKeys(job, agentId, toolStackId),
            tick);
        if (reserved.IsFailure)
        {
            return reserved;
        }

        JobStatus previous = job.Status;
        job.Claim(agentId);
        RaiseStatusChanged(tick, job, previous, null);
        return Result.Success();
    }

    private static IReadOnlyCollection<ReservationKey> CreateClaimKeys(
        JobState job,
        EntityId agentId,
        EntityId? toolStackId)
    {
        List<ReservationKey> keys = new List<ReservationKey>
        {
            ReservationKey.ForJob(job.Id),
            ReservationKey.ForAgent(agentId),
        };
        if (toolStackId.HasValue)
        {
            keys.Add(ReservationKey.ForTool(toolStackId.Value));
        }

        keys.AddRange(job.Definition.CreateReservationKeys());
        return keys;
    }
}
}
