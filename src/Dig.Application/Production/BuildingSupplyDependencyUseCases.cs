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

public sealed class CreateDeferredBuildingSupplyJobHandler
    : ICommandHandler<CreateDeferredBuildingSupplyJobCommand, Result>
{
    private readonly ProductionContentCatalog _content;
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CreateDeferredBuildingSupplyJobHandler(
        ProductionContentCatalog content,
        IBuildingsRepository buildingsRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _content = content;
        _buildingsRepository = buildingsRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CreateDeferredBuildingSupplyJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        BuildingSnapshot? building = _buildingsRepository.Get().Get(command.BuildingId);
        if (building == null || building.Status != BuildingStatus.Completed
            || !_content.ContainsWorkstation(building.Definition.Id))
        {
            return Result.Failure(ProductionErrors.WorkstationMismatch);
        }

        ItemConsumptionRequest[] requested = command.RequestedItems
            .GroupBy(value => value.ItemId)
            .Select(group => new ItemConsumptionRequest(
                group.Key,
                group.Sum(value => value.Quantity)))
            .OrderBy(value => value.ItemId)
            .ToArray();
        if (requested.Length == 0 || command.DependencyJobIds.Count == 0)
        {
            return Result.Failure(InventoryErrors.InvalidQuantity);
        }

        ProductionWorkstationDefinition workstation =
            _content.GetWorkstation(building.Definition.Id);
        foreach (ItemConsumptionRequest request in requested)
        {
            workstation.GetStockRule(request.ItemId);
        }

        JobSystem jobs = _jobRepository.Get();
        if (command.DependencyJobIds.Any(value => jobs.Get(value) == null))
        {
            return Result.Failure(JobErrors.NotFound);
        }

        BuildingSupplyJobDefinition definition = new BuildingSupplyJobDefinition(
            command.JobId,
            command.BuildingId,
            building.WorkPosition,
            requested,
            command.TransitStackIds,
            command.DepositStackIds,
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default,
            command.DependencyJobIds);
        Result created = jobs.Add(definition);
        if (created.IsSuccess)
        {
            _jobRepository.Save(jobs);
            _eventSink.Append(jobs.DequeueUncommittedEvents());
        }

        return created;
    }
}

public sealed class ResolveDeferredBuildingSupplyJobHandler
    : ICommandHandler<ResolveDeferredBuildingSupplyJobCommand, Result>
{
    private readonly ProductionContentCatalog _content;
    private readonly IBuildingSupplyRepository _supplyRepository;
    private readonly IBuildingsRepository _buildingsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public ResolveDeferredBuildingSupplyJobHandler(
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

    public Result Handle(ResolveDeferredBuildingSupplyJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? pending = jobs.Get(command.JobId);
        if (pending?.Definition is not BuildingSupplyJobDefinition deferred
            || deferred.IsSourceResolved
            || pending.Status != JobStatus.Created
            || !jobs.AreDependenciesCompleted(command.JobId))
        {
            return Result.Failure(JobErrors.DependenciesIncomplete);
        }

        BuildingSnapshot? building = _buildingsRepository.Get().Get(deferred.BuildingId);
        if (building == null || building.Status != BuildingStatus.Completed
            || !_content.ContainsWorkstation(building.Definition.Id))
        {
            return Result.Failure(ProductionErrors.WorkstationMismatch);
        }

        BuildingSupplyState supply = _supplyRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        supply.Register(
            building.Id,
            _content.GetWorkstation(building.Definition.Id),
            command.Tick);
        BuildingSupplySnapshot snapshot = supply.Get(
            building.Id,
            inventory.CreateSnapshot())!;
        ResidentInventoryLayoutSnapshot layout =
            inventory.GetResidentInventoryLayout(command.ResidentId);
        int freeSlots = layout.Slots.Count(value => value.IsEmpty);
        BuildingSupplyPlan plan = BuildingSupplyPlanner.PlanRequests(
            snapshot,
            inventory.GetAvailableWorldStacks(),
            command.RevealedCells,
            command.ReachableCells,
            building.WorkPosition,
            freeSlots,
            deferred.RequestedItems);
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
        foreach (ItemReservationAllocation allocation in allocations)
        {
            Result reserved = inventory.ReserveQuantity(
                allocation.StackId,
                command.JobId,
                allocation.Quantity,
                command.Tick);
            if (reserved.IsFailure)
            {
                Rollback(inventory, command.JobId, command.Tick);
                return reserved;
            }
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
            Rollback(inventory, command.JobId, command.Tick);
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
        if (deferred.TransitStackIds.Count < requiredTransitIds
            || deferred.DepositStackIds.Count < requests.Length)
        {
            Rollback(inventory, command.JobId, command.Tick);
            supply.ReleaseSupply(building.Id, command.JobId, command.Tick);
            return Result.Failure(InventoryErrors.SplitIdRequired);
        }

        BuildingSupplyJobDefinition resolved = new BuildingSupplyJobDefinition(
            deferred.Id,
            deferred.BuildingId,
            deferred.WorkPosition,
            allocations,
            deferred.TransitStackIds.Take(requiredTransitIds),
            deferred.DepositStackIds.Take(requests.Length),
            deferred.Priority,
            deferred.CreatedTick,
            deferred.RetryPolicy,
            deferred.Dependencies);
        Result result = jobs.ResolveCreatedDefinition(command.JobId, resolved, command.Tick);
        if (result.IsSuccess)
        {
            result = jobs.MakeAvailable(command.JobId, command.Tick);
        }
        if (result.IsSuccess)
        {
            result = jobs.Claim(command.JobId, command.ResidentId, command.Tick);
        }
        if (result.IsFailure)
        {
            Rollback(inventory, command.JobId, command.Tick);
            supply.ReleaseSupply(building.Id, command.JobId, command.Tick);
            return result;
        }

        _supplyRepository.Save(supply);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }

    private static void Rollback(InventoryState inventory, EntityId jobId, long tick)
    {
        inventory.ReleaseReservations(jobId, tick);
        inventory.ReleaseResidentSlotClaims(jobId, tick);
    }
}

public sealed class CancelDeferredBuildingSupplyJobHandler
    : ICommandHandler<CancelDeferredBuildingSupplyJobCommand, Result>
{
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CancelDeferredBuildingSupplyJobHandler(
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CancelDeferredBuildingSupplyJobCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BuildingSupplyJobDefinition supply
            || supply.IsSourceResolved
            || job.Status != JobStatus.Created)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        Result cancelled = jobs.Cancel(
            command.JobId,
            new JobBlockReason("dependency_failed", command.Reason),
            command.Tick);
        if (cancelled.IsSuccess)
        {
            _jobRepository.Save(jobs);
            _eventSink.Append(jobs.DequeueUncommittedEvents());
        }

        return cancelled;
    }
}

}
