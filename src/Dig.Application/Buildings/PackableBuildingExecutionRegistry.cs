using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;

namespace Dig.Application.Buildings
{

public sealed class PackableBuildingExecutionRegistry
{
    private readonly Dictionary<EntityId, PackableBuildingExecutionState> _byOperation =
        new Dictionary<EntityId, PackableBuildingExecutionState>();
    private readonly Dictionary<EntityId, EntityId> _operationByPackage =
        new Dictionary<EntityId, EntityId>();

    public Result<PackableBuildingExecutionState> GetOrCreate(
        EntityId operationId,
        EntityId packageId,
        BuildingDefinitionId definitionId,
        PackableBuildingOperationKind operation,
        int totalIterations)
    {
        if (_byOperation.TryGetValue(operationId, out PackableBuildingExecutionState? existing))
        {
            return existing.PackageId == packageId
                && existing.DefinitionId == definitionId
                && existing.Operation == operation
                && existing.TotalIterations == totalIterations
                    ? Result<PackableBuildingExecutionState>.Success(existing)
                    : Result<PackableBuildingExecutionState>.Failure(
                        BuildingBoxErrors.JobTypeMismatch);
        }

        if (_operationByPackage.TryGetValue(packageId, out EntityId activeOperationId))
        {
            PackableBuildingExecutionState active = _byOperation[activeOperationId];
            if (!IsTerminal(active))
            {
                return Result<PackableBuildingExecutionState>.Failure(
                    BuildingBoxErrors.JobAlreadyExists);
            }
        }

        PackableBuildingExecutionState created = new PackableBuildingExecutionState(
            operationId,
            packageId,
            definitionId,
            operation,
            totalIterations);
        _byOperation.Add(operationId, created);
        _operationByPackage[packageId] = operationId;
        return Result<PackableBuildingExecutionState>.Success(created);
    }

    public PackableBuildingExecutionState? Get(EntityId operationId)
    {
        if (operationId.IsEmpty)
        {
            throw new ArgumentException("Operation id is required.", nameof(operationId));
        }

        return _byOperation.TryGetValue(operationId, out PackableBuildingExecutionState? state)
            ? state
            : null;
    }

    public PackableBuildingIterationClockSnapshot? GetIterationClock(EntityId operationId)
    {
        return Get(operationId)?.IterationClock;
    }

    public Result StartOrResume(EntityId operationId, EntityId workerId)
    {
        PackableBuildingExecutionState? state = Get(operationId);
        return state is null
            ? Result.Failure(BuildingBoxErrors.JobTypeMismatch)
            : state.Start(workerId);
    }

    public Result BeginIteration(
        EntityId operationId,
        EntityId workerId,
        long startTick,
        int durationSeconds)
    {
        PackableBuildingExecutionState? state = Get(operationId);
        return state is null
            ? Result.Failure(BuildingBoxErrors.JobTypeMismatch)
            : state.BeginIteration(workerId, startTick, durationSeconds);
    }

    public Result<bool> IsIterationReady(
        EntityId operationId,
        EntityId workerId,
        long tick)
    {
        PackableBuildingExecutionState? state = Get(operationId);
        return state is null
            ? Result<bool>.Failure(BuildingBoxErrors.JobTypeMismatch)
            : state.IsIterationReady(workerId, tick);
    }

    public Result CompleteIteration(EntityId operationId, EntityId workerId)
    {
        PackableBuildingExecutionState? state = Get(operationId);
        return state is null
            ? Result.Failure(BuildingBoxErrors.JobTypeMismatch)
            : state.CompleteIteration(workerId);
    }

    public Result Interrupt(EntityId operationId)
    {
        PackableBuildingExecutionState? state = Get(operationId);
        return state is null
            ? Result.Failure(BuildingBoxErrors.JobTypeMismatch)
            : state.Interrupt();
    }

    public Result Cancel(EntityId operationId)
    {
        PackableBuildingExecutionState? state = Get(operationId);
        return state is null
            ? Result.Failure(BuildingBoxErrors.JobTypeMismatch)
            : state.Cancel();
    }

    public IReadOnlyList<PackableBuildingExecutionSnapshot> CreateSnapshot()
    {
        return new ReadOnlyCollection<PackableBuildingExecutionSnapshot>(
            _byOperation.Values
                .Select(value => value.CreateSnapshot())
                .OrderBy(value => value.OperationId.ToString(), StringComparer.Ordinal)
                .ToArray());
    }

    public static Result<PackableBuildingExecutionRegistry> Restore(
        IEnumerable<PackableBuildingExecutionSnapshot> snapshots)
    {
        if (snapshots is null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        PackableBuildingExecutionRegistry registry = new PackableBuildingExecutionRegistry();
        foreach (PackableBuildingExecutionSnapshot snapshot in snapshots.OrderBy(
            value => value.OperationId.ToString(),
            StringComparer.Ordinal))
        {
            if (snapshot is null || registry._byOperation.ContainsKey(snapshot.OperationId))
            {
                return Result<PackableBuildingExecutionRegistry>.Failure(
                    BuildingBoxErrors.JobAlreadyExists);
            }

            PackableBuildingExecutionState restored =
                PackableBuildingExecutionState.Restore(snapshot);
            registry._byOperation.Add(restored.OperationId, restored);

            if (!registry._operationByPackage.TryGetValue(
                    restored.PackageId,
                    out EntityId currentOperationId))
            {
                registry._operationByPackage.Add(restored.PackageId, restored.OperationId);
                continue;
            }

            PackableBuildingExecutionState current = registry._byOperation[currentOperationId];
            if (!IsTerminal(current) && !IsTerminal(restored))
            {
                return Result<PackableBuildingExecutionRegistry>.Failure(
                    BuildingBoxErrors.JobAlreadyExists);
            }

            if (IsTerminal(current) || !IsTerminal(restored))
            {
                registry._operationByPackage[restored.PackageId] = restored.OperationId;
            }
        }

        return Result<PackableBuildingExecutionRegistry>.Success(registry);
    }

    private static bool IsTerminal(PackableBuildingExecutionState state)
    {
        return state.Status == PackableBuildingExecutionStatus.Completed
            || state.Status == PackableBuildingExecutionStatus.Cancelled;
    }
}

}