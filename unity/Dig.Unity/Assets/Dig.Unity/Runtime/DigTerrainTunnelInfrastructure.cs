using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.Tunnels;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.World;

namespace Dig.Unity
{
internal sealed partial class DigTerrainWorkSession
{
    private readonly TunnelRuntimeTopologyProjector _tunnelTopologyProjector =
        new TunnelRuntimeTopologyProjector();
    private readonly TunnelInfrastructureVisualPresenter _tunnelVisualPresenter =
        new TunnelInfrastructureVisualPresenter();
    private InMemoryTunnelInfrastructureRepository? _tunnelInfrastructure;
    private SynchronizeTunnelTopologyHandler? _tunnelTopologySync;
    private SynchronizeTunnelAutomaticSupportHandler? _tunnelSupportSync;
    private SynchronizeTunnelJunctionTrimPlacementHandler? _tunnelTrimPlacementSync;
    private CompleteTunnelAutomaticWorkHandler? _tunnelWorkCompletion;
    private Action<TunnelInfrastructureVisualVolumeViewModel>? _tunnelVisualSink;
    private ulong _tunnelAutomaticJobSequence = 1UL;

    internal void BindTunnelInfrastructureVisualSink(
        Action<TunnelInfrastructureVisualVolumeViewModel> sink)
    {
        _tunnelVisualSink = sink
            ?? throw new ArgumentNullException(nameof(sink));
        if (_tunnelInfrastructure == null)
        {
            sink(TunnelInfrastructureVisualVolumeViewModel.Empty());
            return;
        }

        PublishTunnelInfrastructureVisuals();
    }

    internal Result SynchronizeTunnelInfrastructureRuntime(
        long tick,
        IReadOnlyList<AgentViewModel> agents,
        IReadOnlyCollection<CellId> reachableCells)
    {
        if (agents == null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        if (reachableCells == null)
        {
            throw new ArgumentNullException(nameof(reachableCells));
        }

        EnsureTunnelInfrastructureRuntime();
        WorldSnapshot world = _worldSession.LoadSnapshot();
        IReadOnlyList<TunnelTopologySegmentProvenance> provenance =
            _tunnelTopologyProjector.Project(
                world,
                _worldSession.LoadCompletedCaveRoomPlans(),
                _worldSession.PlannedTunnelCells,
                _worldSession.PlannedVerticalTunnelCells);
        Result<TunnelTopologySynchronizationResult> topology =
            _tunnelTopologySync!.Handle(new SynchronizeTunnelTopologyCommand(
                provenance,
                tick));
        if (topology.IsFailure)
        {
            return Result.Failure(topology.Error!);
        }

        CellId[] completedBuildingCells = LoadBuildings()
            .Where(building => building.Status == BuildingStatus.Completed)
            .SelectMany(building => building.Footprint)
            .Select(cell => new CellId(cell.X, cell.Y, cell.Z))
            .Distinct()
            .OrderBy(cell => cell)
            .ToArray();
        CellId[] revealedCells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.State.IsExplored)
            .Select(cell => cell.Id)
            .OrderBy(cell => cell)
            .ToArray();
        CellId[] reachable = reachableCells
            .Distinct()
            .OrderBy(cell => cell)
            .ToArray();

        TunnelInfrastructureSnapshot snapshot =
            _tunnelInfrastructure!.Get().CaptureSnapshot();
        for (int index = 0; index < snapshot.Segments.Count; index++)
        {
            HorizontalTunnelSegmentSnapshot segment = snapshot.Segments[index];
            Result<TunnelAutomaticSupportSyncResult> support =
                _tunnelSupportSync!.Handle(
                    new SynchronizeTunnelAutomaticSupportCommand(
                        segment.SegmentId,
                        ResolveSupportJobId(segment),
                        completedBuildingCells,
                        revealedCells,
                        reachable,
                        tick));
            if (support.IsFailure)
            {
                return Result.Failure(support.Error!);
            }
        }

        Result<TunnelJunctionTrimPlacementSyncResult> placementOnly =
            _tunnelTrimPlacementSync!.Handle(
                new SynchronizeTunnelJunctionTrimPlacementCommand(tick));
        if (placementOnly.IsFailure)
        {
            return Result.Failure(placementOnly.Error!);
        }

        SynchronizeTunnelAutomaticCandidates(agents);
        PublishTunnelInfrastructureVisuals();
        return Result.Success();
    }

    internal TunnelInfrastructureSnapshot LoadTunnelInfrastructureRuntime()
    {
        EnsureTunnelInfrastructureRuntime();
        return _tunnelInfrastructure!.Get().CaptureSnapshot();
    }

    private void SynchronizeTunnelAutomaticCandidates(
        IReadOnlyList<AgentViewModel> agents)
    {
        if (_candidateProvider == null)
        {
            throw new InvalidOperationException(
                "Dynamic job assignment is not initialized.");
        }

        JobSnapshot[] automaticJobs = _jobRepository.Get().GetAll()
            .Where(job => !job.IsTerminal
                && job.Definition is TunnelAutomaticWorkJobDefinition definition
                && definition.Kind == TunnelAutomaticWorkKind.WoodenSupport)
            .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
            .ToArray();
        for (int index = 0; index < automaticJobs.Length; index++)
        {
            JobSnapshot job = automaticJobs[index];
            TunnelAutomaticWorkJobDefinition definition =
                (TunnelAutomaticWorkJobDefinition)job.Definition;
            _candidateProvider.SetCandidates(
                job.Id,
                CreateDynamicCandidates(agents, definition.TargetCell));
        }
    }

    private bool TryPlanTunnelAutomaticWorkMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement,
        long tick)
    {
        if (job.Definition is not TunnelAutomaticWorkJobDefinition definition)
        {
            return false;
        }

        CellId? destination = ResolveTunnelAutomaticDestination(job, definition);
        if (!destination.HasValue)
        {
            return true;
        }

        CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        PathResult path = new NavigationPathfinder().FindPath(
            navigation,
            new PathRequest(start, destination.Value, navigation.NavigationVersion));
        if (!path.Succeeded || path.Path == null)
        {
            ReleaseTunnelAutomaticAssignment(job, tick);
            return true;
        }

        _routePlans[job.Id] = new TerrainWorkRoutePlan(
            job.Id,
            definition.TargetCell,
            destination,
            path,
            candidateCount: 1);
        movement[agent.Id] = path.Path.Cells.Count > 1
            ? path.Path.Cells[1]
            : destination.Value;
        return true;
    }

    private Result AdvanceTunnelAutomaticWork(
        JobSnapshot job,
        AgentViewModel agent,
        long tick)
    {
        if (job.Definition is not TunnelAutomaticWorkJobDefinition definition)
        {
            return Result.Success();
        }

        CellId? destination = ResolveTunnelAutomaticDestination(job, definition);
        if (!destination.HasValue)
        {
            return Result.Success();
        }

        CellId current = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        if (current != destination.Value)
        {
            return Result.Success();
        }

        if (job.Stage == JobStageKind.Finalize)
        {
            Result completed = _tunnelWorkCompletion!.Handle(
                new CompleteTunnelAutomaticWorkCommand(job.Id, tick));
            if (completed.IsSuccess)
            {
                PublishTunnelInfrastructureVisuals();
            }

            return completed;
        }

        return _advanceHandler.Handle(new AdvanceJobCommand(job.Id, tick));
    }

    private static CellId? ResolveTunnelAutomaticDestination(
        JobSnapshot job,
        TunnelAutomaticWorkJobDefinition definition)
    {
        if (job.Stage == JobStageKind.TravelToTarget
            || job.Stage == JobStageKind.AcquireItem)
        {
            return definition.SourceCell;
        }

        return definition.TargetCell;
    }

    private void ReleaseTunnelAutomaticAssignment(JobSnapshot job, long tick)
    {
        if (!job.AssignedAgentId.HasValue || _releaseAssignment == null)
        {
            return;
        }

        Result released = _releaseAssignment.Handle(
            new ReleaseJobAssignmentCommand(job.Id, tick));
        if (released.IsSuccess)
        {
            _routePlans.Remove(job.Id);
        }
    }

    private EntityId ResolveSupportJobId(HorizontalTunnelSegmentSnapshot segment)
    {
        CellId? target = segment.NextAutomaticSupportTarget?.TargetCell;
        JobSnapshot? existing = _jobRepository.Get().GetAll()
            .FirstOrDefault(job => !job.IsTerminal
                && job.Definition is TunnelAutomaticWorkJobDefinition definition
                && definition.Kind == TunnelAutomaticWorkKind.WoodenSupport
                && definition.SegmentId == segment.SegmentId
                && target.HasValue
                && definition.TargetCell == target.Value);
        return existing?.Id ?? NextTunnelAutomaticJobId();
    }

    private EntityId NextTunnelAutomaticJobId()
    {
        return EntityId.Parse(
            "a" + (_tunnelAutomaticJobSequence++).ToString("x31"));
    }

    private void PublishTunnelInfrastructureVisuals()
    {
        if (_tunnelVisualSink == null || _tunnelInfrastructure == null)
        {
            return;
        }

        _tunnelVisualSink(_tunnelVisualPresenter.Present(
            _tunnelInfrastructure.Get().CaptureSnapshot()));
    }

    private void EnsureTunnelInfrastructureRuntime()
    {
        if (_tunnelInfrastructure != null)
        {
            return;
        }

        _tunnelInfrastructure = new InMemoryTunnelInfrastructureRepository();
        _tunnelTopologySync = new SynchronizeTunnelTopologyHandler(
            _tunnelInfrastructure,
            _inventoryRepository,
            _jobRepository,
            _journal);
        _tunnelSupportSync = new SynchronizeTunnelAutomaticSupportHandler(
            _tunnelInfrastructure,
            _inventoryRepository,
            _jobRepository,
            _journal);
        _tunnelTrimPlacementSync = new SynchronizeTunnelJunctionTrimPlacementHandler(
            _tunnelInfrastructure,
            _inventoryRepository,
            _jobRepository,
            _journal);
        _tunnelWorkCompletion = new CompleteTunnelAutomaticWorkHandler(
            _tunnelInfrastructure,
            _inventoryRepository,
            _jobRepository,
            _journal,
            _skillGrants);
    }
}
}
