using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public enum TunnelManualWorkKind
{
    WoodenSupport = 0,
    JunctionStoneTrim = 1,
    StoneFloorTrim = 2,
}

public sealed class TunnelManualWorkJobDefinition : JobDefinition
{
    public const int ManualPriority = 100;

    private static readonly JobStageKind[] WorkStages =
    {
        JobStageKind.TravelToDestination,
        JobStageKind.DepositItem,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public TunnelManualWorkJobDefinition(
        EntityId id,
        EntityId ownerResidentId,
        EntityId sourceStackId,
        EntityId segmentId,
        TunnelManualWorkKind kind,
        CellId targetCell,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(
            id,
            ManualPriority,
            createdTick,
            retryPolicy,
            WorkStages,
            dependencies)
    {
        if (ownerResidentId.IsEmpty || sourceStackId.IsEmpty || segmentId.IsEmpty)
        {
            throw new ArgumentException(
                "Manual tunnel work requires owner, source stack and segment ids.");
        }

        if (!Enum.IsDefined(typeof(TunnelManualWorkKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        OwnerResidentId = ownerResidentId;
        SourceStackId = sourceStackId;
        SegmentId = segmentId;
        Kind = kind;
        TargetCell = targetCell;
        RequiredItemId = ResolveRequiredItem(kind);
    }

    public EntityId OwnerResidentId { get; }
    public EntityId SourceStackId { get; }
    public EntityId SegmentId { get; }
    public TunnelManualWorkKind Kind { get; }
    public CellId TargetCell { get; }
    public ItemId RequiredItemId { get; }

    public override string Description =>
        $"TunnelManual:{Kind}:{TargetCell}:Owner:{OwnerResidentId}:Source:{SourceStackId}";

    public override JobToolKind? PreferredToolKind => JobToolKind.Construction;

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForPosition(TargetCell),
            ReservationKey.ForItem(SourceStackId),
        });
    }

    private static ItemId ResolveRequiredItem(TunnelManualWorkKind kind)
    {
        return kind switch
        {
            TunnelManualWorkKind.WoodenSupport =>
                new ItemId("material.mushroom_leg"),
            TunnelManualWorkKind.JunctionStoneTrim =>
                new ItemId("material.stone"),
            TunnelManualWorkKind.StoneFloorTrim =>
                new ItemId("material.stone"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}

}
