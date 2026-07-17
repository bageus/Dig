using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class ExcavationAssignmentTests
{
    [Fact]
    public void Cluster_contains_only_connected_designations_within_radius_four()
    {
        CellId seed = new CellId(5, 5);
        CellId[] designated =
        {
            seed,
            new CellId(6, 5),
            new CellId(7, 5),
            new CellId(7, 6),
            new CellId(8, 6),
            new CellId(9, 6),
            new CellId(10, 6),
            new CellId(5, 7),
        };

        var selected = new ExcavationClusterPlanner().Select(seed, designated);

        Assert.Equal(
            new[]
            {
                seed,
                new CellId(6, 5),
                new CellId(7, 5),
                new CellId(7, 6),
                new CellId(8, 6),
            },
            selected);
    }

    [Fact]
    public void Explicit_assignment_redirects_selected_agent_and_releases_previous_jobs()
    {
        InMemoryJobRepository repository = new InMemoryJobRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        JobSystem jobs = repository.Get();
        EntityId firstJob = Id("1");
        EntityId targetJob = Id("2");
        EntityId selectedAgent = Id("a");
        EntityId automaticAgent = Id("b");
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
        EntityId jobId = Id("3");
        EntityId agentId = Id("c");
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
        Assert.Empty(repository.Get().GetReservations()
            .Where(value => value.JobId == jobId));
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
