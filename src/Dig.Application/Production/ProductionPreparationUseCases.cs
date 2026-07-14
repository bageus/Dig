using Dig.Application.Buildings;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.Technology;

namespace Dig.Application.Production;

public sealed class EnqueueProductionOrderHandler
    : ICommandHandler<EnqueueProductionOrderCommand, Result>
{
    private readonly ProductionContentCatalog _content;
    private readonly IProductionRepository _repository;

    public EnqueueProductionOrderHandler(
        ProductionContentCatalog content,
        IProductionRepository repository)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Result Handle(EnqueueProductionOrderCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ProductionState production = _repository.Get();
        Result result = production.Enqueue(
            command.OrderId,
            _content.GetRecipe(command.RecipeId),
            command.BuildingId,
            command.Tick);
        if (result.IsSuccess)
        {
            _repository.Save(production);
        }

        return result;
    }
}

public sealed class PrepareProductionOrderHandler
    : ICommandHandler<PrepareProductionOrderCommand, Result>
{
    private readonly IProductionRepository _productionRepository;
    private readonly ITechnologyRepository _technologyRepository;
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEnergyAvailability _energy;
    private readonly IEventSink _eventSink;

    public PrepareProductionOrderHandler(
        IProductionRepository productionRepository,
        ITechnologyRepository technologyRepository,
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEnergyAvailability energy,
        IEventSink eventSink)
    {
        _productionRepository = productionRepository;
        _technologyRepository = technologyRepository;
        _buildingsRepository = buildingsRepository;
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _energy = energy;
        _eventSink = eventSink;
    }

    public Result Handle(PrepareProductionOrderCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ProductionState production = _productionRepository.Get();
        TechnologyState technology = _technologyRepository.Get();
        BuildingsState buildings = _buildingsRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        ProductionOrderSnapshot? order = production.GetNextQueued(command.BuildingId);
        if (order is null)
        {
            return Result.Failure(ProductionErrors.QueueBlocked);
        }

        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        Result validation = ValidatePrerequisites(
            order,
            building,
            technology,
            inventory,
            command.ReachableCells);
        if (validation.IsFailure)
        {
            return validation;
        }

        ItemLocation workstationInventory = ItemLocation.InBuilding(command.BuildingId);
        ItemConsumptionRequest[] inputs = order.Recipe.Inputs
            .Select(value => new ItemConsumptionRequest(value.ItemId, value.Quantity))
            .ToArray();
        Result<IReadOnlyList<ItemReservationAllocation>> reserved =
            inventory.ReserveProductionInputs(
                order.Id,
                workstationInventory,
                inputs,
                command.Tick);
        if (reserved.IsFailure)
        {
            return Result.Failure(reserved.Error!);
        }

        ProductionWorkJobDefinition job = new ProductionWorkJobDefinition(
            command.JobId,
            order.Id,
            command.BuildingId,
            order.Recipe.Id,
            building!.WorkPosition,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result jobAdded = jobs.Add(job);
        if (jobAdded.IsSuccess)
        {
            jobAdded = jobs.MakeAvailable(command.JobId, command.Tick);
        }

        if (jobAdded.IsFailure)
        {
            inventory.ReleaseReservations(order.Id, command.Tick);
            SaveAndPublish(production, inventory, jobs);
            return jobAdded;
        }

        Result orderReserved = production.ReserveInputs(
            order.Id,
            reserved.Value,
            command.Tick);
        if (orderReserved.IsFailure)
        {
            inventory.ReleaseReservations(order.Id, command.Tick);
            jobs.Cancel(
                command.JobId,
                new JobBlockReason("production_queue_changed", orderReserved.Error!.Message),
                command.Tick);
            SaveAndPublish(production, inventory, jobs);
            return orderReserved;
        }

        SaveAndPublish(production, inventory, jobs);
        return Result.Success();
    }

    private Result ValidatePrerequisites(
        ProductionOrderSnapshot order,
        BuildingSnapshot? building,
        TechnologyState technology,
        InventoryState inventory,
        IReadOnlyCollection<Dig.Domain.World.CellId> reachableCells)
    {
        if (building is null
            || building.Status != BuildingStatus.Completed
            || building.Definition.Id != order.Recipe.WorkstationId)
        {
            return Result.Failure(ProductionErrors.WorkstationMismatch);
        }

        if (!technology.IsRecipeUnlocked(order.Recipe))
        {
            return Result.Failure(ProductionErrors.TechnologyLocked);
        }

        if (!reachableCells.Contains(building.WorkPosition))
        {
            return Result.Failure(ProductionErrors.WorkPositionUnavailable);
        }

        if (!_energy.CanSupply(building.Id, order.Recipe.EnergyPerWorkTick))
        {
            return Result.Failure(ProductionErrors.EnergyUnavailable);
        }

        if (order.Recipe.RequiredToolItemId.HasValue
            && inventory.GetAvailableQuantityAt(
                order.Recipe.RequiredToolItemId.Value,
                ItemLocation.InBuilding(building.Id)) < 1)
        {
            return Result.Failure(ProductionErrors.ToolUnavailable);
        }

        return Result.Success();
    }

    private void SaveAndPublish(
        ProductionState production,
        InventoryState inventory,
        JobSystem jobs)
    {
        _productionRepository.Save(production);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(production.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}
