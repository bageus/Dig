using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Storage;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static class StorageSaveAdapter
{
    public static StorageSaveData Encode(StorageState storage)
    {
        if (storage is null) throw new ArgumentNullException(nameof(storage));
        StorageStateSnapshot snapshot = storage.CreateSnapshot();
        StorageSaveData data = new StorageSaveData { Version = snapshot.Version };
        foreach (StorageZoneStateSnapshot zone in snapshot.Zones)
        {
            data.Zones.Add(new StorageZoneSaveData
            {
                Id = zone.Id.ToString(),
                Name = zone.Name,
                Priority = zone.Priority,
                Capacity = zone.Capacity,
                X = zone.Cell.X,
                Y = zone.Cell.Y,
                Z = zone.Cell.Z,
                AcceptsAll = zone.AcceptsAll,
                AllowedItems = zone.AllowedItems.Select(value => value.ToString()).ToList(),
                AllowedCategories = zone.AllowedCategories.Select(value => value.ToString()).ToList(),
            });
        }
        foreach (StorageReservationStateSnapshot reservation in snapshot.Reservations)
        {
            data.Reservations.Add(new StorageReservationSaveData
            {
                JobId = reservation.JobId.ToString(),
                ZoneId = reservation.ZoneId.ToString(),
                ItemId = reservation.ItemId.ToString(),
                Quantity = reservation.Quantity,
            });
        }
        return data;
    }

    public static Result<StorageState> Decode(StorageSaveData data)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        try
        {
            StorageStateSnapshot snapshot = new StorageStateSnapshot(
                data.Version,
                data.Zones.Select(zone => new StorageZoneStateSnapshot(
                    EntityId.Parse(zone.Id), zone.Name, zone.Priority, zone.Capacity,
                    new CellId(zone.X, zone.Y, zone.Z), zone.AcceptsAll,
                    zone.AllowedItems.Select(value => new ItemId(value)),
                    zone.AllowedCategories.Select(value => new ItemCategoryId(value))),
                data.Reservations.Select(reservation => new StorageReservationStateSnapshot(
                    EntityId.Parse(reservation.JobId), EntityId.Parse(reservation.ZoneId),
                    new ItemId(reservation.ItemId), reservation.Quantity)));
            return StorageState.Restore(snapshot);
        }
        catch (ArgumentException exception)
        {
            return Result<StorageState>.Failure(new DomainError(
                "storage.save.invalid", exception.Message));
        }
    }
}

}
