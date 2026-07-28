using System;
using System.Collections.Generic;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public static class ResidentInventoryPlacementErrors
{
    public static readonly DomainError SourceUnavailable = new DomainError(
        "inventory.placement.source_unavailable",
        "The inventory stack is not available for placement.");

    public static readonly DomainError ExpansionRequiresExplicitDrop = new DomainError(
        "inventory.placement.expansion_requires_explicit_drop",
        "Inventory expansions require the explicit spill-aware drop action.");

    public static readonly DomainError TargetUnavailable = new DomainError(
        "inventory.placement.target_unavailable",
        "The placement target must be an explored, reachable, open cell with walkable support.");

    public static readonly DomainError ResidentMismatch = new DomainError(
        "inventory.placement.resident_mismatch",
        "The placement job is bound to a different resident.");

    public static readonly DomainError DependencyFailed = new DomainError(
        "inventory.placement.dependency_failed",
        "An earlier resident placement job did not complete successfully.");
}

public sealed class CreateResidentInventoryPlacementCommand : ICommand<Result>
{
    public CreateResidentInventoryPlacementCommand(
        EntityId jobId,
        EntityId residentId,
        EntityId stackId,
        int quantity,
        CellId destinationCell,
        IReadOnlyCollection<CellId> reachableCells,
        int priority,
        long tick)
    {
        if (jobId.IsEmpty || residentId.IsEmpty || stackId.IsEmpty)
        {
            throw new ArgumentException("Job, resident and stack ids are required.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
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
        ResidentId = residentId;
        StackId = stackId;
        Quantity = quantity;
        DestinationCell = destinationCell;
        ReachableCells = reachableCells;
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId ResidentId { get; }
    public EntityId StackId { get; }
    public int Quantity { get; }
    public CellId DestinationCell { get; }
    public IReadOnlyCollection<CellId> ReachableCells { get; }
    public int Priority { get; }
    public long Tick { get; }
}

public sealed class CompleteResidentInventoryPlacementCommand : ICommand<Result>
{
    public CompleteResidentInventoryPlacementCommand(
        EntityId jobId,
        CellId workerCell,
        long tick)
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
        WorkerCell = workerCell;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public CellId WorkerCell { get; }
    public long Tick { get; }
}
}
