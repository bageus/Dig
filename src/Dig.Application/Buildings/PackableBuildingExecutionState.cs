using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;

namespace Dig.Application.Buildings
{

public enum PackableBuildingExecutionStatus
{
    Planned = 0,
    Active = 1,
    Interrupted = 2,
    Completed = 3,
    Cancelled = 4,
}

public sealed class PackableBuildingExecutionSnapshot
{
    public PackableBuildingExecutionSnapshot(
        EntityId operationId,
        EntityId packageId,
        BuildingDefinitionId definitionId,
        PackableBuildingOperationKind operation,
        PackableBuildingExecutionStatus status,
        int totalIterations,
        int completedIterations,
        EntityId? activeWorkerId,
        IReadOnlyCollection<EntityId> completedByWorkers)
    {
        if (operationId.IsEmpty || packageId.IsEmpty || definitionId.IsEmpty)
        {
            throw new ArgumentException("Operation, package and definition ids are required.");
        }

        if (totalIterations <= 0
            || completedIterations < 0
            || completedIterations > totalIterations)
        {
            throw new ArgumentOutOfRangeException(nameof(totalIterations));
        }

        if (completedByWorkers is null || completedByWorkers.Count != completedIterations)
        {
            throw new ArgumentException(
                "Completed iteration attribution must match completed iteration count.",
                nameof(completedByWorkers));
        }

        OperationId = operationId;
        PackageId = packageId;
        DefinitionId = definitionId;
        Operation = operation;
        Status = status;
        TotalIterations = totalIterations;
        CompletedIterations = completedIterations;
        ActiveWorkerId = activeWorkerId;
        CompletedByWorkers = new ReadOnlyCollection<EntityId>(
            completedByWorkers.ToArray());
    }

    public EntityId OperationId { get; }
    public EntityId PackageId { get; }
    public BuildingDefinitionId DefinitionId { get; }
    public PackableBuildingOperationKind Operation { get; }
    public PackableBuildingExecutionStatus Status { get; }
    public int TotalIterations { get; }
    public int CompletedIterations { get; }
    public EntityId? ActiveWorkerId { get; }
    public IReadOnlyList<EntityId> CompletedByWorkers { get; }
}

public sealed class PackableBuildingExecutionState
{
    private readonly List<EntityId> _completedByWorkers = new List<EntityId>();

    public PackableBuildingExecutionState(
        EntityId operationId,
        EntityId packageId,
        BuildingDefinitionId definitionId,
        PackableBuildingOperationKind operation,
        int totalIterations)
    {
        if (operationId.IsEmpty || packageId.IsEmpty || definitionId.IsEmpty)
        {
            throw new ArgumentException("Operation, package and definition ids are required.");
        }

        if (totalIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalIterations));
        }

        OperationId = operationId;
        PackageId = packageId;
        DefinitionId = definitionId;
        Operation = operation;
        TotalIterations = totalIterations;
        Status = PackableBuildingExecutionStatus.Planned;
    }

    public EntityId OperationId { get; }
    public EntityId PackageId { get; }
    public BuildingDefinitionId DefinitionId { get; }
    public PackableBuildingOperationKind Operation { get; }
    public int TotalIterations { get; }
    public int CompletedIterations => _completedByWorkers.Count;
    public PackableBuildingExecutionStatus Status { get; private set; }
    public EntityId? ActiveWorkerId { get; private set; }

    public Result Start(EntityId workerId)
    {
        if (workerId.IsEmpty)
        {
            throw new ArgumentException("Worker id is required.", nameof(workerId));
        }

        if (Status != PackableBuildingExecutionStatus.Planned
            && Status != PackableBuildingExecutionStatus.Interrupted)
        {
            return Result.Failure(BuildingBoxErrors.InvalidJobStage);
        }

        Status = PackableBuildingExecutionStatus.Active;
        ActiveWorkerId = workerId;
        return Result.Success();
    }

    public Result Interrupt()
    {
        if (Status != PackableBuildingExecutionStatus.Active)
        {
            return Result.Failure(BuildingBoxErrors.InvalidJobStage);
        }

        Status = PackableBuildingExecutionStatus.Interrupted;
        ActiveWorkerId = null;
        return Result.Success();
    }

    public Result CompleteIteration(EntityId workerId)
    {
        if (workerId.IsEmpty)
        {
            throw new ArgumentException("Worker id is required.", nameof(workerId));
        }

        if (Status != PackableBuildingExecutionStatus.Active
            || ActiveWorkerId != workerId)
        {
            return Result.Failure(BuildingBoxErrors.InvalidJobStage);
        }

        if (CompletedIterations >= TotalIterations)
        {
            return Result.Failure(BuildingErrors.WorkIncomplete);
        }

        _completedByWorkers.Add(workerId);
        if (CompletedIterations == TotalIterations)
        {
            Status = PackableBuildingExecutionStatus.Completed;
            ActiveWorkerId = null;
        }

        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == PackableBuildingExecutionStatus.Completed
            || Status == PackableBuildingExecutionStatus.Cancelled)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        Status = PackableBuildingExecutionStatus.Cancelled;
        ActiveWorkerId = null;
        return Result.Success();
    }

    public PackableBuildingExecutionSnapshot CreateSnapshot()
    {
        return new PackableBuildingExecutionSnapshot(
            OperationId,
            PackageId,
            DefinitionId,
            Operation,
            Status,
            TotalIterations,
            CompletedIterations,
            ActiveWorkerId,
            _completedByWorkers);
    }
}

}
