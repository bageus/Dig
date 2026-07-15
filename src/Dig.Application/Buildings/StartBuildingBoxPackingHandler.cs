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

public sealed class StartBuildingBoxPackingHandler
    : ICommandHandler<StartBuildingBoxPackingCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public StartBuildingBoxPackingHandler(
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

    public Result Handle(StartBuildingBoxPackingCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        if (building is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        BuildingBoxPolicy? policy = building.Definition.BoxPolicy;
        if (policy is null || !policy.PackingEnabled)
        {
            return Result.Failure(BuildingBoxPackingErrors.PackingDisabled);
        }

        if (building.Status != BuildingStatus.Completed
            || building.PackingPlan?.CommitState == BuildingPackingCommitState.Active)
        {
            return Result.Failure(BuildingErrors.InvalidStatus);
        }

        InventoryState inventory = _inventoryRepository.Get();
        if (inventory.GetStack(command.OutputStackId) is not null)
        {
            return Result.Failure(BuildingBoxPackingErrors.OutputStackExists);
        }

        JobSystem jobs = _jobRepository.Get();
        if (jobs.Get(command.JobId) is not null)
        {
            return Result.Failure(BuildingBoxErrors.JobAlreadyExists);
        }

        ItemDefinition boxItem = inventory.Catalog.Get(policy.BoxItemId);
        if (boxItem.MaximumStackSize != 1)
        {
            return Result.Failure(BuildingBoxErrors.BoxMustBeSingle);
        }

        BuildingBoxPackingJobDefinition definition =
            new BuildingBoxPackingJobDefinition(
                command.JobId,
                building.Id,
                command.OutputStackId,
                building.WorkPosition,
                command.Priority,
                command.Tick,
                JobRetryPolicy.Default);

        Result started = buildings.StartBoxPacking(
            building.Id,
            command.JobId,
            command.OutputStackId,
            command.Tick);
        if (started.IsFailure)
        {
            return started;
        }

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

        _buildingsRepository.Save(buildings);
        _jobRepository.Save(jobs);
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}
}
