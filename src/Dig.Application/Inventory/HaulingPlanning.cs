using System.Collections.ObjectModel;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Storage;

namespace Dig.Application.Inventory;

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
        Created = new ReadOnlyCollection<PlannedHaulingJob>(created
            .OrderBy(value => value.JobId.ToString(), StringComparer.Ordinal)
            .ToArray());
        Skipped = new ReadOnlyCollection<SkippedHaulingStack>(skipped
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .ToArray());
    }

    public long Tick { get; }

    public IReadOnlyList<PlannedHaulingJob> Created { get; }

    public IReadOnlyList<SkippedHaulingStack> Skipped { get; }
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
        ItemStackSnapshot[] candidates = inventory.CreateSnapshot().Stacks
            .Where(value => value.Location.Kind == ItemLocationKind.World)
            .Where(value => value.AvailableQuantity > 0)
            .OrderBy(value => value.Location)
            .ThenBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .ToArray();
        List<PlannedHaulingJob> created = new List<PlannedHaulingJob>();
        List<SkippedHaulingStack> skipped = new List<SkippedHaulingStack>();
        CreateHaulingJobHandler create = new CreateHaulingJobHandler(
            _inventoryRepository,
            _storageRepository,
            _jobRepository,
            _eventSink);

        foreach (ItemStackSnapshot candidate in candidates)
        {
            if (created.Count >= command.MaximumJobs)
            {
                break;
            }

            ItemStackSnapshot? current = inventory.GetStack(candidate.StackId);
            if (current is null
                || current.Location.Kind != ItemLocationKind.World
                || current.AvailableQuantity <= 0)
            {
                skipped.Add(new SkippedHaulingStack(candidate.StackId, "no_available_quantity"));
                continue;
            }

            ItemDefinition item = inventory.Catalog.Get(current.ItemId);
            IReadOnlyList<StorageZoneSnapshot> destinations = storage.FindDestinations(
                item,
                quantity: 1,
                zoneId => GetOccupiedQuantity(inventory, zoneId));
            if (destinations.Count == 0)
            {
                skipped.Add(new SkippedHaulingStack(candidate.StackId, "no_storage_destination"));
                continue;
            }

            StorageZoneSnapshot destination = destinations[0];
            int quantity = Math.Min(current.AvailableQuantity, destination.AvailableCapacity);
            EntityId jobId = _jobIds.Next();
            if (jobId.IsEmpty)
            {
                throw new InvalidOperationException("Hauling job id source returned an empty id.");
            }

            Result result = create.Handle(new CreateHaulingJobCommand(
                jobId,
                current.StackId,
                quantity,
                destination.Definition.Id,
                command.Priority,
                command.Tick));
            if (result.IsFailure)
            {
                skipped.Add(new SkippedHaulingStack(
                    candidate.StackId,
                    result.Error?.Code ?? "creation_failed"));
                continue;
            }

            created.Add(new PlannedHaulingJob(
                jobId,
                current.StackId,
                destination.Definition.Id,
                quantity));
        }

        return new HaulingPlanningReport(command.Tick, created, skipped);
    }

    private static int GetOccupiedQuantity(InventoryState inventory, EntityId zoneId)
    {
        ItemLocation location = ItemLocation.InStorage(zoneId);
        return inventory.CreateSnapshot().Stacks
            .Where(value => value.Location == location)
            .Sum(value => value.Quantity);
    }
}
