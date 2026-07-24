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

public sealed class PackableBuildingIterationClockSnapshot
{
    public PackableBuildingIterationClockSnapshot(
        EntityId workerId,
        long startTick,
        int durationSeconds)
    {
        if (workerId.IsEmpty)
        {
            throw new ArgumentException("Worker id is required.", nameof(workerId));
        }

        if (startTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTick));
        }

        if (durationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        }

        WorkerId = workerId;
        StartTick = startTick;
        DurationSeconds = durationSeconds;
    }

    public EntityId WorkerId { get; }
    public long StartTick { get; }
    public int DurationSeconds { get; }
    public long CompletionTick => checked(StartTick + DurationSeconds);
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
        IReadOnlyCollection<EntityId> completedByWorkers,
        PackableBuildingIterationClockSnapshot? iterationClock = null)
    {
        if (operationId.IsEmpty || packageId.IsEmpty || definitionId.IsEmpty)
        {
            throw new ArgumentException("Operation, package and definition ids are required.");
        }

        if (!Enum.IsDefined(typeof(PackableBuildingOperationKind), operation)
            || !Enum.IsDefined(typeof(PackableBuildingExecutionStatus), status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
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

        bool active = status == PackableBuildingExecutionStatus.Active;
        if (active != activeWorkerId.HasValue
            || (!active && iterationClock is not null)
            || (iterationClock is not null && iterationClock.WorkerId != activeWorkerId!.Value)
            || (status == PackableBuildingExecutionStatus.Planned && completedIterations != 0)
            || (status == PackableBuildingExecutionStatus.Completed
                && completedIterations != totalIterations)
            || (status != PackableBuildingExecutionStatus.Completed
                && completedIterations == totalIterations))
        {
            throw new ArgumentException("Packable building execution state is inconsistent.");
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
        IterationClock = iterationClock;
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
    public PackableBuildingIterationClockSnapshot? IterationClock { get; }
}

public sealed class PackableBuildingExecutionState
{
    private readonly List<EntityId> _completedByWorkers = new List<EntityId>();
    private PackableBuildingIterationClockSnapshot? _iterationClock;

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

    private PackableBuildingExecutionState(PackableBuildingExecutionSnapshot snapshot)
    {
        OperationId = snapshot.OperationId;
        PackageId = snapshot.PackageId;
        DefinitionId = snapshot.DefinitionId;
        Operation = snapshot.Operation;
        TotalIterations = snapshot.TotalIterations;
        Status = snapshot.Status;
        ActiveWorkerId = snapshot.ActiveWorkerId;
        _completedByWorkers.AddRange(snapshot.CompletedByWorkers);
        _iterationClock = snapshot.IterationClock;
    }

    public EntityId OperationId { get; }
    public EntityId PackageId { get; }
    public BuildingDefinitionId DefinitionId { get; }
    public PackableBuildingOperationKind Operation { get; }
    public int TotalIterations { get; }
    public int CompletedIterations => _completedByWorkers.Count;
    public PackableBuildingExecutionStatus Status { get; private set; }
    public EntityId? ActiveWorkerId { get; private set; }
    public PackableBuildingIterationClockSnapshot? IterationClock => _iterationClock;

    public static PackableBuildingExecutionState Restore(
        PackableBuildingExecutionSnapshot snapshot)
    {
        return new PackableBuildingExecutionState(
            snapshot ?? throw new ArgumentNullException(nameof(snapshot)));
    }

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
        _iterationClock = null;
        return Result.Success();
    }

    public Result BeginIteration(EntityId workerId, long startTick, int durationSeconds)
    {
        if (workerId.IsEmpty)
        {
            throw new ArgumentException("Worker id is required.", nameof(workerId));
        }

        if (startTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTick));
        }

        if (durationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        }

        if (Status != PackableBuildingExecutionStatus.Active
            || ActiveWorkerId != workerId)
        {
            return Result.Failure(BuildingBoxErrors.InvalidJobStage);
        }

        _iterationClock ??= new PackableBuildingIterationClockSnapshot(
            workerId,
            startTick,
            durationSeconds);
        return Result.Success();
    }

    public Result<bool> IsIterationReady(EntityId workerId, long tick)
    {
        if (workerId.IsEmpty)
        {
            throw new ArgumentException("Worker id is required.", nameof(workerId));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (Status != PackableBuildingExecutionStatus.Active
            || ActiveWorkerId != workerId)
        {
            return Result<bool>.Failure(BuildingBoxErrors.InvalidJobStage);
        }

        return Result<bool>.Success(
            _iterationClock is not null && tick >= _iterationClock.CompletionTick);
    }

    public Result Interrupt()
    {
        if (Status != PackableBuildingExecutionStatus.Active)
        {
            return Result.Failure(BuildingBoxErrors.InvalidJobStage);
        }

        Status = PackableBuildingExecutionStatus.Interrupted;
        ActiveWorkerId = null;
        _iterationClock = null;
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

        _iterationClock = null;
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
        _iterationClock = null;
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
            _completedByWorkers,
            _iterationClock);
    }
}

}