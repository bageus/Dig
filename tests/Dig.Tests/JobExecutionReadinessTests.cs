using System;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class JobExecutionReadinessTests
{
    private static readonly EntityId JobId = Id(1);
    private static readonly EntityId AgentId = Id(2);
    private static readonly EntityId OtherAgentId = Id(3);
    private static readonly EntityId ToolStackId = Id(4);

    [Fact]
    public void Pending_suggestion_keeps_claimed_job_and_reservations_without_error()
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryJobRepository repository = CreateClaimedJob(reserveTool: true);
        RecordSuggestion(journal, AgentId, tick: 2);
        AdvanceJobHandler advance = new AdvanceJobHandler(
            repository,
            journal,
            new SuggestedToolJobExecutionReadinessPolicy(journal));

        Result result = advance.Handle(new AdvanceJobCommand(JobId, tick: 3));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(JobStatus.Claimed, repository.Get().Get(JobId)!.Status);
        Assert.Contains(
            repository.Get().GetReservations(),
            value => value.Key == ReservationKey.ForTool(ToolStackId));
    }

    [Fact]
    public void Switched_diagnostic_allows_claimed_job_to_start()
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryJobRepository repository = CreateClaimedJob(reserveTool: true);
        RecordAssignment(
            journal,
            AgentId,
            JobToolPreparationOutcome.Switched,
            tick: 3);
        AdvanceJobHandler advance = new AdvanceJobHandler(
            repository,
            journal,
            new SuggestedToolJobExecutionReadinessPolicy(journal));

        Result result = advance.Handle(new AdvanceJobCommand(JobId, tick: 4));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal(JobStatus.InProgress, repository.Get().Get(JobId)!.Status);
    }

    [Fact]
    public void Bypass_releases_only_tool_reservation_and_unblocks_job()
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryJobRepository repository = CreateClaimedJob(reserveTool: true);
        RecordSuggestion(journal, AgentId, tick: 2);
        int reservationCount = repository.Get().GetReservations().Count;
        BypassSuggestedJobToolHandler bypass = new BypassSuggestedJobToolHandler(
            repository,
            journal,
            journal,
            journal);

        Result bypassed = bypass.Handle(new BypassSuggestedJobToolCommand(JobId, tick: 4));

        Assert.True(bypassed.IsSuccess, bypassed.Error?.ToString());
        JobSnapshot claimed = repository.Get().Get(JobId)!;
        Assert.Equal(JobStatus.Claimed, claimed.Status);
        Assert.Equal(AgentId, claimed.AssignedAgentId);
        Assert.Equal(reservationCount - 1, repository.Get().GetReservations().Count);
        Assert.DoesNotContain(
            repository.Get().GetReservations(),
            value => value.Key == ReservationKey.ForTool(ToolStackId));
        Assert.Contains(
            repository.Get().GetReservations(),
            value => value.Key == ReservationKey.ForAgent(AgentId));
        Assert.Contains(
            repository.Get().GetReservations(),
            value => value.Key == ReservationKey.ForJob(JobId));
        JobAssignment retained = Assert.Single(journal.Find(JobId)!.Assignments);
        Assert.Equal(JobToolPreparationOutcome.Bypassed, retained.ToolPreparation);
        Assert.Equal(AgentId, retained.AgentId);
        Assert.Equal(ToolStackId, retained.ToolStackId);
        Assert.Contains(
            journal.Events.OfType<JobReservationsReleased>(),
            value => value.JobId == JobId && value.ReservationCount == 1);

        AdvanceJobHandler advance = new AdvanceJobHandler(
            repository,
            journal,
            new SuggestedToolJobExecutionReadinessPolicy(journal));
        Result advanced = advance.Handle(new AdvanceJobCommand(JobId, tick: 5));

        Assert.True(advanced.IsSuccess, advanced.Error?.ToString());
        Assert.Equal(JobStatus.InProgress, repository.Get().Get(JobId)!.Status);
    }

    [Fact]
    public void Bypass_recovers_stale_suggestion_without_tool_reservation()
    {
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        InMemoryJobRepository repository = CreateClaimedJob(reserveTool: false);
        RecordSuggestion(journal, OtherAgentId, tick: 2);
        BypassSuggestedJobToolHandler bypass = new BypassSuggestedJobToolHandler(
            repository,
            journal,
            journal,
            journal);

        Result result = bypass.Handle(new BypassSuggestedJobToolCommand(JobId, tick: 4));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        JobAssignment retained = Assert.Single(journal.Find(JobId)!.Assignments);
        Assert.Equal(JobToolPreparationOutcome.Bypassed, retained.ToolPreparation);
        Assert.Equal(AgentId, retained.AgentId);
        Assert.Equal(JobStatus.Claimed, repository.Get().Get(JobId)!.Status);
    }

    private static InMemoryJobRepository CreateClaimedJob(bool reserveTool)
    {
        InMemoryJobRepository repository = new InMemoryJobRepository();
        JobSystem jobs = repository.Get();
        Assert.True(jobs.Add(new DigJobDefinition(
            JobId,
            new DigJobTarget(new CellId(4, 5)),
            priority: 500,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default)).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, tick: 0).IsSuccess);
        Result claimed = reserveTool
            ? jobs.Claim(JobId, AgentId, ToolStackId, tick: 1)
            : jobs.Claim(JobId, AgentId, tick: 1);
        Assert.True(claimed.IsSuccess, claimed.Error?.ToString());
        repository.Save(jobs);
        return repository;
    }

    private static void RecordSuggestion(
        InMemoryExecutionJournal journal,
        EntityId agentId,
        long tick)
    {
        RecordAssignment(
            journal,
            agentId,
            JobToolPreparationOutcome.Suggested,
            tick);
    }

    private static void RecordAssignment(
        InMemoryExecutionJournal journal,
        EntityId agentId,
        JobToolPreparationOutcome outcome,
        long tick)
    {
        journal.Record(new JobAssignmentReport(
            tick,
            new[]
            {
                new JobAssignment(
                    JobId,
                    agentId,
                    score: 700,
                    toolPreparation: outcome,
                    toolStackId: ToolStackId),
            },
            Array.Empty<JobAssignmentFailure>()));
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
