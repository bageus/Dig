using System;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Core;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private PackableBuildingExecutionRegistry? _packableBuildingExecutions;

    private Result<PackableBuildingExecutionState> GetOrCreatePackableBuildingExecution(
        EntityId operationId,
        EntityId packageId,
        BuildingDefinitionId definitionId,
        PackableBuildingOperationKind operation,
        int totalIterations)
    {
        if (_packableBuildingExecutions == null)
        {
            throw new InvalidOperationException(
                "Packable building execution registry is not initialized.");
        }

        return _packableBuildingExecutions.GetOrCreate(
            operationId,
            packageId,
            definitionId,
            operation,
            totalIterations);
    }

    private Result StartOrResumePackableBuildingExecution(
        EntityId operationId,
        EntityId workerId)
    {
        if (_packableBuildingExecutions == null)
        {
            throw new InvalidOperationException(
                "Packable building execution registry is not initialized.");
        }

        return _packableBuildingExecutions.StartOrResume(operationId, workerId);
    }

    private Result CompletePackableBuildingIteration(
        EntityId operationId,
        EntityId workerId)
    {
        if (_packableBuildingExecutions == null)
        {
            throw new InvalidOperationException(
                "Packable building execution registry is not initialized.");
        }

        return _packableBuildingExecutions.CompleteIteration(operationId, workerId);
    }
}

}
