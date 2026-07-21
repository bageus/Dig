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

public sealed class AddBuildingBoxAssemblyWorkHandler
    : ICommandHandler<AddBuildingBoxAssemblyWorkCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;
    private Func<EntityId, long, bool>? _workDuePolicy;

    public AddBuildingBoxAssemblyWorkHandler(
        IBuildingsRepository buildingsRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _buildingsRepository = buildingsRepository
            ?? throw new ArgumentNullException(nameof(buildingsRepository));
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public void SetWorkDuePolicy(Func<EntityId, long, bool> workDuePolicy)
    {
        _workDuePolicy = workDuePolicy
            ?? throw new ArgumentNullException(nameof(workDuePolicy));
    }

    public Result Handle(AddBuildingBoxAssemblyWorkCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (command.WorkAmount <= 0 || command.Tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command.WorkAmount));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        JobSnapshot? job = _jobRepository.Get().Get(command.JobId);
        if (!BuildingBoxJobValidation.Matches(job, building))
        {
            return Result.Failure(BuildingBoxErrors.JobTypeMismatch);
        }

        if (job!.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.PerformWork
            || building!.BoxPlan!.CommitState != BuildingBoxCommitState.AtSite)
        {
            return Result.Failure(BuildingBoxErrors.InvalidJobStage);
        }

        if (_workDuePolicy != null
            && job.AssignedAgentId.HasValue
            && !_workDuePolicy(job.AssignedAgentId.Value, command.Tick))
        {
            return Result.Success();
        }

        if (building.Status == BuildingStatus.ReadyToBuild)
        {
            Result started = buildings.StartConstruction(command.BuildingId);
            if (started.IsFailure)
            {
                return started;
            }
        }
        else if (building.Status != BuildingStatus.UnderConstruction)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        Result worked = buildings.AddConstructionWork(
            command.BuildingId,
            command.WorkAmount,
            command.Tick);
        if (worked.IsFailure)
        {
            return worked;
        }

        _buildingsRepository.Save(buildings);
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class CompleteBuildingBoxAssemblyHandler
    : ICommandHandler<CompleteBuildingBoxAssemblyCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CompleteBuildingBoxAssemblyHandler(
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

    public Result Handle(CompleteBuildingBoxAssemblyCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (!BuildingBoxJobValidation.Matches(job, building))
        {
            return Result.Failure(BuildingBoxErrors.JobTypeMismatch);
        }

        if (job!.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.Finalize
            || building!.Status != BuildingStatus.ReadyToComplete
            || building.BoxPlan!.CommitState != BuildingBoxCommitState.AtSite)
        {
            return Result.Failure(BuildingBoxErrors.InvalidJobStage);
        }

        InventoryState inventory = _inventoryRepository.Get();
        ItemStackSnapshot? box = inventory.GetStack(building.BoxPlan.SourceStackId);
        if (box is null
            || box.Location != ItemLocation.InBuilding(command.BuildingId)
            || box.ItemId != building.Definition.BoxPolicy!.BoxItemId
            || box.AvailableQuantity != 1)
        {
            return Result.Failure(BuildingBoxErrors.SourceStackMissing);
        }

        Result consumed = inventory.ConsumeAvailableStack(
            box.StackId,
            quantity: 1,
            command.Tick);
        if (consumed.IsFailure)
        {
            return consumed;
        }

        Result completedBuilding = buildings.CompleteBoxConstruction(
            command.BuildingId,
            command.Tick);
        if (completedBuilding.IsFailure)
        {
            return completedBuilding;
        }

        Result completedJob = jobs.Complete(command.JobId, command.Tick);
        if (completedJob.IsFailure)
        {
            return completedJob;
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

internal static class BuildingBoxJobValidation
{
    public static bool Matches(JobSnapshot? job, BuildingSnapshot? building)
    {
        return building?.BoxPlan is not null
            && job?.Definition is BuildingBoxAssemblyJobDefinition definition
            && job.Id == building.BoxPlan.JobId
            && definition.BuildingId == building.Id
            && definition.SourceStackId == building.BoxPlan.SourceStackId;
    }
}

}
