using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Jobs;

public sealed class HaulJobDefinition : JobDefinition
{
    private static readonly JobStageKind[] HaulStages =
    {
        JobStageKind.AcquireItem,
        JobStageKind.TravelToDestination,
        JobStageKind.DepositItem,
    };

    public HaulJobDefinition(
        EntityId id,
        EntityId sourceStackId,
        ItemId itemId,
        int quantity,
        EntityId destinationStorageId,
        int priority,
        long createdTick,
        JobRetryPolicy retryPolicy,
        IEnumerable<EntityId>? dependencies = null)
        : base(id, priority, createdTick, retryPolicy, HaulStages, dependencies)
    {
        if (sourceStackId.IsEmpty)
        {
            throw new ArgumentException("Source stack id cannot be empty.", nameof(sourceStackId));
        }

        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (destinationStorageId.IsEmpty)
        {
            throw new ArgumentException(
                "Destination storage id cannot be empty.",
                nameof(destinationStorageId));
        }

        SourceStackId = sourceStackId;
        ItemId = itemId;
        Quantity = quantity;
        DestinationStorageId = destinationStorageId;
    }

    public EntityId SourceStackId { get; }

    public ItemId ItemId { get; }

    public int Quantity { get; }

    public EntityId DestinationStorageId { get; }

    public override string Description =>
        $"Haul:{Quantity} {ItemId} {SourceStackId}->{DestinationStorageId}";

    public override IReadOnlyList<ReservationKey> CreateReservationKeys()
    {
        return new ReadOnlyCollection<ReservationKey>(Array.Empty<ReservationKey>());
    }
}
