using System;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public enum JobStatus
{
    Created = 0,
    Available = 1,
    Claimed = 2,
    InProgress = 3,
    Blocked = 4,
    Completed = 5,
    Cancelled = 6,
    Failed = 7,
}

public enum JobStageKind
{
    None = 0,
    TravelToTarget = 1,
    PerformWork = 2,
    Finalize = 3,
    AcquireItem = 4,
    TravelToDestination = 5,
    DepositItem = 6,
}

public enum ReservationKind
{
    Job = 0,
    Agent = 1,
    Item = 2,
    Tool = 3,
    Position = 4,
    Designation = 5,
    Destination = 6,
}

public readonly struct JobRetryPolicy
{
    public JobRetryPolicy(int maximumRetries, long retryDelayTicks)
    {
        if (maximumRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumRetries));
        }

        if (retryDelayTicks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelayTicks));
        }

        MaximumRetries = maximumRetries;
        RetryDelayTicks = retryDelayTicks;
    }

    public int MaximumRetries { get; }

    public long RetryDelayTicks { get; }

    public static JobRetryPolicy Default => new JobRetryPolicy(3, 10);
}

public readonly struct ReservationKey : IEquatable<ReservationKey>, IComparable<ReservationKey>
{
    private ReservationKey(ReservationKind kind, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Reservation value is required.", nameof(value));
        }

        Kind = kind;
        Value = value;
    }

    public ReservationKind Kind { get; }

    public string Value { get; }

    public static ReservationKey ForJob(EntityId jobId)
    {
        return ForEntity(ReservationKind.Job, jobId);
    }

    public static ReservationKey ForAgent(EntityId agentId)
    {
        return ForEntity(ReservationKind.Agent, agentId);
    }

    public static ReservationKey ForItem(EntityId itemId)
    {
        return ForEntity(ReservationKind.Item, itemId);
    }

    public static ReservationKey ForTool(EntityId toolId)
    {
        return ForEntity(ReservationKind.Tool, toolId);
    }

    public static ReservationKey ForDestination(EntityId destinationId)
    {
        return ForEntity(ReservationKind.Destination, destinationId);
    }

    public static ReservationKey ForPosition(CellId cellId)
    {
        return new ReservationKey(ReservationKind.Position, cellId.ToString());
    }

    public static ReservationKey ForDesignation(CellId cellId)
    {
        return new ReservationKey(ReservationKind.Designation, cellId.ToString());
    }

    public int CompareTo(ReservationKey other)
    {
        int kindComparison = Kind.CompareTo(other.Kind);
        return kindComparison != 0
            ? kindComparison
            : string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    public bool Equals(ReservationKey other)
    {
        return Kind == other.Kind
            && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReservationKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Kind, Value);
    }

    public override string ToString()
    {
        return $"{Kind}:{Value}";
    }

    public static bool operator ==(ReservationKey left, ReservationKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReservationKey left, ReservationKey right)
    {
        return !left.Equals(right);
    }

    private static ReservationKey ForEntity(ReservationKind kind, EntityId entityId)
    {
        if (entityId.IsEmpty)
        {
            throw new ArgumentException("Reserved entity id cannot be empty.", nameof(entityId));
        }

        return new ReservationKey(kind, entityId.ToString());
    }
}

public sealed class JobBlockReason
{
    public JobBlockReason(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Block reason code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Block reason message is required.", nameof(message));
        }

        Code = code.Trim();
        Message = message.Trim();
    }

    public string Code { get; }

    public string Message { get; }

    public override string ToString()
    {
        return $"{Code}: {Message}";
    }
}
}
