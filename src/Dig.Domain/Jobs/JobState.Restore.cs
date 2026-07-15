using System;
using Dig.Domain.Core;

namespace Dig.Domain.Jobs
{

public sealed partial class JobState
{
    internal static Result<JobState> Restore(JobSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (snapshot.RetryCount < 0
            || snapshot.NextRetryTick < 0
            || snapshot.Version < 0)
        {
            return Invalid("Job retry or version metadata cannot be negative.");
        }

        bool requiresAgent = snapshot.Status == JobStatus.Claimed
            || snapshot.Status == JobStatus.InProgress;
        if (requiresAgent != snapshot.AssignedAgentId.HasValue)
        {
            return Invalid("Assigned agent state is inconsistent with the job status.");
        }

        int stageIndex = -1;
        if (snapshot.Status == JobStatus.InProgress)
        {
            for (int index = 0; index < snapshot.Definition.Stages.Count; index++)
            {
                if (snapshot.Definition.Stages[index] == snapshot.Stage)
                {
                    stageIndex = index;
                    break;
                }
            }

            if (stageIndex < 0)
            {
                return Invalid("The saved stage is not part of the job definition.");
            }
        }
        else if (snapshot.Stage != JobStageKind.None)
        {
            return Invalid("Only an in-progress job may have a concrete stage.");
        }

        bool requiresReason = snapshot.Status == JobStatus.Blocked
            || snapshot.Status == JobStatus.Cancelled
            || snapshot.Status == JobStatus.Failed;
        if (requiresReason != (snapshot.Reason is not null))
        {
            return Invalid("Job block reason is inconsistent with the saved status.");
        }

        JobState restored = new JobState(snapshot.Definition)
        {
            Status = snapshot.Status,
            Stage = snapshot.Stage,
            AssignedAgentId = snapshot.AssignedAgentId,
            RetryCount = snapshot.RetryCount,
            NextRetryTick = snapshot.NextRetryTick,
            Version = snapshot.Version,
            Reason = snapshot.Reason,
            _stageIndex = stageIndex,
        };
        return Result<JobState>.Success(restored);
    }

    private static Result<JobState> Invalid(string message)
    {
        return Result<JobState>.Failure(new DomainError(
            "jobs.restore.invalid_snapshot",
            message));
    }
}
}
