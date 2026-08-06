using System;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Rooms;

namespace Dig.Application.Rooms
{

public sealed class CancelRoomUpgradeOperationHandler
    : ICommandHandler<
        CancelRoomUpgradeOperationCommand,
        Result<RoomUpgradeCancellationResult>>
{
    private readonly IRoomInfrastructureRepository _rooms;
    private readonly IInventoryRepository _inventory;
    private readonly IJobRepository _jobs;
    private readonly IEventSink _events;

    public CancelRoomUpgradeOperationHandler(
        IRoomInfrastructureRepository rooms,
        IInventoryRepository inventory,
        IJobRepository jobs,
        IEventSink events)
    {
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Result<RoomUpgradeCancellationResult> Handle(
        CancelRoomUpgradeOperationCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        RoomInfrastructureState rooms = _rooms.Get();
        InventoryState inventory = _inventory.Get();
        JobSystem jobs = _jobs.Get();
        RoomInfrastructureProjectSnapshot? room = rooms.Get(
            command.RoomInfrastructureId);
        if (room == null)
        {
            return Result<RoomUpgradeCancellationResult>.Failure(
                RoomInfrastructureErrors.RoomNotFound);
        }

        Result<RoomUpgradeCancellationResult> cancelled =
            rooms.CancelUpgradeBeforeWork(
                command.RoomInfrastructureId,
                command.Reason,
                command.Tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        for (int index = 0; index < cancelled.Value.ActiveJobIds.Count; index++)
        {
            EntityId jobId = cancelled.Value.ActiveJobIds[index];
            JobSnapshot? job = jobs.Get(jobId);
            if (job != null && !job.IsTerminal)
            {
                Result ended = jobs.Cancel(
                    jobId,
                    new JobBlockReason(
                        "room_upgrade_cancelled",
                        command.Reason),
                    command.Tick);
                if (ended.IsFailure)
                {
                    throw new InvalidOperationException(
                        "Validated room cancellation could not cancel an attached job.");
                }
            }

            inventory.ReleaseReservations(jobId, command.Tick);
            inventory.ReleaseResidentSlotClaims(jobId, command.Tick);
        }

        _rooms.Save(rooms);
        _inventory.Save(inventory);
        _jobs.Save(jobs);
        _events.Append(rooms.DequeueUncommittedEvents());
        _events.Append(inventory.DequeueUncommittedEvents());
        _events.Append(jobs.DequeueUncommittedEvents());
        return cancelled;
    }
}

}
