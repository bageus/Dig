using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public sealed class CreateResidentInventoryPlacementHandler
    : ICommandHandler<CreateResidentInventoryPlacementCommand, Result>
{
    private readonly IWorldRepository _worldRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CreateResidentInventoryPlacementHandler(
        IWorldRepository worldRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _worldRepository = worldRepository ?? throw new ArgumentNullException(nameof(worldRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(CreateResidentInventoryPlacementCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        ItemStackSnapshot? stack = inventory.GetStack(command.StackId);
        Result source = ValidateSource(inventory, stack, command);
        if (source.IsFailure)
        {
            return source;
        }

        Result target = ValidateTarget(
            _worldRepository.Get().CreateSnapshot(),
            command.DestinationCell,
            command.ReachableCells);
        if (target.IsFailure)
        {
            return target;
        }

        if (jobs.Get(command.JobId) is not null)
        {
            return Result.Failure(JobErrors.AlreadyExists);
        }

        JobSnapshot? predecessor = jobs.GetAll()
            .Where(value => !value.IsTerminal
                && value.Definition is ResidentInventoryPlacementJobDefinition placement
                && placement.ResidentId == command.ResidentId)
            .OrderBy(value => value.Definition.CreatedTick)
            .ThenBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .LastOrDefault();
        EntityId[] dependencies = predecessor == null
            ? Array.Empty<EntityId>()
            : new[] { predecessor.Id };

        Result reserved = inventory.ReserveQuantity(
            command.StackId,
            command.JobId,
            command.Quantity,
            command.Tick);
        if (reserved.IsFailure)
        {
            return reserved;
        }

        var definition = new ResidentInventoryPlacementJobDefinition(
            command.JobId,
            command.ResidentId,
            command.StackId,
            command.Quantity,
            command.DestinationCell,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default,
            dependencies);
        Result added = jobs.Add(definition);
        if (added.IsFailure)
        {
            inventory.ReleaseReservations(command.JobId, command.Tick);
            return added;
        }

        Result activated = predecessor == null
            ? Activate(jobs, command.JobId, command.ResidentId, command.Tick)
            : Result.Success();
        if (activated.IsFailure)
        {
            jobs.Cancel(
                command.JobId,
                new JobBlockReason("inventory_placement_create_failed", activated.Error!.Message),
                command.Tick);
            inventory.ReleaseReservations(command.JobId, command.Tick);
            SaveAndPublish(inventory, jobs);
            return activated;
        }

        SaveAndPublish(inventory, jobs);
        return Result.Success();
    }

    private static Result ValidateSource(
        InventoryState inventory,
        ItemStackSnapshot? stack,
        CreateResidentInventoryPlacementCommand command)
    {
        if (stack == null
            || stack.Location.Kind != ItemLocationKind.AgentInventory
            || !DropResidentInventoryStackHandler.IsOwnedByResident(
                stack.Location,
                command.ResidentId)
            || stack.Quantity != command.Quantity
            || stack.AvailableQuantity != command.Quantity
            || stack.ReservedQuantity != 0
            || inventory.CreateSnapshot().HeldItems.Any(value => value.StackId == command.StackId))
        {
            return Result.Failure(ResidentInventoryPlacementErrors.SourceUnavailable);
        }

        return Result.Success();
    }

    public static Result ValidateTarget(
        WorldSnapshot world,
        CellId destination,
        IReadOnlyCollection<CellId> reachableCells)
    {
        CellSnapshot? target = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.Id == destination)
            .Select(cell => (CellSnapshot?)cell)
            .FirstOrDefault();
        if (!world.Size.Contains(destination)
            || !reachableCells.Contains(destination)
            || !target.HasValue
            || target.Value.IsSolid
            || !target.Value.State.IsExplored
            || !HasWalkableSupport(world, destination))
        {
            return Result.Failure(ResidentInventoryPlacementErrors.TargetUnavailable);
        }

        return Result.Success();
    }

    internal static Result Activate(
        JobSystem jobs,
        EntityId jobId,
        EntityId residentId,
        long tick)
    {
        Result available = jobs.MakeAvailable(jobId, tick);
        return available.IsFailure ? available : jobs.Claim(jobId, residentId, tick);
    }

    private static bool HasWalkableSupport(
        WorldSnapshot world,
        CellId destination)
    {
        CellId support = new CellId(
            destination.X,
            destination.Y + 1,
            destination.Z);
        if (!world.Size.Contains(support))
        {
            return false;
        }

        CellSnapshot? supportCell = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.Id == support)
            .Select(cell => (CellSnapshot?)cell)
            .FirstOrDefault();
        return supportCell.HasValue
            && supportCell.Value.IsSolid
            && supportCell.Value.State.IsExplored;
    }

    private void SaveAndPublish(InventoryState inventory, JobSystem jobs)
    {
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class CompleteResidentInventoryPlacementHandler
    : ICommandHandler<CompleteResidentInventoryPlacementCommand, Result>
{
    private readonly IWorldRepository _worldRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CompleteResidentInventoryPlacementHandler(
        IWorldRepository worldRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _worldRepository = worldRepository ?? throw new ArgumentNullException(nameof(worldRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(CompleteResidentInventoryPlacementCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not ResidentInventoryPlacementJobDefinition placement
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.DepositItem
            || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        if (job.AssignedAgentId.Value != placement.ResidentId)
        {
            return Result.Failure(ResidentInventoryPlacementErrors.ResidentMismatch);
        }

        if (command.WorkerCell != placement.DestinationCell)
        {
            return Result.Failure(ResidentInventoryPlacementErrors.TargetUnavailable);
        }

        Result target = CreateResidentInventoryPlacementHandler.ValidateTarget(
            _worldRepository.Get().CreateSnapshot(),
            placement.DestinationCell,
            new[] { placement.DestinationCell });
        if (target.IsFailure)
        {
            Result blocked = jobs.Block(
                job.Id,
                new JobBlockReason(target.Error!.Code, target.Error.Message),
                command.Tick);
            _jobRepository.Save(jobs);
            _eventSink.Append(jobs.DequeueUncommittedEvents());
            return blocked.IsFailure ? blocked : Result.Success();
        }

        InventoryState inventory = _inventoryRepository.Get();
        ItemStackSnapshot? stack = inventory.GetStack(placement.StackId);
        if (stack == null
            || stack.Location.Kind != ItemLocationKind.AgentInventory
            || !DropResidentInventoryStackHandler.IsOwnedByResident(
                stack.Location,
                placement.ResidentId)
            || !stack.Reservations.Any(value =>
                value.JobId == job.Id && value.Quantity == placement.Quantity))
        {
            return Result.Failure(ResidentInventoryPlacementErrors.SourceUnavailable);
        }

        ItemDefinition definition = inventory.Catalog.Get(stack.ItemId);
        Result moved = definition.IsInventoryExpansion
            ? inventory.DropReservedResidentStackWithSpill(
                placement.StackId,
                job.Id,
                ItemLocation.InWorld(placement.DestinationCell),
                command.Tick)
            : ResidentItemTransferService.MoveReserved(
                inventory,
                placement.StackId,
                job.Id,
                placement.Quantity,
                ItemLocation.InWorld(placement.DestinationCell),
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
