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

public sealed class AddBuildingBoxPackingWorkHandler
    : ICommandHandler<AddBuildingBoxPackingWorkCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public AddBuildingBoxPackingWorkHandler(
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

    public Result Handle(AddBuildingBoxPackingWorkCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        JobSnapshot? job = _jobRepository.Get().Get(command.JobId);
        if (!BuildingBoxPackingJobValidation.Matches(job, building))
        {
            return Result.Failure(BuildingBoxPackingErrors.JobTypeMismatch);
        }

        if (job!.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.PerformWork
            || building!.Status != BuildingStatus.Completed
            || building.PackingPlan!.CommitState != BuildingPackingCommitState.Active)
        {
            return Result.Failure(BuildingBoxPackingErrors.InvalidJobStage);
        }

        Result worked = buildings.AddBoxPackingWork(
            building.Id,
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

public sealed class CompleteBuildingBoxPackingHandler
    : ICommandHandler<CompleteBuildingBoxPackingCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CompleteBuildingBoxPackingHandler(
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

    public Result Handle(CompleteBuildingBoxPackingCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (!BuildingBoxPackingJobValidation.Matches(job, building))
        {
            return Result.Failure(BuildingBoxPackingErrors.JobTypeMismatch);
        }

        BuildingPackingPlanSnapshot packing = building!.PackingPlan!;
        BuildingBoxPolicy policy = building.Definition.BoxPolicy!;
        if (job!.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.Finalize
            || building.Status != BuildingStatus.Completed
            || packing.CommitState != BuildingPackingCommitState.Active
            || packing.CompletedWork != policy.PackingWork)
        {
            return Result.Failure(BuildingBoxPackingErrors.InvalidJobStage);
        }

        InventoryState inventory = _inventoryRepository.Get();
        if (inventory.GetStack(packing.OutputStackId) is not null)
        {
            return Result.Failure(BuildingBoxPackingErrors.OutputStackExists);
        }

        ItemDefinition boxItem = inventory.Catalog.Get(policy.BoxItemId);
        if (boxItem.MaximumStackSize != 1)
        {
            return Result.Failure(BuildingBoxErrors.BoxMustBeSingle);
        }

        Result added = inventory.AddStack(
            packing.OutputStackId,
            policy.BoxItemId,
            quantity: 1,
            location: ItemLocation.InWorld(building.Origin),
            tick: command.Tick);
        if (added.IsFailure)
        {
            return added;
        }

        Result packed = buildings.CompleteBoxPacking(building.Id, command.Tick);
        if (packed.IsFailure)
        {
            return packed;
        }

        Result completedJob = jobs.Complete(job.Id, command.Tick);
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

public sealed class CancelBuildingBoxPackingHandler
    : ICommandHandler<CancelBuildingBoxPackingCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CancelBuildingBoxPackingHandler(
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

    public Result Handle(CancelBuildingBoxPackingCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        if (building?.PackingPlan is null
            || building.PackingPlan.CommitState != BuildingPackingCommitState.Active)
        {
            return Result.Failure(BuildingBoxPackingErrors.PackingPlanMissing);
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(building.PackingPlan.JobId);
        if (!BuildingBoxPackingJobValidation.Matches(job, building))
        {
            return Result.Failure(BuildingBoxPackingErrors.JobTypeMismatch);
        }

        if (job!.IsTerminal)
        {
            return Result.Failure(BuildingBoxPackingErrors.InvalidJobStage);
        }

        Result cancelledJob = jobs.Cancel(
            job.Id,
            new JobBlockReason("building_box_packing_cancelled", command.Reason),
            command.Tick);
        if (cancelledJob.IsFailure)
        {
            return cancelledJob;
        }

        Result cancelledBuilding = buildings.CancelBoxPacking(
            building.Id,
            command.Reason,
            command.Tick);
        if (cancelledBuilding.IsFailure)
        {
            return cancelledBuilding;
        }

        _buildingsRepository.Save(buildings);
        _jobRepository.Save(jobs);
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

internal static class BuildingBoxPackingJobValidation
{
    public static bool Matches(JobSnapshot? job, BuildingSnapshot? building)
    {
        return building?.PackingPlan is not null
            && job?.Definition is BuildingBoxPackingJobDefinition definition
            && job.Id == building.PackingPlan.JobId
            && definition.BuildingId == building.Id
            && definition.OutputStackId == building.PackingPlan.OutputStackId;
    }
}
}
