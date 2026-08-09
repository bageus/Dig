using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Navigation
{

public sealed class UnsupportedResidentRecoveryPlan
{
    public UnsupportedResidentRecoveryPlan(
        CellId destination,
        NavigationPath path,
        int shaftGapCount)
    {
        if (shaftGapCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shaftGapCount));
        }

        Destination = destination;
        Path = path ?? throw new ArgumentNullException(nameof(path));
        ShaftGapCount = shaftGapCount;
    }

    public CellId Destination { get; }
    public NavigationPath Path { get; }
    public int ShaftGapCount { get; }
}

public sealed class UnsupportedResidentRecoveryPlanner
{
    private readonly NavigationPathfinder _pathfinder;

    public UnsupportedResidentRecoveryPlanner(NavigationPathfinder pathfinder)
    {
        _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
    }

    public UnsupportedResidentRecoveryPlan? Plan(
        CellId start,
        NavigationSnapshot navigation,
        WorldSnapshot world,
        bool requireFloorRecovery = false)
    {
        if (navigation == null)
        {
            throw new ArgumentNullException(nameof(navigation));
        }

        if (world == null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        if (!navigation.IsWalkable(start)
            || !requireFloorRecovery && HasFullSupport(start, cells))
        {
            return null;
        }

        List<UnsupportedResidentRecoveryPlan> reachable =
            new List<UnsupportedResidentRecoveryPlan>();
        foreach (CellId candidate in navigation.Chunks
            .SelectMany(chunk => chunk.WalkableCells)
            .Distinct()
            .Where(cell => HasFullSupport(cell, cells))
            .OrderBy(cell => cell))
        {
            PathResult path = _pathfinder.FindPath(
                navigation,
                new PathRequest(start, candidate, navigation.NavigationVersion));
            if (path.Succeeded)
            {
                reachable.Add(new UnsupportedResidentRecoveryPlan(
                    candidate,
                    path.Path!,
                    CountShaftGaps(navigation, path.Path!)));
            }
        }

        return reachable
            .OrderBy(plan => plan.ShaftGapCount)
            .ThenBy(plan => plan.Path.TotalCost)
            .ThenBy(plan => plan.Destination)
            .FirstOrDefault();
    }

    private static int CountShaftGaps(
        NavigationSnapshot navigation,
        NavigationPath path)
    {
        int count = 0;
        for (int index = 1; index < path.Cells.Count; index++)
        {
            CellId target = path.Cells[index];
            NavigationTransition transition = navigation.GetTransitions(
                    path.Cells[index - 1])
                .Single(value => value.Target == target);
            if (transition.TraversalKind == TunnelTraversalKind.ShaftGapTraverse)
            {
                count++;
            }
        }

        return count;
    }

    public static bool HasFullSupport(
        CellId cell,
        IReadOnlyDictionary<CellId, CellSnapshot> worldCells)
    {
        CellId below = new CellId(cell.X, cell.Y + 1, cell.Z);
        return worldCells.TryGetValue(below, out CellSnapshot support)
            && support.IsSolid
            && support.State.CompletedExcavationQuarters == ExcavationQuarter.None;
    }
}

}
