using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Storage
{

public sealed class StorageZoneStateSnapshot
{
    public StorageZoneStateSnapshot(
        EntityId id,
        string name,
        int priority,
        int capacity,
        CellId cell,
        bool acceptsAll,
        IEnumerable<ItemId> allowedItems,
        IEnumerable<ItemCategoryId> allowedCategories)
    {
        Id = id;
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Storage name is required.", nameof(name));
        Name = name.Trim();
        Priority = priority;
        Capacity = capacity;
        Cell = cell;
        AcceptsAll = acceptsAll;
        if (allowedItems is null || allowedCategories is null) throw new ArgumentNullException();
        AllowedItems = new ReadOnlyCollection<ItemId>(allowedItems.Distinct().OrderBy(value => value).ToArray());
        AllowedCategories = new ReadOnlyCollection<ItemCategoryId>(allowedCategories.Distinct().OrderBy(value => value).ToArray());
    }

    public EntityId Id { get; }
    public string Name { get; }
    public int Priority { get; }
    public int Capacity { get; }
    public CellId Cell { get; }
    public bool AcceptsAll { get; }
    public IReadOnlyList<ItemId> AllowedItems { get; }
    public IReadOnlyList<ItemCategoryId> AllowedCategories { get; }
}

public sealed class StorageReservationStateSnapshot
{
    public StorageReservationStateSnapshot(
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

public sealed class StorageStateSnapshot
{
    public StorageStateSnapshot(
        long version,
        IEnumerable<StorageZoneStateSnapshot> zones,
        IEnumerable<StorageReservationStateSnapshot> reservations)
    {
        Version = version;
        Zones = new ReadOnlyCollection<StorageZoneStateSnapshot>((zones ?? throw new ArgumentNullException(nameof(zones))).ToArray());
        Reservations = new ReadOnlyCollection<StorageReservationStateSnapshot>((reservations ?? throw new ArgumentNullException(nameof(reservations))).ToArray());
    }

    public long Version { get; }
    public IReadOnlyList<StorageZoneStateSnapshot> Zones { get; }
    public IReadOnlyList<StorageReservationStateSnapshot> Reservations { get; }
}

public sealed partial class StorageState
{
    public StorageStateSnapshot CreateSnapshot()
    {
        StorageZoneStateSnapshot[] zones = _zones.Values
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .Select(value => new StorageZoneStateSnapshot(
                value.Id,
                value.Name,
                value.Priority,
                value.Capacity,
                value.Cell,
                value.Filter.AcceptsAll,
                value.Filter.AllowedItems,
                value.Filter.AllowedCategories))
            .ToArray();
        StorageReservationStateSnapshot[] reservations = _reservations.Values
            .OrderBy(value => value.JobId.ToString(), StringComparer.Ordinal)
            .Select(value => new StorageReservationStateSnapshot(
                value.JobId,
                value.ZoneId,
                value.ItemId,
                value.Quantity))
            .ToArray();
        return new StorageStateSnapshot(Version, zones, reservations);
    }

    public static Result<StorageState> Restore(StorageStateSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (snapshot.Version < 0)
        {
            return Result<StorageState>.Failure(new DomainError(
                "storage.restore.invalid_version",
                "Storage version cannot be negative."));
        }

        StorageState storage = new StorageState();
        foreach (StorageZoneStateSnapshot saved in snapshot.Zones)
        {
            if (saved.Id.IsEmpty || string.IsNullOrWhiteSpace(saved.Name)
                || saved.Priority < 0 || saved.Priority > 1000 || saved.Capacity <= 0)
            {
                return Result<StorageState>.Failure(new DomainError(
                    "storage.restore.invalid_zone",
                    "Saved storage zone is invalid."));
            }

            StorageFilter filter;
            try
            {
                filter = new StorageFilter(
                    saved.AcceptsAll,
                    saved.AllowedItems,
                    saved.AllowedCategories);
            }
            catch (ArgumentException exception)
            {
                return Result<StorageState>.Failure(new DomainError(
                    "storage.restore.invalid_filter",
                    exception.Message));
            }

            if (!storage._zones.TryAdd(
                    saved.Id,
                    new StorageZoneDefinition(
                        saved.Id,
                        saved.Name,
                        saved.Priority,
                        saved.Capacity,
                        filter,
                        saved.Cell)))
            {
                return Result<StorageState>.Failure(new DomainError(
                    "storage.restore.duplicate_zone",
                    "Saved storage zones contain a duplicate id."));
            }
        }

        foreach (StorageReservationStateSnapshot saved in snapshot.Reservations)
        {
            if (saved.JobId.IsEmpty || saved.ZoneId.IsEmpty
                || saved.ItemId.IsEmpty || saved.Quantity <= 0
                || !storage._zones.ContainsKey(saved.ZoneId)
                || storage._reservations.ContainsKey(saved.JobId))
            {
                return Result<StorageState>.Failure(new DomainError(
                    "storage.restore.invalid_reservation",
                    "Saved storage reservation is invalid."));
            }

            storage._reservations.Add(
                saved.JobId,
                new StorageReservationSnapshot(
                    saved.JobId,
                    saved.ZoneId,
                    saved.ItemId,
                    saved.Quantity));
        }

        storage.Version = snapshot.Version;
        return Result<StorageState>.Success(storage);
    }
}

}
