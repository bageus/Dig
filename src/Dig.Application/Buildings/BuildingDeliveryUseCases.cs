using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Buildings;

public static class BuildingUseCaseErrors
{
    public static readonly DomainError MaterialNotRequired = new DomainError(
        "buildings.material.not_required",
        "The selected item is not required by this building.");

    public static readonly DomainError DeliveryExceedsRequirement = new DomainError(
        "buildings.material.delivery_exceeds_requirement",
        "The delivery would exceed the remaining material requirement.");

    public static readonly DomainError JobMismatch = new DomainError(
        "buildings.job_mismatch",
        "The job does not belong to the requested building operation.");
}

public sealed class CreateBuildingDeliveryHandler
    : ICommandHandler<CreateBuildingDeliveryCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CreateBuildingDeliveryHandler(
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _buildingsRepository = buildingsRepository;
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CreateBuildingDeliveryCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        if (building is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        if (building.Status != BuildingStatus.AwaitingMaterials)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        ItemStackSnapshot? stack = inventory.GetStack(command.SourceStackId);
        if (stack is null)
        {
            return Result.Failure(InventoryErrors.StackNotFound);
        }

        BuildingMaterialRequirement? requirement = building.Definition.Materials
            .Where(value => value.ItemId == stack.ItemId)
            .Select(value => (BuildingMaterialRequirement?)value)
            .FirstOrDefault();
        if (!requirement.HasValue)
        {
            return Result.Failure(BuildingUseCaseErrors.MaterialNotRequired);
        }

        ItemLocation site = ItemLocation.InBuilding(command.BuildingId);
        int delivered = inventory.GetQuantityAt(stack.ItemId, site);
        int incoming = jobs.GetAll()
            .Where(job => !job.IsTerminal)
            .Select(job => job.Definition)
            .OfType<HaulJobDefinition>()
            .Where(job => job.Destination == site && job.ItemId == stack.ItemId)
            .Sum(job => job.Quantity);
        if (command.Quantity <= 0
            || checked(delivered + incoming + command.Quantity) > requirement.Value.Quantity)
        {
            return Result.Failure(BuildingUseCaseErrors.DeliveryExceedsRequirement);
        }

        Result reserved = inventory.ReserveQuantity(
            command.SourceStackId,
            command.JobId,
            command.Quantity,
            command.Tick);
        if (reserved.IsFailure)
        {
            return reserved;
        }

        HaulJobDefinition hauling = new HaulJobDefinition(
            command.JobId,
            command.SourceStackId,
            stack.ItemId,
            command.Quantity,
            site,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result added = jobs.Add(hauling);
        if (added.IsFailure)
        {
            inventory.ReleaseReservations(command.JobId, command.Tick);
            SaveAndPublish(inventory, jobs);
            return added;
        }

        Result available = jobs.MakeAvailable(command.JobId, command.Tick);
        if (available.IsFailure)
        {
            inventory.ReleaseReservations(command.JobId, command.Tick);
        }

        SaveAndPublish(inventory, jobs);
        return available;
    }

    private void SaveAndPublish(InventoryState inventory, JobSystem jobs)
    {
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class CompleteBuildingDeliveryHandler
    : ICommandHandler<CompleteBuildingDeliveryCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CompleteBuildingDeliveryHandler(
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CompleteBuildingDeliveryCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? snapshot = jobs.Get(command.JobId);
        if (snapshot?.Definition is not HaulJobDefinition hauling
            || hauling.Destination.Kind != ItemLocationKind.BuildingInventory)
        {
            return Result.Failure(BuildingUseCaseErrors.JobMismatch);
        }

        if (snapshot.Status != JobStatus.InProgress
            || snapshot.Stage != JobStageKind.DepositItem)
        {
            return Result.Failure(HaulingErrors.InvalidStage);
        }

        Result moved = inventory.MoveReserved(
            hauling.SourceStackId,
            command.JobId,
            hauling.Quantity,
            hauling.Destination,
            command.SplitStackId,
            command.Tick);
        if (moved.IsFailure)
        {
            return moved;
        }

        Result completed = jobs.AdvanceStage(command.JobId, command.Tick);
        if (completed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated building delivery failed its final transition.");
        }

        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class RefreshBuildingMaterialsHandler
    : ICommandHandler<RefreshBuildingMaterialsCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IEventSink _eventSink;

    public RefreshBuildingMaterialsHandler(
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IEventSink eventSink)
    {
        _buildingsRepository = buildingsRepository;
        _inventoryRepository = inventoryRepository;
        _eventSink = eventSink;
    }

    public Result Handle(RefreshBuildingMaterialsCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        if (building is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        ItemLocation site = ItemLocation.InBuilding(command.BuildingId);
        bool ready = building.Definition.Materials.All(
            requirement => inventory.GetQuantityAt(requirement.ItemId, site)
                >= requirement.Quantity);
        if (!ready)
        {
            return Result.Failure(BuildingErrors.MaterialsUnavailable);
        }

        Result result = buildings.MarkMaterialsReady(command.BuildingId);
        if (result.IsFailure)
        {
            return result;
        }

        _buildingsRepository.Save(buildings);
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        return Result.Success();
    }
}
