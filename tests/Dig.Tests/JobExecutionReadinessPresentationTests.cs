using System;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Jobs;
using Xunit;

namespace Dig.Tests
{

public sealed class JobExecutionReadinessPresentationTests
{
    private static readonly EntityId JobId = Id(1);
    private static readonly EntityId AgentId = Id(2);
    private static readonly EntityId ToolStackId = Id(3);

    [Fact]
    public void Active_suggestion_is_presented_as_waiting_for_tool_decision()
    {
        JobSystem jobs = CreateClaimedJob();

        JobOverlayViewModel model = Present(
            jobs,
            CreateAssignmentReport(JobToolPreparationOutcome.Suggested));

        JobExecutionReadinessViewModel readiness = model.ExecutionReadiness;
        Assert.False(readiness.IsReady);
        Assert.Equal(
            JobExecutionReadinessKind.WaitingForToolDecision,
            readiness.Kind);
        Assert.Equal("Waiting for tool decision", readiness.Label);
        Assert.Equal("jobs.waiting_for_tool_decision", readiness.ReasonCode);
        Assert.Equal(
            "Equip the suggested tool or proceed without it before this Job can advance.",
            readiness.ReasonMessage);
    }

    [Theory]
    [InlineData(JobToolPreparationOutcome.AlreadyEquipped)]
    [InlineData(JobToolPreparationOutcome.Switched)]
    [InlineData(JobToolPreparationOutcome.Bypassed)]
    public void Resolved_tool_outcome_is_presented_as_ready(
        JobToolPreparationOutcome outcome)
    {
        JobSystem jobs = CreateClaimedJob();

        JobOverlayViewModel model = Present(jobs, CreateAssignmentReport(outcome));

        Assert.True(model.ExecutionReadiness.IsReady);
        Assert.Equal(JobExecutionReadinessKind.Ready, model.ExecutionReadiness.Kind);
        Assert.Equal("Ready", model.ExecutionReadiness.Label);
        Assert.Null(model.ExecutionReadiness.ReasonCode);
        Assert.Null(model.ExecutionReadiness.ReasonMessage);
    }

    [Fact]
    public void Job_without_assignment_report_is_presented_as_ready()
    {
        JobSystem jobs = CreateClaimedJob();

        JobOverlayViewModel model = Present(jobs, report: null);

        Assert.True(model.ExecutionReadiness.IsReady);
        Assert.Equal(JobExecutionReadinessKind.Ready, model.ExecutionReadiness.Kind);
    }

    [Fact]
    public void Terminal_job_with_retained_suggestion_is_not_presented_as_waiting()
    {
        JobSystem jobs = CreateClaimedJob();
        Assert.True(jobs.Complete(JobId, tick: 3).IsSuccess);

        JobOverlayViewModel model = Present(
            jobs,
            CreateAssignmentReport(JobToolPreparationOutcome.Suggested));

        Assert.Equal("Completed", model.Status);
        Assert.True(model.ExecutionReadiness.IsReady);
        Assert.Equal(JobExecutionReadinessKind.Ready, model.ExecutionReadiness.Kind);
    }

    private static JobOverlayViewModel Present(
        JobSystem jobs,
        JobAssignmentReport? report)
    {
        InMemoryJobRepository repository = new InMemoryJobRepository(jobs);
        JobOverlayPresenter presenter = new JobOverlayPresenter(
            new GetJobsHandler(repository),
            new GetJobReservationsHandler(repository));
        return Assert.Single(presenter.Load(report));
    }

    private static JobSystem CreateClaimedJob()
    {
        JobSystem jobs = new JobSystem();
        DigJobDefinition definition = new DigJobDefinition(
            JobId,
            new DigJobTarget(new CellId(4, 5)),
            priority: 500,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, tick: 1).IsSuccess);
        Assert.True(jobs.Claim(JobId, AgentId, ToolStackId, tick: 2).IsSuccess);
        return jobs;
    }

    private static JobAssignmentReport CreateAssignmentReport(
        JobToolPreparationOutcome outcome)
    {
        return new JobAssignmentReport(
            tick: 2,
            assignments: new[]
            {
                new JobAssignment(
                    JobId,
                    AgentId,
                    score: 500,
                    toolPreparation: outcome,
                    toolStackId: ToolStackId),
            },
            failures: Array.Empty<JobAssignmentFailure>());
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
