using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Buildings;

public sealed class CreateConstructionJobHandler
    : ICommandHandler<CreateConstructionJobCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CreateConstructionJobHandler(
        IBuildingsRepository buildingsRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _buildingsRepository = buildingsRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CreateConstructionJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        if (building is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        if (building.Status != BuildingStatus.ReadyToBuild)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        if (!command.ReachableCells.Contains(building.WorkPosition))
        {
            buildings.Block(
                command.BuildingId,
                $"Work position {building.WorkPosition} is unreachable.");
            _buildingsRepository.Save(buildings);
            _eventSink.Append(buildings.DequeueUncommittedEvents());
            return Result.Failure(BuildingErrors.NoReachableWorkPosition);
        }

        BuildingWorkJobDefinition definition = new BuildingWorkJobDefinition(
            command.JobId,
            command.BuildingId,
            BuildingWorkKind.Construction,
            building.WorkPosition,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result added = jobs.Add(definition);
        if (added.IsFailure)
        {
            return added;
        }

        Result available = jobs.MakeAvailable(command.JobId, command.Tick);
        if (available.IsFailure)
        {
            return available;
        }

        Result started = buildings.StartConstruction(command.BuildingId);
        if (started.IsFailure)
        {
            jobs.Cancel(
                command.JobId,
                new JobBlockReason("building_state_changed", started.Error!.Message),
                command.Tick);
            return started;
        }

        SaveAndPublish(buildings, jobs);
        return Result.Success();
    }

    private void SaveAndPublish(BuildingsState buildings, JobSystem jobs)
    {
        _buildingsRepository.Save(buildings);
        _jobRepository.Save(jobs);
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class AddConstructionWorkHandler
    : ICommandHandler<AddConstructionWorkCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public AddConstructionWorkHandler(
        IBuildingsRepository buildingsRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _buildingsRepository = buildingsRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(AddConstructionWorkCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BuildingWorkJobDefinition work
            || work.BuildingId != command.BuildingId
            || work.Kind != BuildingWorkKind.Construction)
        {
            return Result.Failure(BuildingUseCaseErrors.JobMismatch);
        }

        if (job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.PerformWork)
        {
            return Result.Failure(HaulingErrors.InvalidStage);
        }

        Result result = buildings.AddConstructionWork(
            command.BuildingId,
            command.WorkAmount);
        if (result.IsFailure)
        {
            return result;
        }

        if (buildings.Get(command.BuildingId)!.Status == BuildingStatus.ReadyToComplete)
        {
            Result advanced = jobs.AdvanceStage(command.JobId, command.Tick);
            if (advanced.IsFailure)
            {
                throw new InvalidOperationException(
                    "Construction work reached completion but job could not finalize.");
            }
        }

        _buildingsRepository.Save(buildings);
        _jobRepository.Save(jobs);
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class CompleteConstructionHandler
    : ICommandHandler<CompleteConstructionCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CompleteConstructionHandler(
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _buildingsRepository = buildingsRepository;
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CompleteConstructionCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        JobSnapshot? job = jobs.Get(command.JobId);
        if (building is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        if (job?.Definition is not BuildingWorkJobDefinition work
            || work.BuildingId != command.BuildingId
            || work.Kind != BuildingWorkKind.Construction)
        {
            return Result.Failure(BuildingUseCaseErrors.JobMismatch);
        }

        if (building.Status != BuildingStatus.ReadyToComplete
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.Finalize)
        {
            return Result.Failure(BuildingErrors.WorkIncomplete);
        }

        ItemConsumptionRequest[] materials = building.Definition.Materials
            .Select(value => new ItemConsumptionRequest(value.ItemId, value.Quantity))
            .ToArray();
        Result consumed = inventory.ConsumeAvailableAt(
            ItemLocation.InBuilding(command.BuildingId),
            materials,
            command.Tick);
        if (consumed.IsFailure)
        {
            return Result.Failure(BuildingErrors.MaterialsUnavailable);
        }

        Result completedBuilding = buildings.Complete(command.BuildingId, command.Tick);
        if (completedBuilding.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated building could not complete after material consumption.");
        }

        Result completedJob = jobs.AdvanceStage(command.JobId, command.Tick);
        if (completedJob.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated construction job could not complete its final stage.");
        }

        _buildingsRepository.Save(buildings);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}
