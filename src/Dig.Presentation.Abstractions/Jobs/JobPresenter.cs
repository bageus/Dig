using Dig.Domain.Jobs;

namespace Dig.Presentation.Jobs;

public sealed class JobDiagnosticView
{
    public JobDiagnosticView(
        string jobId,
        string target,
        string status,
        string stage,
        string? assignedAgentId,
        int retryCount,
        long nextRetryTick,
        string? reason)
    {
        JobId = jobId;
        Target = target;
        Status = status;
        Stage = stage;
        AssignedAgentId = assignedAgentId;
        RetryCount = retryCount;
        NextRetryTick = nextRetryTick;
        Reason = reason;
    }

    public string JobId { get; }

    public string Target { get; }

    public string Status { get; }

    public string Stage { get; }

    public string? AssignedAgentId { get; }

    public int RetryCount { get; }

    public long NextRetryTick { get; }

    public string? Reason { get; }
}

public sealed class JobPresenter
{
    public JobDiagnosticView Present(JobSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return new JobDiagnosticView(
            snapshot.Id.ToString(),
            snapshot.Definition.Description,
            snapshot.Status.ToString(),
            snapshot.Stage.ToString(),
            snapshot.AssignedAgentId?.ToString(),
            snapshot.RetryCount,
            snapshot.NextRetryTick,
            snapshot.Reason?.ToString());
    }
}
