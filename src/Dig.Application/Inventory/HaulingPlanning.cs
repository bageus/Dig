using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Storage;

namespace Dig.Application.Inventory
{

public interface IHaulingJobIdSource
{
    EntityId Next();
}

public sealed class PlanHaulingCommand : ICommand<HaulingPlanningReport>
{
    public PlanHaulingCommand(int maximumJobs, int priority, long tick)
    {
        if (maximumJobs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumJobs));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        MaximumJobs = maximumJobs;
        Priority = priority;
        Tick = tick;
    }

    public int MaximumJobs { get; }

    public int Priority { get; }

    public long Tick { get; }
}

public sealed class PlannedHaulingJob
{
    public PlannedHaulingJob(
        EntityId jobId,
        EntityId stackId,
        EntityId storageId,
        int quantity)
    {
        JobId = jobId;
        StackId = stackId;
        StorageId = storageId;
        Quantity = quantity;
    }

    public EntityId JobId { get; }

    public EntityId StackId { get; }

    public EntityId StorageId { get; }

    public int Quantity { get; }
}

public sealed class SkippedHaulingStack
{
    public SkippedHaulingStack(EntityId stackId, string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Skip reason is required.", nameof(reasonCode));
        }

        StackId = stackId;
        ReasonCode = reasonCode.Trim();
    }

    public EntityId StackId { get; }

    public string ReasonCode { get; }
}

public sealed class HaulingPlanningReport
{
    public HaulingPlanningReport(
        long tick,
        IReadOnlyCollection<PlannedHaulingJob> created,
        IReadOnlyCollection<SkippedHaulingStack> skipped)
    {
        Tick = tick;
        Created = SortCreated(created);
        Skipped = SortSkipped(skipped);
    }

    public long Tick { get; }

    public IReadOnlyList<PlannedHaulingJob> Created { get; }

    public IReadOnlyList<SkippedHaulingStack> Skipped { get; }

    private static IReadOnlyList<PlannedHaulingJob> SortCreated(
        IReadOnlyCollection<PlannedHaulingJob> values)
    {
        if (values.Count == 0)
        {
            return Array.Empty<PlannedHaulingJob>();
        }

        return new ReadOnlyCollection<PlannedHaulingJob>(values
            .OrderBy(value => value.JobId.ToString(), StringComparer.Ordinal)
            .ToArray());
    }

    private static IReadOnlyList<SkippedHaulingStack> SortSkipped(
        IReadOnlyCollection<SkippedHaulingStack> values)
    {
        if (values.Count == 0)
        {
            return Array.Empty<SkippedHaulingStack>();
        }

        return new ReadOnlyCollection<SkippedHaulingStack>(values
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .ToArray());
    }
}

public sealed class PlanHaulingHandler
    : ICommandHandler<PlanHaulingCommand, HaulingPlanningReport>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStorageRepository _storageRepository;
    private readonly Dig.Application.Jobs.IJobRepository _jobRepository;
    private readonly IHaulingJobIdSource _jobIds;
    private readonly IEventSink _eventSink;

    public PlanHaulingHandler(
        IInventoryRepository inventoryRepository,
        IStorageRepository storageRepository,
        Dig.Application.Jobs.IJobRepository jobRepository,
        IHaulingJobIdSource jobIds,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _storageRepository = storageRepository
            ?? throw new ArgumentNullException(nameof(storageRepository));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _jobIds = jobIds ?? throw new ArgumentNullException(nameof(jobIds));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public HaulingPlanningReport Handle(PlanHaulingCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventoryRepository.Get();
        StorageState storage = _storageRepository.Get();
        IReadOnlyList<ItemStackSnapshot> candidates = inventory.GetAvailableWorldStacks();
        List<PlannedHaulingJob>? created = null;
        List<SkippedHaulingStack>? skipped = null;
        CreateHaulingJobHandler create = new CreateHaulingJobHandler(
            _inventoryRepository,
            _storageRepository,
            _jobRepository,
            _eventSink);

        foreach (ItemStackSnapshot candidate in candidates)
        {
            if (created?.Count >= command.MaximumJobs)
            {
                break;
            }

            ItemDefinition item = inventory.Catalog.Get(candidate.ItemId);
            StorageZoneSnapshot? destination = storage.FindFirstDestination(
                item,
                minimumQuantity: 1,
                zoneId => inventory.GetTotalQuantityAt(ItemLocation.InStorage(zoneId)));
            if (destination is null)
            {
                (skipped ??= new List<SkippedHaulingStack>()).Add(
                    new SkippedHaulingStack(candidate.StackId, "no_storage_destination"));
                continue;
            }

            int quantity = Math.Min(candidate.AvailableQuantity, destination.AvailableCapacity);
            EntityId jobId = _jobIds.Next();
            if (jobId.IsEmpty)
            {
                throw new InvalidOperationException("Hauling job id source returned an empty id.");
            }

            Result result = create.Handle(new CreateHaulingJobCommand(
                jobId,
                candidate.StackId,
                quantity,
                destination.Definition.Id,
                command.Priority,
                command.Tick));
            if (result.IsFailure)
            {
                (skipped ??= new List<SkippedHaulingStack>()).Add(
                    new SkippedHaulingStack(
                        candidate.StackId,
                        result.Error?.Code ?? "creation_failed"));
                continue;
            }

            (created ??= new List<PlannedHaulingJob>()).Add(new PlannedHaulingJob(
                jobId,
                candidate.StackId,
                destination.Definition.Id,
                quantity));
        }

        return new HaulingPlanningReport(
            command.Tick,
            (IReadOnlyCollection<PlannedHaulingJob>?)created
                ?? Array.Empty<PlannedHaulingJob>(),
            (IReadOnlyCollection<SkippedHaulingStack>?)skipped
                ?? Array.Empty<SkippedHaulingStack>());
    }
}
}
