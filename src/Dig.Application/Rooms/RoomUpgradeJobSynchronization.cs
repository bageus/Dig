using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Rooms;
using Dig.Domain.World;

namespace Dig.Application.Rooms
{

public sealed class SynchronizeRoomUpgradeJobsHandler
    : ICommandHandler<
        SynchronizeRoomUpgradeJobsCommand,
        Result<RoomUpgradeJobSynchronizationReport>>
{
    private readonly IRoomInfrastructureRepository _rooms;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IRoomUpgradeJobIdSource _jobIds;
    private readonly IEventSink _events;

    public SynchronizeRoomUpgradeJobsHandler(
        IRoomInfrastructureRepository rooms,
        IInventoryRepository inventory,
        IJobRepository jobs,
        IRoomUpgradeJobIdSource jobIds,
        IEventSink events)
    {
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _jobIds = jobIds ?? throw new ArgumentNullException(nameof(jobIds));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result<RoomUpgradeJobSynchronizationReport> Handle(
        SynchronizeRoomUpgradeJobsCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        RoomInfrastructureState rooms = _rooms.Get();
        InventoryState inventory = _inventory.Get();
        JobSystem jobs = _jobs.Get();
        HashSet<CellId> revealed = command.RevealedCells.ToHashSet();
        HashSet<CellId> reachable = command.ReachableCells.ToHashSet();
        List<EntityId> workCreated = new List<EntityId>();
        List<RoomUpgradeDeliveryJobPlan> deliveries =
            new List<RoomUpgradeDeliveryJobPlan>();

        RoomInfrastructureProjectSnapshot[] projects = rooms.CaptureSnapshot().Rooms
            .Where(room => room.UpgradeOrderCount == 1
                && room.Status != RoomImprovementStatus.Improved
                && room.TemporaryStockCell.HasValue)
            .OrderBy(room => room.RoomInfrastructureId.ToString(), StringComparer.Ordinal)
            .ToArray();
        for (int index = 0; index < projects.Length; index++)
        {
            RoomInfrastructureProjectSnapshot room = projects[index];
            Result<EntityId> work = EnsureWorkJob(
                rooms,
                inventory,
                jobs,
                room,
                command.Priority,
                command.Tick);
            if (work.IsFailure)
            {
                return Result<RoomUpgradeJobSynchronizationReport>.Failure(work.Error!);
            }

            if (!room.ActiveJobIds.Contains(work.Value))
            {
                workCreated.Add(work.Value);
            }

            if (room.Status != RoomImprovementStatus.AwaitingMaterials
                || deliveries.Count >= command.MaximumDeliveryJobs)
            {
                continue;
            }

            RoomInfrastructureProjectSnapshot current = rooms.Get(
                room.RoomInfrastructureId)!;
            Result planned = PlanDeliveries(
                rooms,
                inventory,
                jobs,
                current,
                revealed,
                reachable,
                command,
                deliveries);
            if (planned.IsFailure)
            {
                return Result<RoomUpgradeJobSynchronizationReport>.Failure(planned.Error!);
            }
        }

        SaveAndPublish(rooms, inventory, jobs);
        return Result<RoomUpgradeJobSynchronizationReport>.Success(
            new RoomUpgradeJobSynchronizationReport(workCreated, deliveries));
    }

    private Result<EntityId> EnsureWorkJob(
        RoomInfrastructureState rooms,
        InventoryState inventory,
        JobSystem jobs,
        RoomInfrastructureProjectSnapshot room,
        int priority,
        long tick)
    {
        EntityId? existing = room.ActiveJobIds
            .Select(jobs.Get)
            .Where(value => value?.Definition is RoomUpgradeWorkJobDefinition)
            .Select(value => (EntityId?)value!.Id)
            .FirstOrDefault();
        if (existing.HasValue)
        {
            return Result<EntityId>.Success(existing.Value);
        }

        EntityId jobId = NextJobId();
        RoomUpgradeWorkJobDefinition definition = new RoomUpgradeWorkJobDefinition(
            jobId,
            room.RoomInfrastructureId,
            room.TemporaryStockCell!.Value,
            priority,
            tick,
            JobRetryPolicy.Default);
        Result added = jobs.Add(definition);
        if (added.IsFailure)
        {
            return Result<EntityId>.Failure(added.Error!);
        }

        Result attached = rooms.AttachJob(room.RoomInfrastructureId, jobId, tick);
        if (attached.IsFailure)
        {
            return Result<EntityId>.Failure(attached.Error!);
        }

        Result reserved = ReserveDeliveredStock(inventory, room, jobId, tick);
        if (reserved.IsFailure)
        {
            return Result<EntityId>.Failure(reserved.Error!);
        }

        if (room.Status == RoomImprovementStatus.ReadyForWork)
        {
            Result available = jobs.MakeAvailable(jobId, tick);
            if (available.IsFailure)
            {
                return Result<EntityId>.Failure(available.Error!);
            }
        }

        return Result<EntityId>.Success(jobId);
    }

    private static Result ReserveDeliveredStock(
        InventoryState inventory,
        RoomInfrastructureProjectSnapshot room,
        EntityId workJobId,
        long tick)
    {
        ItemLocation stock = ItemLocation.InWorld(room.TemporaryStockCell!.Value);
        foreach (RoomMaterialLedgerSnapshot material in room.Materials)
        {
            int requiredReservation = material.Delivered - material.Consumed;
            int currentReservation = inventory.GetReservedQuantityAt(
                workJobId,
                material.ItemId,
                stock);
            int missing = requiredReservation - currentReservation;
            if (missing < 0)
            {
                return Result.Failure(RoomUpgradeExecutionErrors.StockReservationInvalid);
            }

            if (missing > 0)
            {
                Result reserved = inventory.ReserveAvailableAt(
                    stock,
                    material.ItemId,
                    workJobId,
                    missing,
                    tick);
                if (reserved.IsFailure)
                {
                    return reserved;
                }
            }
        }

        return Result.Success();
    }

    private Result PlanDeliveries(
        RoomInfrastructureState rooms,
        InventoryState inventory,
        JobSystem jobs,
        RoomInfrastructureProjectSnapshot room,
        HashSet<CellId> revealed,
        HashSet<CellId> reachable,
        SynchronizeRoomUpgradeJobsCommand command,
        List<RoomUpgradeDeliveryJobPlan> deliveries)
    {
        ItemLocation destination = ItemLocation.InWorld(
            room.TemporaryStockCell!.Value);
        foreach (RoomMaterialRequirement requirement in
            RoomUpgradeCostCatalog.Get(room.TemplateKind))
        {
            if (deliveries.Count >= command.MaximumDeliveryJobs)
            {
                break;
            }

            RoomMaterialLedgerSnapshot ledger = room.Materials.Single(
                value => value.ItemId == requirement.ItemId);
            int incoming = room.ActiveJobIds
                .Select(jobs.Get)
                .Where(value => value != null && !value.IsTerminal)
                .Select(value => value!.Definition)
                .OfType<HaulJobDefinition>()
                .Where(value => value.ItemId == requirement.ItemId
                    && value.Destination == destination)
                .Sum(value => value.Quantity);
            int remaining = ledger.Required - ledger.Delivered - incoming;
            if (remaining <= 0)
            {
                continue;
            }

            ItemStackSnapshot[] sources = inventory.GetAvailableWorldStacks()
                .Where(stack => stack.ItemId == requirement.ItemId
                    && stack.Location.HasCell
                    && stack.Location.CellId != room.TemporaryStockCell.Value
                    && revealed.Contains(stack.Location.CellId)
                    && reachable.Contains(stack.Location.CellId))
                .OrderBy(stack => Manhattan(
                    stack.Location.CellId,
                    room.TemporaryStockCell.Value))
                .ThenBy(stack => stack.Location.CellId)
                .ThenBy(stack => stack.StackId.ToString(), StringComparer.Ordinal)
                .ToArray();
            for (int sourceIndex = 0;
                sourceIndex < sources.Length
                    && remaining > 0
                    && deliveries.Count < command.MaximumDeliveryJobs;
                sourceIndex++)
            {
                ItemStackSnapshot source = inventory.GetStack(
                    sources[sourceIndex].StackId)!;
                int quantity = Math.Min(remaining, source.AvailableQuantity);
                if (quantity <= 0)
                {
                    continue;
                }

                EntityId jobId = NextJobId();
                Result reserved = inventory.ReserveQuantity(
                    source.StackId,
                    jobId,
                    quantity,
                    command.Tick);
                if (reserved.IsFailure)
                {
                    continue;
                }

                HaulJobDefinition hauling = new HaulJobDefinition(
                    jobId,
                    source.StackId,
                    source.ItemId,
                    quantity,
                    destination,
                    command.Priority,
                    command.Tick,
                    JobRetryPolicy.Default);
                Result added = jobs.Add(hauling);
                Result available = added.IsSuccess
                    ? jobs.MakeAvailable(jobId, command.Tick)
                    : added;
                Result attached = available.IsSuccess
                    ? rooms.AttachJob(room.RoomInfrastructureId, jobId, command.Tick)
                    : available;
                if (attached.IsFailure)
                {
                    inventory.ReleaseReservations(jobId, command.Tick);
                    return attached;
                }

                deliveries.Add(new RoomUpgradeDeliveryJobPlan(
                    room.RoomInfrastructureId,
                    jobId,
                    source.StackId,
                    quantity));
                remaining -= quantity;
            }
        }

        return Result.Success();
    }

    private EntityId NextJobId()
    {
        EntityId id = _jobIds.NextJobId();
        if (id.IsEmpty)
        {
            throw new InvalidOperationException(
                "Room upgrade job id source returned an empty id.");
        }

        return id;
    }

    private void SaveAndPublish(
        RoomInfrastructureState rooms,
        InventoryState inventory,
        JobSystem jobs)
    {
        _rooms.Save(rooms);
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(rooms.DequeueUncommittedEvents());
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
    }

    private static int Manhattan(CellId left, CellId right)
    {
        return Math.Abs(left.X - right.X)
            + Math.Abs(left.Y - right.Y)
            + Math.Abs(left.Z - right.Z);
    }
}

}
