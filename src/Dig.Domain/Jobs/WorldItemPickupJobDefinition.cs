using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Jobs
{

public enum WorldItemPickupCompletionAction
{
    None = 0,
    UseConsumable = 1,
}

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
        IEnumerable<EntityId>? dependencies = null,
        WorldItemPickupCompletionAction completionAction =
            WorldItemPickupCompletionAction.None)
        : this(
            id,
            stackId,
            quantity,
            sourceCell,
            ItemLocation.InWorld(sourceCell),
            destinationStackId: default,
            priority: priority,
            createdTick: createdTick,
            retryPolicy: retryPolicy,
            dependencies: dependencies,
            completionAction: completionAction)
    {
    }

    public WorldItemPickupJobDefinition(
        EntityId id,
        EntityId stackId,
        int quantity,
        CellId sourceCell,
        ItemLocation sourceLocation,
        EntityId destinationStackId,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null,
        WorldItemPickupCompletionAction completionAction =
            WorldItemPickupCompletionAction.None)
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
            throw new ArgumentException("Pickup stack id is required.", nameof(stackId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (sourceLocation.Kind != ItemLocationKind.World
            && sourceLocation.Kind != ItemLocationKind.BuildingInventory)
        {
            throw new ArgumentException(
                "Pickup source must be a world or building location.",
                nameof(sourceLocation));
        }

        if (!Enum.IsDefined(typeof(WorldItemPickupCompletionAction), completionAction))
        {
            throw new ArgumentOutOfRangeException(nameof(completionAction));
        }

        StackId = stackId;
        Quantity = quantity;
        SourceCell = sourceCell;
        SourceLocation = sourceLocation;
        DestinationStackId = destinationStackId;
        CompletionAction = completionAction;
    }

    public EntityId StackId { get; }

    public int Quantity { get; }

    public CellId SourceCell { get; }

    public ItemLocation SourceLocation { get; }

    public EntityId DestinationStackId { get; }

    public WorldItemPickupCompletionAction CompletionAction { get; }

    public override string Description => $"Pick up item {StackId} x{Quantity}";

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
