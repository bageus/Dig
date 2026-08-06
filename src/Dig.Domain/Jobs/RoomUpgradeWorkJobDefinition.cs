using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class RoomUpgradeWorkJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] WorkStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.PerformWork,
        JobStageKind.Finalize,
    };

    public RoomUpgradeWorkJobDefinition(
        EntityId id,
        EntityId roomInfrastructureId,
        CellId workCell,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(id, priority, createdTick, retryPolicy, WorkStages, dependencies)
    {
        if (roomInfrastructureId.IsEmpty)
        {
            throw new ArgumentException(
                "Room infrastructure id cannot be empty.",
                nameof(roomInfrastructureId));
        }

        RoomInfrastructureId = roomInfrastructureId;
        WorkCell = workCell;
    }

    public EntityId RoomInfrastructureId { get; }

    public CellId WorkCell { get; }

    public override string Description =>
        $"RoomUpgrade:{RoomInfrastructureId}@{WorkCell}";

    public override JobToolKind? PreferredToolKind => JobToolKind.Construction;

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForPosition(WorkCell),
        });
    }
}

}
