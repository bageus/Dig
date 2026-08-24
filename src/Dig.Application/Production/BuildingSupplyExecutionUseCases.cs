using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;

namespace Dig.Application.Production
{

public sealed class AcquireBuildingSupplyHandler
    : ICommandHandler<AcquireBuildingSupplyCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public AcquireBuildingSupplyHandler(
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(AcquireBuildingSupplyCommand command)
    {
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BuildingSupplyJobDefinition supply
            || job.Status != JobStatus.Claimed
            || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        Result started = jobs.Start(command.JobId, command.Tick);
        if (started.IsFailure) return started;
        Result checkedStock = jobs.AdvanceStage(command.JobId, command.Tick);
        if (checkedStock.IsFailure) return checkedStock;
        Result acquired = ResidentItemTransferService.AcquireReservedBatchIntoResidentSlots(
            inventory,
            command.JobId,
            job.AssignedAgentId.Value,
            supply.Allocations,
            supply.TransitStackIds,
            command.Tick);
        if (acquired.IsFailure)
        {
            jobs.Block(
                command.JobId,
                new JobBlockReason("production.supply.acquire_failed", acquired.Error!.Message),
                command.Tick);
            Save(inventory, jobs);
            return acquired;
        }

        Result advanced = jobs.AdvanceStage(command.JobId, command.Tick);
        if (advanced.IsFailure)
        {
            throw new InvalidOperationException(
                "Acquired supply job could not advance to travel.");
        }

        Save(inventory, jobs);
        return Result.Success();
    }

    private void Save(InventoryState inventory, JobSystem jobs)
    {
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class AcquireBuildingSupplySourceHandler
    : ICommandHandler<AcquireBuildingSupplySourceCommand, Result>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public AcquireBuildingSupplySourceHandler(
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(AcquireBuildingSupplySourceCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BuildingSupplyJobDefinition supply
            || job.AssignedAgentId is not EntityId workerId
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.AcquireItem)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        ItemReservationAllocation? allocation = supply.Allocations
            .Where(value => value.StackId == command.SourceStackId)
            .Select(value => (ItemReservationAllocation?)value)
            .FirstOrDefault();
        if (!allocation.HasValue)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
        }

        EntityId[] unusedTransitIds = supply.TransitStackIds
            .Where(value => inventory.GetStack(value) is null)
            .ToArray();
        Result<bool> acquired = ResidentItemTransferService.AcquireReservedSupplySourceIntoResidentSlots(
            inventory,
            command.JobId,
            workerId,
            allocation.Value,
            unusedTransitIds,
            command.Tick);
        if (acquired.IsFailure)
        {
            jobs.Block(
                command.JobId,
                new JobBlockReason(
                    "production.supply.acquire_source_failed",
                    acquired.Error!.Message),
                command.Tick);
            Save(inventory, jobs);
            return Result.Failure(acquired.Error!);
        }

        if (acquired.Value)
        {
            Result advanced = jobs.AdvanceStage(command.JobId, command.Tick);
            if (advanced.IsFailure)
            {
                throw new InvalidOperationException(
                    "The final supply source was acquired but the job could not travel.");
            }
        }

        Save(inventory, jobs);
        return Result.Success();
    }

    private void Save(InventoryState inventory, JobSystem jobs)
    {
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class DepositBuildingSupplyHandler
    : ICommandHandler<DepositBuildingSupplyCommand, Result>
{
    private readonly IBuildingSupplyRepository _supplyRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public DepositBuildingSupplyHandler(
        IBuildingSupplyRepository supplyRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _supplyRepository = supplyRepository;
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(DepositBuildingSupplyCommand command)
    {
        BuildingSupplyState supplyState = _supplyRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BuildingSupplyJobDefinition supply
            || !job.AssignedAgentId.HasValue)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        if (job.Status == JobStatus.Completed)
        {
            return Result.Success();
        }

        if (job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.DepositItem)
        {
            return Result.Failure(JobErrors.InvalidStatus);
        }

        var groups = supply.Allocations
            .GroupBy(value => value.ItemId)
            .OrderBy(group => group.Key)
            .ToArray();
        if (groups.Length != supply.DepositStackIds.Count)
        {
            throw new InvalidOperationException("Supply output ids do not match item groups.");
        }

        for (int index = 0; index < groups.Length; index++)
        {
            var group = groups[index];
            Result deposited = ResidentItemTransferService.DepositReservedResidentItems(
                inventory,
                group.First().StackId,
                command.JobId,
                job.AssignedAgentId.Value,
                group.Key,
                group.Sum(value => value.Quantity),
                ItemLocation.InBuilding(supply.BuildingId),
                supply.DepositStackIds[index],
                command.Tick);
            if (deposited.IsFailure)
            {
                return deposited;
            }
        }

        bool hasCarriedReservation = inventory.CreateSnapshot().Stacks.Any(value =>
            value.Location.Kind == ItemLocationKind.AgentInventory
            && value.Location.HasOwner
            && value.Location.OwnerId == job.AssignedAgentId.Value
            && value.Reservations.Any(reservation =>
                reservation.JobId == command.JobId
                && reservation.Quantity > 0));
        if (hasCarriedReservation)
        {
            return Result.Failure(InventoryErrors.ReservationNotFound);
        }

        Result completedSupply = supplyState.CompleteSupply(
            supply.BuildingId,
            command.JobId,
            command.Tick);
        if (completedSupply.IsFailure)
        {
            throw new InvalidOperationException(
                "Deposited building supply lost its incoming ledger.");
        }

        Result completedJob = jobs.AdvanceStage(command.JobId, command.Tick);
        if (completedJob.IsFailure)
        {
            throw new InvalidOperationException("Deposited supply job could not complete.");
        }

        _supplyRepository.Save(supplyState);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class CancelBuildingSupplyHandler
    : ICommandHandler<CancelBuildingSupplyCommand, Result>
{
    private readonly IBuildingSupplyRepository _supplyRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CancelBuildingSupplyHandler(
        IBuildingSupplyRepository supplyRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _supplyRepository = supplyRepository;
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CancelBuildingSupplyCommand command)
    {
        BuildingSupplyState supply = _supplyRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not BuildingSupplyJobDefinition definition)
        {
            return Result.Failure(JobErrors.NotFound);
        }

        if (job.AssignedAgentId.HasValue)
        {
            Result recovered = ProductionReservedResidentRecovery.DropCarriedItems(
                inventory,
                command.JobId,
                job.AssignedAgentId.Value,
                command.RecoveryCell,
                command.Tick);
            if (recovered.IsFailure)
            {
                return recovered;
            }
        }

        inventory.ReleaseReservations(command.JobId, command.Tick);
        inventory.ReleaseResidentSlotClaims(command.JobId, command.Tick);
        Result released = supply.ReleaseSupply(
            definition.BuildingId,
            command.JobId,
            command.Tick);
        if (released.IsFailure) return released;
        if (!job.IsTerminal)
        {
            Result cancelled = jobs.Cancel(
                command.JobId,
                new JobBlockReason("production.supply.cancelled", command.Reason),
                command.Tick);
            if (cancelled.IsFailure) return cancelled;
        }

        _supplyRepository.Save(supply);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
