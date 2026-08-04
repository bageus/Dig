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

public sealed class SynchronizeTunnelAutomaticJunctionTrimHandler
    : ICommandHandler<
        SynchronizeTunnelAutomaticJunctionTrimCommand,
        Result<TunnelAutomaticJunctionTrimSyncResult>>
{
    private readonly ITunnelInfrastructureRepository _tunnelRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public SynchronizeTunnelAutomaticJunctionTrimHandler(
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

    public Result<TunnelAutomaticJunctionTrimSyncResult> Handle(
        SynchronizeTunnelAutomaticJunctionTrimCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        TunnelInfrastructureState tunnels = _tunnelRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        Result<JobSnapshot?> existingResult = FindActiveJob(jobs, command.TargetCell);
        if (existingResult.IsFailure)
        {
            return Failure(existingResult.Error!);
        }

        JobSnapshot? existing = existingResult.Value;
        TunnelJunctionStoneTrimTargetSnapshot[] targets = tunnels
            .CaptureSnapshot()
            .PendingJunctionStoneTrimTargets
            .Where(value => value.Cell == command.TargetCell)
            .ToArray();
        TunnelJunctionStoneTrimTargetSnapshot? target = targets.Length == 0
            ? (TunnelJunctionStoneTrimTargetSnapshot?)null
            : targets.Single();
        if (!target.HasValue)
        {
            Result ended = EndObsolete(existing, inventory, jobs, command.Tick);
            if (ended.IsFailure)
            {
                return Failure(ended.Error!);
            }

            Save(tunnels, inventory, jobs);
            return Success(
                TunnelAutomaticJunctionTrimSyncStatus.NoTarget,
                null,
                command.TargetCell);
        }

        if (!TunnelAutomaticWorkPlanner.IsWithinCompletedBuildingRange(
            target.Value.Cell,
            command.CompletedBuildingCells))
        {
            Result ended = EndObsolete(existing, inventory, jobs, command.Tick);
            if (ended.IsFailure)
            {
                return Failure(ended.Error!);
            }

            Save(tunnels, inventory, jobs);
            return Success(
                TunnelAutomaticJunctionTrimSyncStatus.OutOfRange,
                null,
                command.TargetCell);
        }

        if (existing?.Definition is TunnelAutomaticWorkJobDefinition activeDefinition
            && activeDefinition.SegmentId != target.Value.OwnerSegmentId)
        {
            Result ended = EndObsolete(existing, inventory, jobs, command.Tick);
            if (ended.IsFailure)
            {
                return Failure(ended.Error!);
            }

            existing = null;
        }

        if (existing is null)
        {
            TunnelAutomaticWorkJobDefinition definition = CreateDefinition(
                command.NewJobId,
                target.Value.OwnerSegmentId,
                target.Value.Cell,
                command.Tick,
                source: null);
            Result added = jobs.Add(definition);
            if (added.IsFailure)
            {
                return Failure(added.Error!);
            }

            existing = jobs.Get(command.NewJobId)!;
        }

        if (existing.Status != JobStatus.Created)
        {
            Save(tunnels, inventory, jobs);
            return Success(
                TunnelAutomaticJunctionTrimSyncStatus.Retained,
                existing.Id,
                command.TargetCell);
        }

        TunnelAutomaticWorkJobDefinition pending =
            (TunnelAutomaticWorkJobDefinition)existing.Definition;
        if (pending.IsSourceResolved)
        {
            Result available = jobs.MakeAvailable(existing.Id, command.Tick);
            if (available.IsFailure)
            {
                return Failure(available.Error!);
            }

            Save(tunnels, inventory, jobs);
            return Success(
                TunnelAutomaticJunctionTrimSyncStatus.Available,
                existing.Id,
                command.TargetCell);
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
                TunnelAutomaticJunctionTrimSyncStatus.PendingSource,
                existing.Id,
                command.TargetCell);
        }

        Result reserved = inventory.ReserveQuantity(
            source.Value.StackId,
            existing.Id,
            quantity: 1,
            command.Tick);
        if (reserved.IsFailure)
        {
            return Failure(reserved.Error!);
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
            return Failure(definitionResolved.Error!);
        }

        Result madeAvailable = jobs.MakeAvailable(existing.Id, command.Tick);
        if (madeAvailable.IsFailure)
        {
            inventory.ReleaseReservations(existing.Id, command.Tick);
            Result cancelled = jobs.Cancel(
                existing.Id,
                new JobBlockReason(
                    "tunnel.junction_trim.resolve_rolled_back",
                    "Automatic junction trim source could not become available."),
                command.Tick);
            if (cancelled.IsFailure)
            {
                throw new InvalidOperationException(
                    "Resolved automatic junction trim job could not be rolled back.");
            }

            Save(tunnels, inventory, jobs);
            return Failure(madeAvailable.Error!);
        }

        Save(tunnels, inventory, jobs);
        return Success(
            TunnelAutomaticJunctionTrimSyncStatus.Available,
            existing.Id,
            command.TargetCell);
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
            TunnelAutomaticWorkKind.JunctionStoneTrim,
            targetCell,
            createdTick,
            JobRetryPolicy.Default,
            source?.StackId,
            source?.Cell);
    }

    private static Result<JobSnapshot?> FindActiveJob(
        JobSystem jobs,
        CellId targetCell)
    {
        JobSnapshot[] matches = jobs.GetAll()
            .Where(job => !job.IsTerminal
                && job.Definition is TunnelAutomaticWorkJobDefinition definition
                && definition.Kind == TunnelAutomaticWorkKind.JunctionStoneTrim
                && definition.TargetCell == targetCell)
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
                "tunnel.junction_trim.target_obsolete",
                "The authoritative vertical junction no longer requires stone trim."),
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

    private static Result<TunnelAutomaticJunctionTrimSyncResult> Success(
        TunnelAutomaticJunctionTrimSyncStatus status,
        EntityId? jobId,
        CellId targetCell)
    {
        return Result<TunnelAutomaticJunctionTrimSyncResult>.Success(
            new TunnelAutomaticJunctionTrimSyncResult(status, jobId, targetCell));
    }

    private static Result<TunnelAutomaticJunctionTrimSyncResult> Failure(
        DomainError error)
    {
        return Result<TunnelAutomaticJunctionTrimSyncResult>.Failure(error);
    }
}
}
