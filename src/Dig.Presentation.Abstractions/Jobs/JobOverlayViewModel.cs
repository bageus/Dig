using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(description)
            || string.IsNullOrWhiteSpace(status)
            || string.IsNullOrWhiteSpace(stage))
        {
            throw new ArgumentException("Job presentation text is required.");
        }

        if (targetX.HasValue != targetY.HasValue)
        {
            throw new ArgumentException("Target coordinates must both be present or absent.");
        }

        if ((targetX.HasValue && targetX.Value < 0)
            || (targetY.HasValue && targetY.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(targetX));
        }

        Id = id.Trim();
        Description = description.Trim();
        Status = status.Trim();
        Stage = stage.Trim();
        Priority = priority;
        AssignedAgentId = assignedAgentId;
        TargetX = targetX;
        TargetY = targetY;
        RetryCount = retryCount;
        NextRetryTick = nextRetryTick;
        Reason = reason;
        Reservations = new ReadOnlyCollection<JobReservationViewModel>(
            (reservations ?? throw new ArgumentNullException(nameof(reservations)))
                .ToArray());
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