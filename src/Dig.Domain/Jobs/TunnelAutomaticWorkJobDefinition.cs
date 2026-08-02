using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public enum TunnelAutomaticWorkKind
{
    WoodenSupport = 0,
    JunctionStoneTrim = 1,
}

public sealed class TunnelAutomaticWorkJobDefinition : JobDefinition
{
    public const int AutomaticPriority = 0;

    private static readonly JobStageKind[] WorkStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.AcquireItem,
        JobStageKind.TravelToDestination,
        JobStageKind.DepositItem,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public TunnelAutomaticWorkJobDefinition(
        EntityId id,
        EntityId segmentId,
        TunnelAutomaticWorkKind kind,
        CellId targetCell,
        long createdTick,
        JobRetryPolicy retryPolicy,
        EntityId? sourceStackId = null,
        CellId? sourceCell = null,
        IEnumerable<EntityId>? dependencies = null)
        : base(
            id,
            AutomaticPriority,
            createdTick,
            retryPolicy,
            WorkStages,
            dependencies)
    {
        if (segmentId.IsEmpty)
        {
            throw new ArgumentException("Tunnel segment id cannot be empty.", nameof(segmentId));
        }

        if (sourceStackId.HasValue != sourceCell.HasValue
            || (sourceStackId.HasValue && sourceStackId.Value.IsEmpty))
        {
            throw new ArgumentException(
                "A resolved automatic tunnel job requires both source stack and source cell.",
                nameof(sourceStackId));
        }

        SegmentId = segmentId;
        Kind = kind;
        TargetCell = targetCell;
        SourceStackId = sourceStackId;
        SourceCell = sourceCell;
        RequiredItemId = ResolveRequiredItem(kind);
    }

    public EntityId SegmentId { get; }

    public TunnelAutomaticWorkKind Kind { get; }

    public CellId TargetCell { get; }

    public ItemId RequiredItemId { get; }

    public EntityId? SourceStackId { get; }

    public CellId? SourceCell { get; }

    public bool IsSourceResolved => SourceStackId.HasValue;

    public override string Description => IsSourceResolved
        ? $"TunnelAutomatic:{Kind}:{TargetCell}:Source:{SourceStackId}"
        : $"TunnelAutomatic:{Kind}:{TargetCell}:AwaitingSource";

    public override JobToolKind? PreferredToolKind => JobToolKind.Construction;

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        List<ReservationKey> keys = new List<ReservationKey>
        {
            ReservationKey.ForPosition(TargetCell),
        };
        if (SourceStackId.HasValue)
        {
            keys.Add(ReservationKey.ForItem(SourceStackId.Value));
        }

        return new ReadOnlyCollection<ReservationKey>(keys);
    }

    private static ItemId ResolveRequiredItem(TunnelAutomaticWorkKind kind)
    {
        switch (kind)
        {
            case TunnelAutomaticWorkKind.WoodenSupport:
                return new ItemId("material.mushroom_leg");
            case TunnelAutomaticWorkKind.JunctionStoneTrim:
                return new ItemId("material.stone");
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
}
}
