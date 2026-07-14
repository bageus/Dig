using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.Inventory;

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
        IReadOnlyCollection<ItemQuantityReservationSnapshot> reservations)
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

        StackId = stackId;
        ItemId = itemId;
        Quantity = quantity;
        Location = location;
        Reservations = new ReadOnlyCollection<ItemQuantityReservationSnapshot>(
            reservations.OrderBy(item => item.JobId.ToString(), StringComparer.Ordinal).ToArray());
        ReservedQuantity = Reservations.Sum(item => item.Quantity);
    }

    public EntityId StackId { get; }

    public ItemId ItemId { get; }

    public int Quantity { get; }

    public int ReservedQuantity { get; }

    public int AvailableQuantity => Quantity - ReservedQuantity;

    public ItemLocation Location { get; }

    public IReadOnlyList<ItemQuantityReservationSnapshot> Reservations { get; }
}

public sealed class InventorySnapshot
{
    public InventorySnapshot(long version, IReadOnlyCollection<ItemStackSnapshot> stacks)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (stacks is null)
        {
            throw new ArgumentNullException(nameof(stacks));
        }

        Version = version;
        Stacks = new ReadOnlyCollection<ItemStackSnapshot>(
            stacks.OrderBy(item => item.StackId.ToString(), StringComparer.Ordinal).ToArray());
    }

    public long Version { get; }

    public IReadOnlyList<ItemStackSnapshot> Stacks { get; }

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
