using System;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public static class BuildingBoxPickupErrors
{
    public static readonly DomainError StackMissing = new DomainError(
        "building_box.pickup.stack_missing",
        "The selected BuildingBox stack does not exist.");

    public static readonly DomainError StackNotInWorld = new DomainError(
        "building_box.pickup.not_in_world",
        "The selected BuildingBox is not at the requested world cell.");

    public static readonly DomainError ItemMismatch = new DomainError(
        "building_box.pickup.item_mismatch",
        "The selected stack is not the expected BuildingBox item.");

    public static readonly DomainError BoxUnavailable = new DomainError(
        "building_box.pickup.unavailable",
        "The selected BuildingBox is reserved or otherwise unavailable.");

    public static readonly DomainError JobTypeMismatch = new DomainError(
        "building_box.pickup.job_type_mismatch",
        "The requested job is not a BuildingBox pickup job.");

    public static readonly DomainError InvalidJobStage = new DomainError(
        "building_box.pickup.invalid_job_stage",
        "The BuildingBox pickup job is not ready to complete.");
}

public sealed class CreateBuildingBoxPickupCommand : ICommand<Result>
{
    public CreateBuildingBoxPickupCommand(
        EntityId jobId,
        EntityId stackId,
        EntityId residentId,
        ItemId expectedItemId,
        CellId sourceCell,
        int priority,
        long tick)
    {
        if (jobId.IsEmpty || stackId.IsEmpty || residentId.IsEmpty)
        {
            throw new ArgumentException("Job, stack and resident ids are required.");
        }

        if (expectedItemId.IsEmpty)
        {
            throw new ArgumentException("Expected item id is required.", nameof(expectedItemId));
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
        ExpectedItemId = expectedItemId;
        SourceCell = sourceCell;
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId StackId { get; }
    public EntityId ResidentId { get; }
    public ItemId ExpectedItemId { get; }
    public CellId SourceCell { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class CompleteBuildingBoxPickupCommand : ICommand<Result>
{
    public CompleteBuildingBoxPickupCommand(EntityId jobId, long tick)
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

public sealed class CancelBuildingBoxPickupCommand : ICommand<Result>
{
    public CancelBuildingBoxPickupCommand(EntityId jobId, string reason, long tick)
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
