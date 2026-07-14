using System;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public readonly struct InventoryStackInspection
{
    public InventoryStackInspection(EntityId stackId, int quantity, int reservedQuantity)
    {
        StackId = stackId;
        Quantity = quantity;
        ReservedQuantity = reservedQuantity;
    }

    public EntityId StackId { get; }

    public int Quantity { get; }

    public int ReservedQuantity { get; }
}

public interface IInventoryInspectionVisitor
{
    void VisitStack(InventoryStackInspection stack);

    void VisitReservation(EntityId stackId, EntityId ownerId, int quantity);
}

public sealed partial class InventoryState
{
    public void VisitInspection(IInventoryInspectionVisitor visitor)
    {
        if (visitor is null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        foreach (ItemStackState stack in _stacks.Values)
        {
            visitor.VisitStack(new InventoryStackInspection(
                stack.Id,
                stack.Quantity,
                stack.ReservedQuantity));
            stack.VisitReservations(visitor);
        }
    }

    public int GetReservedQuantity(EntityId stackId, EntityId ownerId)
    {
        ItemStackState? stack = Find(stackId);
        return stack?.GetReservedQuantity(ownerId) ?? 0;
    }
}
}
