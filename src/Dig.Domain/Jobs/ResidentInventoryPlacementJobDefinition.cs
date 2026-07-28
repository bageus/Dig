using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public sealed class ResidentInventoryPlacementJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] PlacementStages =
    {
        JobStageKind.TravelToDestination,
        JobStageKind.DepositItem,
    };

    public ResidentInventoryPlacementJobDefinition(
        EntityId id,
        EntityId residentId,
        EntityId stackId,
        int quantity,
        CellId destinationCell,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(
            id,
            priority,
            createdTick,
            retryPolicy,
            PlacementStages,
            dependencies)
    {
        if (residentId.IsEmpty || stackId.IsEmpty)
        {
            throw new ArgumentException("Resident and stack ids are required.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        ResidentId = residentId;
        StackId = stackId;
        Quantity = quantity;
        DestinationCell = destinationCell;
    }

    public EntityId ResidentId { get; }

    public EntityId StackId { get; }

    public int Quantity { get; }

    public CellId DestinationCell { get; }

    public override string Description =>
        $"Place inventory item {StackId} x{Quantity} at {DestinationCell}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(new[]
        {
            ReservationKey.ForItem(StackId),
            ReservationKey.ForPosition(DestinationCell),
        });
    }
}
}
