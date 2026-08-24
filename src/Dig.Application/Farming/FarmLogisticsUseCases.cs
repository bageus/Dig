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
using Dig.Domain.World;

namespace Dig.Application.Farming
{

public sealed class SynchronizeFarmLogisticsHandler
    : ICommandHandler<
        SynchronizeFarmLogisticsCommand,
        Result<FarmLogisticsSynchronizationReport>>
{
    private static readonly JobBlockReason ObsoleteDemandReason = new JobBlockReason(
        "farm_demand_changed",
        "The farm no longer requests this delivery.");
    private readonly IFarmRepository _farms;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly FarmItemCatalog _items;
    private readonly FarmLogisticsReservations _reservations;
    private readonly IFarmLogisticsJobIdSource _jobIds;
    private readonly IEventSink _events;

    public SynchronizeFarmLogisticsHandler(
        IFarmRepository farms,
        IInventoryRepository inventory,
        IJobRepository jobs,
        FarmItemCatalog items,
        FarmLogisticsReservations reservations,
        IFarmLogisticsJobIdSource jobIds,
        IEventSink events)
    {
        _farms = farms ?? throw new ArgumentNullException(nameof(farms));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
        _jobIds = jobIds ?? throw new ArgumentNullException(nameof(jobIds));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result<FarmLogisticsSynchronizationReport> Handle(
        SynchronizeFarmLogisticsCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        InventoryState inventory = _inventory.Get();
        JobSystem jobs = _jobs.Get();
        int released = ReconcileReservations(jobs, command.Tick, inventory);
        HashSet<CellId> reachable = command.ReachableCells.ToHashSet();
        List<FarmLogisticsJobPlan> created = new List<FarmLogisticsJobPlan>();

        foreach (EntityId farmId in _farms.GetFarmIds()
            .OrderBy(value => value.ToString(), StringComparer.Ordinal))
        {
            FarmState? farm = _farms.Get(farmId);
            if (farm == null) continue;
            foreach (FarmDeliveryDemand demand in farm.GetDeliveryDemands())
            {
                if (created.Count >= command.MaximumJobs) break;
                int remaining = _reservations.GetUnreservedIncoming(
                    farmId, demand.Kind, demand.Quantity);
                if (remaining <= 0) continue;
                PlanDemand(
                    inventory,
                    jobs,
                    farmId,
                    demand.Kind,
                    demand.Quantity,
                    remaining,
                    reachable,
                    command,
                    created);
            }
        }

        SaveAndPublish(inventory, jobs);
        return Result<FarmLogisticsSynchronizationReport>.Success(
            new FarmLogisticsSynchronizationReport(created, released));
    }

    private void PlanDemand(
        InventoryState inventory,
        JobSystem jobs,
        EntityId farmId,
        FarmDeliveryKind kind,
        int demandedQuantity,
        int remaining,
        HashSet<CellId> reachable,
        SynchronizeFarmLogisticsCommand command,
        List<FarmLogisticsJobPlan> created)
    {
        ItemId itemId = _items.Resolve(kind);
        ItemStackSnapshot[] sources = inventory.GetAvailableWorldStacks()
            .Where(value => value.ItemId == itemId
                && value.Location.HasCell
                && reachable.Contains(value.Location.CellId))
            .OrderBy(value => value.Location.CellId)
            .ThenBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .ToArray();
        foreach (ItemStackSnapshot candidate in sources)
        {
            if (remaining <= 0 || created.Count >= command.MaximumJobs) break;
            ItemStackSnapshot? source = inventory.GetStack(candidate.StackId);
            int quantity = Math.Min(remaining, source?.AvailableQuantity ?? 0);
            if (quantity <= 0) continue;
            EntityId jobId = NextJobId();
            if (!_reservations.TryReserveIncoming(
                jobId, farmId, kind, demandedQuantity, quantity))
            {
                continue;
            }

            Result reserved = inventory.ReserveQuantity(
                source!.StackId, jobId, quantity, command.Tick);
            if (reserved.IsFailure)
            {
                _reservations.Release(jobId);
                continue;
            }

            HaulJobDefinition haul = new HaulJobDefinition(
                jobId,
                source.StackId,
                source.ItemId,
                quantity,
                ItemLocation.InBuilding(farmId),
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
                continue;
            }

            created.Add(new FarmLogisticsJobPlan(
                farmId, jobId, source.StackId, kind, quantity));
            remaining -= quantity;
        }
    }

    private int ReconcileReservations(JobSystem jobs, long tick, InventoryState inventory)
    {
        int released = 0;
        Dictionary<string, int> retainedIncoming = new Dictionary<string, int>(
            StringComparer.Ordinal);
        foreach (FarmLogisticsReservation reservation in _reservations.GetAll())
        {
            JobSnapshot? job = jobs.Get(reservation.JobId);
            FarmState? farm = _farms.Get(reservation.BuildingId);
            bool canContinue = job != null && !job.IsTerminal && farm != null;
            if (canContinue && reservation.Direction == FarmLogisticsDirection.Incoming)
            {
                canContinue = CanRetainIncoming(reservation, farm, retainedIncoming);
            }

            if (canContinue)
            {
                continue;
            }

            if (job != null && !job.IsTerminal)
            {
                Result cancelled = jobs.Cancel(job.Id, ObsoleteDemandReason, tick);
                if (cancelled.IsFailure)
                {
                    throw new InvalidOperationException(
                        "Obsolete farm delivery could not be cancelled.");
                }
            }

            inventory.ReleaseReservations(reservation.JobId, tick);
            inventory.ReleaseResidentSlotClaims(reservation.JobId, tick);
            if (_reservations.Release(reservation.JobId)) released++;
        }

        return released;
    }

    private static bool CanRetainIncoming(
        FarmLogisticsReservation reservation,
        FarmState? farm,
        IDictionary<string, int> retained)
    {
        if (farm == null) return false;
        FarmDeliveryDemand? demand = farm.GetDeliveryDemands()
            .FirstOrDefault(value => value.Kind == reservation.Kind);
        if (!demand.HasValue) return false;
        string key = reservation.BuildingId + ":" + reservation.Kind;
        retained.TryGetValue(key, out int quantity);
        int next = checked(quantity + reservation.Quantity);
        if (next > demand.Value.Quantity) return false;
        retained[key] = next;
        return true;
    }

    private EntityId NextJobId()
    {
        EntityId id = _jobIds.NextJobId();
        if (id.IsEmpty) throw new InvalidOperationException("Farm job id cannot be empty.");
        return id;
    }

    private void SaveAndPublish(InventoryState inventory, JobSystem jobs)
    {
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
    }
}

public sealed class CompleteFarmDeliveryHandler
    : ICommandHandler<CompleteFarmDeliveryCommand, Result>
{
    private readonly IFarmRepository _farms;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly FarmItemCatalog _items;
    private readonly FarmLogisticsReservations _reservations;
    private readonly IEventSink _events;

    public CompleteFarmDeliveryHandler(
        IFarmRepository farms,
        IInventoryRepository inventory,
        IJobRepository jobs,
        FarmItemCatalog items,
        FarmLogisticsReservations reservations,
        IEventSink events)
    {
        _farms = farms;
        _inventory = inventory;
        _jobs = jobs;
        _items = items;
        _reservations = reservations;
        _events = events;
    }

    public Result Handle(CompleteFarmDeliveryCommand command)
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
            || reservation.Direction != FarmLogisticsDirection.Incoming)
        {
            return Result.Failure(FarmLogisticsErrors.JobMismatch);
        }

        InventoryState inventory = _inventory.Get();
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job?.Definition is not HaulJobDefinition hauling
            || !job.AssignedAgentId.HasValue
            || hauling.Destination != ItemLocation.InBuilding(reservation.BuildingId)
            || hauling.ItemId != _items.Resolve(reservation.Kind)
            || hauling.Quantity != reservation.Quantity)
        {
            return Result.Failure(FarmLogisticsErrors.JobMismatch);
        }

        if (job.Status != JobStatus.InProgress
            || job.Stage != JobStageKind.DepositItem)
        {
            return Result.Failure(FarmLogisticsErrors.InvalidStage);
        }

        FarmState? farm = _farms.Get(reservation.BuildingId);
        FarmDeliveryDemand? demand = farm?.GetDeliveryDemands()
            .FirstOrDefault(value => value.Kind == reservation.Kind);
        if (farm == null || !demand.HasValue || demand.Value.Quantity < hauling.Quantity)
        {
            return Result.Failure(FarmApplicationErrors.InvalidDelivery);
        }

        Result deposited = ResidentItemTransferService.DepositReservedResidentItems(
            inventory,
            hauling.SourceStackId,
            job.Id,
            job.AssignedAgentId.Value,
            hauling.ItemId,
            hauling.Quantity,
            hauling.Destination,
            command.DepositedStackId,
            command.Tick);
        if (deposited.IsFailure) return deposited;
        Result consumed = inventory.ConsumeAvailableAt(
            hauling.Destination,
            new[] { new ItemConsumptionRequest(hauling.ItemId, hauling.Quantity) },
            command.Tick);
        if (consumed.IsFailure)
        {
            throw new InvalidOperationException("Validated farm stock could not be consumed.");
        }

        farm.Deliver(reservation.Kind, hauling.Quantity, command.Tick);
        Result completed = jobs.AdvanceStage(job.Id, command.Tick);
        if (completed.IsFailure)
        {
            throw new InvalidOperationException("Validated farm delivery could not complete.");
        }

        inventory.ReleaseResidentSlotClaims(job.Id, command.Tick);
        _reservations.Release(job.Id);
        _farms.Save(reservation.BuildingId, farm);
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

}
