using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Strategy;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class StrategicExecutionJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] PositionedStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    private static readonly JobStageKind[] AbstractStages =
    {
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public StrategicExecutionJobDefinition(
        EntityId id,
        StrategicExecutionPlanId planId,
        FactionId factionId,
        StrategicGoalKind goal,
        CellId? targetCell,
        FactionId? targetFactionId,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(
            id,
            priority,
            createdTick,
            retryPolicy,
            targetCell.HasValue ? PositionedStages : AbstractStages,
            dependencies)
    {
        if (planId.IsEmpty)
        {
            throw new ArgumentException("Strategic plan id cannot be empty.", nameof(planId));
        }

        if (factionId.IsEmpty)
        {
            throw new ArgumentException("Faction id cannot be empty.", nameof(factionId));
        }

        if (goal == StrategicGoalKind.Attack && !targetFactionId.HasValue)
        {
            throw new ArgumentException("Attack jobs require a target faction.");
        }

        PlanId = planId;
        FactionId = factionId;
        Goal = goal;
        TargetCell = targetCell;
        TargetFactionId = targetFactionId;
    }

    public StrategicExecutionPlanId PlanId { get; }
    public FactionId FactionId { get; }
    public StrategicGoalKind Goal { get; }
    public CellId? TargetCell { get; }
    public FactionId? TargetFactionId { get; }

    public override string Description =>
        $"Strategy:{FactionId}:{Goal}:{PlanId}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return TargetCell.HasValue
            ? new ReadOnlyCollection<ReservationKey>(new[]
            {
                ReservationKey.ForPosition(TargetCell.Value),
            })
            : Array.Empty<ReservationKey>();
    }
}
}
