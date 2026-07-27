using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public static class BuildingBoxRelocationErrors
{
    public static readonly DomainError TargetMustBeZ0 = new DomainError(
        "building_box.relocation.target_layer",
        "BuildingBox relocation is only valid on Z0.");

    public static readonly DomainError TargetUnavailable = new DomainError(
        "building_box.relocation.target_unavailable",
        "The BuildingBox relocation target is not an open reachable cell.");

    public static readonly DomainError SourceOwnedByAnotherAgent = new DomainError(
        "building_box.relocation.source_owner",
        "The BuildingBox is carried by a resident other than the assigned worker.");
}

public sealed class CreateBuildingBoxRelocationCommand : ICommand<Result>
{
    public CreateBuildingBoxRelocationCommand(
        EntityId jobId,
        EntityId stackId,
        ItemId expectedItemId,
        CellId destinationCell,
        IReadOnlyCollection<CellId> reachableCells,
        int priority,
        long tick)
    {
        if (jobId.IsEmpty || stackId.IsEmpty)
        {
            throw new ArgumentException("Job and stack ids are required.");
        }

        if (expectedItemId.IsEmpty)
        {
            throw new ArgumentException("Expected item id is required.", nameof(expectedItemId));
        }

        if (reachableCells is null)
        {
            throw new ArgumentNullException(nameof(reachableCells));
        }

        if (priority < 0 || priority > 1000 || tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        JobId = jobId;
        StackId = stackId;
        ExpectedItemId = expectedItemId;
        DestinationCell = destinationCell;
        ReachableCells = reachableCells;
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId StackId { get; }
    public ItemId ExpectedItemId { get; }
    public CellId DestinationCell { get; }
    public IReadOnlyCollection<CellId> ReachableCells { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class AcquireBuildingBoxForRelocationCommand : ICommand<Result>
{
    public AcquireBuildingBoxForRelocationCommand(
        EntityId jobId,
        CellId workerCell,
        long tick)
    {
        JobId = jobId;
        WorkerCell = workerCell;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public CellId WorkerCell { get; }
    public long Tick { get; }
}

public sealed class CompleteBuildingBoxRelocationCommand : ICommand<Result>
{
    public CompleteBuildingBoxRelocationCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

}
