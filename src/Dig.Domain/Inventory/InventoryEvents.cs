using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed class ItemStackMoved : IDomainEvent
{
    public ItemStackMoved(
        long tick,
        EntityId sourceStackId,
        EntityId destinationStackId,
        ItemId itemId,
        int quantity,
        ItemLocation source,
        ItemLocation destination)
    {
        Tick = tick;
        SourceStackId = sourceStackId;
        DestinationStackId = destinationStackId;
        ItemId = itemId;
        Quantity = quantity;
        Source = source;
        Destination = destination;
    }

    public long Tick { get; }

    public EntityId SourceStackId { get; }

    public EntityId DestinationStackId { get; }

    public ItemId ItemId { get; }

    public int Quantity { get; }

    public ItemLocation Source { get; }

    public ItemLocation Destination { get; }
}

public sealed class ItemQuantityReservationChanged : IDomainEvent
{
    public ItemQuantityReservationChanged(
        long tick,
        EntityId stackId,
        EntityId jobId,
        int reservedQuantity)
    {
        Tick = tick;
        StackId = stackId;
        JobId = jobId;
        ReservedQuantity = reservedQuantity;
    }

    public long Tick { get; }

    public EntityId StackId { get; }

    public EntityId JobId { get; }

    public int ReservedQuantity { get; }
}

public sealed class HeldItemReferenceChanged : IDomainEvent
{
    public HeldItemReferenceChanged(
        long tick,
        EntityId residentId,
        EntityId stackId,
        int quantity,
        HeldItemPurpose purpose,
        bool isActive)
    {
        Tick = tick;
        ResidentId = residentId;
        StackId = stackId;
        Quantity = quantity;
        Purpose = purpose;
        IsActive = isActive;
    }

    public long Tick { get; }

    public EntityId ResidentId { get; }

    public EntityId StackId { get; }

    public int Quantity { get; }

    public HeldItemPurpose Purpose { get; }

    public bool IsActive { get; }
}

}
