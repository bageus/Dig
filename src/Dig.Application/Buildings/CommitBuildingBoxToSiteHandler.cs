using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Buildings
{

public sealed class CommitBuildingBoxToSiteHandler
    : ICommandHandler<CommitBuildingBoxToSiteCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CommitBuildingBoxToSiteHandler(
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
            || box.Location != ItemLocation.InAgent(job.AssignedAgentId.Value)
            || box.ItemId != building.Definition.BoxPolicy!.BoxItemId
            || box.Quantity != 1
            || !ownsReservation)
        {
            return Result.Failure(BuildingBoxErrors.SourceStackMissing);
        }

        Result moved = inventory.MoveReserved(
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
