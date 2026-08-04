using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;

namespace Dig.Application.Tunnels
{

public sealed class SynchronizeTunnelJunctionTrimPlacementHandler
    : ICommandHandler<
        SynchronizeTunnelJunctionTrimPlacementCommand,
        Result<TunnelJunctionTrimPlacementSyncResult>>
{
    private readonly ITunnelInfrastructureRepository _tunnelRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public SynchronizeTunnelJunctionTrimPlacementHandler(
        ITunnelInfrastructureRepository tunnelRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _tunnelRepository = tunnelRepository
            ?? throw new ArgumentNullException(nameof(tunnelRepository));
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _jobRepository = jobRepository
            ?? throw new ArgumentNullException(nameof(jobRepository));
        _eventSink = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
    }

    public Result<TunnelJunctionTrimPlacementSyncResult> Handle(
        SynchronizeTunnelJunctionTrimPlacementCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        TunnelInfrastructureState tunnels = _tunnelRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot[] legacyJobs = jobs.GetAll()
            .Where(job => !job.IsTerminal
                && job.Definition is TunnelAutomaticWorkJobDefinition definition
                && definition.Kind == TunnelAutomaticWorkKind.JunctionStoneTrim)
            .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
            .ToArray();

        for (int index = 0; index < legacyJobs.Length; index++)
        {
            JobSnapshot job = legacyJobs[index];
            Result cancelled = jobs.Cancel(
                job.Id,
                new JobBlockReason(
                    "tunnel.junction_trim.manual_placement_only",
                    "Junction stone trim is created only through resident-owned placement mode."),
                command.Tick);
            if (cancelled.IsFailure)
            {
                return Failure(cancelled.Error!);
            }

            inventory.ReleaseReservations(job.Id, command.Tick);
        }

        Save(tunnels, inventory, jobs);
        return Result<TunnelJunctionTrimPlacementSyncResult>.Success(
            new TunnelJunctionTrimPlacementSyncResult(
                legacyJobs.Length == 0
                    ? TunnelJunctionTrimPlacementSyncStatus.PlacementOnly
                    : TunnelJunctionTrimPlacementSyncStatus.LegacyAutomaticJobsCancelled,
                legacyJobs.Select(value => value.Id)));
    }

    private void Save(
        TunnelInfrastructureState tunnels,
        InventoryState inventory,
        JobSystem jobs)
    {
        _tunnelRepository.Save(tunnels);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(tunnels.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
    }

    private static Result<TunnelJunctionTrimPlacementSyncResult> Failure(
        DomainError error)
    {
        return Result<TunnelJunctionTrimPlacementSyncResult>.Failure(error);
    }
}
}
