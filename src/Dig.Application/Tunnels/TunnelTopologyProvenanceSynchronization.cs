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

public sealed class SynchronizeTunnelTopologyHandler
    : ICommandHandler<
        SynchronizeTunnelTopologyCommand,
        Result<TunnelTopologySynchronizationResult>>
{
    private static readonly JobBlockReason TopologyChangedReason = new JobBlockReason(
        "tunnel.automatic_work.topology_changed",
        "Completed excavation changed the authoritative tunnel topology.");

    private readonly ITunnelInfrastructureRepository _tunnelRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public SynchronizeTunnelTopologyHandler(
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

    public Result<TunnelTopologySynchronizationResult> Handle(
        SynchronizeTunnelTopologyCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        TunnelInfrastructureState tunnels = _tunnelRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        TunnelInfrastructureSnapshot before = tunnels.CaptureSnapshot();
        Dictionary<TunnelTopologySegmentKey, HorizontalTunnelSegmentSnapshot> existing =
            before.Segments.ToDictionary(KeyOf);
        Dictionary<TunnelTopologySegmentKey, TunnelTopologySegmentProvenance> desired =
            command.Segments.ToDictionary(value => value.Key);

        Result preflight = Preflight(existing, desired, command.Segments);
        if (preflight.IsFailure)
        {
            return Result<TunnelTopologySynchronizationResult>.Failure(preflight.Error!);
        }

        HorizontalTunnelSegmentSnapshot[] removed = existing
            .Where(entry => !desired.ContainsKey(entry.Key))
            .Select(entry => entry.Value)
            .OrderBy(value => value.SegmentId.ToString(), StringComparer.Ordinal)
            .ToArray();
        TunnelTopologySegmentProvenance[] added = desired
            .Where(entry => !existing.ContainsKey(entry.Key))
            .Select(entry => entry.Value)
            .OrderBy(value => value.Key)
            .ToArray();
        TunnelTopologySegmentProvenance[] updated = desired
            .Where(entry => existing.TryGetValue(entry.Key, out HorizontalTunnelSegmentSnapshot? current)
                && !current.OrderedHorizontalCells.SequenceEqual(
                    entry.Value.OrderedHorizontalCells))
            .Select(entry => entry.Value)
            .OrderBy(value => value.Key)
            .ToArray();

        foreach (HorizontalTunnelSegmentSnapshot segment in removed)
        {
            CancelAllSegmentJobs(segment.SegmentId, inventory, jobs, command.Tick);
            RequireSuccess(tunnels.RemoveSegment(segment.SegmentId, command.Tick));
        }

        foreach (TunnelTopologySegmentProvenance segment in updated)
        {
            HorizontalTunnelSegmentSnapshot current = existing[segment.Key];
            ReplaceSegment(tunnels, current, segment, command.Tick);
            CancelObsoleteSupportJobs(
                tunnels.GetSegment(current.SegmentId)!,
                inventory,
                jobs,
                command.Tick);
        }

        foreach (TunnelTopologySegmentProvenance segment in added)
        {
            RequireSuccess(tunnels.RegisterSegment(
                segment.SegmentId,
                segment.OriginKind,
                segment.OriginCell,
                segment.OrderedHorizontalCells,
                command.Tick));
        }

        Save(tunnels, inventory, jobs);
        int retained = command.Segments.Count - added.Length - updated.Length;
        return Result<TunnelTopologySynchronizationResult>.Success(
            new TunnelTopologySynchronizationResult(
                added.Length,
                updated.Length,
                removed.Length,
                retained));
    }

    private static Result Preflight(
        IReadOnlyDictionary<TunnelTopologySegmentKey, HorizontalTunnelSegmentSnapshot> existing,
        IReadOnlyDictionary<TunnelTopologySegmentKey, TunnelTopologySegmentProvenance> desired,
        IReadOnlyList<TunnelTopologySegmentProvenance> orderedDesired)
    {
        Dictionary<EntityId, TunnelTopologySegmentKey> existingIds = existing
            .ToDictionary(entry => entry.Value.SegmentId, entry => entry.Key);
        foreach (KeyValuePair<TunnelTopologySegmentKey, TunnelTopologySegmentProvenance> entry
            in desired)
        {
            if (existing.TryGetValue(entry.Key, out HorizontalTunnelSegmentSnapshot? current)
                && current.SegmentId != entry.Value.SegmentId)
            {
                return Result.Failure(
                    TunnelTopologySynchronizationErrors.SegmentIdentityMismatch);
            }

            if (existingIds.TryGetValue(
                    entry.Value.SegmentId,
                    out TunnelTopologySegmentKey existingKey)
                && !existingKey.Equals(entry.Key))
            {
                return Result.Failure(
                    TunnelTopologySynchronizationErrors.SegmentIdConflict);
            }
        }

        TunnelInfrastructureState validation = new TunnelInfrastructureState();
        for (int index = 0; index < orderedDesired.Count; index++)
        {
            TunnelTopologySegmentProvenance segment = orderedDesired[index];
            Result registered = validation.RegisterSegment(
                segment.SegmentId,
                segment.OriginKind,
                segment.OriginCell,
                segment.OrderedHorizontalCells,
                tick: 0);
            if (registered.IsFailure)
            {
                return registered;
            }
        }

        return Result.Success();
    }

    private static void ReplaceSegment(
        TunnelInfrastructureState tunnels,
        HorizontalTunnelSegmentSnapshot current,
        TunnelTopologySegmentProvenance desired,
        long tick)
    {
        HashSet<CellId> retainedCells = desired.OrderedHorizontalCells.ToHashSet();
        bool completedTrim = tunnels.CaptureSnapshot()
            .CompletedJunctionStoneTrimCells.Contains(current.OriginCell);
        RequireSuccess(tunnels.RemoveSegment(current.SegmentId, tick));
        RequireSuccess(tunnels.RegisterSegment(
            current.SegmentId,
            desired.OriginKind,
            desired.OriginCell,
            desired.OrderedHorizontalCells,
            tick));

        foreach (TunnelStructuralAnchorSnapshot anchor in current.StructuralAnchors
            .Where(value => value.Kind != TunnelStructuralAnchorKind.Origin
                && retainedCells.Contains(value.Cell))
            .OrderBy(value => value.DistanceFromOrigin)
            .ThenBy(value => value.Kind))
        {
            Result registered = anchor.Kind == TunnelStructuralAnchorKind.WoodenSupport
                ? tunnels.RegisterCompletedWoodenSupport(current.SegmentId, anchor.Cell, tick)
                : tunnels.RegisterCompletedDoor(current.SegmentId, anchor.Cell, tick);
            RequireSuccess(registered);
        }

        if (completedTrim
            && desired.OriginKind == TunnelSegmentOriginKind.VerticalJunction
            && !tunnels.CaptureSnapshot().CompletedJunctionStoneTrimCells
                .Contains(desired.OriginCell))
        {
            RequireSuccess(tunnels.RegisterCompletedJunctionStoneTrim(
                desired.OriginCell,
                tick));
        }
    }

    private static void CancelAllSegmentJobs(
        EntityId segmentId,
        InventoryState inventory,
        JobSystem jobs,
        long tick)
    {
        JobSnapshot[] affected = ActiveSegmentJobs(jobs, segmentId).ToArray();
        foreach (JobSnapshot job in affected)
        {
            Cancel(job, inventory, jobs, tick);
        }
    }

    private static void CancelObsoleteSupportJobs(
        HorizontalTunnelSegmentSnapshot segment,
        InventoryState inventory,
        JobSystem jobs,
        long tick)
    {
        CellId? currentTarget = segment.NextAutomaticSupportTarget?.TargetCell;
        JobSnapshot[] obsolete = ActiveSegmentJobs(jobs, segment.SegmentId)
            .Where(job =>
            {
                TunnelAutomaticWorkJobDefinition definition =
                    (TunnelAutomaticWorkJobDefinition)job.Definition;
                return definition.Kind == TunnelAutomaticWorkKind.WoodenSupport
                    && (!currentTarget.HasValue
                        || definition.TargetCell != currentTarget.Value);
            })
            .ToArray();
        foreach (JobSnapshot job in obsolete)
        {
            Cancel(job, inventory, jobs, tick);
        }
    }

    private static IEnumerable<JobSnapshot> ActiveSegmentJobs(
        JobSystem jobs,
        EntityId segmentId)
    {
        return jobs.GetAll()
            .Where(job => !job.IsTerminal
                && job.Definition is TunnelAutomaticWorkJobDefinition definition
                && definition.SegmentId == segmentId)
            .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal);
    }

    private static void Cancel(
        JobSnapshot job,
        InventoryState inventory,
        JobSystem jobs,
        long tick)
    {
        RequireSuccess(jobs.Cancel(job.Id, TopologyChangedReason, tick));
        inventory.ReleaseReservations(job.Id, tick);
    }

    private static TunnelTopologySegmentKey KeyOf(
        HorizontalTunnelSegmentSnapshot segment)
    {
        int direction = Math.Sign(
            segment.OrderedHorizontalCells[0].X - segment.OriginCell.X);
        return new TunnelTopologySegmentKey(
            segment.OriginKind,
            segment.OriginCell,
            direction);
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

    private static void RequireSuccess(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error!.ToString());
        }
    }
}
}
