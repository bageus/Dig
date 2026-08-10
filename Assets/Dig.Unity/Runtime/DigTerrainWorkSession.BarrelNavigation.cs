using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.World;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private bool TryPlanBarrelMovement(
        JobSnapshot job,
        AgentViewModel agent,
        NavigationSnapshot navigation,
        IDictionary<string, CellId> movement)
    {
        if (job.Definition is not BarrelAttackJobDefinition definition)
        {
            return false;
        }

        CellId start = new CellId(agent.CellX, agent.CellY, agent.CellZ);
        PathResult path = new NavigationPathfinder().FindPath(
            navigation,
            new PathRequest(start, definition.WorkPosition, navigation.NavigationVersion));
        if (!path.Succeeded
            || path.Path == null
            || !IsSupportedBarrelAttackPath(navigation, path.Path))
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

    private bool TryResolveBarrelWorkPosition(
        CellId target,
        CellId workerCell,
        out CellId workPosition)
    {
        workPosition = default;
        if (RefreshNavigation().IsFailure)
        {
            return false;
        }

        NavigationMap? map = _navigationRepository.Get(_profile.Id);
        if (map == null)
        {
            return false;
        }

        Dig.Domain.Core.Result<NavigationSnapshot> snapshotResult = map.GetSnapshot();
        if (snapshotResult.IsFailure)
        {
            return false;
        }

        NavigationSnapshot navigation = snapshotResult.Value;
        HashSet<CellId> reserved = _jobRepository.Get().GetReservations()
            .Where(value => value.Key.Kind == ReservationKind.Position)
            .Select(value => ParsePositionReservation(value.Key.Value))
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToHashSet();
        CellId[] candidates =
        {
            new CellId(target.X - 1, target.Y, target.Z),
            new CellId(target.X + 1, target.Y, target.Z),
            target.Z > CellId.MinimumDepth
                ? new CellId(target.X, target.Y, target.Z - 1)
                : target,
            target.Z < CellId.MaximumDepth
                ? new CellId(target.X, target.Y, target.Z + 1)
                : target,
        };
        PathResult? selectedPath = null;
        CellId selectedCell = default;
        foreach (CellId candidate in candidates
            .Where(navigation.IsWalkable)
            .Where(HasFullStandingSupport)
            .Where(value => !reserved.Contains(value))
            .Distinct()
            .OrderBy(value => value))
        {
            PathResult path = new NavigationPathfinder().FindPath(
                navigation,
                new PathRequest(workerCell, candidate, navigation.NavigationVersion));
            if (!path.Succeeded
                || path.Path == null
                || !IsSupportedBarrelAttackPath(navigation, path.Path))
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


    private bool IsSupportedBarrelAttackPath(
        NavigationSnapshot navigation,
        NavigationPath path)
    {
        if (path.Cells.Any(cell => !HasFullStandingSupport(cell)))
        {
            return false;
        }

        for (int index = 0; index + 1 < path.Cells.Count; index++)
        {
            CellId from = path.Cells[index];
            CellId to = path.Cells[index + 1];
            bool supportedSurfaceTransition = navigation.GetTransitions(from).Any(
                transition => transition.Target == to
                    && (transition.TraversalKind == TunnelTraversalKind.SupportedWalk
                        || transition.TraversalKind == TunnelTraversalKind.DepthTraverse));
            if (!supportedSurfaceTransition)
            {
                return false;
            }
        }

        return true;
    }

    private CellId[] FindBarrelDemoCells(
        bool surface,
        int count,
        HashSet<CellId>? excluded)
    {
        WorldCellViewModel[] cells = _worldSession.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToArray();
        Dictionary<CellId, WorldCellViewModel> byId = cells.ToDictionary(
            value => new CellId(value.X, value.Y, value.Z));
        HashSet<CellId> occupied = excluded ?? new HashSet<CellId>();
        occupied.UnionWith(MushroomBuildingBlockedCells);
        if (_buildingsRepository != null)
        {
            occupied.UnionWith(_buildingsRepository.Get().GetOccupiedCells());
        }

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
        CellId[] selected = candidates.Take(count).ToArray();
        if (selected.Length != count)
        {
            throw new InvalidOperationException(
                surface
                    ? "The demo has insufficient supported surface cells for barrels."
                    : "The demo has insufficient lower-cave cells for barrels.");
        }

        return selected;
    }

    private bool HasSolidSupport(CellId cell) => HasFullStandingSupport(cell);

    private bool TryResolveBarrelLanding(CellId source, out CellId landing)
    {
        landing = default;
        HashSet<CellId> occupied = LoadBarrels()
            .Where(value => value.Lifecycle
                == Dig.Domain.WorldObjects.BarrelLifecycle.Supported)
            .Select(value => value.Cell)
            .ToHashSet();
        for (int y = source.Y + 1; ; y++)
        {
            CellId candidate = new CellId(source.X, y, source.Z);
            CellId below = new CellId(source.X, y + 1, source.Z);
            if (!TryGetWorldCell(candidate, out bool candidateSolid)
                || !TryGetWorldCell(below, out bool belowSolid))
            {
                return false;
            }

            if (!candidateSolid && belowSolid && !occupied.Contains(candidate))
            {
                landing = candidate;
                return true;
            }
        }
    }

    private bool TryGetWorldCell(CellId cell, out bool isSolid)
    {
        foreach (WorldCellViewModel model in _worldSession.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells))
        {
            if (model.X == cell.X && model.Y == cell.Y && model.Z == cell.Z)
            {
                isSolid = model.IsSolid;
                return true;
            }
        }

        isSolid = false;
        return false;
    }

    private static CellId? ParsePositionReservation(string value)
    {
        string[] parts = value.Split(',');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out int x)
            || !int.TryParse(parts[1], out int y)
            || !int.TryParse(parts[2], out int z))
        {
            return null;
        }

        return new CellId(x, y, z);
    }
}

}
