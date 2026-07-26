using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class ExcavationAssignmentTests
{
    [Fact]
    public void Cluster_contains_the_complete_connected_xy_zone_without_radius_limit()
    {
        CellId seed = new CellId(2, 2);
        CellId[] designated = Enumerable.Range(0, 12)
            .Select(offset => new CellId(seed.X + offset, seed.Y, seed.Z))
            .Append(new CellId(seed.X, seed.Y + 2, seed.Z))
            .ToArray();

        IReadOnlyList<CellId> selected = new ExcavationClusterPlanner().Select(
            seed,
            designated);

        Assert.Equal(12, selected.Count);
        Assert.Contains(new CellId(seed.X + 11, seed.Y, seed.Z), selected);
        Assert.DoesNotContain(new CellId(seed.X, seed.Y + 2, seed.Z), selected);
    }

    [Fact]
    public void Cluster_links_room_cells_across_z_but_not_unrelated_z_neighbors()
    {
        CellId seed = new CellId(4, 4, 0);
        CellId sameLayer = new CellId(5, 4, 0);
        CellId roomDepth = new CellId(4, 4, 1);
        CellId unrelatedDepth = new CellId(5, 4, 1);
        CellId[] designated = { seed, sameLayer, roomDepth, unrelatedDepth };
        IReadOnlyCollection<CellId>[] roomGroups =
        {
            new[] { seed, roomDepth },
        };

        IReadOnlyList<CellId> selected = new ExcavationClusterPlanner().Select(
            seed,
            designated,
            roomGroups);

        Assert.Contains(seed, selected);
        Assert.Contains(sameLayer, selected);
        Assert.Contains(roomDepth, selected);
        Assert.DoesNotContain(unrelatedDepth, selected);
    }

    [Fact]
    public void Direct_assignment_selects_nearest_reachable_distinct_jobs()
    {
        NavigationSnapshot navigation = CreateOpenNavigation();
        JobSystem jobs = new JobSystem();
        EntityId leftJob = Id("1");
        EntityId rightJob = Id("2");
        AddAvailable(jobs, leftJob, new CellId(2, 1));
        AddAvailable(jobs, rightJob, new CellId(8, 1));
        DirectJobWorker[] workers =
        {
            new DirectJobWorker(Id("a"), new CellId(0, 1)),
            new DirectJobWorker(Id("b"), new CellId(10, 1)),
        };
        DirectJobAssignmentPlanner planner = new DirectJobAssignmentPlanner(
            new TerrainWorkRoutePlanner(new NavigationPathfinder()));

        Result<DirectJobAssignmentPlan> result = planner.Plan(
            workers,
            jobs.GetAll(),
            navigation);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(2, result.Value.Assignments.Count);
        Assert.Equal(leftJob, result.Value.Assignments[0].JobId);
        Assert.Equal(workers[0].AgentId, result.Value.Assignments[0].AgentId);
        Assert.Equal(rightJob, result.Value.Assignments[1].JobId);
        Assert.Equal(workers[1].AgentId, result.Value.Assignments[1].AgentId);
        Assert.Equal(2, result.Value.Assignments.Select(value => value.JobId).Distinct().Count());
    }

    [Fact]
    public void Direct_spatial_assignment_selects_nearest_reachable_work_cells()
    {
        NavigationSnapshot navigation = CreateOpenNavigation();
        JobSystem jobs = new JobSystem();
        EntityId leftJob = Id("6");
        EntityId rightJob = Id("7");
        AddAvailableSpatial(
            jobs,
            leftJob,
            target: new CellId(2, 2),
            work: new CellId(2, 1));
        AddAvailableSpatial(
            jobs,
            rightJob,
            target: new CellId(8, 2),
            work: new CellId(8, 1));
        DirectJobWorker[] workers =
        {
            new DirectJobWorker(Id("f"), new CellId(0, 1)),
            new DirectJobWorker(Id("9"), new CellId(10, 1)),
        };
        DirectSpatialJobAssignmentPlanner planner =
            new DirectSpatialJobAssignmentPlanner(new NavigationPathfinder());

        Result<DirectJobAssignmentPlan> result = planner.Plan(
            workers,
            jobs.GetAll(),
            navigation);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(2, result.Value.Assignments.Count);
        Assert.Equal(rightJob, result.Value.Assignments[0].JobId);
        Assert.Equal(workers[1].AgentId, result.Value.Assignments[0].AgentId);
        Assert.Equal(leftJob, result.Value.Assignments[1].JobId);
        Assert.Equal(workers[0].AgentId, result.Value.Assignments[1].AgentId);
    }

    [Fact]
    public void Spatial_assignment_prefers_closest_target_when_work_cell_is_shared()
    {
        NavigationSnapshot navigation = CreateOpenNavigation();
        JobSystem jobs = new JobSystem();
        EntityId fartherBottomJob = Id("1");
        EntityId nearerTopJob = Id("f");
        CellId sharedWorkCell = new CellId(5, 2, 0);
        AddAvailableSpatial(
            jobs,
            fartherBottomJob,
            target: new CellId(5, 3, 0),
            work: sharedWorkCell);
        AddAvailableSpatial(
            jobs,
            nearerTopJob,
            target: new CellId(5, 1, 0),
            work: sharedWorkCell);
        DirectJobWorker worker = new DirectJobWorker(
            Id("a"),
            new CellId(5, 0, 0));
        DirectSpatialJobAssignmentPlanner planner =
            new DirectSpatialJobAssignmentPlanner(new NavigationPathfinder());

        Result<DirectJobAssignmentPlan> result = planner.Plan(
            new[] { worker },
            jobs.GetAll(),
            navigation);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        DirectJobAssignment assignment = Assert.Single(result.Value.Assignments);
        Assert.Equal(nearerTopJob, assignment.JobId);
        Assert.Equal(new CellId(5, 1, 0), assignment.Target);
        Assert.Equal(1, assignment.TargetDistance);
        Assert.True(assignment.RouteCost >= 0);
    }

    [Fact]
    public void Explicit_assignment_redirects_selected_agent_and_releases_previous_jobs()
    {
        InMemoryJobRepository repository = new InMemoryJobRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        JobSystem jobs = repository.Get();
        EntityId firstJob = Id("3");
        EntityId targetJob = Id("4");
        EntityId selectedAgent = Id("c");
        EntityId automaticAgent = Id("d");
        AddAvailable(jobs, firstJob, new CellId(2, 2));
        AddAvailable(jobs, targetJob, new CellId(3, 2));
        Assert.True(jobs.Claim(firstJob, selectedAgent, tick: 1).IsSuccess);
        Assert.True(jobs.Claim(targetJob, automaticAgent, tick: 1).IsSuccess);
        repository.Save(jobs);

        Result result = new AssignSpecificJobHandler(repository, journal).Handle(
            new AssignSpecificJobCommand(targetJob, selectedAgent, tick: 2));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(JobStatus.Available, repository.Get().Get(firstJob)!.Status);
        Assert.Equal(JobStatus.Claimed, repository.Get().Get(targetJob)!.Status);
        Assert.Equal(selectedAgent, repository.Get().Get(targetJob)!.AssignedAgentId);
        ReservationSnapshot reservation = Assert.Single(
            repository.Get().GetReservations(),
            value => value.Key == ReservationKey.ForAgent(selectedAgent));
        Assert.Equal(targetJob, reservation.JobId);
        Assert.DoesNotContain(
            repository.Get().GetReservations(),
            value => value.Key == ReservationKey.ForAgent(automaticAgent));
    }

    [Fact]
    public void Releasing_in_progress_assignment_returns_job_to_available()
    {
        InMemoryJobRepository repository = new InMemoryJobRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        JobSystem jobs = repository.Get();
        EntityId jobId = Id("5");
        EntityId agentId = Id("e");
        AddAvailable(jobs, jobId, new CellId(4, 2));
        Assert.True(jobs.Claim(jobId, agentId, tick: 1).IsSuccess);
        Assert.True(jobs.Start(jobId, tick: 1).IsSuccess);
        repository.Save(jobs);

        Result result = new ReleaseJobAssignmentHandler(repository, journal).Handle(
            new ReleaseJobAssignmentCommand(jobId, tick: 2));

        Assert.True(result.IsSuccess);
        JobSnapshot job = repository.Get().Get(jobId)!;
        Assert.Equal(JobStatus.Available, job.Status);
        Assert.Equal(JobStageKind.None, job.Stage);
        Assert.Null(job.AssignedAgentId);
        Assert.DoesNotContain(
            repository.Get().GetReservations(),
            value => value.JobId == jobId);
    }

    private static NavigationSnapshot CreateOpenNavigation()
    {
        Result<WorldState> world = WorldState.CreateFilled(
            new WorldSize(12, 4),
            chunkSize: 4,
            NavigationTestFactory.CreateMaterials(),
            NavigationTestFactory.Air,
            explored: true);
        Assert.True(world.IsSuccess);
        NavigationMap map = NavigationTestFactory.BuildMap(
            world.Value,
            TraversalProfile.CreateFreeMover());
        return NavigationTestFactory.GetSnapshot(map);
    }

    private static void AddAvailableSpatial(
        JobSystem jobs,
        EntityId id,
        CellId target,
        CellId work)
    {
        SpatialDigJobDefinition definition = new SpatialDigJobDefinition(
            id,
            new SpatialDigJobTarget(target, work),
            priority: 700,
            createdTick: 0,
            JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(id, tick: 0).IsSuccess);
    }

    private static void AddAvailable(JobSystem jobs, EntityId id, CellId cell)
    {
        DigJobDefinition definition = new DigJobDefinition(
            id,
            new DigJobTarget(cell),
            priority: 700,
            createdTick: 0,
            JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(id, tick: 0).IsSuccess);
    }

    private static EntityId Id(string suffix)
    {
        return EntityId.Parse(suffix.PadLeft(32, '0'));
    }
}

}
