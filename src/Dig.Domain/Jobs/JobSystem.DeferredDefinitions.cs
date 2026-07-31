using Dig.Domain.Core;

namespace Dig.Domain.Jobs
{

public sealed partial class JobSystem
{
    public bool AreDependenciesCompleted(EntityId jobId)
    {
        JobState? job = FindState(jobId);
        return job != null && DependenciesCompleted(job.Definition);
    }

    public Result ResolveCreatedDefinition(
        EntityId jobId,
        JobDefinition definition,
        long tick)
    {
        ValidateTick(tick);
        JobState? job = FindState(jobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (definition is null || definition.Id != jobId
            || job.Status != JobStatus.Created)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        if (!DependenciesCompleted(definition))
        {
            return Result.Failure(JobErrors.DependenciesIncomplete);
        }

        job.ResolveDefinition(definition);
        return Result.Success();
    }

}

}
