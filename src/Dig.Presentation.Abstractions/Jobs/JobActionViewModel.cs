using System;

namespace Dig.Presentation.Jobs
{

public enum JobActionKind
{
    PrepareSuggestedTool = 0,
    BypassSuggestedTool = 1,
}

public sealed class JobActionViewModel
{
    public JobActionViewModel(
        JobActionKind kind,
        string label,
        bool isEnabled,
        string? disabledReasonCode = null,
        string? disabledReasonMessage = null)
    {
        if (!Enum.IsDefined(typeof(JobActionKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Action label is required.", nameof(label));
        }

        bool hasReasonCode = !string.IsNullOrWhiteSpace(disabledReasonCode);
        bool hasReasonMessage = !string.IsNullOrWhiteSpace(disabledReasonMessage);
        if (isEnabled ? hasReasonCode || hasReasonMessage : !hasReasonCode || !hasReasonMessage)
        {
            throw new ArgumentException(
                "Enabled actions cannot have a disabled reason, and disabled actions require one.");
        }

        Kind = kind;
        Label = label.Trim();
        IsEnabled = isEnabled;
        DisabledReasonCode = hasReasonCode ? disabledReasonCode!.Trim() : null;
        DisabledReasonMessage = hasReasonMessage ? disabledReasonMessage!.Trim() : null;
    }

    public JobActionKind Kind { get; }

    public string Label { get; }

    public bool IsEnabled { get; }

    public string? DisabledReasonCode { get; }

    public string? DisabledReasonMessage { get; }
}
}
