using Dig.Domain.Core;

namespace Dig.Domain.Jobs;

public readonly struct JobInspection
{
    public JobInspection(
        JobDefinition definition,
        JobStatus status,
        EntityId? assignedAgentId)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Status = status;
        AssignedAgentId = assignedAgentId;
    }

    public JobDefinition Definition { get; }

    public EntityId Id => Definition.Id;

    public JobStatus Status { get; }

    public EntityId? AssignedAgentId { get; }

    public bool IsTerminal => Status is JobStatus.Completed or JobStatus.Cancelled or JobStatus.Failed;
}

public interface IJobInspectionVisitor
{
    void VisitJob(JobInspection job);

    void VisitJobReservation(ReservationSnapshot reservation);
}

public sealed partial class JobSystem
{
    public void VisitInspection(IJobInspectionVisitor visitor)
    {
        if (visitor is null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        foreach (JobState job in _jobs.Values)
        {
            visitor.VisitJob(new JobInspection(
                job.Definition,
                job.Status,
                job.AssignedAgentId));
        }

        _reservations.Visit(visitor);
    }

    public bool TryGetInspection(EntityId jobId, out JobInspection inspection)
    {
        JobState? job = FindState(jobId);
        if (job is null)
        {
            inspection = default;
            return false;
        }

        inspection = new JobInspection(job.Definition, job.Status, job.AssignedAgentId);
        return true;
    }
}
