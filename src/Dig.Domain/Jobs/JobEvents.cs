using Dig.Domain.Core;

namespace Dig.Domain.Jobs;

public sealed class JobStatusChanged : IDomainEvent
{
    public JobStatusChanged(
        long tick,
        EntityId jobId,
        JobStatus previousStatus,
        JobStatus currentStatus,
        EntityId? agentId,
        string? reasonCode)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        Tick = tick;
        JobId = jobId;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        AgentId = agentId;
        ReasonCode = reasonCode;
    }

    public long Tick { get; }

    public EntityId JobId { get; }

    public JobStatus PreviousStatus { get; }

    public JobStatus CurrentStatus { get; }

    public EntityId? AgentId { get; }

    public string? ReasonCode { get; }
}

public sealed class JobReservationsReleased : IDomainEvent
{
    public JobReservationsReleased(long tick, EntityId jobId, int reservationCount)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        if (reservationCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reservationCount));
        }

        Tick = tick;
        JobId = jobId;
        ReservationCount = reservationCount;
    }

    public long Tick { get; }

    public EntityId JobId { get; }

    public int ReservationCount { get; }
}
