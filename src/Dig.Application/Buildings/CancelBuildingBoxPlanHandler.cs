using System;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Buildings
{

public sealed class CancelBuildingBoxPlanHandler
    : ICommandHandler<CancelBuildingBoxPlanCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CancelBuildingBoxPlanHandler(
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _buildingsRepository = buildingsRepository
            ?? throw new ArgumentNullException(nameof(buildingsRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result Handle(CancelBuildingBoxPlanCommand command)
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
        JobSnapshot? job = jobs.Get(building.BoxPlan.JobId);
        if (!BuildingBoxJobValidation.Matches(job, building))
        {
            return Result.Failure(BuildingBoxErrors.JobTypeMismatch);
        }

        if (job!.IsTerminal
            || building.Status == BuildingStatus.Completed
            || building.BoxPlan.CommitState == BuildingBoxCommitState.Consumed)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        InventoryState inventory = _inventoryRepository.Get();
        Result boxResult = RestoreBoxLocation(inventory, building, command.Tick);
        if (boxResult.IsFailure)
        {
            return boxResult;
        }

        Result cancelledJob = jobs.Cancel(
            job.Id,
            new JobBlockReason("building_box_cancelled", command.Reason),
            command.Tick);
        if (cancelledJob.IsFailure)
        {
            return cancelledJob;
        }

        Result cancelledBuilding = buildings.Cancel(
            building.Id,
            command.Reason,
            command.Tick);
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

    private static Result RestoreBoxLocation(
        InventoryState inventory,
        BuildingSnapshot building,
        long tick)
    {
        BuildingBoxPlanSnapshot boxPlan = building.BoxPlan!;
        if (boxPlan.CommitState == BuildingBoxCommitState.Reserved)
        {
            int released = inventory.ReleaseReservations(boxPlan.JobId, tick);
            return released == 1
                ? Result.Success()
                : Result.Failure(InventoryErrors.ReservationNotFound);
        }

        if (boxPlan.CommitState != BuildingBoxCommitState.AtSite)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        ItemStackSnapshot? box = inventory.GetStack(boxPlan.SourceStackId);
        if (box is null
            || box.Location != ItemLocation.InBuilding(building.Id)
            || box.AvailableQuantity != 1)
        {
            return Result.Failure(BuildingBoxErrors.SourceStackMissing);
        }

        return inventory.MoveAvailable(
            box.StackId,
            quantity: 1,
            ItemLocation.InWorld(building.Origin),
            splitStackId: default,
            tick);
    }
}
}