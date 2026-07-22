using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

public sealed class TerrainWorkRoutePlan
{
    public TerrainWorkRoutePlan(
        EntityId jobId,
        CellId targetCell,
        CellId? workCell,
        PathResult pathResult,
        int candidateCount)
    {
        if (candidateCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(candidateCount));
        }

        JobId = jobId;
        TargetCell = targetCell;
        WorkCell = workCell;
        PathResult = pathResult ?? throw new ArgumentNullException(nameof(pathResult));
        CandidateCount = candidateCount;
    }

    public EntityId JobId { get; }
    public CellId TargetCell { get; }
    public CellId? WorkCell { get; }
    public PathResult PathResult { get; }
    public int CandidateCount { get; }
    public bool Succeeded => WorkCell.HasValue && PathResult.Succeeded;
}

public sealed class TerrainWorkRoutePlanner
{
    private readonly NavigationPathfinder _pathfinder;

    public TerrainWorkRoutePlanner(NavigationPathfinder pathfinder)
    {
        _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
    }

    public Result<TerrainWorkRoutePlan> Plan(
        JobSnapshot job,
        CellId start,
        NavigationSnapshot navigation)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (navigation is null)
        {
            throw new ArgumentNullException(nameof(navigation));
        }

        if (job.Definition is not DigJobDefinition terrainJob)
        {
            return Result<TerrainWorkRoutePlan>.Failure(
                TerrainWorkCompletionErrors.JobTypeUnsupported);
        }

        CellId[] candidates = GetCandidateWorkCells(
            terrainJob.Target.CellId,
            navigation);
        if (candidates.Length == 0)
        {
            return Result<TerrainWorkRoutePlan>.Success(
                new TerrainWorkRoutePlan(
                    job.Id,
                    terrainJob.Target.CellId,
                    workCell: null,
                    CreateNoWorkCellResult(start, navigation),
                    candidateCount: 0));
        }

        List<RouteCandidate> evaluated = new List<RouteCandidate>(candidates.Length);
        foreach (CellId candidate in candidates)
        {
            PathResult path = _pathfinder.FindPath(
                navigation,
                new PathRequest(
                    start,
                    candidate,
                    navigation.NavigationVersion));
            evaluated.Add(new RouteCandidate(candidate, path));
        }

        RouteCandidate selected = evaluated
            .Where(item => item.Path.Succeeded)
            .OrderBy(item => item.Path.Path!.TotalCost)
            .ThenBy(item => item.WorkCell)
            .FirstOrDefault()
            ?? evaluated.OrderBy(item => item.WorkCell).First();
        return Result<TerrainWorkRoutePlan>.Success(
            new TerrainWorkRoutePlan(
                job.Id,
                terrainJob.Target.CellId,
                selected.WorkCell,
                selected.Path,
                candidates.Length));
    }

    private static CellId[] GetCandidateWorkCells(
        CellId target,
        NavigationSnapshot navigation)
    {
        List<CellId> adjacent = new List<CellId>
        {
            new CellId(target.X - 1, target.Y, target.Z),
            new CellId(target.X + 1, target.Y, target.Z),
            new CellId(target.X, target.Y - 1, target.Z),
            new CellId(target.X, target.Y + 1, target.Z),
        };
        if (target.Z > CellId.MinimumDepth)
        {
            adjacent.Add(new CellId(target.X, target.Y, target.Z - 1));
        }

        if (target.Z < CellId.MaximumDepth)
        {
            adjacent.Add(new CellId(target.X, target.Y, target.Z + 1));
        }

        return adjacent
            .Where(navigation.IsWalkable)
            .Distinct()
            .OrderBy(cell => cell)
            .ToArray();
    }

    private static PathResult CreateNoWorkCellResult(
        CellId start,
        NavigationSnapshot navigation)
    {
        int? startRegion = navigation.TryGetRegion(start, out int region)
            ? region
            : null;
        return PathResult.Failure(
            new PathSearchDiagnostics(
                PathFailureReason.InvalidGoal,
                expandedNodes: 0,
                startRegion,
                goalRegion: null,
                navigation.NavigationVersion,
                "No adjacent walkable work cell exists."));
    }

    private sealed class RouteCandidate
    {
        public RouteCandidate(CellId workCell, PathResult path)
        {
            WorkCell = workCell;
            Path = path;
        }

        public CellId WorkCell { get; }
        public PathResult Path { get; }
    }
}

}
