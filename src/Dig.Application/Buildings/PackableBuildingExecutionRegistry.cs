using System;
using System.Collections.Generic;
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
            if (active.Status != PackableBuildingExecutionStatus.Completed
                && active.Status != PackableBuildingExecutionStatus.Cancelled)
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

    public Result StartOrResume(EntityId operationId, EntityId workerId)
    {
        PackableBuildingExecutionState? state = Get(operationId);
        return state is null
            ? Result.Failure(BuildingBoxErrors.JobTypeMismatch)
            : state.Start(workerId);
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
}

}
