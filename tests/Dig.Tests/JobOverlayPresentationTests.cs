using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Jobs;
using Xunit;

namespace Dig.Tests
{

public sealed class JobOverlayPresentationTests
{
    [Fact]
    public void Presenter_maps_dig_target_worker_stage_and_reservations()
    {
        EntityId jobId = EntityId.Parse("20000000000000000000000000000001");
        EntityId agentId = EntityId.Parse("30000000000000000000000000000001");
        JobSystem jobs = new JobSystem();
        DigJobDefinition definition = new DigJobDefinition(
            jobId,
            new DigJobTarget(new CellId(8, 5)),
            priority: 700,
            createdTick: 2,
            JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(jobId, tick: 2).IsSuccess);
        Assert.True(jobs.Claim(jobId, agentId, tick: 3).IsSuccess);
        Assert.True(jobs.Start(jobId, tick: 4).IsSuccess);
        InMemoryJobRepository repository = new InMemoryJobRepository(jobs);
        JobOverlayPresenter presenter = new JobOverlayPresenter(
            new GetJobsHandler(repository),
            new GetJobReservationsHandler(repository));

        JobOverlayViewModel model = Assert.Single(presenter.Load());

        Assert.Equal(jobId.ToString(), model.Id);
        Assert.Equal("InProgress", model.Status);
        Assert.Equal("TravelToTarget", model.Stage);
        Assert.Equal(agentId.ToString(), model.AssignedAgentId);
        Assert.Equal(8, model.TargetX);
        Assert.Equal(5, model.TargetY);
        Assert.Equal(JobToolKind.Mining, model.PreferredToolKind);
        Assert.Null(model.AssignmentDiagnostic);
        Assert.Equal(4, model.Reservations.Count);
        Assert.Equal(
            new[] { "Agent", "Designation", "Job", "Position" },
            model.Reservations.Select(item => item.Kind));
    }

    [Fact]
    public void Presenter_maps_tool_switch_assignment_diagnostic()
    {
        EntityId jobId = EntityId.Parse("20000000000000000000000000000003");
        EntityId agentId = EntityId.Parse("30000000000000000000000000000003");
        EntityId toolStackId = EntityId.Parse("40000000000000000000000000000003");
        JobSystem jobs = new JobSystem();
        DigJobDefinition definition = new DigJobDefinition(
            jobId,
            new DigJobTarget(new CellId(9, 4)),
            priority: 650,
            createdTick: 1,
            JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(jobId, tick: 1).IsSuccess);
        Assert.True(jobs.Claim(jobId, agentId, toolStackId, tick: 2).IsSuccess);
        JobAssignmentReport report = new JobAssignmentReport(
            tick: 2,
            assignments: new[]
            {
                new JobAssignment(
                    jobId,
                    agentId,
                    score: 5_650_499_999L,
                    toolPreparation: JobToolPreparationOutcome.Switched,
                    toolStackId: toolStackId),
            },
            failures: System.Array.Empty<JobAssignmentFailure>());
        InMemoryJobRepository repository = new InMemoryJobRepository(jobs);
        JobOverlayPresenter presenter = new JobOverlayPresenter(
            new GetJobsHandler(repository),
            new GetJobReservationsHandler(repository));

        JobOverlayViewModel model = Assert.Single(presenter.Load(report));

        JobAssignmentDiagnosticViewModel diagnostic =
            Assert.IsType<JobAssignmentDiagnosticViewModel>(model.AssignmentDiagnostic);
        Assert.False(diagnostic.IsFailure);
        Assert.Equal(2, diagnostic.Tick);
        Assert.Equal(5_650_499_999L, diagnostic.Score);
        Assert.Equal(JobToolPreparationOutcome.Switched, diagnostic.ToolPreparation);
        Assert.Equal(toolStackId.ToString(), diagnostic.ToolStackId);
        Assert.Contains(model.Reservations, value => value.Kind == "Tool");
    }

    [Fact]
    public void Presenter_maps_assignment_failure_reason()
    {
        EntityId jobId = EntityId.Parse("20000000000000000000000000000004");
        JobSystem jobs = new JobSystem();
        DigJobDefinition definition = new DigJobDefinition(
            jobId,
            new DigJobTarget(new CellId(3, 7)),
            priority: 500,
            createdTick: 1,
            JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(jobId, tick: 1).IsSuccess);
        JobAssignmentReport report = new JobAssignmentReport(
            tick: 2,
            assignments: System.Array.Empty<JobAssignment>(),
            failures: new[]
            {
                new JobAssignmentFailure(
                    jobId,
                    JobErrors.ToolPreparationUnavailable),
            });
        InMemoryJobRepository repository = new InMemoryJobRepository(jobs);
        JobOverlayPresenter presenter = new JobOverlayPresenter(
            new GetJobsHandler(repository),
            new GetJobReservationsHandler(repository));

        JobOverlayViewModel model = Assert.Single(presenter.Load(report));

        JobAssignmentDiagnosticViewModel diagnostic =
            Assert.IsType<JobAssignmentDiagnosticViewModel>(model.AssignmentDiagnostic);
        Assert.True(diagnostic.IsFailure);
        Assert.Equal(JobErrors.ToolPreparationUnavailable.Code, diagnostic.FailureCode);
        Assert.Equal(JobErrors.ToolPreparationUnavailable.Message, diagnostic.FailureMessage);
        Assert.Null(diagnostic.ToolPreparation);
        Assert.Null(diagnostic.ToolStackId);
    }

    [Fact]
    public void Terminal_job_has_no_active_reservations()
    {
        EntityId jobId = EntityId.Parse("20000000000000000000000000000002");
        EntityId agentId = EntityId.Parse("30000000000000000000000000000002");
        JobSystem jobs = new JobSystem();
        DigJobDefinition definition = new DigJobDefinition(
            jobId,
            new DigJobTarget(new CellId(4, 6)),
            priority: 500,
            createdTick: 1,
            new JobRetryPolicy(maximumRetries: 1, retryDelayTicks: 2));
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(jobId, tick: 1).IsSuccess);
        Assert.True(jobs.Claim(jobId, agentId, tick: 2).IsSuccess);
        Assert.True(jobs.Start(jobId, tick: 3).IsSuccess);
        Assert.True(jobs.AdvanceStage(jobId, tick: 4).IsSuccess);
        Assert.True(jobs.AdvanceStage(jobId, tick: 5).IsSuccess);
        Assert.True(jobs.AdvanceStage(jobId, tick: 6).IsSuccess);
        InMemoryJobRepository repository = new InMemoryJobRepository(jobs);
        JobOverlayPresenter presenter = new JobOverlayPresenter(
            new GetJobsHandler(repository),
            new GetJobReservationsHandler(repository));

        JobOverlayViewModel model = Assert.Single(presenter.Load());

        Assert.Equal("Completed", model.Status);
        Assert.Empty(model.Reservations);
    }
}
}
