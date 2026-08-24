using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Farming
{

public sealed class SynchronizeFarmOutputsHandler
    : ICommandHandler<
        SynchronizeFarmOutputsCommand,
        Result<FarmLogisticsSynchronizationReport>>
{
    private readonly IFarmRepository _farms;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly FarmItemCatalog _items;
    private readonly FarmLogisticsReservations _reservations;
    private readonly IFarmLogisticsJobIdSource _ids;
    private readonly IEventSink _events;

    public SynchronizeFarmOutputsHandler(
        IFarmRepository farms,
        IInventoryRepository inventory,
        IJobRepository jobs,
        FarmItemCatalog items,
        FarmLogisticsReservations reservations,
        IFarmLogisticsJobIdSource ids,
        IEventSink events)
    {
        _farms = farms;
        _inventory = inventory;
        _jobs = jobs;
        _items = items;
        _reservations = reservations;
        _ids = ids;
        _events = events;
    }

    public Result<FarmLogisticsSynchronizationReport> Handle(
        SynchronizeFarmOutputsCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        InventoryState inventory = _inventory.Get();
        JobSystem jobs = _jobs.Get();
        List<FarmLogisticsJobPlan> created = new List<FarmLogisticsJobPlan>();
        foreach (FarmLogisticsSite site in command.Sites
            .OrderBy(value => value.FarmId.ToString(), StringComparer.Ordinal))
        {
            if (created.Count >= command.MaximumJobs) break;
            FarmState? farm = _farms.Get(site.FarmId);
            if (farm == null) continue;
            PlanExistingInternalOutputs(inventory, jobs, site, command, created);
            if (created.Count >= command.MaximumJobs) break;
            PlanNewOutputs(farm, inventory, jobs, site, command, created);
            _farms.Save(site.FarmId, farm);
        }

        SaveAndPublish(inventory, jobs);
        return Result<FarmLogisticsSynchronizationReport>.Success(
            new FarmLogisticsSynchronizationReport(created, 0));
    }

    private void PlanExistingInternalOutputs(
        InventoryState inventory,
        JobSystem jobs,
        FarmLogisticsSite site,
        SynchronizeFarmOutputsCommand command,
        List<FarmLogisticsJobPlan> created)
    {
        ItemLocation internalStock = ItemLocation.InBuilding(site.FarmId);
        foreach (ItemStackSnapshot stack in inventory.CreateSnapshot().Stacks
            .Where(value => value.Location == internalStock
                && value.AvailableQuantity > 0
                && (value.ItemId == _items.Hamster || value.ItemId == _items.Grub))
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal))
        {
            if (created.Count >= command.MaximumJobs) break;
            FarmDeliveryKind kind = stack.ItemId == _items.Hamster
                ? FarmDeliveryKind.Hamster
                : FarmDeliveryKind.Grub;
            CreateOutputJob(inventory, jobs, site, kind, stack.StackId, command, created);
        }
    }

    private void PlanNewOutputs(
        FarmState farm,
        InventoryState inventory,
        JobSystem jobs,
        FarmLogisticsSite site,
        SynchronizeFarmOutputsCommand command,
        List<FarmLogisticsJobPlan> created)
    {
        FarmDeliveryKind kind;
        int available;
        if (farm.Mode == FarmMode.Hamsters)
        {
            kind = FarmDeliveryKind.Hamster;
            available = farm.AvailableHamsters;
        }
        else if (farm.Mode == FarmMode.Grubs)
        {
            kind = FarmDeliveryKind.Grub;
            available = farm.AvailableGrubs;
        }
        else
        {
            return;
        }

        while (available > 0 && created.Count < command.MaximumJobs)
        {
            EntityId stackId = _ids.NextStackId();
            ItemId itemId = _items.Resolve(kind);
            bool collected = kind == FarmDeliveryKind.Hamster
                ? farm.CollectHamster()
                : farm.CollectGrub();
            if (!collected) break;
            Result added = inventory.AddUnit(
                stackId, itemId, ItemLocation.InBuilding(site.FarmId), command.Tick);
            if (added.IsFailure)
            {
                throw new InvalidOperationException(
                    "Validated farm output could not enter internal stock.");
            }

            CreateOutputJob(inventory, jobs, site, kind, stackId, command, created);
            available--;
        }
    }

    private void CreateOutputJob(
        InventoryState inventory,
        JobSystem jobs,
        FarmLogisticsSite site,
        FarmDeliveryKind kind,
        EntityId stackId,
        SynchronizeFarmOutputsCommand command,
        List<FarmLogisticsJobPlan> created)
    {
        EntityId jobId = _ids.NextJobId();
        if (!_reservations.TryReserveOutgoing(
            jobId, site.FarmId, kind, collectableQuantity: 1, quantity: 1))
        {
            return;
        }

        Result reserved = inventory.ReserveQuantity(stackId, jobId, 1, command.Tick);
        if (reserved.IsFailure)
        {
            _reservations.Release(jobId);
            return;
        }

        HaulJobDefinition haul = new HaulJobDefinition(
            jobId,
            stackId,
            _items.Resolve(kind),
            1,
            ItemLocation.InWorld(site.OutputCell),
            command.Priority,
            command.Tick,
            JobRetryPolicy.Default);
        Result added = jobs.Add(haul);
        Result available = added.IsSuccess
            ? jobs.MakeAvailable(jobId, command.Tick)
            : added;
        if (available.IsFailure)
        {
            inventory.ReleaseReservations(jobId, command.Tick);
            _reservations.Release(jobId);
            return;
        }

        created.Add(new FarmLogisticsJobPlan(
            site.FarmId, jobId, stackId, kind, 1));
    }

    private void SaveAndPublish(InventoryState inventory, JobSystem jobs)
    {
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class CompleteFarmOutputHandler
    : ICommandHandler<CompleteFarmOutputCommand, Result>
{
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly FarmLogisticsReservations _reservations;
    private readonly IEventSink _events;

    public CompleteFarmOutputHandler(
        IInventoryRepository inventory,
        IJobRepository jobs,
        FarmLogisticsReservations reservations,
        IEventSink events)
    {
        _inventory = inventory;
        _jobs = jobs;
        _reservations = reservations;
        _events = events;
    }

    public Result Handle(CompleteFarmOutputCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        JobSystem jobs = _jobs.Get();
        JobSnapshot? existing = jobs.Get(command.JobId);
        if (existing?.Definition is HaulJobDefinition
            && existing.Status == JobStatus.Completed)
        {
            return Result.Success();
        }

        if (!_reservations.TryGet(command.JobId, out FarmLogisticsReservation reservation)
            || reservation.Direction != FarmLogisticsDirection.Outgoing)
        {
            return Result.Failure(FarmLogisticsErrors.JobMismatch);
        }

        InventoryState inventory = _inventory.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not HaulJobDefinition haul
            || !job.AssignedAgentId.HasValue
            || job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.DepositItem
            || !haul.Destination.HasCell)
        {
            return Result.Failure(FarmLogisticsErrors.InvalidStage);
        }

        Result deposited = ResidentItemTransferService.DepositReservedResidentItems(
            inventory,
            haul.SourceStackId,
            job.Id,
            job.AssignedAgentId.Value,
            haul.ItemId,
            1,
            haul.Destination,
            command.DepositedStackId,
            command.Tick);
        if (deposited.IsFailure) return deposited;
        Result completed = jobs.AdvanceStage(job.Id, command.Tick);
        if (completed.IsFailure)
        {
            throw new InvalidOperationException("Validated farm output could not complete.");
        }

        inventory.ReleaseResidentSlotClaims(job.Id, command.Tick);
        _reservations.Release(job.Id);
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
