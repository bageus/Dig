using Dig.Domain.Core;

namespace Dig.Domain.Inventory;

public sealed class ReservedItemConsumed : IDomainEvent
{
    public ReservedItemConsumed(
        long tick,
        EntityId reservationOwnerId,
        EntityId stackId,
        ItemId itemId,
        int quantity)
    {
        Tick = tick;
        ReservationOwnerId = reservationOwnerId;
        StackId = stackId;
        ItemId = itemId;
        Quantity = quantity;
    }

    public long Tick { get; }

    public EntityId ReservationOwnerId { get; }

    public EntityId StackId { get; }

    public ItemId ItemId { get; }

    public int Quantity { get; }
}

public sealed partial class InventoryState
{
    public Result ConsumeReserved(
        EntityId reservationOwnerId,
        EntityId stackId,
        int quantity,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(reservationOwnerId);
        ItemStackState? stack = Find(stackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        if (quantity <= 0)
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        if (stack.GetReservedQuantity(reservationOwnerId) < quantity)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
        }

        ItemId itemId = stack.ItemId;
        stack.ConsumeReservedQuantity(reservationOwnerId, quantity);
        Raise(new ItemQuantityReservationChanged(
            tick,
            stack.Id,
            reservationOwnerId,
            stack.GetReservedQuantity(reservationOwnerId)));
        if (stack.Quantity == 0)
        {
            _stacks.Remove(stack.Id);
        }

        IncrementVersion();
        Raise(new ReservedItemConsumed(
            tick,
            reservationOwnerId,
            stackId,
            itemId,
            quantity));
        return Result.Success();
    }
}
