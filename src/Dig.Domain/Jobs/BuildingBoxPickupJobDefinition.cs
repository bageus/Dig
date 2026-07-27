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

    private static readonly JobStageKind[] WorldRelocationStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.AcquireItem,
        JobStageKind.TravelToDestination,
        JobStageKind.DepositItem,
    };

    private static readonly JobStageKind[] HeldRelocationStages =
    {
        JobStageKind.TravelToDestination,
        JobStageKind.DepositItem,
    };

    public BuildingBoxPickupJobDefinition(
        EntityId id,
        EntityId stackId,
        CellId sourceCell,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(id, priority, createdTick, retryPolicy, PickupStages, dependencies)
    {
        ValidateStackId(stackId);
        StackId = stackId;
        SourceCell = sourceCell;
        DestinationCell = null;
        StartsHeld = false;
    }

    public BuildingBoxPickupJobDefinition(
        EntityId id,
        EntityId stackId,
        CellId sourceCell,
        CellId destinationCell,
        bool startsHeld,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(
            id,
            priority,
            createdTick,
            retryPolicy,
            startsHeld ? HeldRelocationStages : WorldRelocationStages,
            dependencies)
    {
        ValidateStackId(stackId);
        StackId = stackId;
        SourceCell = sourceCell;
        DestinationCell = destinationCell;
        StartsHeld = startsHeld;
    }

    public EntityId StackId { get; }

    public CellId SourceCell { get; }

    public CellId? DestinationCell { get; }

    public bool StartsHeld { get; }

    public bool IsRelocation => DestinationCell.HasValue;

    public override string Description => IsRelocation
        ? $"Relocate BuildingBox {StackId} to {DestinationCell!.Value}"
        : $"Pick up BuildingBox {StackId}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        List<ReservationKey> keys = new List<ReservationKey>
        {
            ReservationKey.ForItem(StackId),
        };
        if (!StartsHeld)
        {
            keys.Add(ReservationKey.ForPosition(SourceCell));
        }

        if (DestinationCell.HasValue)
        {
            keys.Add(ReservationKey.ForPosition(DestinationCell.Value));
        }

        return new ReadOnlyCollection<ReservationKey>(keys);
    }

    private static void ValidateStackId(EntityId stackId)
    {
        if (stackId.IsEmpty)
        {
            throw new ArgumentException("BuildingBox stack id is required.", nameof(stackId));
        }
    }
}

}
