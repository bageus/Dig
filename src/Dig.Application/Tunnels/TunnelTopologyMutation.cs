using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Tunnels
{

public sealed class RemoveTunnelSegmentHandler
    : ICommandHandler<RemoveTunnelSegmentCommand, Result>
{
    private static readonly JobBlockReason SegmentRemovedReason = new JobBlockReason(
        "tunnel.automatic_work.segment_removed",
        "The authoritative tunnel segment no longer exists.");

    private readonly ITunnelInfrastructureRepository _tunnelRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public RemoveTunnelSegmentHandler(
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

    public Result Handle(RemoveTunnelSegmentCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        TunnelInfrastructureState tunnels = _tunnelRepository.Get();
        if (tunnels.GetSegment(command.SegmentId) is null)
        {
            return Result.Failure(TunnelInfrastructureErrors.SegmentNotFound);
        }

        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        JobSnapshot[] affected = jobs.GetAll()
            .Where(job => !job.IsTerminal
                && job.Definition is TunnelAutomaticWorkJobDefinition definition
                && definition.SegmentId == command.SegmentId)
            .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
            .ToArray();

        foreach (JobSnapshot job in affected)
        {
            Result cancelled = jobs.Cancel(job.Id, SegmentRemovedReason, command.Tick);
            if (cancelled.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Validated automatic tunnel job '{job.Id}' could not be cancelled.");
            }

            inventory.ReleaseReservations(job.Id, command.Tick);
        }

        Result removed = tunnels.RemoveSegment(command.SegmentId, command.Tick);
        if (removed.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated tunnel segment could not be removed.");
        }

        _tunnelRepository.Save(tunnels);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(tunnels.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}
}
