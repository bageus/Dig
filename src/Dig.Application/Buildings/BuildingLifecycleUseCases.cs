using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Buildings;

public sealed class DamageBuildingCommand : ICommand<Result>
{
    public DamageBuildingCommand(EntityId buildingId, int amount, long tick)
    {
        BuildingId = buildingId;
        Amount = amount;
        Tick = tick;
    }

    public EntityId BuildingId { get; }

    public int Amount { get; }

    public long Tick { get; }
}

public sealed class CancelBuildingHandler : ICommandHandler<CancelBuildingCommand, Result>
{
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CancelBuildingHandler(
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

    public Result Handle(CancelBuildingCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _buildingsRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        if (building is null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        JobSnapshot[] relatedJobs = jobs.GetAll()
            .Where(job => !job.IsTerminal && BelongsToBuilding(job, command.BuildingId))
            .ToArray();
        foreach (JobSnapshot job in relatedJobs)
        {
            Result cancelled = jobs.Cancel(
                job.Id,
                new JobBlockReason("building_cancelled", command.Reason),
                command.Tick);
            if (cancelled.IsFailure)
            {
                return cancelled;
            }

            inventory.ReleaseReservations(job.Id, command.Tick);
        }

        inventory.ReturnUnreservedStacks(
            ItemLocation.InBuilding(command.BuildingId),
            ItemLocation.InWorld(building.Origin),
            command.Tick);
        Result result = buildings.Cancel(
            command.BuildingId,
            command.Reason,
            command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        SaveAndPublish(buildings, inventory, jobs);
        return Result.Success();
    }

    private static bool BelongsToBuilding(JobSnapshot job, EntityId buildingId)
    {
        return job.Definition switch
        {
            HaulJobDefinition hauling =>
                hauling.Destination == ItemLocation.InBuilding(buildingId),
            BuildingWorkJobDefinition work => work.BuildingId == buildingId,
            _ => false,
        };
    }

    private void SaveAndPublish(
        BuildingsState buildings,
        InventoryState inventory,
        JobSystem jobs)
    {
        _buildingsRepository.Save(buildings);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class RemoveBuildingHandler : ICommandHandler<RemoveBuildingCommand, Result>
{
    private readonly IBuildingsRepository _repository;
    private readonly IEventSink _eventSink;

    public RemoveBuildingHandler(IBuildingsRepository repository, IEventSink eventSink)
    {
        _repository = repository;
        _eventSink = eventSink;
    }

    public Result Handle(RemoveBuildingCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _repository.Get();
        Result result = buildings.Remove(command.BuildingId, command.Reason, command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        _repository.Save(buildings);
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class DamageBuildingHandler : ICommandHandler<DamageBuildingCommand, Result>
{
    private readonly IBuildingsRepository _repository;
    private readonly IEventSink _eventSink;

    public DamageBuildingHandler(IBuildingsRepository repository, IEventSink eventSink)
    {
        _repository = repository;
        _eventSink = eventSink;
    }

    public Result Handle(DamageBuildingCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _repository.Get();
        Result result = buildings.Damage(command.BuildingId, command.Amount, command.Tick);
        if (result.IsFailure)
        {
            return result;
        }

        _repository.Save(buildings);
        _eventSink.Append(buildings.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class RepairBuildingHandler : ICommandHandler<RepairBuildingCommand, Result>
{
    private readonly IBuildingsRepository _repository;

    public RepairBuildingHandler(IBuildingsRepository repository)
    {
        _repository = repository;
    }

    public Result Handle(RepairBuildingCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingsState buildings = _repository.Get();
        Result result = buildings.Repair(command.BuildingId, command.Amount);
        if (result.IsSuccess)
        {
            _repository.Save(buildings);
        }

        return result;
    }
}
