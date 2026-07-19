using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Jobs
{

public enum JobToolReadiness
{
    NotApplicable = 0,
    Unavailable = 1,
    SwitchAvailable = 2,
    Equipped = 3,
}

public sealed class JobCandidate
{
    public JobCandidate(
        EntityId agentId,
        int skillLevel,
        int distanceCost,
        bool isAvailable,
        JobToolReadiness toolReadiness = JobToolReadiness.NotApplicable,
        EntityId? toolStackId = null)
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

        if (!Enum.IsDefined(typeof(JobToolReadiness), toolReadiness))
        {
            throw new ArgumentOutOfRangeException(nameof(toolReadiness));
        }

        bool needsStack = toolReadiness == JobToolReadiness.Equipped
            || toolReadiness == JobToolReadiness.SwitchAvailable;
        if (needsStack != toolStackId.HasValue
            || (toolStackId.HasValue && toolStackId.Value.IsEmpty))
        {
            throw new ArgumentException(
                "Equipped and switchable candidates require a valid tool stack id.",
                nameof(toolStackId));
        }

        AgentId = agentId;
        SkillLevel = skillLevel;
        DistanceCost = distanceCost;
        IsAvailable = isAvailable;
        ToolReadiness = toolReadiness;
        ToolStackId = toolStackId;
    }

    public EntityId AgentId { get; }

    public int SkillLevel { get; }

    public int DistanceCost { get; }

    public bool IsAvailable { get; }

    public JobToolReadiness ToolReadiness { get; }

    public EntityId? ToolStackId { get; }

    public JobCandidate WithToolReadiness(
        JobToolReadiness readiness,
        EntityId? toolStackId = null)
    {
        return new JobCandidate(
            AgentId,
            SkillLevel,
            DistanceCost,
            IsAvailable,
            readiness,
            toolStackId);
    }

    public JobCandidate WithDistanceCost(int distanceCost)
    {
        return new JobCandidate(
            AgentId,
            SkillLevel,
            distanceCost,
            IsAvailable,
            ToolReadiness,
            ToolStackId);
    }
}

public static class JobCandidateEvaluator
{
    private const long EquippedToolScore = 10_000_000_000L;
    private const long SwitchableToolScore = 5_000_000_000L;

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
        long toolScore = candidate.ToolReadiness switch
        {
            JobToolReadiness.Equipped => EquippedToolScore,
            JobToolReadiness.SwitchAvailable => SwitchableToolScore,
            _ => 0L,
        };
        long skillScore = checked((long)candidate.SkillLevel * 100L);
        return checked(priorityScore + toolScore + skillScore - candidate.DistanceCost);
    }
}

}