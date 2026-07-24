using System;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Buildings
{

public sealed class ConfirmBuildingBoxPlacementHandler
    : ICommandHandler<ConfirmBuildingBoxPlacementCommand, Result>
{
    private readonly BuildingCatalog _catalog;
    private readonly IWorldRepository _worldRepository;
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly BuildingPlacementValidator _validator;
    private readonly PackableBuildingPlacementPolicyValidator _physicalValidator;
    private readonly BuildingPlacementSurfaceFactProjector _surfaceFacts;
    private readonly PackableBuildingContentCatalog _packableCatalog;
    private readonly IEventSink _eventSink;

    public ConfirmBuildingBoxPlacementHandler(
        BuildingCatalog catalog,
        IWorldRepository worldRepository,
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        BuildingPlacementValidator validator,
        IEventSink eventSink)
        : this(
            catalog,
            worldRepository,
            buildingsRepository,
            inventoryRepository,
            jobRepository,
            validator,
            new PackableBuildingPlacementPolicyValidator(),
            CampfireBuildingBoxContent.Catalog,
            eventSink)
    {
    }

    public ConfirmBuildingBoxPlacementHandler(
        BuildingCatalog catalog,
        IWorldRepository worldRepository,
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        BuildingPlacementValidator validator,
        PackableBuildingPlacementPolicyValidator physicalValidator,
        PackableBuildingContentCatalog packableCatalog,
        IEventSink eventSink)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _worldRepository = worldRepository
            ?? throw new ArgumentNullException(nameof(worldRepository));
        _buildingsRepository = buildingsRepository
            ?? throw new ArgumentNullException(nameof(buildingsRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _physicalValidator = physicalValidator
            ?? throw new ArgumentNullException(nameof(physicalValidator));
        _surfaceFacts = new BuildingPlacementSurfaceFactProjector(_physicalValidator);
        _packableCatalog = packableCatalog
            ?? throw new ArgumentNullException(nameof(packableCatalog));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(ConfirmBuildingBoxPlacementCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingDefinition definition = _catalog.Get(command.DefinitionId);
        BuildingBoxPolicy? boxPolicy = definition.BoxPolicy;
        if (boxPolicy is null)
        {
            return Result.Failure(BuildingBoxErrors.DefinitionNotBoxEnabled);
        }

        BuildingsState buildings = _buildingsRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        if (buildings.Get(command.BuildingId) is not null)
        {
            return Result.Failure(BuildingErrors.AlreadyExists);
        }

        if (jobs.Get(command.JobId) is not null)
        {
            return Result.Failure(BuildingBoxErrors.JobAlreadyExists);
        }

        ItemStackSnapshot? source = inventory.GetStack(command.SourceStackId);
        if (source is null)
        {
            return Result.Failure(BuildingBoxErrors.SourceStackMissing);
        }

        if (source.ItemId != boxPolicy.BoxItemId)
        {
            return Result.Failure(BuildingBoxErrors.SourceItemMismatch);
        }

        ItemDefinition itemDefinition = inventory.Catalog.Get(source.ItemId);
        if (itemDefinition.MaximumStackSize != 1 || source.Quantity != 1)
        {
            return Result.Failure(BuildingBoxErrors.BoxMustBeSingle);
        }

        WorldSnapshot world = _worldRepository.Get().CreateSnapshot();
        var occupiedCells = buildings.GetOccupiedCells();
        BuildingPlacementResult placement = _validator.Validate(
            definition,
            command.Origin,
            command.Orientation,
            world,
            occupiedCells,
            command.ReachableCells);
        if (!placement.Succeeded)
        {
            return Result.Failure(placement.Error!);
        }

        PackableBuildingContentDefinition content = _packableCatalog.Get(definition.Id);
        PackableBuildingSurfacePolicy policy = content.Placement.ToSurfacePolicy();
        PackableBuildingPlacementPolicyResult physical = _physicalValidator.Validate(
            policy,
            command.Origin,
            _surfaceFacts.Project(policy, command.Origin, world),
            occupiedCells);
        if (!physical.Succeeded)
        {
            return Result.Failure(physical.Error!);
        }

        Result reserved = inventory.ReserveQuantity(
            command.SourceStackId,
            command.JobId,
            quantity: 1,
            command.Tick);
        if (reserved.IsFailure)
        {
            return reserved;
        }

        BuildingBoxAssemblyJobDefinition job = new BuildingBoxAssemblyJobDefinition(
            command.JobId,
            command.BuildingId,
            command.SourceStackId,
            command.Origin,
            placement.WorkPosition,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result added = jobs.Add(job);
        if (added.IsFailure)
        {
            inventory.ReleaseReservations(command.JobId, command.Tick);
            return added;
        }

        Result placed = buildings.PlaceBoxPlan(
            command.BuildingId,
            command.SourceStackId,
            command.JobId,
            definition,
            command.Origin,
            command.Orientation,
            placement,
            command.Tick);
        if (placed.IsFailure)
        {
            jobs.Cancel(
                command.JobId,
                new JobBlockReason("box_plan_failed", "BuildingBox plan creation failed."),
                command.Tick);
            inventory.ReleaseReservations(command.JobId, command.Tick);
            return placed;
        }

        Result available = jobs.MakeAvailable(command.JobId, command.Tick);
        if (available.IsFailure)
        {
            buildings.Cancel(command.BuildingId, "box_job_unavailable", command.Tick);
            jobs.Cancel(
                command.JobId,
                new JobBlockReason("box_job_unavailable", "BuildingBox job could not start."),
                command.Tick);
            inventory.ReleaseReservations(command.JobId, command.Tick);
            return available;
        }

        _inventoryRepository.Save(inventory);
        _buildingsRepository.Save(buildings);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}
}