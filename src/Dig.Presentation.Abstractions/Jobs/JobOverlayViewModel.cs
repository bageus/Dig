using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Jobs;

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
        IReadOnlyList<JobReservationViewModel> reservations,
        JobToolKind? preferredToolKind = null,
        JobAssignmentDiagnosticViewModel? assignmentDiagnostic = null,
        IReadOnlyList<JobActionViewModel>? actions = null,
        JobExecutionReadinessViewModel? executionReadiness = null,
        int? targetZ = null)
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

        int? normalizedZ = targetX.HasValue ? targetZ ?? 0 : null;
        if (targetZ.HasValue && !targetX.HasValue)
        {
            throw new ArgumentException(
                "Target depth requires horizontal target coordinates.",
                nameof(targetZ));
        }

        if (normalizedZ.HasValue && (normalizedZ.Value < 0 || normalizedZ.Value > 3))
        {
            throw new ArgumentOutOfRangeException(nameof(targetZ));
        }

        if (preferredToolKind.HasValue
            && !Enum.IsDefined(typeof(JobToolKind), preferredToolKind.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(preferredToolKind));
        }

        JobActionViewModel[] actionValues = (actions ?? Array.Empty<JobActionViewModel>())
            .ToArray();
        if (actionValues.Any(action => action is null)
            || actionValues.Select(action => action.Kind).Distinct().Count() != actionValues.Length)
        {
            throw new ArgumentException(
                "Job actions must be non-null and unique by kind.",
                nameof(actions));
        }

        Id = id.Trim();
        Description = description.Trim();
        Status = status.Trim();
        Stage = stage.Trim();
        Priority = priority;
        AssignedAgentId = assignedAgentId;
        TargetX = targetX;
        TargetY = targetY;
        TargetZ = normalizedZ;
        RetryCount = retryCount;
        NextRetryTick = nextRetryTick;
        Reason = reason;
        Reservations = new ReadOnlyCollection<JobReservationViewModel>(
            (reservations ?? throw new ArgumentNullException(nameof(reservations)))
                .ToArray());
        PreferredToolKind = preferredToolKind;
        AssignmentDiagnostic = assignmentDiagnostic;
        Actions = new ReadOnlyCollection<JobActionViewModel>(actionValues);
        ExecutionReadiness = executionReadiness ?? new JobExecutionReadinessViewModel(
            JobExecutionReadinessKind.Ready,
            "Ready");
    }

    public string Id { get; }
    public string Description { get; }
    public string Status { get; }
    public string Stage { get; }
    public int Priority { get; }
    public string? AssignedAgentId { get; }
    public int? TargetX { get; }
    public int? TargetY { get; }
    public int? TargetZ { get; }
    public int RetryCount { get; }
    public long NextRetryTick { get; }
    public string? Reason { get; }
    public IReadOnlyList<JobReservationViewModel> Reservations { get; }
    public JobToolKind? PreferredToolKind { get; }
    public JobAssignmentDiagnosticViewModel? AssignmentDiagnostic { get; }
    public IReadOnlyList<JobActionViewModel> Actions { get; }
    public JobExecutionReadinessViewModel ExecutionReadiness { get; }
    public bool HasTarget => TargetX.HasValue && TargetY.HasValue && TargetZ.HasValue;
}
}