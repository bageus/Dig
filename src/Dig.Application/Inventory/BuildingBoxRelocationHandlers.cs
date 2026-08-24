using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public sealed class CreateBuildingBoxRelocationHandler
    : ICommandHandler<CreateBuildingBoxRelocationCommand, Result>
{
    private readonly IWorldRepository _worldRepository;
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CreateBuildingBoxRelocationHandler(
        IWorldRepository worldRepository,
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _worldRepository = worldRepository ?? throw new ArgumentNullException(nameof(worldRepository));
        _buildingsRepository = buildingsRepository
            ?? throw new ArgumentNullException(nameof(buildingsRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(CreateBuildingBoxRelocationCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (command.DestinationCell.Z != 0)
        {
            return Result.Failure(BuildingBoxRelocationErrors.TargetMustBeZ0);
        }

        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        ItemStackSnapshot? stack = inventory.GetStack(command.StackId);
        Result source = ValidateSource(inventory, stack, command.ExpectedItemId);
        if (source.IsFailure)
        {
            return source;
        }

        Result target = ValidateTarget(command);
        if (target.IsFailure)
        {
            return target;
        }

        if (jobs.Get(command.JobId) is not null)
        {
            return Result.Failure(JobErrors.AlreadyExists);
        }

        Result reserved = inventory.ReserveQuantity(
            command.StackId,
            command.JobId,
            quantity: 1,
            command.Tick);
        if (reserved.IsFailure)
        {
            return reserved;
        }

        bool startsHeld = stack!.Location.Kind == ItemLocationKind.AgentInventory
            && stack.Location.HasOwner;
        CellId sourceCell = stack.Location.HasCell
            ? stack.Location.CellId
            : command.DestinationCell;
        BuildingBoxPickupJobDefinition definition = new BuildingBoxPickupJobDefinition(
            command.JobId,
            command.StackId,
            sourceCell,
            command.DestinationCell,
            startsHeld,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result added = jobs.Add(definition);
        if (added.IsFailure)
        {
            inventory.ReleaseReservations(command.JobId, command.Tick);
            return added;
        }

        Result available = jobs.MakeAvailable(command.JobId, command.Tick);
        if (available.IsFailure)
        {
            return RollBack(inventory, jobs, command, available.Error!);
        }

        if (startsHeld)
        {
            Result claimed = jobs.Claim(
                command.JobId,
                stack.Location.OwnerId,
                command.Tick);
            if (claimed.IsFailure)
            {
                return RollBack(inventory, jobs, command, claimed.Error!);
            }
        }

        SaveAndPublish(inventory, jobs);
        return Result.Success();
    }

    private Result ValidateTarget(CreateBuildingBoxRelocationCommand command)
    {
        WorldSnapshot world = _worldRepository.Get().CreateSnapshot();
        if (!world.Size.Contains(command.DestinationCell)
            || !command.ReachableCells.Contains(command.DestinationCell)
            || _buildingsRepository.Get().GetOccupiedCells().Contains(command.DestinationCell)
            || !BuildingPlacementSurfaceFactProjector.HasSupportingPlane(
                command.DestinationCell,
                world))
        {
            return Result.Failure(BuildingBoxRelocationErrors.TargetUnavailable);
        }

        CellSnapshot? target = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .FirstOrDefault(cell => cell.Id == command.DestinationCell);
        return target.HasValue
            && !target.Value.IsSolid
            && target.Value.State.IsExplored
                ? Result.Success()
                : Result.Failure(BuildingBoxRelocationErrors.TargetUnavailable);
    }

    private static Result ValidateSource(
        InventoryState inventory,
        ItemStackSnapshot? stack,
        ItemId expectedItemId)
    {
        if (stack is null)
        {
            return Result.Failure(BuildingBoxPickupErrors.StackMissing);
        }

        ItemDefinition item = inventory.Catalog.Get(stack.ItemId);
        bool validLocation = stack.Location.Kind == ItemLocationKind.World
            || (stack.Location.Kind == ItemLocationKind.AgentInventory
                && stack.Location.HasOwner);
        return stack.ItemId == expectedItemId
            && item.MaximumStackSize == 1
            && stack.Quantity == 1
            && stack.AvailableQuantity == 1
            && validLocation
                ? Result.Success()
                : Result.Failure(BuildingBoxPickupErrors.BoxUnavailable);
    }

    private Result RollBack(
        InventoryState inventory,
        JobSystem jobs,
        CreateBuildingBoxRelocationCommand command,
        DomainError error)
    {
        jobs.Cancel(
            command.JobId,
            new JobBlockReason("building_box_relocation_create_failed", error.Message),
            command.Tick);
        inventory.ReleaseReservations(command.JobId, command.Tick);
        SaveAndPublish(inventory, jobs);
        return Result.Failure(error);
    }

    private void SaveAndPublish(InventoryState inventory, JobSystem jobs)
    {
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class AcquireBuildingBoxForRelocationHandler
    : ICommandHandler<AcquireBuildingBoxForRelocationCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public AcquireBuildingBoxForRelocationHandler(
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(AcquireBuildingBoxForRelocationCommand command)
    {
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BuildingBoxPickupJobDefinition relocation
            || !relocation.IsRelocation
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.AcquireItem
            || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(BuildingBoxPickupErrors.InvalidJobStage);
        }

        InventoryState inventory = _inventoryRepository.Get();
        ItemStackSnapshot? box = inventory.GetStack(relocation.StackId);
        if (box is null
            || !box.Reservations.Any(value => value.JobId == job.Id && value.Quantity == 1))
        {
            return Result.Failure(BuildingBoxPickupErrors.BoxUnavailable);
        }

        EntityId workerId = job.AssignedAgentId.Value;
        ItemLocation carried = ItemLocation.InAgent(workerId);
        if (DropResidentInventoryStackHandler.IsOwnedByResident(box.Location, workerId))
        {
            return Result.Success();
        }

        if (box.Location.Kind == ItemLocationKind.AgentInventory)
        {
            return Result.Failure(BuildingBoxRelocationErrors.SourceOwnedByAnotherAgent);
        }

        if (box.Location.Kind != ItemLocationKind.World
            || !box.Location.HasCell
            || box.Location.CellId != command.WorkerCell)
        {
            return Result.Failure(BuildingBoxPickupErrors.StackNotInWorld);
        }

        Result moved = ResidentItemTransferService.AcquireReservedStack(
            inventory,
            box.StackId,
            job.Id,
            workerId,
            command.Tick);
        if (moved.IsFailure)
        {
            return moved;
        }

        _inventoryRepository.Save(inventory);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class CompleteBuildingBoxRelocationHandler
    : ICommandHandler<CompleteBuildingBoxRelocationCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CompleteBuildingBoxRelocationHandler(
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CompleteBuildingBoxRelocationCommand command)
    {
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BuildingBoxPickupJobDefinition relocation
            || !relocation.DestinationCell.HasValue
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.DepositItem
            || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(BuildingBoxPickupErrors.InvalidJobStage);
        }

        InventoryState inventory = _inventoryRepository.Get();
        ItemStackSnapshot? box = inventory.GetStack(relocation.StackId);
        if (box is null
            || !DropResidentInventoryStackHandler.IsOwnedByResident(
                box.Location,
                job.AssignedAgentId.Value))
        {
            return Result.Failure(BuildingBoxPickupErrors.BoxUnavailable);
        }

        Result moved = ResidentItemTransferService.MoveReserved(
            inventory,
            box.StackId,
            job.Id,
            quantity: 1,
            ItemLocation.InWorld(relocation.DestinationCell.Value),
            splitStackId: default,
            command.Tick);
        if (moved.IsFailure)
        {
            return moved;
        }

        Result completed = jobs.AdvanceStage(job.Id, command.Tick);
        if (completed.IsFailure)
        {
            return completed;
        }

        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
