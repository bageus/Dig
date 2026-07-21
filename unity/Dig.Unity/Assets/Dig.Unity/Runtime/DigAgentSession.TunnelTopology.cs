using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private readonly HashSet<SpatialCellId> _tunnelDepthExcavations =
        new HashSet<SpatialCellId>();
    private readonly TunnelDepthExcavationPolicy _tunnelDepthExcavation =
        new TunnelDepthExcavationPolicy();

    internal IReadOnlyCollection<SpatialCellId> TunnelDepthExcavations =>
        _tunnelDepthExcavations.OrderBy(cell => cell).ToArray();

    internal TunnelDepthExcavationPlanResult PlanTunnelDepthExcavation(
        SpatialCellId source)
    {
        return _tunnelDepthExcavation.Plan(TunnelVolume, source);
    }

    internal Result CompleteTunnelDepthExcavation(SpatialCellId target)
    {
        TunnelNavigationVolume volume = TunnelVolume;
        if (!volume.Contains(target) || target.Z <= 0)
        {
            return Result.Failure(new DomainError(
                "unity.depth.target_invalid",
                "The spatial excavation target is outside the deep rock volume."));
        }

        if (volume.IsOpen(target))
        {
            return Result.Failure(new DomainError(
                "unity.depth.target_open",
                "The spatial excavation target is already open."));
        }

        SpatialCellId source = new SpatialCellId(target.X, target.Y, target.Z - 1);
        if (!volume.IsOpen(source))
        {
            return Result.Failure(new DomainError(
                "unity.depth.source_closed",
                "The spatial excavation work cell is no longer open."));
        }

        _tunnelDepthExcavations.Add(target);
        ExpandTunnelVolume(new[] { target });
        return Result.Success();
    }

    internal void ExpandTunnelVolume(
        IReadOnlyCollection<SpatialCellId> additionalOpenCells)
    {
        RequireTunnelMovement();
        _tunnelVolume = _tunnelVolume!.WithAdditionalOpenCells(additionalOpenCells);
        EnsureOccupiedCellsOpen(_repository.GetAll()
            .Where(agent => agent.IsAlive)
            .Select(agent => agent.SpatialPosition)
            .ToArray());
        CreateTunnelRoutePlanners();
    }

    internal void SynchronizeFrontNavigation(
        WorldSnapshot world,
        IReadOnlyCollection<CellId> plannedVerticalCells)
    {
        RequireTunnelMovement();
        TunnelNavigationVolume next = _tunnelVolume!.WithSynchronizedFrontLayer(
            world,
            plannedVerticalCells);
        SpatialCellId[] occupied = _repository.GetAll()
            .Where(agent => agent.IsAlive && next.Contains(agent.SpatialPosition))
            .Select(agent => agent.SpatialPosition)
            .Distinct()
            .ToArray();
        if (occupied.Length > 0)
        {
            next = next.WithAdditionalOpenCells(occupied);
        }

        if (ReferenceEquals(next, _tunnelVolume))
        {
            return;
        }

        _tunnelVolume = next;
        CreateTunnelRoutePlanners();
    }

    private void InitializeTunnelMovement(
        TunnelNavigationVolume volume,
        InMemoryExecutionJournal journal)
    {
        _tunnelVolume = volume ?? throw new ArgumentNullException(nameof(volume));
        _tunnelJournal = journal ?? throw new ArgumentNullException(nameof(journal));
        CreateTunnelRoutePlanners();
    }

    private void CreateTunnelRoutePlanners()
    {
        _tunnelRoutePlanner = new PlanAgentTunnelRouteCommandHandler(
            _repository,
            _tunnelVolume!);
        _groupTunnelRoutePlanner = new PlanAgentsTunnelRoutesCommandHandler(
            _repository,
            _tunnelVolume!);
    }

    private void RequireTunnelMovement()
    {
        if (_tunnelVolume == null || _tunnelJournal == null)
        {
            throw new InvalidOperationException("Tunnel movement is not initialized.");
        }
    }
}

}
