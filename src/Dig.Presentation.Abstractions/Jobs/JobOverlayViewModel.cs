using System.Collections.Generic;

namespace Dig.Presentation.Jobs
{

public sealed class JobOverlayViewModel
{
    public JobOverlayViewModel(
        string id,
        string description,
        string status,
        string stage,
        int priority,
        string? assignedAgentId,
        int? targetX,
        int? targetY,
        int retryCount,
        long nextRetryTick,
        string? reason,
        IReadOnlyList<JobReservationViewModel> reservations)
    {
        Id = id;
        Description = description;
        Status = status;
        Stage = stage;
        Priority = priority;
        AssignedAgentId = assignedAgentId;
        TargetX = targetX;
        TargetY = targetY;
        RetryCount = retryCount;
        NextRetryTick = nextRetryTick;
        Reason = reason;
        Reservations = reservations;
    }

    public string Id { get; }
    public string Description { get; }
    public string Status { get; }
    public string Stage { get; }
    public int Priority { get; }
    public string? AssignedAgentId { get; }
    public int? TargetX { get; }
    public int? TargetY { get; }
    public int RetryCount { get; }
    public long NextRetryTick { get; }
    public string? Reason { get; }
    public IReadOnlyList<JobReservationViewModel> Reservations { get; }
    public bool HasTarget => TargetX.HasValue && TargetY.HasValue;
}
}