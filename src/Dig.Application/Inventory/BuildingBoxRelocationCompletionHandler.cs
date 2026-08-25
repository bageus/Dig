using System;
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

public sealed class CompleteBuildingBoxRelocationHandler
    : ICommandHandler<CompleteBuildingBoxRelocationCommand, Result>
{
    private readonly IWorldRepository _worldRepository;
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CompleteBuildingBoxRelocationHandler(
        IWorldRepository worldRepository,
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _worldRepository = worldRepository
            ?? throw new ArgumentNullException(nameof(worldRepository));
        _buildingsRepository = buildingsRepository
            ?? throw new ArgumentNullException(nameof(buildingsRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(CompleteBuildingBoxRelocationCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

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

        CellId destination = relocation.DestinationCell.Value;
        Result target = CreateBuildingBoxRelocationHandler.ValidateTarget(
            _worldRepository.Get().CreateSnapshot(),
            _buildingsRepository.Get(),
            destination,
            new[] { destination });
        if (target.IsFailure)
        {
            return CancelAndKeepCarriedBox(inventory, jobs, job, target.Error!, command.Tick);
        }

        Result moved = ResidentItemTransferService.MoveReserved(
            inventory,
            box.StackId,
            job.Id,
            quantity: 1,
            ItemLocation.InWorld(destination),
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

        SaveAndPublish(inventory, jobs);
        return Result.Success();
    }

    private Result CancelAndKeepCarriedBox(
        InventoryState inventory,
        JobSystem jobs,
        JobSnapshot job,
        DomainError reason,
        long tick)
    {
        Result cancelled = jobs.Cancel(
            job.Id,
            new JobBlockReason(reason.Code, reason.Message),
            tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        inventory.ReleaseReservations(job.Id, tick);
        SaveAndPublish(inventory, jobs);
        return Result.Success();
    }

    private void SaveAndPublish(InventoryState inventory, JobSystem jobs)
    {
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}

}
