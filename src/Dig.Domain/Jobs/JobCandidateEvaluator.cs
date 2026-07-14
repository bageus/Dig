using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Jobs
{

public sealed class JobCandidate
{
    public JobCandidate(EntityId agentId, int skillLevel, int distanceCost, bool isAvailable)
    {
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        if (skillLevel < 0 || skillLevel > 10000)
        {
            throw new ArgumentOutOfRangeException(nameof(skillLevel));
        }

        if (distanceCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceCost));
        }

        AgentId = agentId;
        SkillLevel = skillLevel;
        DistanceCost = distanceCost;
        IsAvailable = isAvailable;
    }

    public EntityId AgentId { get; }

    public int SkillLevel { get; }

    public int DistanceCost { get; }

    public bool IsAvailable { get; }
}

public static class JobCandidateEvaluator
{
    public static Result<EntityId> SelectBest(
        JobSnapshot job,
        IEnumerable<JobCandidate> candidates)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        JobCandidate? best = candidates
            .Where(candidate => candidate.IsAvailable)
            .OrderByDescending(candidate => Score(job, candidate))
            .ThenBy(candidate => candidate.AgentId.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();

        return best is null
            ? Result<EntityId>.Failure(JobErrors.CandidateUnavailable)
            : Result<EntityId>.Success(best.AgentId);
    }

    public static long Score(JobSnapshot job, JobCandidate candidate)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        long priorityScore = checked((long)job.Definition.Priority * 1_000_000L);
        long skillScore = checked((long)candidate.SkillLevel * 100L);
        return checked(priorityScore + skillScore - candidate.DistanceCost);
    }
}
}
