using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory
{

public readonly struct ItemQuantityReservationSnapshot
{
    public ItemQuantityReservationSnapshot(EntityId jobId, int quantity)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        JobId = jobId;
        Quantity = quantity;
    }

    public EntityId JobId { get; }

    public int Quantity { get; }
}

public sealed class ItemStackSnapshot
{
    public ItemStackSnapshot(
        EntityId stackId,
        ItemId itemId,
        int quantity,
        ItemLocation location,
        IReadOnlyCollection<ItemQuantityReservationSnapshot> reservations,
        int heldQuantity = 0)
    {
        if (stackId.IsEmpty)
        {
            throw new ArgumentException("Stack id cannot be empty.", nameof(stackId));
        }

        if (itemId.IsEmpty)
        {
            throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (reservations is null)
        {
            throw new ArgumentNullException(nameof(reservations));
        }

        if (heldQuantity < 0 || heldQuantity > quantity)
        {
            throw new ArgumentOutOfRangeException(nameof(heldQuantity));
        }

        StackId = stackId;
        ItemId = itemId;
        Quantity = quantity;
        Location = location;
        Reservations = new ReadOnlyCollection<ItemQuantityReservationSnapshot>(
            reservations.OrderBy(item => item.JobId.ToString(), StringComparer.Ordinal).ToArray());
        ReservedQuantity = Reservations.Sum(item => item.Quantity);
        HeldQuantity = heldQuantity;
        if (ReservedQuantity + HeldQuantity > Quantity)
        {
            throw new ArgumentException(
                "Reserved and held quantities cannot exceed stack quantity.");
        }
    }

    public EntityId StackId { get; }

    public ItemId ItemId { get; }

    public int Quantity { get; }

    public int ReservedQuantity { get; }

    public int HeldQuantity { get; }

    public int AvailableQuantity => Quantity - ReservedQuantity - HeldQuantity;

    public ItemLocation Location { get; }

    public IReadOnlyList<ItemQuantityReservationSnapshot> Reservations { get; }
}

public sealed class InventorySnapshot
{
    public InventorySnapshot(
        long version,
        IReadOnlyCollection<ItemStackSnapshot> stacks,
        IReadOnlyCollection<HeldItemReferenceSnapshot>? heldItems = null)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (stacks is null)
        {
            throw new ArgumentNullException(nameof(stacks));
        }

        ItemStackSnapshot[] orderedStacks = stacks
            .OrderBy(item => item.StackId.ToString(), StringComparer.Ordinal)
            .ToArray();
        HeldItemReferenceSnapshot[] orderedHeld = (heldItems
                ?? Array.Empty<HeldItemReferenceSnapshot>())
            .OrderBy(item => item.ResidentId.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (orderedHeld.Select(item => item.ResidentId).Distinct().Count()
            != orderedHeld.Length)
        {
            throw new ArgumentException(
                "A resident cannot hold more than one item.",
                nameof(heldItems));
        }

        Dictionary<EntityId, ItemStackSnapshot> byStack = orderedStacks
            .ToDictionary(item => item.StackId);
        foreach (IGrouping<EntityId, HeldItemReferenceSnapshot> group in orderedHeld
            .GroupBy(item => item.StackId))
        {
            if (!byStack.TryGetValue(group.Key, out ItemStackSnapshot? stack)
                || group.Sum(item => item.Quantity) != stack.HeldQuantity)
            {
                throw new ArgumentException(
                    "Held references must match stack held quantities.",
                    nameof(heldItems));
            }
        }

        if (orderedStacks.Any(stack => stack.HeldQuantity > 0
            && !orderedHeld.Any(item => item.StackId == stack.StackId)))
        {
            throw new ArgumentException(
                "Every held stack quantity requires a held reference.",
                nameof(heldItems));
        }

        Version = version;
        Stacks = new ReadOnlyCollection<ItemStackSnapshot>(orderedStacks);
        HeldItems = new ReadOnlyCollection<HeldItemReferenceSnapshot>(orderedHeld);
    }

    public long Version { get; }

    public IReadOnlyList<ItemStackSnapshot> Stacks { get; }

    public IReadOnlyList<HeldItemReferenceSnapshot> HeldItems { get; }

    public int GetTotal(ItemId itemId)
    {
        return Stacks.Where(stack => stack.ItemId == itemId).Sum(stack => stack.Quantity);
    }

    public int GetQuantityAt(ItemId itemId, ItemLocation location)
    {
        return Stacks
            .Where(stack => stack.ItemId == itemId && stack.Location == location)
            .Sum(stack => stack.Quantity);
    }
}

}
