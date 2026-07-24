using System;
using Dig.Application.Agents;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Core;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private PackableBuildingExecutionRegistry? _packableBuildingExecutions;
    private CampfireIterationProgressionService? _campfireIterationProgression;

    private void InitializePackableBuildingIterationProgression()
    {
        if (_packableBuildingExecutions == null)
        {
            throw new InvalidOperationException(
                "Packable building execution registry is not initialized.");
        }

        if (_skillGrants is not IAgentSkillLevelReader skillLevels)
        {
            throw new InvalidOperationException(
                "The campfire runtime requires authoritative agent skill levels.");
        }

        _campfireIterationProgression ??=
            new CampfireIterationProgressionService(_skillGrants, skillLevels);
    }

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

    private Result ExecutePackableBuildingIteration(
        EntityId operationId,
        EntityId workerId,
        long tick,
        Func<Result> applyAuthoritativeWork)
    {
        if (applyAuthoritativeWork == null)
        {
            throw new ArgumentNullException(nameof(applyAuthoritativeWork));
        }

        if (_packableBuildingExecutions == null || _campfireIterationProgression == null)
        {
            throw new InvalidOperationException(
                "Packable building iteration progression is not initialized.");
        }

        PackableBuildingIterationClockSnapshot? clock =
            _packableBuildingExecutions.GetIterationClock(operationId);
        if (clock == null)
        {
            Result<int> duration = _campfireIterationProgression.ResolveDurationSeconds(workerId);
            if (duration.IsFailure)
            {
                return Result.Failure(duration.Error!);
            }

            return _packableBuildingExecutions.BeginIteration(
                operationId,
                workerId,
                tick,
                duration.Value);
        }

        Result<bool> ready = _packableBuildingExecutions.IsIterationReady(
            operationId,
            workerId,
            tick);
        if (ready.IsFailure)
        {
            return Result.Failure(ready.Error!);
        }

        if (!ready.Value)
        {
            return Result.Success();
        }

        Result applied = applyAuthoritativeWork();
        if (applied.IsFailure)
        {
            return applied;
        }

        return _campfireIterationProgression.CompleteIteration(
            _packableBuildingExecutions,
            operationId,
            workerId,
            tick);
    }
}

}