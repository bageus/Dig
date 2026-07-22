using System;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Inventory
{

public sealed class CreateWorldItemPickupHandler
    : ICommandHandler<CreateWorldItemPickupCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CreateWorldItemPickupHandler(
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

    public Result Handle(CreateWorldItemPickupCommand command)
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
        Result validation = ValidateSource(stack, command);
        if (validation.IsFailure)
        {
            return validation;
        }

        int quantity = stack!.Quantity;
        Result reserved = inventory.ReserveQuantity(
            command.StackId,
            command.JobId,
            quantity,
            command.Tick);
        if (reserved.IsFailure)
        {
            return reserved;
        }

        WorldItemPickupJobDefinition definition = new WorldItemPickupJobDefinition(
            command.JobId,
            command.StackId,
            quantity,
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
        ItemStackSnapshot? stack,
        CreateWorldItemPickupCommand command)
    {
        if (stack is null)
        {
            return Result.Failure(WorldItemPickupErrors.StackMissing);
        }

        if (stack.Location != ItemLocation.InWorld(command.SourceCell))
        {
            return Result.Failure(WorldItemPickupErrors.StackNotInWorld);
        }

        return stack.Quantity > 0 && stack.ReservedQuantity == 0
            ? Result.Success()
            : Result.Failure(WorldItemPickupErrors.StackUnavailable);
    }

    private Result RollBack(
        CreateWorldItemPickupCommand command,
        InventoryState inventory,
        JobSystem jobs,
        DomainError error)
    {
        jobs.Cancel(
            command.JobId,
            new JobBlockReason("world_item_pickup_create_failed", error.Message),
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

public sealed class CompleteWorldItemPickupHandler
    : ICommandHandler<CompleteWorldItemPickupCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CompleteWorldItemPickupHandler(
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

    public Result Handle(CompleteWorldItemPickupCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not WorldItemPickupJobDefinition pickup)
        {
            return Result.Failure(WorldItemPickupErrors.JobTypeMismatch);
        }

        if (job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.AcquireItem
            || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(WorldItemPickupErrors.InvalidJobStage);
        }

        InventoryState inventory = _inventoryRepository.Get();
        ItemStackSnapshot? stack = inventory.GetStack(pickup.StackId);
        if (stack is null)
        {
            return Result.Failure(WorldItemPickupErrors.StackMissing);
        }

        bool ownsReservation = stack.Reservations.Any(
            value => value.JobId == job.Id && value.Quantity == pickup.Quantity);
        if (!ownsReservation
            || stack.Quantity != pickup.Quantity
            || stack.Location != ItemLocation.InWorld(pickup.SourceCell))
        {
            return Result.Failure(WorldItemPickupErrors.StackUnavailable);
        }

        Result moved = inventory.MoveReserved(
            pickup.StackId,
            job.Id,
            pickup.Quantity,
            ItemLocation.InAgent(job.AssignedAgentId.Value),
            splitStackId: default,
            command.Tick);
        if (moved.IsFailure)
        {
            return moved;
        }

        Result normalized = inventory.NormalizeResidentInventory(
            job.AssignedAgentId.Value,
            command.Tick);
        if (normalized.IsFailure)
        {
            return normalized;
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

public sealed class CancelWorldItemPickupHandler
    : ICommandHandler<CancelWorldItemPickupCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CancelWorldItemPickupHandler(
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

    public Result Handle(CancelWorldItemPickupCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not WorldItemPickupJobDefinition)
        {
            return Result.Failure(WorldItemPickupErrors.JobTypeMismatch);
        }

        if (job.IsTerminal)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        InventoryState inventory = _inventoryRepository.Get();
        Result cancelled = jobs.Cancel(
            job.Id,
            new JobBlockReason("world_item_pickup_cancelled", command.Reason),
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
