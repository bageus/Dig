using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class MushroomChopJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] WorkStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public MushroomChopJobDefinition(
        EntityId id,
        EntityId siteId,
        CellId targetCell,
        CellId workPosition,
        long growthGeneration,
        int requiredSwings,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(id, priority, createdTick, retryPolicy, WorkStages, dependencies)
    {
        if (siteId.IsEmpty)
        {
            throw new ArgumentException("Mushroom site id cannot be empty.", nameof(siteId));
        }

        if (growthGeneration < 0 || requiredSwings <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(growthGeneration));
        }

        SiteId = siteId;
        TargetCell = targetCell;
        WorkPosition = workPosition;
        GrowthGeneration = growthGeneration;
        RequiredSwings = requiredSwings;
    }

    public EntityId SiteId { get; }
    public CellId TargetCell { get; }
    public CellId WorkPosition { get; }
    public long GrowthGeneration { get; }
    public int RequiredSwings { get; }

    public override string Description => $"ChopMushroom:{SiteId}@{TargetCell}";


    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForEcologyTarget(SiteId),
            ReservationKey.ForPosition(WorkPosition),
        });
    }
}

}
