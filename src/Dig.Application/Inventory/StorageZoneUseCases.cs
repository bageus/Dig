using System;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Storage;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public static class StoragePlacementErrors
{
    public static readonly DomainError CellMustBeOpen = new DomainError(
        "storage.placement_cell_must_be_open",
        "A storage zone can only be placed on an open cell.");

    public static readonly DomainError ZoneOccupied = new DomainError(
        "storage.placement_zone_occupied",
        "A storage zone containing items cannot be moved.");
}

public sealed class MoveStorageZoneCommand : ICommand<Result>
{
    public MoveStorageZoneCommand(EntityId zoneId, CellId cellId, long tick)
    {
        ZoneId = zoneId;
        CellId = cellId;
        Tick = tick;
    }

    public EntityId ZoneId { get; }

    public CellId CellId { get; }

    public long Tick { get; }
}

public sealed class MoveStorageZoneHandler
    : ICommandHandler<MoveStorageZoneCommand, Result>
{
    private readonly IStorageRepository _storageRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IWorldRepository _worldRepository;
    private readonly IEventSink _eventSink;

    public MoveStorageZoneHandler(
        IStorageRepository storageRepository,
        IInventoryRepository inventoryRepository,
        IWorldRepository worldRepository,
        IEventSink eventSink)
    {
        _storageRepository = storageRepository
            ?? throw new ArgumentNullException(nameof(storageRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _worldRepository = worldRepository
            ?? throw new ArgumentNullException(nameof(worldRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(MoveStorageZoneCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        StorageState storage = _storageRepository.Get();
        if (storage.GetZone(command.ZoneId) == null)
        {
            return Result.Failure(StorageErrors.ZoneNotFound);
        }

        Result<CellSnapshot> cell = _worldRepository.Get().GetCell(command.CellId);
        if (cell.IsFailure)
        {
            return Result.Failure(cell.Error!);
        }

        if (cell.Value.IsSolid)
        {
            return Result.Failure(StoragePlacementErrors.CellMustBeOpen);
        }

        int storedQuantity = _inventoryRepository.Get().GetTotalQuantityAt(
            ItemLocation.InStorage(command.ZoneId));
        if (storedQuantity > 0)
        {
            return Result.Failure(StoragePlacementErrors.ZoneOccupied);
        }

        Result moved = storage.MoveZone(command.ZoneId, command.CellId, command.Tick);
        if (moved.IsFailure)
        {
            return moved;
        }

        _storageRepository.Save(storage);
        _eventSink.Append(storage.DequeueUncommittedEvents());
        return Result.Success();
    }
}
}
