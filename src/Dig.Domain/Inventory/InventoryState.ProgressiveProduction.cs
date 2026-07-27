using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public sealed partial class InventoryState
{
    public Result ConsumeNextReserved(
        EntityId reservationOwnerId,
        ItemId itemId,
        int quantity,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(reservationOwnerId);
        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        ItemStackState[] sources = _stacks.Values
            .Where(stack => stack.ItemId == itemId
                && stack.GetReservedQuantity(reservationOwnerId) > 0)
            .OrderBy(stack => stack.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (sources.Sum(stack => stack.GetReservedQuantity(reservationOwnerId)) < quantity)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
        }

        int remaining = quantity;
        foreach (ItemStackState source in sources)
        {
            int consumed = Math.Min(
                remaining,
                source.GetReservedQuantity(reservationOwnerId));
            source.ConsumeReservedQuantity(reservationOwnerId, consumed);
            Raise(new ItemQuantityReservationChanged(
                tick,
                source.Id,
                reservationOwnerId,
                source.GetReservedQuantity(reservationOwnerId)));
            Raise(new ReservedItemConsumed(
                tick,
                reservationOwnerId,
                source.Id,
                itemId,
                consumed));
            if (source.Quantity == 0)
            {
                _stacks.Remove(source.Id);
            }

            remaining -= consumed;
            if (remaining == 0)
            {
                break;
            }
        }

        IncrementVersion();
        return Result.Success();
    }

    public Result CreateProductionOutputs(
        EntityId orderId,
        IReadOnlyCollection<ItemStackCreation> outputs,
        ItemLocation outputLocation,
        long tick)
    {
        ValidateTick(tick);
        ValidateJobId(orderId);
        if (outputs is null)
        {
            throw new ArgumentNullException(nameof(outputs));
        }

        ItemStackCreation[] values = outputs.ToArray();
        if (values.Length == 0)
        {
            throw new ArgumentException("Production needs outputs.", nameof(outputs));
        }

        EntityId[] ids = values.Select(value => value.StackId).ToArray();
        if (ids.Distinct().Count() != ids.Length || ids.Any(_stacks.ContainsKey))
        {
            return Result.Failure(InventoryErrors.StackAlreadyExists);
        }

        foreach (ItemStackCreation output in values)
        {
            if (output.Quantity > Catalog.Get(output.ItemId).MaximumStackSize)
            {
                return Result.Failure(InventoryErrors.StackSizeExceeded);
            }
        }

        foreach (ItemStackCreation output in values)
        {
            _stacks.Add(
                output.StackId,
                new ItemStackState(
                    output.StackId,
                    output.ItemId,
                    output.Quantity,
                    outputLocation));
        }

        IncrementVersion();
        Raise(new ProductionInventoryCommitted(
            tick,
            orderId,
            Array.Empty<ItemReservationAllocation>(),
            values,
            outputLocation));
        return Result.Success();
    }
}

}
