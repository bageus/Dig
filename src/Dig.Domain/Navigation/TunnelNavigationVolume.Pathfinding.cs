using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{
public sealed partial class TunnelNavigationVolume
{
    public TunnelPathResult FindPath(CellId start, CellId goal)
    {
        if (!Contains(start))
        {
            return TunnelPathResult.Failure(
                TunnelPathFailureReason.InvalidStart,
                "The resident start cell is outside the tunnel volume.");
        }

        if (!Contains(goal))
        {
            return TunnelPathResult.Failure(
                TunnelPathFailureReason.InvalidGoal,
                "The requested destination is outside the tunnel volume.");
        }

        if (!IsOpen(start))
        {
            return TunnelPathResult.Failure(
                TunnelPathFailureReason.BlockedStart,
                "The resident is not standing in an open tunnel cell.");
        }

        if (!IsOpen(goal))
        {
            return TunnelPathResult.Failure(
                TunnelPathFailureReason.BlockedGoal,
                "The requested destination is not an open tunnel cell.");
        }

        if (start == goal)
        {
            return TunnelPathResult.Success(
                new TunnelPath(
                    new[] { start },
                    Array.Empty<TunnelTraversalKind>()));
        }

        List<CellId> frontier = new List<CellId> { start };
        Dictionary<CellId, PathCost> costs = new Dictionary<CellId, PathCost>
        {
            [start] = new PathCost(0, 0, 0),
        };
        Dictionary<CellId, CellId> previous =
            new Dictionary<CellId, CellId>();
        while (frontier.Count > 0)
        {
            int currentIndex = FindLowestCostIndex(frontier, costs);
            CellId current = frontier[currentIndex];
            frontier.RemoveAt(currentIndex);
            if (current == goal)
            {
                return TunnelPathResult.Success(Reconstruct(previous, start, goal));
            }

            foreach (CellId neighbor in GetNeighbors(current))
            {
                TunnelTraversalKind kind = ClassifyTraversal(current, neighbor);
                PathCost candidate = costs[current].Advance(kind);
                if (costs.TryGetValue(neighbor, out PathCost known)
                    && known.CompareTo(candidate) <= 0)
                {
                    continue;
                }

                costs[neighbor] = candidate;
                previous[neighbor] = current;
                if (!frontier.Contains(neighbor))
                {
                    frontier.Add(neighbor);
                }
            }
        }

        return TunnelPathResult.Failure(
            TunnelPathFailureReason.Unreachable,
            "No route connects the resident to the requested tunnel cell.");
    }

    private IEnumerable<CellId> GetNeighbors(CellId cell)
    {
        CellId[] candidates =
        {
            new CellId(cell.X - 1, cell.Y, cell.Z),
            new CellId(cell.X + 1, cell.Y, cell.Z),
            new CellId(cell.X, cell.Y, cell.Z - 1),
            new CellId(cell.X, cell.Y, cell.Z + 1),
            new CellId(cell.X, cell.Y - 1, cell.Z),
            new CellId(cell.X, cell.Y + 1, cell.Z),
        };
        for (int index = 0; index < candidates.Length; index++)
        {
            if (CanTraverseStep(cell, candidates[index]))
            {
                yield return candidates[index];
            }
        }
    }

    private TunnelPath Reconstruct(
        IReadOnlyDictionary<CellId, CellId> previous,
        CellId start,
        CellId goal)
    {
        List<CellId> reverse = new List<CellId> { goal };
        CellId current = goal;
        while (current != start)
        {
            current = previous[current];
            reverse.Add(current);
        }

        reverse.Reverse();
        List<TunnelTraversalKind> traversalKinds =
            new List<TunnelTraversalKind>(reverse.Count - 1);
        for (int index = 0; index < reverse.Count - 1; index++)
        {
            traversalKinds.Add(ClassifyTraversal(reverse[index], reverse[index + 1]));
        }

        return new TunnelPath(reverse, traversalKinds);
    }

    private static int FindLowestCostIndex(
        IReadOnlyList<CellId> frontier,
        IReadOnlyDictionary<CellId, PathCost> costs)
    {
        int bestIndex = 0;
        for (int index = 1; index < frontier.Count; index++)
        {
            int comparison = costs[frontier[index]].CompareTo(costs[frontier[bestIndex]]);
            if (comparison < 0
                || (comparison == 0 && frontier[index].CompareTo(frontier[bestIndex]) < 0))
            {
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    private readonly struct PathCost : IComparable<PathCost>
    {
        internal PathCost(
            int shaftGapCount,
            int verticalClimbCount,
            int stepCount)
        {
            ShaftGapCount = shaftGapCount;
            VerticalClimbCount = verticalClimbCount;
            StepCount = stepCount;
        }

        internal int ShaftGapCount { get; }

        internal int VerticalClimbCount { get; }

        internal int StepCount { get; }

        internal PathCost Advance(TunnelTraversalKind kind)
        {
            return new PathCost(
                checked(ShaftGapCount
                    + (kind == TunnelTraversalKind.ShaftGapTraverse ? 1 : 0)),
                checked(VerticalClimbCount
                    + (kind == TunnelTraversalKind.VerticalClimb ? 1 : 0)),
                checked(StepCount + 1));
        }

        public int CompareTo(PathCost other)
        {
            int gap = ShaftGapCount.CompareTo(other.ShaftGapCount);
            if (gap != 0)
            {
                return gap;
            }

            int climb = VerticalClimbCount.CompareTo(other.VerticalClimbCount);
            return climb != 0 ? climb : StepCount.CompareTo(other.StepCount);
        }
    }
}
}
