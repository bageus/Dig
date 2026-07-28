using System;
using Dig.Application.Agents;
using Dig.Domain.Agents;

namespace Dig.Presentation.Agents
{

public sealed class ResidentMovementModeViewModel
{
    public ResidentMovementModeViewModel(ResidentMovementModeResolution resolution)
    {
        if (resolution is null)
        {
            throw new ArgumentNullException(nameof(resolution));
        }

        ResidentId = resolution.ResidentId.ToString();
        Mode = resolution.Mode;
        Reason = resolution.Reason;
        Mobility = resolution.Mobility;
        CommandSource = resolution.CommandSource;
        EffectiveSpeedMultiplier = resolution.EffectiveSpeedMultiplier;
        TransitionDurationMultiplier = resolution.TransitionDurationMultiplier;
        RepeatedManualCommand = resolution.RepeatedManualCommand;
    }

    public string ResidentId { get; }
    public ResidentMovementMode Mode { get; }
    public ResidentMovementModeReason Reason { get; }
    public ResidentMobilityKind Mobility { get; }
    public ResidentMovementCommandSource CommandSource { get; }
    public double EffectiveSpeedMultiplier { get; }
    public double TransitionDurationMultiplier { get; }
    public bool RepeatedManualCommand { get; }
    public bool IsCarrying => Mode == ResidentMovementMode.Carrying;
}

public sealed class ResidentMovementInterruptionViewModel
{
    public ResidentMovementInterruptionViewModel(
        string residentId,
        ResidentMovementInterruptionReason reason,
        long tick,
        string detail)
    {
        if (string.IsNullOrWhiteSpace(residentId))
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        ResidentId = residentId.Trim();
        Reason = reason;
        Tick = tick;
        Detail = detail ?? string.Empty;
    }

    public string ResidentId { get; }
    public ResidentMovementInterruptionReason Reason { get; }
    public long Tick { get; }
    public string Detail { get; }
}

}
