using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public enum TunnelManualReinforcementKind
{
    WoodenSupport = 0,
    StoneFloorTrim = 1,
    JunctionStoneTrim = 2,
}

public sealed class TunnelManualReinforcementJobDefinition : JobDefinition
{
    public const int ForcedPriority = 900;

    private static readonly JobStageKind[] WorkStages =
    {
        JobStageKind.TravelToDestination,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public TunnelManualReinforcementJobDefinition(
        EntityId id,
        EntityId residentId,
        EntityId sourceStackId,
        EntityId segmentId,
        TunnelManualReinforcementKind kind,
        CellId targetCell,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(
            id,
            ForcedPriority,
            createdTick,
            retryPolicy,
            WorkStages,
            dependencies)
    {
        if (residentId.IsEmpty || sourceStackId.IsEmpty || segmentId.IsEmpty)
        {
            throw new ArgumentException(
                "Resident, source stack and segment ids are required.");
        }

        if (!Enum.IsDefined(typeof(TunnelManualReinforcementKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ResidentId = residentId;
        SourceStackId = sourceStackId;
        SegmentId = segmentId;
        Kind = kind;
        TargetCell = targetCell;
        RequiredItemId = kind == TunnelManualReinforcementKind.WoodenSupport
            ? new ItemId("material.mushroom_leg")
            : new ItemId("material.stone");
    }

    public EntityId ResidentId { get; }
    public EntityId SourceStackId { get; }
    public EntityId SegmentId { get; }
    public TunnelManualReinforcementKind Kind { get; }
    public CellId TargetCell { get; }
    public ItemId RequiredItemId { get; }

    public override string Description =>
        $"TunnelManual:{Kind}:{TargetCell}:Source:{SourceStackId}:Resident:{ResidentId}";

    public override JobToolKind? PreferredToolKind => JobToolKind.Construction;

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForItem(SourceStackId),
            ReservationKey.ForPosition(TargetCell),
        });
    }
}

}
