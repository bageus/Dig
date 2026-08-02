using System;
using System.Collections.Generic;
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

public sealed class SynchronizeTunnelAutomaticSupportHandler
    : ICommandHandler<
        SynchronizeTunnelAutomaticSupportCommand,
        Result<TunnelAutomaticSupportSyncResult>>
{
    private readonly ITunnelInfrastructureRepository _tunnelRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public SynchronizeTunnelAutomaticSupportHandler(
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

    public Result<TunnelAutomaticSupportSyncResult> Handle(
        SynchronizeTunnelAutomaticSupportCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        TunnelInfrastructureState tunnels = _tunnelRepository.Get();
        HorizontalTunnelSegmentSnapshot? segment = tunnels.GetSegment(command.SegmentId);
        if (segment is null)
        {
            return Result<TunnelAutomaticSupportSyncResult>.Failure(
                TunnelInfrastructureApplicationErrors.SegmentNotFound);
        }

        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        Result<JobSnapshot?> existingResult = FindActiveJob(jobs, command.SegmentId);
        if (existingResult.IsFailure)
        {
            return Result<TunnelAutomaticSupportSyncResult>.Failure(existingResult.Error!);
        }

        JobSnapshot? existing = existingResult.Value;
        TunnelAutomaticSupportTargetSnapshot? target = segment.NextAutomaticSupportTarget;
        if (!target.HasValue)
        {
            Result ended = EndObsolete(existing, inventory, jobs, command.Tick);
            if (ended.IsFailure)
            {
                return Result<TunnelAutomaticSupportSyncResult>.Failure(ended.Error!);
            }

            Save(tunnels, inventory, jobs);
            return Success(TunnelAutomaticSupportSyncStatus.NoTarget, null, null);
        }

        if (!TunnelAutomaticWorkPlanner.IsWithinCompletedBuildingRange(
            target.Value.TargetCell,
            command.CompletedBuildingCells))
        {
            Result ended = EndObsolete(existing, inventory, jobs, command.Tick);
            if (ended.IsFailure)
            {
                return Result<TunnelAutomaticSupportSyncResult>.Failure(ended.Error!);
            }

            Save(tunnels, inventory, jobs);
            return Success(
                TunnelAutomaticSupportSyncStatus.OutOfRange,
                null,
                target.Value.TargetCell);
        }

        if (existing?.Definition is TunnelAutomaticWorkJobDefinition existingDefinition
            && existingDefinition.TargetCell != target.Value.TargetCell)
        {
            Result ended = EndObsolete(existing, inventory, jobs, command.Tick);
            if (ended.IsFailure)
            {
                return Result<TunnelAutomaticSupportSyncResult>.Failure(ended.Error!);
            }

            existing = null;
        }

        if (existing is null)
        {
            TunnelAutomaticWorkJobDefinition definition = CreateDefinition(
                command.NewJobId,
                command.SegmentId,
                target.Value.TargetCell,
                command.Tick,
                source: null);
            Result added = jobs.Add(definition);
            if (added.IsFailure)
            {
                return Result<TunnelAutomaticSupportSyncResult>.Failure(added.Error!);
            }

            existing = jobs.Get(command.NewJobId)!;
        }

        if (existing.Status != JobStatus.Created)
        {
            Save(tunnels, inventory, jobs);
            return Success(
                TunnelAutomaticSupportSyncStatus.Retained,
                existing.Id,
                target.Value.TargetCell);
        }

        TunnelAutomaticWorkJobDefinition pending =
            (TunnelAutomaticWorkJobDefinition)existing.Definition;
        if (pending.IsSourceResolved)
        {
            Result available = jobs.MakeAvailable(existing.Id, command.Tick);
            if (available.IsFailure)
            {
                return Result<TunnelAutomaticSupportSyncResult>.Failure(available.Error!);
            }

            Save(tunnels, inventory, jobs);
            return Success(
                TunnelAutomaticSupportSyncStatus.Available,
                existing.Id,
                target.Value.TargetCell);
        }

        TunnelAutomaticWorkSource? source = TunnelAutomaticWorkPlanner.SelectSource(
            pending.RequiredItemId,
            pending.TargetCell,
            inventory.GetAvailableWorldStacks(),
            command.RevealedCells,
            command.ReachableCells);
        if (!source.HasValue)
        {
            Save(tunnels, inventory, jobs);
            return Success(
                TunnelAutomaticSupportSyncStatus.PendingSource,
                existing.Id,
                target.Value.TargetCell);
        }

        Result reserved = inventory.ReserveQuantity(
            source.Value.StackId,
            existing.Id,
            quantity: 1,
            command.Tick);
        if (reserved.IsFailure)
        {
            return Result<TunnelAutomaticSupportSyncResult>.Failure(reserved.Error!);
        }

        TunnelAutomaticWorkJobDefinition resolved = CreateDefinition(
            existing.Id,
            pending.SegmentId,
            pending.TargetCell,
            pending.CreatedTick,
            source);
        Result definitionResolved = jobs.ResolveCreatedDefinition(
            existing.Id,
            resolved,
            command.Tick);
        if (definitionResolved.IsFailure)
        {
            inventory.ReleaseReservations(existing.Id, command.Tick);
            return Result<TunnelAutomaticSupportSyncResult>.Failure(
                definitionResolved.Error!);
        }

        Result madeAvailable = jobs.MakeAvailable(existing.Id, command.Tick);
        if (madeAvailable.IsFailure)
        {
            inventory.ReleaseReservations(existing.Id, command.Tick);
            Result cancelled = jobs.Cancel(
                existing.Id,
                new JobBlockReason(
                    "tunnel.automatic_support.resolve_rolled_back",
                    "Automatic support source resolution could not become available."),
                command.Tick);
            if (cancelled.IsFailure)
            {
                throw new InvalidOperationException(
                    "Resolved automatic tunnel job could not be rolled back.");
            }

            Save(tunnels, inventory, jobs);
            return Result<TunnelAutomaticSupportSyncResult>.Failure(
                madeAvailable.Error!);
        }

        Save(tunnels, inventory, jobs);
        return Success(
            TunnelAutomaticSupportSyncStatus.Available,
            existing.Id,
            target.Value.TargetCell);
    }

    private static TunnelAutomaticWorkJobDefinition CreateDefinition(
        EntityId jobId,
        EntityId segmentId,
        CellId targetCell,
        long createdTick,
        TunnelAutomaticWorkSource? source)
    {
        return new TunnelAutomaticWorkJobDefinition(
            jobId,
            segmentId,
            TunnelAutomaticWorkKind.WoodenSupport,
            targetCell,
            createdTick,
            JobRetryPolicy.Default,
            source?.StackId,
            source?.Cell);
    }

    private static Result<JobSnapshot?> FindActiveJob(JobSystem jobs, EntityId segmentId)
    {
        JobSnapshot[] matches = jobs.GetAll()
            .Where(job => !job.IsTerminal
                && job.Definition is TunnelAutomaticWorkJobDefinition definition
                && definition.Kind == TunnelAutomaticWorkKind.WoodenSupport
                && definition.SegmentId == segmentId)
            .ToArray();
        return matches.Length <= 1
            ? Result<JobSnapshot?>.Success(matches.SingleOrDefault())
            : Result<JobSnapshot?>.Failure(
                TunnelInfrastructureApplicationErrors.MultipleActiveAutomaticJobs);
    }

    private static Result EndObsolete(
        JobSnapshot? existing,
        InventoryState inventory,
        JobSystem jobs,
        long tick)
    {
        if (existing is null)
        {
            return Result.Success();
        }

        Result cancelled = jobs.Cancel(
            existing.Id,
            new JobBlockReason(
                "tunnel.automatic_support.target_obsolete",
                "The rolling structural anchor changed the automatic support target."),
            tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        inventory.ReleaseReservations(existing.Id, tick);
        return Result.Success();
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

    private static Result<TunnelAutomaticSupportSyncResult> Success(
        TunnelAutomaticSupportSyncStatus status,
        EntityId? jobId,
        CellId? targetCell)
    {
        return Result<TunnelAutomaticSupportSyncResult>.Success(
            new TunnelAutomaticSupportSyncResult(status, jobId, targetCell));
    }
}
}
