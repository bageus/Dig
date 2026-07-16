using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Buildings
{

public static class BuildingBoxAssemblyTransitErrors
{
    public static readonly DomainError SourceNotAtWorker = new DomainError(
        "building_box.assembly.source_not_at_worker",
        "The reserved BuildingBox is not at the assigned worker position.");

    public static readonly DomainError SourceOwnedByAnotherAgent = new DomainError(
        "building_box.assembly.source_owned_by_another_agent",
        "The reserved BuildingBox is carried by another resident.");
}

public sealed class AcquireBuildingBoxForAssemblyCommand : ICommand<Result>
{
    public AcquireBuildingBoxForAssemblyCommand(
        EntityId buildingId,
        EntityId jobId,
        CellId workerCell,
        long tick)
    {
        BuildingId = buildingId;
        JobId = jobId;
        WorkerCell = workerCell;
        Tick = tick;
    }

    public EntityId BuildingId { get; }
    public EntityId JobId { get; }
    public CellId WorkerCell { get; }
    public long Tick { get; }
}

public sealed class AcquireBuildingBoxForAssemblyHandler
    : ICommandHandler<AcquireBuildingBoxForAssemblyCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public AcquireBuildingBoxForAssemblyHandler(
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

    public Result Handle(AcquireBuildingBoxForAssemblyCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        JobSnapshot? job = _jobRepository.Get().Get(command.JobId);
        if (!BuildingBoxJobValidation.Matches(job, building))
        {
            return Result.Failure(BuildingBoxErrors.JobTypeMismatch);
        }

        if (job!.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.AcquireItem
            || !job.AssignedAgentId.HasValue
            || building!.BoxPlan!.CommitState != BuildingBoxCommitState.Reserved)
        {
            return Result.Failure(BuildingBoxErrors.InvalidJobStage);
        }

        InventoryState inventory = _inventoryRepository.Get();
        ItemStackSnapshot? box = inventory.GetStack(building.BoxPlan.SourceStackId);
        if (box is null
            || box.ItemId != building.Definition.BoxPolicy!.BoxItemId
            || box.Quantity != 1
            || !box.Reservations.Any(value => value.JobId == job.Id && value.Quantity == 1))
        {
            return Result.Failure(BuildingBoxErrors.SourceStackMissing);
        }

        ItemLocation carried = ItemLocation.InAgent(job.AssignedAgentId.Value);
        if (box.Location == carried)
        {
            return Result.Success();
        }

        if (box.Location.Kind == ItemLocationKind.AgentInventory)
        {
            return Result.Failure(BuildingBoxAssemblyTransitErrors.SourceOwnedByAnotherAgent);
        }

        if (box.Location.Kind != ItemLocationKind.World
            || !box.Location.HasCell
            || box.Location.CellId != command.WorkerCell)
        {
            return Result.Failure(BuildingBoxAssemblyTransitErrors.SourceNotAtWorker);
        }

        Result moved = inventory.MoveFullyReservedPreservingReservation(
            box.StackId,
            job.Id,
            carried,
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
}
