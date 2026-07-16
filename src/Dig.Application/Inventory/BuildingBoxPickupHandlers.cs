using System;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Inventory
{

public sealed class CreateBuildingBoxPickupHandler
    : ICommandHandler<CreateBuildingBoxPickupCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CreateBuildingBoxPickupHandler(
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(CreateBuildingBoxPickupCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        if (jobs.Get(command.JobId) is not null)
        {
            return Result.Failure(JobErrors.AlreadyExists);
        }

        ItemStackSnapshot? stack = inventory.GetStack(command.StackId);
        Result validation = ValidateSource(inventory, stack, command);
        if (validation.IsFailure)
        {
            return validation;
        }

        Result reserved = inventory.ReserveQuantity(
            command.StackId,
            command.JobId,
            quantity: 1,
            tick: command.Tick);
        if (reserved.IsFailure)
        {
            return reserved;
        }

        BuildingBoxPickupJobDefinition definition = new BuildingBoxPickupJobDefinition(
            command.JobId,
            command.StackId,
            command.SourceCell,
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
            return RollBack(command, inventory, jobs, available.Error!);
        }

        Result claimed = jobs.Claim(command.JobId, command.ResidentId, command.Tick);
        if (claimed.IsFailure)
        {
            return RollBack(command, inventory, jobs, claimed.Error!);
        }

        SaveAndPublish(inventory, jobs);
        return Result.Success();
    }

    private static Result ValidateSource(
        InventoryState inventory,
        ItemStackSnapshot? stack,
        CreateBuildingBoxPickupCommand command)
    {
        if (stack is null)
        {
            return Result.Failure(BuildingBoxPickupErrors.StackMissing);
        }

        if (stack.ItemId != command.ExpectedItemId)
        {
            return Result.Failure(BuildingBoxPickupErrors.ItemMismatch);
        }

        ItemDefinition item = inventory.Catalog.Get(stack.ItemId);
        if (item.MaximumStackSize != 1 || stack.Quantity != 1 || stack.AvailableQuantity != 1)
        {
            return Result.Failure(BuildingBoxPickupErrors.BoxUnavailable);
        }

        return stack.Location == ItemLocation.InWorld(command.SourceCell)
            ? Result.Success()
            : Result.Failure(BuildingBoxPickupErrors.StackNotInWorld);
    }

    private Result RollBack(
        CreateBuildingBoxPickupCommand command,
        InventoryState inventory,
        JobSystem jobs,
        DomainError error)
    {
        jobs.Cancel(
            command.JobId,
            new JobBlockReason("building_box_pickup_create_failed", error.Message),
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

public sealed class CompleteBuildingBoxPickupHandler
    : ICommandHandler<CompleteBuildingBoxPickupCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CompleteBuildingBoxPickupHandler(
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(CompleteBuildingBoxPickupCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BuildingBoxPickupJobDefinition pickup)
        {
            return Result.Failure(BuildingBoxPickupErrors.JobTypeMismatch);
        }

        if (job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.AcquireItem
            || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(BuildingBoxPickupErrors.InvalidJobStage);
        }

        InventoryState inventory = _inventoryRepository.Get();
        ItemStackSnapshot? stack = inventory.GetStack(pickup.StackId);
        if (stack is null)
        {
            return Result.Failure(BuildingBoxPickupErrors.StackMissing);
        }

        bool ownsReservation = stack.Reservations.Any(
            value => value.JobId == job.Id && value.Quantity == 1);
        if (!ownsReservation || stack.Location != ItemLocation.InWorld(pickup.SourceCell))
        {
            return Result.Failure(BuildingBoxPickupErrors.BoxUnavailable);
        }

        Result moved = inventory.MoveReserved(
            pickup.StackId,
            job.Id,
            quantity: 1,
            destination: ItemLocation.InAgent(job.AssignedAgentId.Value),
            splitStackId: default,
            tick: command.Tick);
        if (moved.IsFailure)
        {
            return moved;
        }

        Result completed = jobs.Complete(job.Id, command.Tick);
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

public sealed class CancelBuildingBoxPickupHandler
    : ICommandHandler<CancelBuildingBoxPickupCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CancelBuildingBoxPickupHandler(
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(CancelBuildingBoxPickupCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BuildingBoxPickupJobDefinition)
        {
            return Result.Failure(BuildingBoxPickupErrors.JobTypeMismatch);
        }

        if (job.IsTerminal)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        InventoryState inventory = _inventoryRepository.Get();
        Result cancelled = jobs.Cancel(
            job.Id,
            new JobBlockReason("building_box_pickup_cancelled", command.Reason),
            command.Tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        inventory.ReleaseReservations(job.Id, command.Tick);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
