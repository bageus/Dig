using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class BuildingBoxPickupJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] PickupStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.AcquireItem,
    };

    public BuildingBoxPickupJobDefinition(
        EntityId id,
        EntityId stackId,
        CellId sourceCell,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(
            id,
            priority,
            createdTick,
            retryPolicy,
            PickupStages,
            dependencies)
    {
        if (stackId.IsEmpty)
        {
            throw new ArgumentException("BuildingBox stack id is required.", nameof(stackId));
        }

        StackId = stackId;
        SourceCell = sourceCell;
    }

    public EntityId StackId { get; }

    public CellId SourceCell { get; }

    public override string Description => $"Pick up BuildingBox {StackId}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForItem(StackId),
            ReservationKey.ForPosition(SourceCell),
        });
    }
}

}
