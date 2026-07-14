using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests;

public sealed class JobSystemTests
{
    private static readonly EntityId FirstJobId =
        EntityId.Parse("10000000000000000000000000000001");
    private static readonly EntityId SecondJobId =
        EntityId.Parse("10000000000000000000000000000002");
    private static readonly EntityId ThirdJobId =
        EntityId.Parse("10000000000000000000000000000003");
    private static readonly EntityId FirstAgentId =
        EntityId.Parse("20000000000000000000000000000001");
    private static readonly EntityId SecondAgentId =
        EntityId.Parse("20000000000000000000000000000002");

    [Fact]
    public void Target_and_agent_cannot_be_reserved_by_two_jobs()
    {
        JobSystem jobs = new JobSystem();
        AddAvailable(jobs, CreateJob(FirstJobId, new CellId(4, 5)), tick: 0);
        AddAvailable(jobs, CreateJob(SecondJobId, new CellId(4, 5)), tick: 0);
        AddAvailable(jobs, CreateJob(ThirdJobId, new CellId(8, 5)), tick: 0);

        Assert.True(jobs.Claim(FirstJobId, FirstAgentId, tick: 1).IsSuccess);

        Result sameTarget = jobs.Claim(SecondJobId, SecondAgentId, tick: 1);
        Result sameAgent = jobs.Claim(ThirdJobId, FirstAgentId, tick: 1);

        Assert.Equal(JobErrors.ReservationConflict, sameTarget.Error);
        Assert.Equal(JobErrors.AgentUnavailable, sameAgent.Error);
        Assert.Equal(4, jobs.GetReservations().Count);
    }

    [Fact]
    public void Completion_cancel_and_failure_release_all_reservations()
    {
        JobSystem jobs = new JobSystem();
        AddAvailable(jobs, CreateJob(FirstJobId, new CellId(1, 1)), tick: 0);
        Assert.True(jobs.Claim(FirstJobId, FirstAgentId, tick: 1).IsSuccess);
        Assert.True(jobs.Complete(FirstJobId, tick: 2).IsSuccess);
        Assert.Empty(jobs.GetReservations());

        AddAvailable(jobs, CreateJob(SecondJobId, new CellId(2, 1)), tick: 3);
        Assert.True(jobs.Claim(SecondJobId, FirstAgentId, tick: 4).IsSuccess);
        Assert.True(jobs.Cancel(
            SecondJobId,
            new JobBlockReason("player_cancelled", "Player cancelled the job."),
            tick: 5).IsSuccess);
        Assert.Empty(jobs.GetReservations());

        AddAvailable(jobs, CreateJob(ThirdJobId, new CellId(3, 1)), tick: 6);
        Assert.True(jobs.Claim(ThirdJobId, FirstAgentId, tick: 7).IsSuccess);
        Assert.True(jobs.Fail(
            ThirdJobId,
            new JobBlockReason("target_destroyed", "Target no longer exists."),
            tick: 8).IsSuccess);
        Assert.Empty(jobs.GetReservations());
    }

    [Fact]
    public void Typed_dig_stages_complete_in_order()
    {
        CellId target = new CellId(6, 7);
        JobSystem jobs = new JobSystem();
        AddAvailable(jobs, CreateJob(FirstJobId, target), tick: 0);
        Assert.True(jobs.Claim(FirstJobId, FirstAgentId, tick: 1).IsSuccess);
        Assert.True(jobs.Start(FirstJobId, tick: 2).IsSuccess);

        Assert.Equal(JobStageKind.TravelToTarget, jobs.Get(FirstJobId)!.Stage);
        Assert.True(jobs.AdvanceStage(FirstJobId, tick: 3).IsSuccess);
        Assert.Equal(JobStageKind.PerformWork, jobs.Get(FirstJobId)!.Stage);
        Assert.True(jobs.AdvanceStage(FirstJobId, tick: 4).IsSuccess);
        Assert.Equal(JobStageKind.Finalize, jobs.Get(FirstJobId)!.Stage);
        Assert.True(jobs.AdvanceStage(FirstJobId, tick: 5).IsSuccess);

        JobSnapshot completed = jobs.Get(FirstJobId)!;
        DigJobDefinition definition = Assert.IsType<DigJobDefinition>(completed.Definition);
        Assert.Equal(JobStatus.Completed, completed.Status);
        Assert.Equal(target, definition.Target.CellId);
        Assert.Empty(jobs.GetReservations());
    }

    [Fact]
    public void Retry_policy_is_bounded_and_diagnosable()
    {
        JobSystem jobs = new JobSystem();
        DigJobDefinition job = CreateJob(
            FirstJobId,
            new CellId(9, 9),
            retryPolicy: new JobRetryPolicy(maximumRetries: 1, retryDelayTicks: 2));
        AddAvailable(jobs, job, tick: 0);
        JobBlockReason blocked = new JobBlockReason("path_missing", "No path to work position.");

        Assert.True(jobs.Claim(FirstJobId, FirstAgentId, tick: 1).IsSuccess);
        Assert.True(jobs.Block(FirstJobId, blocked, tick: 2).IsSuccess);
        Assert.Equal(JobErrors.RetryNotReady, jobs.Retry(FirstJobId, tick: 3).Error);
        Assert.True(jobs.Retry(FirstJobId, tick: 4).IsSuccess);
        Assert.True(jobs.Claim(FirstJobId, FirstAgentId, tick: 5).IsSuccess);

        Result exhausted = jobs.Block(FirstJobId, blocked, tick: 6);

        Assert.Equal(JobErrors.RetryLimitReached, exhausted.Error);
        JobSnapshot failed = jobs.Get(FirstJobId)!;
        Assert.Equal(JobStatus.Failed, failed.Status);
        Assert.Equal("retry_exhausted", failed.Reason!.Code);
        Assert.Empty(jobs.GetReservations());
    }

    [Fact]
    public void Dependencies_must_complete_before_job_becomes_available()
    {
        JobSystem jobs = new JobSystem();
        Assert.True(jobs.Add(CreateJob(FirstJobId, new CellId(1, 2))).IsSuccess);
        Assert.True(jobs.Add(CreateJob(
            SecondJobId,
            new CellId(2, 2),
            dependencies: new[] { FirstJobId })).IsSuccess);

        Assert.Equal(
            JobErrors.DependenciesIncomplete,
            jobs.MakeAvailable(SecondJobId, tick: 0).Error);
        Assert.True(jobs.MakeAvailable(FirstJobId, tick: 0).IsSuccess);
        Assert.True(jobs.Claim(FirstJobId, FirstAgentId, tick: 1).IsSuccess);
        Assert.True(jobs.Complete(FirstJobId, tick: 2).IsSuccess);
        Assert.True(jobs.MakeAvailable(SecondJobId, tick: 3).IsSuccess);
    }

    [Fact]
    public void Assignment_selects_best_workers_without_double_claiming()
    {
        InMemoryJobRepository repository = new InMemoryJobRepository();
        InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        CreateDigJobHandler create = new CreateDigJobHandler(repository, journal);
        AssignAvailableJobsHandler assign = new AssignAvailableJobsHandler(
            repository,
            candidates,
            journal);
        DigJobDefinition highPriority = CreateJob(
            FirstJobId,
            new CellId(3, 3),
            priority: 900);
        DigJobDefinition lowPriority = CreateJob(
            SecondJobId,
            new CellId(4, 3),
            priority: 100);
        Assert.True(create.Handle(new CreateDigJobCommand(highPriority, true)).IsSuccess);
        Assert.True(create.Handle(new CreateDigJobCommand(lowPriority, true)).IsSuccess);
        JobCandidate skilled = new JobCandidate(
            FirstAgentId,
            skillLevel: 9000,
            distanceCost: 30,
            isAvailable: true);
        JobCandidate nearby = new JobCandidate(
            SecondAgentId,
            skillLevel: 4000,
            distanceCost: 1,
            isAvailable: true);
        candidates.SetCandidates(FirstJobId, new[] { nearby, skilled });
        candidates.SetCandidates(SecondJobId, new[] { nearby, skilled });

        JobAssignmentReport report = assign.Handle(new AssignAvailableJobsCommand(tick: 1));

        Assert.Equal(2, report.Assignments.Count);
        Assert.Equal(FirstAgentId, repository.Get().Get(FirstJobId)!.AssignedAgentId);
        Assert.Equal(SecondAgentId, repository.Get().Get(SecondJobId)!.AssignedAgentId);
        Assert.Empty(report.Failures);
        Assert.Contains(journal.Events, item => item is JobStatusChanged changed
            && changed.CurrentStatus == JobStatus.Claimed);
    }

    [Fact]
    public void Assignment_reports_competition_when_only_one_worker_exists()
    {
        InMemoryJobRepository repository = new InMemoryJobRepository();
        InMemoryJobCandidateProvider candidates = new InMemoryJobCandidateProvider();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        CreateDigJobHandler create = new CreateDigJobHandler(repository, journal);
        AssignAvailableJobsHandler assign = new AssignAvailableJobsHandler(
            repository,
            candidates,
            journal);
        Assert.True(create.Handle(new CreateDigJobCommand(
            CreateJob(FirstJobId, new CellId(1, 4), priority: 900),
            true)).IsSuccess);
        Assert.True(create.Handle(new CreateDigJobCommand(
            CreateJob(SecondJobId, new CellId(2, 4), priority: 100),
            true)).IsSuccess);
        JobCandidate onlyWorker = new JobCandidate(
            FirstAgentId,
            skillLevel: 5000,
            distanceCost: 1,
            isAvailable: true);
        candidates.SetCandidates(FirstJobId, new[] { onlyWorker });
        candidates.SetCandidates(SecondJobId, new[] { onlyWorker });

        JobAssignmentReport report = assign.Handle(new AssignAvailableJobsCommand(tick: 1));

        Assert.Single(report.Assignments);
        JobAssignmentFailure failure = Assert.Single(report.Failures);
        Assert.Equal(SecondJobId, failure.JobId);
        Assert.Equal(JobErrors.AgentUnavailable, failure.Error);
    }

    private static DigJobDefinition CreateJob(
        EntityId id,
        CellId target,
        int priority = 500,
        JobRetryPolicy? retryPolicy = null,
        IEnumerable<EntityId>? dependencies = null)
    {
        return new DigJobDefinition(
            id,
            new DigJobTarget(target),
            priority,
            createdTick: 0,
            retryPolicy ?? JobRetryPolicy.Default,
            dependencies);
    }

    private static void AddAvailable(JobSystem jobs, DigJobDefinition job, long tick)
    {
        Assert.True(jobs.Add(job).IsSuccess);
        Assert.True(jobs.MakeAvailable(job.Id, tick).IsSuccess);
    }
}
