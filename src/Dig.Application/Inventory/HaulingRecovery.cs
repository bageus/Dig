using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;

namespace Dig.Application.Inventory
{

public sealed class BlockHaulingJobCommand : ICommand<Result>
{
    public BlockHaulingJobCommand(EntityId jobId, string reasonCode, string message, long tick)
    {
        JobId = jobId;
        Reason = new JobBlockReason(reasonCode, message);
        Tick = tick;
    }

    public EntityId JobId { get; }

    public JobBlockReason Reason { get; }

    public long Tick { get; }
}

public sealed class RetryHaulingJobCommand : ICommand<Result>
{
    public RetryHaulingJobCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }

    public long Tick { get; }
}

public sealed class ReconcileHaulingCommand : ICommand<HaulingReconciliationReport>
{
    public ReconcileHaulingCommand(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
    }

    public long Tick { get; }
}

public sealed class HaulingReconciliationEntry
{
    public HaulingReconciliationEntry(EntityId jobId, string actionCode)
    {
        if (string.IsNullOrWhiteSpace(actionCode))
        {
            throw new ArgumentException("Action code is required.", nameof(actionCode));
        }

        JobId = jobId;
        ActionCode = actionCode.Trim();
    }

    public EntityId JobId { get; }

    public string ActionCode { get; }
}

public sealed class HaulingReconciliationReport
{
    public HaulingReconciliationReport(
        long tick,
        IReadOnlyCollection<HaulingReconciliationEntry> entries)
    {
        Tick = tick;
        Entries = new ReadOnlyCollection<HaulingReconciliationEntry>(entries
            .OrderBy(value => value.JobId.ToString(), StringComparer.Ordinal)
            .ThenBy(value => value.ActionCode, StringComparer.Ordinal)
            .ToArray());
    }

    public long Tick { get; }

    public IReadOnlyList<HaulingReconciliationEntry> Entries { get; }
}

public sealed class BlockHaulingJobHandler
    : ICommandHandler<BlockHaulingJobCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public BlockHaulingJobHandler(
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

    public Result Handle(BlockHaulingJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventoryRepository.Get();
        StorageState storage = _storageRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? before = jobs.Get(command.JobId);
        if (before?.Definition is not HaulJobDefinition)
        {
            return Result.Failure(before is null ? JobErrors.NotFound : HaulingErrors.JobNotHauling);
        }

        Result blocked = jobs.Block(command.JobId, command.Reason, command.Tick);
        JobSnapshot? after = jobs.Get(command.JobId);
        if (after?.Status == JobStatus.Failed)
        {
            ReleaseExternalReservations(inventory, storage, command.JobId, command.Tick);
        }

        SaveAndPublish(inventory, storage, jobs);
        return blocked;
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

    private static void ReleaseExternalReservations(
        InventoryState inventory,
        StorageState storage,
        EntityId jobId,
        long tick)
    {
        inventory.ReleaseReservations(jobId, tick);
        storage.ReleaseIncoming(jobId, tick);
    }
}

public sealed class RetryHaulingJobHandler
    : ICommandHandler<RetryHaulingJobCommand, Result>
{
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public RetryHaulingJobHandler(IJobRepository jobRepository, IEventSink eventSink)
    {
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(RetryHaulingJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? snapshot = jobs.Get(command.JobId);
        if (snapshot?.Definition is not HaulJobDefinition)
        {
            return Result.Failure(snapshot is null ? JobErrors.NotFound : HaulingErrors.JobNotHauling);
        }

        Result result = jobs.Retry(command.JobId, command.Tick);
        _jobRepository.Save(jobs);
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return result;
    }
}

public sealed class ReconcileHaulingHandler
    : ICommandHandler<ReconcileHaulingCommand, HaulingReconciliationReport>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public ReconcileHaulingHandler(
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

    public HaulingReconciliationReport Handle(ReconcileHaulingCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventoryRepository.Get();
        StorageState storage = _storageRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        List<HaulingReconciliationEntry> entries = new List<HaulingReconciliationEntry>();

        foreach (JobSnapshot job in jobs.GetAll())
        {
            if (job.Definition is not HaulJobDefinition hauling)
            {
                continue;
            }

            if (job.IsTerminal)
            {
                if (ReleaseExternalReservations(inventory, storage, job.Id, command.Tick) > 0)
                {
                    entries.Add(new HaulingReconciliationEntry(job.Id, "released_terminal"));
                }

                continue;
            }

            if (!HasExpectedReservations(inventory, storage, job.Id, hauling))
            {
                jobs.Fail(
                    job.Id,
                    new JobBlockReason(
                        "hauling_reservation_mismatch",
                        "Hauling job external reservations do not match its definition."),
                    command.Tick);
                ReleaseExternalReservations(inventory, storage, job.Id, command.Tick);
                entries.Add(new HaulingReconciliationEntry(job.Id, "failed_mismatched"));
            }
        }

        HashSet<EntityId> validJobs = jobs.GetAll()
            .Where(value => !value.IsTerminal)
            .Where(value => value.Definition is HaulJobDefinition)
            .Select(value => value.Id)
            .ToHashSet();
        foreach (StorageReservationSnapshot reservation in storage.GetReservations())
        {
            if (validJobs.Contains(reservation.JobId))
            {
                continue;
            }

            storage.ReleaseIncoming(reservation.JobId, command.Tick);
            inventory.ReleaseReservations(reservation.JobId, command.Tick);
            entries.Add(new HaulingReconciliationEntry(reservation.JobId, "released_orphan_storage"));
        }

        EntityId[] inventoryReservationJobs = inventory.CreateSnapshot().Stacks
            .SelectMany(value => value.Reservations)
            .Select(value => value.JobId)
            .Distinct()
            .Where(value => !validJobs.Contains(value))
            .OrderBy(value => value.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (EntityId jobId in inventoryReservationJobs)
        {
            inventory.ReleaseReservations(jobId, command.Tick);
            storage.ReleaseIncoming(jobId, command.Tick);
            entries.Add(new HaulingReconciliationEntry(jobId, "released_orphan_inventory"));
        }

        _inventoryRepository.Save(inventory);
        _storageRepository.Save(storage);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(storage.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return new HaulingReconciliationReport(command.Tick, entries);
    }

    private static bool HasExpectedReservations(
        InventoryState inventory,
        StorageState storage,
        EntityId jobId,
        HaulJobDefinition hauling)
    {
        ItemStackSnapshot? stack = inventory.GetStack(hauling.SourceStackId);
        int itemQuantity = stack?.Reservations
            .Where(value => value.JobId == jobId)
            .Sum(value => value.Quantity) ?? 0;
        if (itemQuantity != hauling.Quantity)
        {
            return false;
        }

        if (hauling.Destination.Kind != ItemLocationKind.Storage)
        {
            return true;
        }

        StorageReservationSnapshot? reservation = storage.GetReservation(jobId);
        return reservation.HasValue
            && reservation.Value.ZoneId == hauling.Destination.OwnerId
            && reservation.Value.ItemId == hauling.ItemId
            && reservation.Value.Quantity == hauling.Quantity;
    }

    private static int ReleaseExternalReservations(
        InventoryState inventory,
        StorageState storage,
        EntityId jobId,
        long tick)
    {
        int releasedItems = inventory.ReleaseReservations(jobId, tick);
        int releasedStorage = storage.ReleaseIncoming(jobId, tick);
        return checked(releasedItems + releasedStorage);
    }
}
}
