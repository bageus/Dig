using System;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;

namespace Dig.Application.Inventory
{

public sealed class CreateHaulingJobHandler
    : ICommandHandler<CreateHaulingJobCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CreateHaulingJobHandler(
        IInventoryRepository inventoryRepository,
        IStorageRepository storageRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _storageRepository = storageRepository
            ?? throw new ArgumentNullException(nameof(storageRepository));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(CreateHaulingJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventoryRepository.Get();
        StorageState storage = _storageRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        ItemStackSnapshot? stack = inventory.GetStack(command.SourceStackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        ItemDefinition item = inventory.Catalog.Get(stack.ItemId);
        int occupied = inventory.GetTotalQuantityAt(
            ItemLocation.InStorage(command.DestinationStorageId));
        Result destinationReserved = storage.ReserveIncoming(
            command.DestinationStorageId,
            command.JobId,
            item,
            command.Quantity,
            occupied,
            command.Tick);
        if (destinationReserved.IsFailure)
        {
            Flush(inventory, storage, jobs);
            return destinationReserved;
        }

        Result itemsReserved = inventory.ReserveQuantity(
            command.SourceStackId,
            command.JobId,
            command.Quantity,
            command.Tick);
        if (itemsReserved.IsFailure)
        {
            storage.ReleaseIncoming(command.JobId, command.Tick);
            Flush(inventory, storage, jobs);
            return itemsReserved;
        }

        HaulJobDefinition definition = new HaulJobDefinition(
            command.JobId,
            command.SourceStackId,
            stack.ItemId,
            command.Quantity,
            command.DestinationStorageId,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result added = jobs.Add(definition);
        if (added.IsFailure)
        {
            RollbackReservations(inventory, storage, command.JobId, command.Tick);
            Flush(inventory, storage, jobs);
            return added;
        }

        Result available = jobs.MakeAvailable(command.JobId, command.Tick);
        if (available.IsFailure)
        {
            RollbackReservations(inventory, storage, command.JobId, command.Tick);
        }

        Flush(inventory, storage, jobs);
        return available;
    }

    private void Flush(InventoryState inventory, StorageState storage, JobSystem jobs)
    {
        _inventoryRepository.Save(inventory);
        _storageRepository.Save(storage);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(storage.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }

    private static void RollbackReservations(
        InventoryState inventory,
        StorageState storage,
        EntityId jobId,
        long tick)
    {
        inventory.ReleaseReservations(jobId, tick);
        inventory.ReleaseResidentSlotClaims(jobId, tick);
        storage.ReleaseIncoming(jobId, tick);
    }
}

public sealed class CancelHaulingJobHandler
    : ICommandHandler<CancelHaulingJobCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CancelHaulingJobHandler(
        IInventoryRepository inventoryRepository,
        IStorageRepository storageRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository;
        _storageRepository = storageRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CancelHaulingJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventoryRepository.Get();
        StorageState storage = _storageRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? snapshot = jobs.Get(command.JobId);
        if (snapshot is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (snapshot.Definition is not HaulJobDefinition)
        {
            return Result.Failure(HaulingErrors.JobNotHauling);
        }

        Result cancelled = jobs.Cancel(
            command.JobId,
            new JobBlockReason("hauling_cancelled", command.Reason),
            command.Tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        inventory.ReleaseReservations(command.JobId, command.Tick);
        inventory.ReleaseResidentSlotClaims(command.JobId, command.Tick);
        storage.ReleaseIncoming(command.JobId, command.Tick);
        SaveAndPublish(inventory, storage, jobs);
        return Result.Success();
    }

    private void SaveAndPublish(
        InventoryState inventory,
        StorageState storage,
        JobSystem jobs)
    {
        _inventoryRepository.Save(inventory);
        _storageRepository.Save(storage);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(storage.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class CompleteHaulingJobHandler
    : ICommandHandler<CompleteHaulingJobCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CompleteHaulingJobHandler(
        IInventoryRepository inventoryRepository,
        IStorageRepository storageRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository;
        _storageRepository = storageRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CompleteHaulingJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventoryRepository.Get();
        StorageState storage = _storageRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? snapshot = jobs.Get(command.JobId);
        if (snapshot is null)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (snapshot.Definition is not HaulJobDefinition hauling)
        {
            return Result.Failure(HaulingErrors.JobNotHauling);
        }

        if (snapshot.Status != JobStatus.InProgress
            || snapshot.Stage != JobStageKind.DepositItem
            || !snapshot.AssignedAgentId.HasValue)
        {
            return Result.Failure(HaulingErrors.InvalidStage);
        }

        Result moved = inventory.DepositReservedResidentItems(
            command.JobId,
            snapshot.AssignedAgentId.Value,
            hauling.ItemId,
            hauling.Quantity,
            ItemLocation.InStorage(hauling.DestinationStorageId),
            command.SplitStackId,
            command.Tick);
        if (moved.IsFailure && moved.Error == InventoryErrors.ReservationNotFound)
        {
            Result legacyMove = inventory.MoveReserved(
                hauling.SourceStackId,
                command.JobId,
                hauling.Quantity,
                ItemLocation.InStorage(hauling.DestinationStorageId),
                command.SplitStackId,
                command.Tick);
            if (legacyMove.IsSuccess)
            {
                moved = legacyMove;
            }
        }

        if (moved.IsFailure)
        {
            return moved;
        }

        Result completed = jobs.AdvanceStage(command.JobId, command.Tick);
        if (completed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated hauling job failed its final lifecycle transition.");
        }

        inventory.ReleaseResidentSlotClaims(command.JobId, command.Tick);
        storage.ReleaseIncoming(command.JobId, command.Tick);
        _inventoryRepository.Save(inventory);
        _storageRepository.Save(storage);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(storage.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}