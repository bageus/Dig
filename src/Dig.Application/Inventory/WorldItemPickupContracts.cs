using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public static class WorldItemPickupErrors
{
    public static readonly DomainError StackMissing = new DomainError(
        "world_item.pickup.stack_missing",
        "The selected world item stack does not exist.");

    public static readonly DomainError StackNotInWorld = new DomainError(
        "world_item.pickup.not_in_world",
        "The selected item is not at the requested world cell.");

    public static readonly DomainError StackUnavailable = new DomainError(
        "world_item.pickup.unavailable",
        "The selected world item stack is reserved or otherwise unavailable.");

    public static readonly DomainError JobTypeMismatch = new DomainError(
        "world_item.pickup.job_type_mismatch",
        "The requested job is not a world item pickup job.");

    public static readonly DomainError InvalidJobStage = new DomainError(
        "world_item.pickup.invalid_job_stage",
        "The world item pickup job is not ready to complete.");
}

public sealed class CreateWorldItemPickupCommand : ICommand<Result>
{
    public CreateWorldItemPickupCommand(
        EntityId jobId,
        EntityId stackId,
        EntityId residentId,
        CellId sourceCell,
        int priority,
        long tick)
    {
        if (jobId.IsEmpty || stackId.IsEmpty || residentId.IsEmpty)
        {
            throw new ArgumentException("Job, stack and resident ids are required.");
        }

        if (priority < 0 || priority > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        JobId = jobId;
        StackId = stackId;
        ResidentId = residentId;
        SourceCell = sourceCell;
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId StackId { get; }
    public EntityId ResidentId { get; }
    public CellId SourceCell { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class CompleteWorldItemPickupCommand : ICommand<Result>
{
    public CompleteWorldItemPickupCommand(EntityId jobId, long tick)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id is required.", nameof(jobId));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CancelWorldItemPickupCommand : ICommand<Result>
{
    public CancelWorldItemPickupCommand(EntityId jobId, string reason, long tick)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id is required.", nameof(jobId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        JobId = jobId;
        Reason = reason.Trim();
        Tick = tick;
    }

    public EntityId JobId { get; }
    public string Reason { get; }
    public long Tick { get; }
}

}
