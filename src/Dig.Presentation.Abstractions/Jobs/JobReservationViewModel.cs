using System;

namespace Dig.Presentation.Jobs
{

public readonly struct JobReservationViewModel
{
    public JobReservationViewModel(
        string kind,
        string value,
        string agentId,
        long acquiredTick)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Reservation kind is required.", nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Reservation value is required.", nameof(value));
        }

        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent id is required.", nameof(agentId));
        }

        if (acquiredTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(acquiredTick));
        }

        Kind = kind.Trim();
        Value = value.Trim();
        AgentId = agentId.Trim();
        AcquiredTick = acquiredTick;
    }

    public string Kind { get; }

    public string Value { get; }

    public string AgentId { get; }

    public long AcquiredTick { get; }
}
}