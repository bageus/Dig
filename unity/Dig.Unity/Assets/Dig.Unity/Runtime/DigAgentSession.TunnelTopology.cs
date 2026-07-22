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
    private readonly HashSet<CellId> _tunnelDepthExcavations =
        new HashSet<CellId>();
    private readonly TunnelDepthExcavationPolicy _tunnelDepthExcavation =
        new TunnelDepthExcavationPolicy();

    internal IReadOnlyCollection<CellId> TunnelDepthExcavations =>
        _tunnelDepthExcavations.OrderBy(cell => cell).ToArray();

    internal TunnelDepthExcavationPlanResult PlanTunnelDepthExcavation(
        CellId source)
    {
        return _tunnelDepthExcavation.Plan(TunnelVolume, source);
    }

    internal Result CompleteTunnelDepthExcavation(
        CellId target,
        WorldSnapshot world,
        IReadOnlyCollection<CellId> plannedVerticalCells)
    {
        TunnelNavigationVolume previous = TunnelVolume;
        if (!previous.Contains(target) || target.Z <= CellId.MinimumDepth)
        {
            return Result.Failure(new DomainError(
                "unity.depth.target_invalid",
                "The spatial excavation target is outside the deep rock volume."));
        }

        CellId source = new CellId(target.X, target.Y, target.Z - 1);
        if (!previous.IsOpen(source))
        {
            return Result.Failure(new DomainError(
                "unity.depth.source_closed",
                "The spatial excavation work cell is no longer open."));
        }

        CellSnapshot? excavated = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(cell => cell.Id == target)
            .Select(cell => (CellSnapshot?)cell)
            .SingleOrDefault();
        if (!excavated.HasValue || excavated.Value.IsSolid)
        {
            return Result.Failure(new DomainError(
                "unity.depth.target_not_excavated",
                "The authoritative World cell must be excavated before navigation is rebuilt."));
        }

        _tunnelDepthExcavations.Add(target);
        SynchronizeNavigation(world, plannedVerticalCells);
        return Result.Success();
    }

    internal void SynchronizeNavigation(
        WorldSnapshot world,
        IReadOnlyCollection<CellId> plannedVerticalCells)
    {
        RequireTunnelMovement();
        _tunnelVolume = TunnelNavigationVolume.FromWorldSnapshot(
            world,
            plannedVerticalCells,
            _tunnelVolume!.DemoLayout);
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
