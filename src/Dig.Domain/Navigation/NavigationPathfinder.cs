using System.Collections.ObjectModel;
using Dig.Domain.World;

namespace Dig.Domain.Navigation;

public sealed class NavigationPathfinder
{
    public PathResult FindPath(
        NavigationSnapshot snapshot,
        PathRequest request)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.ExpectedNavigationVersion != snapshot.NavigationVersion)
        {
            return Failure(
                snapshot,
                PathFailureReason.StaleSnapshot,
                expandedNodes: 0,
                startRegion: null,
                goalRegion: null,
                "The request targets an outdated navigation snapshot.");
        }

        if (!snapshot.WorldSize.Contains(request.Start))
        {
            return Failure(
                snapshot,
                PathFailureReason.InvalidStart,
                expandedNodes: 0,
                startRegion: null,
                goalRegion: null,
                "The start cell is outside the world bounds.");
        }

        if (!snapshot.WorldSize.Contains(request.Goal))
        {
            return Failure(
                snapshot,
                PathFailureReason.InvalidGoal,
                expandedNodes: 0,
                startRegion: null,
                goalRegion: null,
                "The goal cell is outside the world bounds.");
        }

        if (!snapshot.IsWalkable(request.Start))
        {
            return Failure(
                snapshot,
                PathFailureReason.BlockedStart,
                expandedNodes: 0,
                startRegion: null,
                goalRegion: null,
                "The start cell is not traversable for this profile.");
        }

        if (!snapshot.IsWalkable(request.Goal))
        {
            return Failure(
                snapshot,
                PathFailureReason.BlockedGoal,
                expandedNodes: 0,
                startRegion: null,
                goalRegion: null,
                "The goal cell is not traversable for this profile.");
        }

        snapshot.TryGetRegion(request.Start, out int startRegion);
        snapshot.TryGetRegion(request.Goal, out int goalRegion);
        if (startRegion != goalRegion)
        {
            return Failure(
                snapshot,
                PathFailureReason.Unreachable,
                expandedNodes: 0,
                startRegion,
                goalRegion,
                "The cells belong to disconnected navigation regions.");
        }

        if (request.Start == request.Goal)
        {
            NavigationPath sameCellPath = CreatePath(
                snapshot,
                new[] { request.Start },
                totalCost: 0);
            return PathResult.Success(
                sameCellPath,
                new PathSearchDiagnostics(
                    PathFailureReason.None,
                    expandedNodes: 0,
                    startRegion,
                    goalRegion,
                    snapshot.NavigationVersion,
                    "Start and goal are the same cell."));
        }

        NavigationOpenSet open = new NavigationOpenSet();
        Dictionary<CellId, int> costs = new Dictionary<CellId, int>();
        Dictionary<CellId, CellId> previous = new Dictionary<CellId, CellId>();
        costs.Add(request.Start, 0);
        open.Push(new NavigationOpenNode(request.Start, cost: 0, heuristic: 0));
        int expandedNodes = 0;

        while (open.Count > 0)
        {
            NavigationOpenNode current = open.Pop();
            if (!costs.TryGetValue(current.Cell, out int currentCost)
                || current.Cost != currentCost)
            {
                continue;
            }

            if (current.Cell == request.Goal)
            {
                IReadOnlyList<CellId> cells = Reconstruct(
                    previous,
                    request.Start,
                    request.Goal);
                NavigationPath path = CreatePath(snapshot, cells, currentCost);
                return PathResult.Success(
                    path,
                    new PathSearchDiagnostics(
                        PathFailureReason.None,
                        expandedNodes,
                        startRegion,
                        goalRegion,
                        snapshot.NavigationVersion,
                        "A deterministic route was found."));
            }

            expandedNodes++;
            if (expandedNodes > request.MaxExpandedNodes)
            {
                return Failure(
                    snapshot,
                    PathFailureReason.SearchLimitExceeded,
                    expandedNodes,
                    startRegion,
                    goalRegion,
                    "The path search exceeded its expansion budget.");
            }

            foreach (NavigationTransition transition in snapshot.GetTransitions(current.Cell))
            {
                int candidateCost = checked(currentCost + transition.Cost);
                if (costs.TryGetValue(transition.Target, out int knownCost)
                    && knownCost <= candidateCost)
                {
                    continue;
                }

                costs[transition.Target] = candidateCost;
                previous[transition.Target] = current.Cell;
                open.Push(new NavigationOpenNode(
                    transition.Target,
                    candidateCost,
                    heuristic: 0));
            }
        }

        return Failure(
            snapshot,
            PathFailureReason.Unreachable,
            expandedNodes,
            startRegion,
            goalRegion,
            "The search exhausted the reachable cells.");
    }

    private static NavigationPath CreatePath(
        NavigationSnapshot snapshot,
        IReadOnlyCollection<CellId> cells,
        int totalCost)
    {
        NavigationChunkStamp[] stamps = cells
            .Select(snapshot.GetChunkStamp)
            .Distinct()
            .OrderBy(stamp => stamp.ChunkId)
            .ToArray();
        return new NavigationPath(
            snapshot.Profile.Id,
            snapshot.WorldVersion,
            snapshot.NavigationVersion,
            snapshot.LinkVersion,
            totalCost,
            cells,
            stamps);
    }

    private static IReadOnlyList<CellId> Reconstruct(
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
        return new ReadOnlyCollection<CellId>(reverse);
    }

    private static PathResult Failure(
        NavigationSnapshot snapshot,
        PathFailureReason reason,
        int expandedNodes,
        int? startRegion,
        int? goalRegion,
        string detail)
    {
        return PathResult.Failure(
            new PathSearchDiagnostics(
                reason,
                expandedNodes,
                startRegion,
                goalRegion,
                snapshot.NavigationVersion,
                detail));
    }
}
