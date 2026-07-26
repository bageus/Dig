using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Jobs
{

public readonly struct DirectJobWorker
{
    public DirectJobWorker(EntityId agentId, CellId start)
    {
        if (agentId.IsEmpty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        AgentId = agentId;
        Start = start;
    }

    public EntityId AgentId { get; }
    public CellId Start { get; }
}

public sealed class DirectJobAssignment
{
    public DirectJobAssignment(
        EntityId jobId,
        EntityId agentId,
        CellId target,
        int routeCost)
    {
        JobId = jobId;
        AgentId = agentId;
        Target = target;
        RouteCost = routeCost;
    }

    public EntityId JobId { get; }
    public EntityId AgentId { get; }
    public CellId Target { get; }
    public int RouteCost { get; }
}

public sealed class DirectJobAssignmentPlan
{
    public DirectJobAssignmentPlan(IEnumerable<DirectJobAssignment> assignments)
    {
        if (assignments is null)
        {
            throw new ArgumentNullException(nameof(assignments));
        }

        Assignments = new ReadOnlyCollection<DirectJobAssignment>(
            assignments.ToArray());
    }

    public IReadOnlyList<DirectJobAssignment> Assignments { get; }
}

public sealed class DirectJobAssignmentPlanner
{
    private readonly TerrainWorkRoutePlanner _routePlanner;

    public DirectJobAssignmentPlanner(TerrainWorkRoutePlanner routePlanner)
    {
        _routePlanner = routePlanner
            ?? throw new ArgumentNullException(nameof(routePlanner));
    }

    public Result<DirectJobAssignmentPlan> Plan(
        IReadOnlyCollection<DirectJobWorker> workers,
        IReadOnlyCollection<JobSnapshot> jobs,
        NavigationSnapshot navigation)
    {
        if (workers is null)
        {
            throw new ArgumentNullException(nameof(workers));
        }

        if (jobs is null)
        {
            throw new ArgumentNullException(nameof(jobs));
        }

        if (navigation is null)
        {
            throw new ArgumentNullException(nameof(navigation));
        }

        List<JobSnapshot> remaining = jobs
            .Where(job => job != null
                && !job.IsTerminal
                && job.Definition is DigJobDefinition)
            .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
            .ToList();
        List<DirectJobAssignment> assignments =
            new List<DirectJobAssignment>();
        foreach (DirectJobWorker worker in workers
            .OrderBy(value => value.AgentId.ToString(), StringComparer.Ordinal))
        {
            RouteCandidate? selected = SelectNearest(worker, remaining, navigation);
            if (selected == null)
            {
                continue;
            }

            assignments.Add(new DirectJobAssignment(
                selected.Job.Id,
                worker.AgentId,
                selected.Target,
                selected.RouteCost));
            remaining.Remove(selected.Job);
        }

        return Result<DirectJobAssignmentPlan>.Success(
            new DirectJobAssignmentPlan(assignments));
    }

    private RouteCandidate? SelectNearest(
        DirectJobWorker worker,
        IReadOnlyCollection<JobSnapshot> jobs,
        NavigationSnapshot navigation)
    {
        List<RouteCandidate> reachable = new List<RouteCandidate>();
        foreach (JobSnapshot job in jobs)
        {
            Result<TerrainWorkRoutePlan> planned = _routePlanner.Plan(
                job,
                worker.Start,
                navigation);
            if (planned.IsFailure || !planned.Value.Succeeded)
            {
                continue;
            }

            DigJobDefinition definition = (DigJobDefinition)job.Definition;
            reachable.Add(new RouteCandidate(
                job,
                definition.Target.CellId,
                planned.Value.PathResult.Path!.TotalCost));
        }

        return reachable
            .OrderBy(candidate => candidate.RouteCost)
            .ThenBy(candidate => candidate.Target)
            .ThenBy(candidate => candidate.Job.Id.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private sealed class RouteCandidate
    {
        public RouteCandidate(JobSnapshot job, CellId target, int routeCost)
        {
            Job = job;
            Target = target;
            RouteCost = routeCost;
        }

        public JobSnapshot Job { get; }
        public CellId Target { get; }
        public int RouteCost { get; }
    }
}


public sealed class DirectSpatialJobAssignmentPlanner
{
    private readonly NavigationPathfinder _pathfinder;

    public DirectSpatialJobAssignmentPlanner(NavigationPathfinder pathfinder)
    {
        _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
    }

    public Result<DirectJobAssignmentPlan> Plan(
        IReadOnlyCollection<DirectJobWorker> workers,
        IReadOnlyCollection<JobSnapshot> jobs,
        NavigationSnapshot navigation)
    {
        if (workers is null)
        {
            throw new ArgumentNullException(nameof(workers));
        }

        if (jobs is null)
        {
            throw new ArgumentNullException(nameof(jobs));
        }

        if (navigation is null)
        {
            throw new ArgumentNullException(nameof(navigation));
        }

        List<JobSnapshot> remaining = jobs
            .Where(job => job != null
                && !job.IsTerminal
                && job.Definition is SpatialDigJobDefinition)
            .OrderBy(job => job.Id.ToString(), StringComparer.Ordinal)
            .ToList();
        List<DirectJobAssignment> assignments =
            new List<DirectJobAssignment>();
        foreach (DirectJobWorker worker in workers
            .OrderBy(value => value.AgentId.ToString(), StringComparer.Ordinal))
        {
            SpatialRouteCandidate? selected = SelectNearest(
                worker,
                remaining,
                navigation);
            if (selected == null)
            {
                continue;
            }

            assignments.Add(new DirectJobAssignment(
                selected.Job.Id,
                worker.AgentId,
                selected.Target,
                selected.RouteCost));
            remaining.Remove(selected.Job);
        }

        return Result<DirectJobAssignmentPlan>.Success(
            new DirectJobAssignmentPlan(assignments));
    }

    private SpatialRouteCandidate? SelectNearest(
        DirectJobWorker worker,
        IReadOnlyCollection<JobSnapshot> jobs,
        NavigationSnapshot navigation)
    {
        List<SpatialRouteCandidate> reachable = new List<SpatialRouteCandidate>();
        foreach (JobSnapshot job in jobs)
        {
            SpatialDigJobDefinition definition =
                (SpatialDigJobDefinition)job.Definition;
            PathResult path = _pathfinder.FindPath(
                navigation,
                new PathRequest(
                    worker.Start,
                    definition.Target.WorkCell,
                    navigation.NavigationVersion));
            if (!path.Succeeded)
            {
                continue;
            }

            reachable.Add(new SpatialRouteCandidate(
                job,
                definition.Target.TargetCell,
                path.Path!.TotalCost));
        }

        return reachable
            .OrderBy(candidate => candidate.RouteCost)
            .ThenBy(candidate => candidate.Target)
            .ThenBy(candidate => candidate.Job.Id.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private sealed class SpatialRouteCandidate
    {
        public SpatialRouteCandidate(JobSnapshot job, CellId target, int routeCost)
        {
            Job = job;
            Target = target;
            RouteCost = routeCost;
        }

        public JobSnapshot Job { get; }
        public CellId Target { get; }
        public int RouteCost { get; }
    }
}

}
