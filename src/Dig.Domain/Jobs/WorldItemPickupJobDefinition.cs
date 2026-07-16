using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class WorldItemPickupJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] PickupStages =
    {
        JobStageKind.TravelToTarget,
        JobStageKind.AcquireItem,
    };

    public WorldItemPickupJobDefinition(
        EntityId id,
        EntityId stackId,
        int quantity,
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
            throw new ArgumentException("World item stack id is required.", nameof(stackId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        StackId = stackId;
        Quantity = quantity;
        SourceCell = sourceCell;
    }

    public EntityId StackId { get; }

    public int Quantity { get; }

    public CellId SourceCell { get; }

    public override string Description => $"Pick up world item {StackId} x{Quantity}";

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
