using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.World;

namespace Dig.Unity
{
internal sealed partial class DigTerrainWorkSession
{
    private bool TryPlanMushroomMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement)
    {
        if (job.Definition is not MushroomChopJobDefinition definition)
        {
            return false;
        }

        CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        PathResult path = new NavigationPathfinder().FindPath(
            navigation,
            new PathRequest(start, definition.WorkPosition, navigation.NavigationVersion));
        if (!path.Succeeded
            || path.Path == null
            || !HasFullStandingSupport(definition.WorkPosition))
        {
            return true;
        }

        _routePlans[job.Id] = new TerrainWorkRoutePlan(
            job.Id,
            definition.TargetCell,
            definition.WorkPosition,
            path,
            candidateCount: 1);
        movement[agent.Id] = path.Path.Cells.Count > 1
            ? path.Path.Cells[1]
            : definition.WorkPosition;
        return true;
    }

    private bool TryResolveMushroomWorkPosition(
        CellId target,
        CellId workerCell,
        out CellId workPosition)
    {
        workPosition = default;
        Result refresh = RefreshNavigation();
        if (refresh.IsFailure)
        {
            return false;
        }

        NavigationMap? map = _navigationRepository.Get(_profile.Id);
        if (map == null)
        {
            return false;
        }

        Result<NavigationSnapshot> snapshotResult = map.GetSnapshot();
        if (snapshotResult.IsFailure)
        {
            return false;
        }

        NavigationSnapshot navigation = snapshotResult.Value;
        CellId[] candidates = GetSameHeightActionCandidates(target);
        PathResult? selectedPath = null;
        CellId selectedCell = default;
        foreach (CellId candidate in candidates
            .Where(navigation.IsWalkable)
            .Where(HasFullStandingSupport)
            .Distinct())
        {
            PathResult path = new NavigationPathfinder().FindPath(
                navigation,
                new PathRequest(workerCell, candidate, navigation.NavigationVersion));
            if (!path.Succeeded
                || path.Path == null)
            {
                continue;
            }

            if (selectedPath == null
                || path.Path.TotalCost < selectedPath.Path!.TotalCost)
            {
                selectedPath = path;
                selectedCell = candidate;
            }
        }

        if (selectedPath == null)
        {
            return false;
        }

        workPosition = selectedCell;
        return true;
    }

    private CellId FindMushroomDemoCell(bool surface, CellId? excluded)
    {
        WorldCellViewModel[] cells = _worldSession.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToArray();
        Dictionary<CellId, WorldCellViewModel> byId = cells.ToDictionary(
            value => new CellId(value.X, value.Y, value.Z));
        HashSet<CellId> occupied = _buildingsRepository == null
            ? new HashSet<CellId>()
            : new HashSet<CellId>(_buildingsRepository.Get().GetOccupiedCells());
        foreach (Dig.Domain.Inventory.ItemStackSnapshot stack
            in _inventoryRepository.Get().CreateSnapshot().Stacks)
        {
            if (stack.Location.Kind == Dig.Domain.Inventory.ItemLocationKind.World
                && stack.Location.HasCell)
            {
                occupied.Add(stack.Location.CellId);
            }
        }

        IEnumerable<CellId> candidates = cells
            .Where(value => !value.IsSolid)
            .Select(value => new CellId(value.X, value.Y, value.Z))
            .Where(value => !excluded.HasValue || value != excluded.Value)
            .Where(value => !occupied.Contains(value))
            .Where(value => byId.TryGetValue(
                new CellId(value.X, value.Y + 1, value.Z),
                out WorldCellViewModel below)
                && below.IsSolid);
        candidates = surface
            ? candidates.Where(value => value.Z == 0)
                .OrderBy(value => value.Y)
                .ThenBy(value => value.X)
            : candidates.Where(value => value.Z > 0)
                .OrderByDescending(value => value.Z)
                .ThenByDescending(value => value.Y)
                .ThenByDescending(value => value.X);
        CellId? selected = candidates.Cast<CellId?>().FirstOrDefault();
        if (!selected.HasValue)
        {
            throw new InvalidOperationException(
                surface
                    ? "The demo has no supported surface cell for a mushroom."
                    : "The demo has no supported lower-cave cell for a mushroom.");
        }

        return selected.Value;
    }


}
}
