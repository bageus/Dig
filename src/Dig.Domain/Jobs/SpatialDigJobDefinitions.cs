using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class SpatialDigJobTarget
{
    public SpatialDigJobTarget(
        CellId targetCell,
        CellId workCell)
    {
        if (targetCell == workCell)
        {
            throw new ArgumentException(
                "The excavation target and work cell must differ.",
                nameof(workCell));
        }

        int deltaX = Math.Abs(targetCell.X - workCell.X);
        int deltaY = Math.Abs(targetCell.Y - workCell.Y);
        int deltaZ = targetCell.Z - workCell.Z;
        bool faceNeighbor = deltaX + deltaY + Math.Abs(deltaZ) == 1;
        bool sourceLayerSideTunnel = deltaX == 1
            && deltaY == 0
            && deltaZ == 1;
        if (!faceNeighbor && !sourceLayerSideTunnel)
        {
            throw new ArgumentException(
                "The work cell must share a face with the target or be an approved side tunnel one depth layer before it.",
                nameof(workCell));
        }

        TargetCell = targetCell;
        WorkCell = workCell;
    }

    public CellId TargetCell { get; }

    public CellId WorkCell { get; }

    public override string ToString()
    {
        return $"SpatialDig:{TargetCell} from {WorkCell}";
    }
}

public sealed class SpatialDigJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] SpatialDigStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public SpatialDigJobDefinition(
        EntityId id,
        SpatialDigJobTarget target,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(
            id,
            priority,
            createdTick,
            retryPolicy,
            SpatialDigStages,
            dependencies)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public SpatialDigJobTarget Target { get; }

    public override string Description => Target.ToString();

    public override JobToolKind? PreferredToolKind => JobToolKind.Mining;

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForPosition(Target.WorkCell),
            ReservationKey.ForDesignation(Target.TargetCell),
        });
    }
}

}