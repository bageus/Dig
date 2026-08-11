using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Dig.Application.Production
{

public sealed class CreateBuildingSupplyJobHandler
    : ICommandHandler<CreateBuildingSupplyJobCommand, Result>
{
    private readonly ProductionContentCatalog _content;
    private readonly IBuildingSupplyRepository _supplyRepository;
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CreateBuildingSupplyJobHandler(
        ProductionContentCatalog content,
        IBuildingSupplyRepository supplyRepository,
        IProductionRepository productionRepository,
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
        : this(
            content,
            supplyRepository,
            buildingsRepository,
            inventoryRepository,
            jobRepository,
            eventSink)
    {
        _ = productionRepository
            ?? throw new ArgumentNullException(nameof(productionRepository));
    }

    public CreateBuildingSupplyJobHandler(
        ProductionContentCatalog content,
        IBuildingSupplyRepository supplyRepository,
        IBuildingsRepository buildingsRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _content = content;
        _supplyRepository = supplyRepository;
        _buildingsRepository = buildingsRepository;
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CreateBuildingSupplyJobCommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        BuildingsState buildings = _buildingsRepository.Get();
        BuildingSnapshot? building = buildings.Get(command.BuildingId);
        if (building is null || building.Status != BuildingStatus.Completed
            || !_content.ContainsWorkstation(building.Definition.Id))
        {
            return Result.Failure(ProductionErrors.WorkstationMismatch);
        }

        BuildingSupplyState supply = _supplyRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        supply.Register(
            building.Id,
            _content.GetWorkstation(building.Definition.Id),
            command.Tick);
        InventorySnapshot inventorySnapshot = inventory.CreateSnapshot();
        BuildingSupplySnapshot snapshot = supply.Get(building.Id, inventorySnapshot)!;
        ResidentInventoryLayoutSnapshot layout =
            inventory.GetResidentInventoryLayout(command.ResidentId);
        int freeSlots = layout.Slots.Count(value => value.IsEmpty);
        BuildingSupplyPlan plan = command.HasTargetItemFilter
            ? BuildingSupplyPlanner.PlanForItems(
                inventory.Catalog,
                snapshot,
                inventory.GetAvailableWorldStacks(),
                command.RevealedCells,
                command.ReachableCells,
                building.WorkPosition,
                freeSlots,
                command.TargetItemIds)
            : BuildingSupplyPlanner.Plan(
                inventory.Catalog,
                snapshot,
                inventory.GetAvailableWorldStacks(),
                command.RevealedCells,
                command.ReachableCells,
                building.WorkPosition,
                freeSlots);
        if (plan.Allocations.Count == 0)
        {
            return Result.Failure(InventoryErrors.InsufficientAvailableQuantity);
        }

        ItemReservationAllocation[] allocations = plan.Allocations
            .Select(value => new ItemReservationAllocation(
                value.SourceStackId,
                value.ItemId,
                value.Quantity))
            .ToArray();
        Result reserved = ReserveSources(inventory, command.JobId, allocations, command.Tick);
        if (reserved.IsFailure)
        {
            return reserved;
        }

        ItemConsumptionRequest[] requests = allocations
            .GroupBy(value => value.ItemId)
            .Select(group => new ItemConsumptionRequest(
                group.Key,
                group.Sum(value => value.Quantity)))
            .ToArray();
        Result<IReadOnlyList<ResidentInventorySlotClaimSnapshot>> slotClaims =
            inventory.ReserveResidentBatchSlotCapacity(
                command.JobId,
                command.ResidentId,
                requests,
                command.Tick);
        if (slotClaims.IsFailure)
        {
            inventory.ReleaseReservations(command.JobId, command.Tick);
            return Result.Failure(slotClaims.Error!);
        }

        Dictionary<ItemId, int> current = snapshot.Stocks.ToDictionary(
            value => value.ItemId,
            value => value.Current);
        Result incoming = supply.ReserveIncoming(
            building.Id,
            command.JobId,
            requests,
            current,
            command.Tick);
        if (incoming.IsFailure)
        {
            Rollback(inventory, command.JobId, command.Tick);
            return incoming;
        }

        int requiredTransitIds = slotClaims.Value.Count(value =>
            layout.Slots.First(slot => slot.Slot == value.Slot).IsEmpty);
        if (command.TransitStackIds.Count < requiredTransitIds)
        {
            Rollback(inventory, command.JobId, command.Tick);
            supply.ReleaseSupply(building.Id, command.JobId, command.Tick);
            return Result.Failure(InventoryErrors.SplitIdRequired);
        }

        int requiredDepositIds = requests.Length;
        if (command.DepositStackIds.Count < requiredDepositIds)
        {
            Rollback(inventory, command.JobId, command.Tick);
            supply.ReleaseSupply(building.Id, command.JobId, command.Tick);
            return Result.Failure(InventoryErrors.SplitIdRequired);
        }

        BuildingSupplyJobDefinition definition = new BuildingSupplyJobDefinition(
            command.JobId,
            building.Id,
            building.WorkPosition,
            allocations,
            command.TransitStackIds.Take(requiredTransitIds),
            command.DepositStackIds.Take(requiredDepositIds),
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result created = jobs.Add(definition);
        bool jobAdded = created.IsSuccess;
        if (created.IsSuccess) created = jobs.MakeAvailable(command.JobId, command.Tick);
        if (created.IsSuccess) created = jobs.Claim(command.JobId, command.ResidentId, command.Tick);
        if (created.IsFailure)
        {
            Rollback(inventory, command.JobId, command.Tick);
            supply.ReleaseSupply(building.Id, command.JobId, command.Tick);
            if (jobAdded && jobs.Get(command.JobId) is JobSnapshot failedJob
                && !failedJob.IsTerminal)
            {
                Result cancelled = jobs.Cancel(
                    command.JobId,
                    new JobBlockReason(
                        "production.supply.creation_rolled_back",
                        "Building supply creation failed and released its operation."),
                    command.Tick);
                if (cancelled.IsFailure)
                {
                    throw new InvalidOperationException(
                        "Failed building supply job could not be terminalized.");
                }
            }

            Save(supply, inventory, jobs);
            return created;
        }

        Save(supply, inventory, jobs);
        return Result.Success();
    }

    private static Result ReserveSources(
        InventoryState inventory,
        EntityId jobId,
        IEnumerable<ItemReservationAllocation> allocations,
        long tick)
    {
        foreach (ItemReservationAllocation allocation in allocations)
        {
            Result reserved = inventory.ReserveQuantity(
                allocation.StackId,
                jobId,
                allocation.Quantity,
                tick);
            if (reserved.IsFailure)
            {
                inventory.ReleaseReservations(jobId, tick);
                return reserved;
            }
        }

        return Result.Success();
    }

    private static void Rollback(InventoryState inventory, EntityId jobId, long tick)
    {
        inventory.ReleaseReservations(jobId, tick);
        inventory.ReleaseResidentSlotClaims(jobId, tick);
    }

    private void Save(BuildingSupplyState supply, InventoryState inventory, JobSystem jobs)
    {
        _supplyRepository.Save(supply);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class EnableProductionInputDeliveryHandler
    : ICommandHandler<EnableProductionInputDeliveryCommand, Result>
{
    private readonly IBuildingSupplyRepository _repository;

    public EnableProductionInputDeliveryHandler(IBuildingSupplyRepository repository)
    {
        _repository = repository;
    }

    public Result Handle(EnableProductionInputDeliveryCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingSupplyState supply = _repository.Get();
        Result result = supply.EnableProductionInputDelivery(
            command.BuildingId,
            command.Inputs,
            command.Tick);
        if (result.IsSuccess)
        {
            _repository.Save(supply);
        }

        return result;
    }
}

public sealed class SetBuildingStockDeliveryHandler
    : ICommandHandler<SetBuildingStockDeliveryCommand, Result>
{
    private readonly IBuildingSupplyRepository _repository;

    public SetBuildingStockDeliveryHandler(IBuildingSupplyRepository repository)
    {
        _repository = repository;
    }

    public Result Handle(SetBuildingStockDeliveryCommand command)
    {
        BuildingSupplyState supply = _repository.Get();
        Result result = supply.SetDeliveryEnabled(
            command.BuildingId,
            command.ItemId,
            command.Enabled,
            command.Tick);
        if (result.IsSuccess) _repository.Save(supply);
        return result;
    }
}

}
