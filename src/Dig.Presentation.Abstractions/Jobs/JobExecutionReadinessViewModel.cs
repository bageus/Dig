using System;

namespace Dig.Presentation.Jobs
{

public enum JobExecutionReadinessKind
{
    Ready = 0,
    WaitingForToolDecision = 1,
}

public sealed class JobExecutionReadinessViewModel
{
    public JobExecutionReadinessViewModel(
        JobExecutionReadinessKind kind,
        string label,
        string? reasonCode = null,
        string? reasonMessage = null)
    {
        if (!Enum.IsDefined(typeof(JobExecutionReadinessKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Execution readiness label is required.", nameof(label));
        }

        bool hasReasonCode = !string.IsNullOrWhiteSpace(reasonCode);
        bool hasReasonMessage = !string.IsNullOrWhiteSpace(reasonMessage);
        if (kind == JobExecutionReadinessKind.Ready
            ? hasReasonCode || hasReasonMessage
            : !hasReasonCode || !hasReasonMessage)
        {
            throw new ArgumentException(
                "Ready execution cannot have a reason, and waiting execution requires one.");
        }

        Kind = kind;
        Label = label.Trim();
        ReasonCode = hasReasonCode ? reasonCode!.Trim() : null;
        ReasonMessage = hasReasonMessage ? reasonMessage!.Trim() : null;
    }

    public JobExecutionReadinessKind Kind { get; }

    public string Label { get; }

    public string? ReasonCode { get; }

    public string? ReasonMessage { get; }

    public bool IsReady => Kind == JobExecutionReadinessKind.Ready;
}
}
