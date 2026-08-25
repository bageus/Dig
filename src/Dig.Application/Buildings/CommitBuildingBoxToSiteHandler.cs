using System;
using System.Linq;
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

public sealed class CommitBuildingBoxToSiteHandler
    : ICommandHandler<CommitBuildingBoxToSiteCommand, Result>
{
    private readonly IWorldRepository _worldRepository;
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;
    private readonly BuildingPlacementValidator _validator;
    private readonly PackableBuildingPlacementPolicyValidator _physicalValidator;
    private readonly BuildingPlacementSurfaceFactProjector _surfaceFacts;
    private readonly PackableBuildingContentCatalog _packableCatalog;

    public CommitBuildingBoxToSiteHandler(
        IWorldRepository worldRepository,
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        BuildingPlacementValidator validator,
        PackableBuildingPlacementPolicyValidator physicalValidator,
        PackableBuildingContentCatalog packableCatalog,
        IEventSink eventSink)
    {
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

    public Result Handle(CommitBuildingBoxToSiteCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        if (building?.BoxPlan is null)
        {
            return Result.Failure(BuildingErrors.BoxPlanNotFound);
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (!Matches(job, building))
        {
            return Result.Failure(BuildingBoxErrors.JobTypeMismatch);
        }

        if (job!.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.DepositItem
            || !job.AssignedAgentId.HasValue
            || building.BoxPlan.CommitState != BuildingBoxCommitState.Reserved)
        {
            return Result.Failure(BuildingBoxErrors.InvalidJobStage);
        }

        InventoryState inventory = _inventoryRepository.Get();
        ItemStackSnapshot? box = inventory.GetStack(building.BoxPlan.SourceStackId);
        bool ownsReservation = box?.Reservations.Any(
            value => value.JobId == job.Id && value.Quantity == 1) ?? false;
        if (box is null
            || !DropResidentInventoryStackHandler.IsOwnedByResident(
                box.Location,
                job.AssignedAgentId.Value)
            || box.ItemId != building.Definition.BoxPolicy!.BoxItemId
            || box.Quantity != 1
            || !ownsReservation)
        {
            return Result.Failure(BuildingBoxErrors.SourceStackMissing);
        }

        Result target = ValidateTarget(buildings, building, command);
        if (target.IsFailure)
        {
            return CancelBeforeCommit(
                inventory,
                buildings,
                jobs,
                job,
                building,
                target.Error!,
                command.Tick);
        }

        Result moved = ResidentItemTransferService.MoveReserved(
            inventory,
            box.StackId,
            command.JobId,
            quantity: 1,
            ItemLocation.InBuilding(command.BuildingId),
            splitStackId: default,
            command.Tick);
        if (moved.IsFailure)
        {
            return moved;
        }

        Result committed = buildings.MarkBoxAtSite(command.BuildingId, command.Tick);
        if (committed.IsFailure)
        {
            return committed;
        }

        _inventoryRepository.Save(inventory);
        _buildingsRepository.Save(buildings);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        return Result.Success();
    }

    private Result ValidateTarget(
        BuildingsState buildings,
        BuildingSnapshot building,
        CommitBuildingBoxToSiteCommand command)
    {
        WorldSnapshot world = _worldRepository.Get().CreateSnapshot();
        CellId[] occupiedByOthers = buildings.GetAll()
            .Where(candidate => candidate.Id != building.Id && candidate.IsActive)
            .SelectMany(candidate => candidate.Footprint)
            .Distinct()
            .ToArray();
        BuildingPlacementResult placement = _validator.Validate(
            building.Definition,
            building.Origin,
            building.Orientation,
            world,
            occupiedByOthers,
            new[] { building.WorkPosition },
            command.EcologyBlockedCells);
        if (!placement.Succeeded)
        {
            return Result.Failure(placement.Error!);
        }

        if (building.Definition.Id.ToString() != "building.ladder"
            && !BuildingPlacementSurfaceFactProjector.HasSupportingPlane(
                placement.Footprint,
                world))
        {
            return Result.Failure(PackableBuildingPlacementErrors.SurfaceMissing);
        }

        if (!_packableCatalog.TryGet(
            building.Definition.Id,
            out PackableBuildingContentDefinition? content))
        {
            return Result.Success();
        }

        PackableBuildingSurfacePolicy policy = content!.Placement.ToSurfacePolicy();
        PackableBuildingPlacementPolicyResult physical = _physicalValidator.Validate(
            policy,
            building.Origin,
            _surfaceFacts.Project(policy, building.Origin, world),
            occupiedByOthers);
        return physical.Succeeded
            ? Result.Success()
            : Result.Failure(physical.Error!);
    }

    private Result CancelBeforeCommit(
        InventoryState inventory,
        BuildingsState buildings,
        JobSystem jobs,
        JobSnapshot job,
        BuildingSnapshot building,
        DomainError reason,
        long tick)
    {
        Result cancelledJob = jobs.Cancel(
            job.Id,
            new JobBlockReason(reason.Code, reason.Message),
            tick);
        if (cancelledJob.IsFailure)
        {
            return cancelledJob;
        }

        inventory.ReleaseReservations(job.Id, tick);
        Result cancelledBuilding = buildings.Cancel(building.Id, reason.Code, tick);
        if (cancelledBuilding.IsFailure)
        {
            return cancelledBuilding;
        }

        _inventoryRepository.Save(inventory);
        _buildingsRepository.Save(buildings);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }

    private static bool Matches(JobSnapshot? job, BuildingSnapshot building)
    {
        return job?.Definition is BuildingBoxAssemblyJobDefinition definition
            && building.BoxPlan is not null
            && job.Id == building.BoxPlan.JobId
            && definition.BuildingId == building.Id
            && definition.SourceStackId == building.BoxPlan.SourceStackId;
    }
}
}
