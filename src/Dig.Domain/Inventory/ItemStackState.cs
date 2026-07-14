using Dig.Domain.Core;

namespace Dig.Domain.Inventory;

internal sealed class ItemStackState
{
    private readonly Dictionary<EntityId, int> _reservations =
        new Dictionary<EntityId, int>();

    public ItemStackState(
        EntityId id,
        ItemId itemId,
        int quantity,
        ItemLocation location)
    {
        Id = id;
        ItemId = itemId;
        Quantity = quantity;
        Location = location;
    }

    public EntityId Id { get; }

    public ItemId ItemId { get; }

    public int Quantity { get; private set; }

    public ItemLocation Location { get; private set; }

    public int ReservedQuantity
    {
        get
        {
            int quantity = 0;
            foreach (int reserved in _reservations.Values)
            {
                quantity = checked(quantity + reserved);
            }

            return quantity;
        }
    }

    public int AvailableQuantity => Quantity - ReservedQuantity;

    public int GetReservedQuantity(EntityId jobId)
    {
        return _reservations.TryGetValue(jobId, out int quantity) ? quantity : 0;
    }

    public void VisitReservations(IInventoryInspectionVisitor visitor)
    {
        foreach (KeyValuePair<EntityId, int> reservation in _reservations)
        {
            visitor.VisitReservation(Id, reservation.Key, reservation.Value);
        }
    }

    public void Reserve(EntityId jobId, int quantity)
    {
        int current = GetReservedQuantity(jobId);
        _reservations[jobId] = checked(current + quantity);
    }

    public int Release(EntityId jobId)
    {
        if (!_reservations.TryGetValue(jobId, out int quantity))
        {
            return 0;
        }

        _reservations.Remove(jobId);
        return quantity;
    }

    public void ConsumeReservation(EntityId jobId, int quantity)
    {
        int reserved = GetReservedQuantity(jobId);
        int remaining = reserved - quantity;
        if (remaining < 0)
        {
            throw new InvalidOperationException("Cannot consume more than the reserved quantity.");
        }

        if (remaining == 0)
        {
            _reservations.Remove(jobId);
        }
        else
        {
            _reservations[jobId] = remaining;
        }
    }

    public void ConsumeReservedQuantity(EntityId jobId, int quantity)
    {
        if (quantity <= 0 || quantity > Quantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        ConsumeReservation(jobId, quantity);
        Quantity = checked(Quantity - quantity);
    }

    public void ConsumeAvailable(int quantity)
    {
        if (quantity <= 0 || quantity > AvailableQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        Quantity = checked(Quantity - quantity);
    }

    public void MoveFull(ItemLocation destination)
    {
        Location = destination;
    }

    public ItemStackState Split(EntityId newStackId, int quantity, ItemLocation destination)
    {
        Quantity = checked(Quantity - quantity);
        return new ItemStackState(newStackId, ItemId, quantity, destination);
    }

    public ItemStackSnapshot CreateSnapshot()
    {
        ItemQuantityReservationSnapshot[] reservations = _reservations
            .Select(pair => new ItemQuantityReservationSnapshot(pair.Key, pair.Value))
            .ToArray();
        return new ItemStackSnapshot(Id, ItemId, Quantity, Location, reservations);
    }
}
