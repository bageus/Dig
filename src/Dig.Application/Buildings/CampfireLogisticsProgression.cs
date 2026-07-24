using System;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;

namespace Dig.Application.Buildings
{

public static class CampfireLogisticsTimingPolicy
{
    public const int BaseIterationMinutes = 10;
    public const int ExperienceUnitsPerIteration = 70;

    public static int ResolveReductionPercent(int logisticsUnits)
    {
        if (logisticsUnits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logisticsUnits));
        }

        int points = logisticsUnits / AgentSkillCatalog.UnitsPerPoint;
        if (points > 100)
        {
            return 60;
        }

        return points switch
        {
            <= 10 => Interpolate(points, 0, 10, 0, 5),
            <= 20 => Interpolate(points, 11, 20, 6, 10),
            <= 40 => Interpolate(points, 21, 40, 11, 30),
            <= 60 => Interpolate(points, 41, 60, 31, 40),
            <= 80 => Interpolate(points, 61, 80, 41, 50),
            _ => Interpolate(points, 81, 100, 51, 60),
        };
    }

    public static int ResolveDurationSeconds(int logisticsUnits)
    {
        int reduction = ResolveReductionPercent(logisticsUnits);
        int baseSeconds = BaseIterationMinutes * 60;
        return checked(baseSeconds * (100 - reduction) / 100);
    }

    public static SkillGrantBundle CreateIterationGrant(
        EntityId operationId,
        EntityId workerId,
        int iterationNumber,
        long tick)
    {
        if (operationId.IsEmpty || workerId.IsEmpty)
        {
            throw new ArgumentException("Operation and worker ids are required.");
        }

        if (iterationNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterationNumber));
        }

        return new SkillGrantBundle(
            workerId,
            SkillGrantSourceKind.JobCompleted,
            $"campfire:{operationId}:iteration:{iterationNumber}",
            tick,
            new[]
            {
                new SkillGrant(AgentSkillCatalog.Logistics, ExperienceUnitsPerIteration),
            });
    }

    private static int Interpolate(
        int value,
        int minimumValue,
        int maximumValue,
        int minimumPercent,
        int maximumPercent)
    {
        if (value <= minimumValue)
        {
            return minimumPercent;
        }

        int valueRange = maximumValue - minimumValue;
        int percentRange = maximumPercent - minimumPercent;
        return minimumPercent
            + ((value - minimumValue) * percentRange / valueRange);
    }
}

public sealed class CampfireIterationProgressionService
{
    private readonly IAgentSkillGrantService _skillGrants;

    public CampfireIterationProgressionService(IAgentSkillGrantService skillGrants)
    {
        _skillGrants = skillGrants ?? throw new ArgumentNullException(nameof(skillGrants));
    }

    public Result CompleteIteration(
        PackableBuildingExecutionRegistry executions,
        EntityId operationId,
        EntityId workerId,
        long tick)
    {
        if (executions == null)
        {
            throw new ArgumentNullException(nameof(executions));
        }

        PackableBuildingExecutionState? execution = executions.Get(operationId);
        if (execution == null)
        {
            return Result.Failure(BuildingBoxErrors.JobTypeMismatch);
        }

        SkillGrantBundle grant = CampfireLogisticsTimingPolicy.CreateIterationGrant(
            operationId,
            workerId,
            checked(execution.CompletedIterations + 1),
            tick);
        Result validation = _skillGrants.Validate(grant);
        if (validation.IsFailure)
        {
            return validation;
        }

        Result completed = executions.CompleteIteration(operationId, workerId);
        if (completed.IsFailure)
        {
            return completed;
        }

        Result<SkillRedistributionReport> applied = _skillGrants.ApplyConfirmed(grant);
        return applied.IsFailure
            ? Result.Failure(applied.Error!)
            : Result.Success();
    }
}

}
