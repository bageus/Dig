using Dig.Domain.Core;

namespace Dig.Domain.Jobs
{

public sealed partial class JobSystem
{
    public Result ReleaseAssignment(EntityId jobId, long tick)
    {
        ValidateTick(tick);
        JobState? job = FindState(jobId);
        if (job is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.Status != JobStatus.Claimed
            && job.Status != JobStatus.InProgress)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        JobStatus previous = job.Status;
        ReleaseReservations(tick, jobId);
        job.MakeAvailable();
        RaiseStatusChanged(tick, job, previous, "assignment_released");
        return Result.Success();
    }
}

}
