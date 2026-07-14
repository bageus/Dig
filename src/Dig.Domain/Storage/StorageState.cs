using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Storage;

public static class StorageErrors
{
    public static readonly DomainError ZoneAlreadyExists = new DomainError(
        "storage.zone_already_exists",
        "A storage zone with the same id already exists.");

    public static readonly DomainError ZoneNotFound = new DomainError(
        "storage.zone_not_found",
        "The requested storage zone does not exist.");

    public static readonly DomainError ItemRejected = new DomainError(
        "storage.item_rejected",
        "The storage filter rejects this item.");

    public static readonly DomainError CapacityExceeded = new DomainError(
        "storage.capacity_exceeded",
        "The storage zone does not have enough unreserved capacity.");

    public static readonly DomainError JobAlreadyReserved = new DomainError(
        "storage.job_already_reserved",
        "The hauling job already owns an incoming storage reservation.");
}

public readonly struct StorageReservationSnapshot
{
    public StorageReservationSnapshot(
        EntityId jobId,
        EntityId zoneId,
        ItemId itemId,
        int quantity)
    {
        JobId = jobId;
        ZoneId = zoneId;
        ItemId = itemId;
        Quantity = quantity;
    }

    public EntityId JobId { get; }

    public EntityId ZoneId { get; }

    public ItemId ItemId { get; }

    public int Quantity { get; }
}

public sealed class StorageZoneSnapshot
{
    public StorageZoneSnapshot(
        StorageZoneDefinition definition,
        int occupiedQuantity,
        int reservedIncomingQuantity)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        OccupiedQuantity = occupiedQuantity;
        ReservedIncomingQuantity = reservedIncomingQuantity;
    }

    public StorageZoneDefinition Definition { get; }

    public int OccupiedQuantity { get; }

    public int ReservedIncomingQuantity { get; }

    public int AvailableCapacity =>
        Definition.Capacity - OccupiedQuantity - ReservedIncomingQuantity;
}

public sealed class StorageReservationChanged : IDomainEvent
{
    public StorageReservationChanged(
        long tick,
        EntityId jobId,
        EntityId zoneId,
        int reservedQuantity)
    {
        Tick = tick;
        JobId = jobId;
        ZoneId = zoneId;
        ReservedQuantity = reservedQuantity;
    }

    public long Tick { get; }

    public EntityId JobId { get; }

    public EntityId ZoneId { get; }

    public int ReservedQuantity { get; }
}

public sealed partial class StorageState : AggregateRoot
{
    private readonly Dictionary<EntityId, StorageZoneDefinition> _zones =
        new Dictionary<EntityId, StorageZoneDefinition>();
    private readonly Dictionary<EntityId, StorageReservationSnapshot> _reservations =
        new Dictionary<EntityId, StorageReservationSnapshot>();

    public long Version { get; private set; }

    public Result AddZone(StorageZoneDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (!_zones.TryAdd(definition.Id, definition))
        {
            return Result.Failure(StorageErrors.ZoneAlreadyExists);
        }

        IncrementVersion();
        return Result.Success();
    }

    public Result ReserveIncoming(
        EntityId zoneId,
        EntityId jobId,
        ItemDefinition item,
        int quantity,
        int occupiedQuantity,
        long tick)
    {
        ValidateIds(zoneId, jobId);
        ValidateQuantities(quantity, occupiedQuantity);
        ValidateTick(tick);
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (!_zones.TryGetValue(zoneId, out StorageZoneDefinition? zone))
        {
            return Result.Failure(StorageErrors.ZoneNotFound);
        }

        if (_reservations.ContainsKey(jobId))
        {
            return Result.Failure(StorageErrors.JobAlreadyReserved);
        }

        if (!zone.Filter.Accepts(item))
        {
            return Result.Failure(StorageErrors.ItemRejected);
        }

        int reserved = GetReservedIncoming(zoneId);
        if (checked(occupiedQuantity + reserved + quantity) > zone.Capacity)
        {
            return Result.Failure(StorageErrors.CapacityExceeded);
        }

        _reservations.Add(
            jobId,
            new StorageReservationSnapshot(jobId, zoneId, item.Id, quantity));
        IncrementVersion();
        Raise(new StorageReservationChanged(tick, jobId, zoneId, quantity));
        return Result.Success();
    }

    public int ReleaseIncoming(EntityId jobId, long tick)
    {
        if (jobId.IsEmpty)
        {
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        }

        ValidateTick(tick);
        if (!_reservations.Remove(jobId, out StorageReservationSnapshot reservation))
        {
            return 0;
        }

        IncrementVersion();
        Raise(new StorageReservationChanged(tick, jobId, reservation.ZoneId, 0));
        return reservation.Quantity;
    }

    public StorageReservationSnapshot? GetReservation(EntityId jobId)
    {
        return _reservations.TryGetValue(jobId, out StorageReservationSnapshot value)
            ? value
            : null;
    }

    public StorageZoneDefinition? GetZone(EntityId zoneId)
    {
        return _zones.TryGetValue(zoneId, out StorageZoneDefinition? zone) ? zone : null;
    }

    public IReadOnlyList<StorageZoneSnapshot> FindDestinations(
        ItemDefinition item,
        int quantity,
        Func<EntityId, int> occupiedQuantityProvider)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (occupiedQuantityProvider is null)
        {
            throw new ArgumentNullException(nameof(occupiedQuantityProvider));
        }

        StorageZoneSnapshot[] result = _zones.Values
            .Where(zone => zone.Filter.Accepts(item))
            .Select(zone => new StorageZoneSnapshot(
                zone,
                occupiedQuantityProvider(zone.Id),
                GetReservedIncoming(zone.Id)))
            .Where(zone => zone.AvailableCapacity >= quantity)
            .OrderByDescending(zone => zone.Definition.Priority)
            .ThenBy(zone => zone.Definition.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<StorageZoneSnapshot>(result);
    }

    public IReadOnlyList<StorageReservationSnapshot> GetReservations()
    {
        StorageReservationSnapshot[] values = _reservations.Values
            .OrderBy(value => value.JobId.ToString(), StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<StorageReservationSnapshot>(values);
    }

    private int GetReservedIncoming(EntityId zoneId)
    {
        return _reservations.Values
            .Where(reservation => reservation.ZoneId == zoneId)
            .Sum(reservation => reservation.Quantity);
    }

    private void IncrementVersion()
    {
        Version = checked(Version + 1);
    }

    private static void ValidateIds(EntityId zoneId, EntityId jobId)
    {
        if (zoneId.IsEmpty || jobId.IsEmpty)
        {
            throw new ArgumentException("Storage and job ids cannot be empty.");
        }
    }

    private static void ValidateQuantities(int quantity, int occupiedQuantity)
    {
        if (quantity <= 0 || occupiedQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}
