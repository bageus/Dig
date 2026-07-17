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

public sealed class JobActionPresentationTests
{
    private static readonly EntityId JobId = Id(1);
    private static readonly EntityId FirstAgentId = Id(2);
    private static readonly EntityId SecondAgentId = Id(3);
    private static readonly EntityId ToolStackId = Id(4);

    [Fact]
    public void Suggested_tool_action_is_enabled_for_matching_active_reservation()
    {
        JobSystem jobs = CreateAvailableJob();
        Assert.True(jobs.Claim(JobId, FirstAgentId, ToolStackId, tick: 2).IsSuccess);

        JobActionViewModel action = PresentAction(
            jobs,
            CreateSuggestion(FirstAgentId));

        Assert.Equal(JobActionKind.PrepareSuggestedTool, action.Kind);
        Assert.Equal("Equip suggested tool", action.Label);
        Assert.True(action.IsEnabled);
        Assert.Null(action.DisabledReasonCode);
        Assert.Null(action.DisabledReasonMessage);
    }

    [Fact]
    public void Suggested_tool_action_reports_invalid_status_before_missing_reservation()
    {
        JobSystem jobs = CreateAvailableJob();

        JobActionViewModel action = PresentAction(
            jobs,
            CreateSuggestion(FirstAgentId));

        Assert.False(action.IsEnabled);
        Assert.Equal(JobErrors.InvalidStatus.Code, action.DisabledReasonCode);
        Assert.Equal(JobErrors.InvalidStatus.Message, action.DisabledReasonMessage);
    }

    [Fact]
    public void Suggested_tool_action_reports_stale_resident_before_reservation_mismatch()
    {
        JobSystem jobs = CreateAvailableJob();
        Assert.True(jobs.Claim(JobId, SecondAgentId, ToolStackId, tick: 2).IsSuccess);

        JobActionViewModel action = PresentAction(
            jobs,
            CreateSuggestion(FirstAgentId));

        Assert.False(action.IsEnabled);
        Assert.Equal(
            PrepareSuggestedJobToolErrors.SuggestionStale.Code,
            action.DisabledReasonCode);
        Assert.Equal(
            PrepareSuggestedJobToolErrors.SuggestionStale.Message,
            action.DisabledReasonMessage);
    }

    [Fact]
    public void Suggested_tool_action_reports_missing_tool_reservation()
    {
        JobSystem jobs = CreateAvailableJob();
        Assert.True(jobs.Claim(JobId, FirstAgentId, tick: 2).IsSuccess);

        JobActionViewModel action = PresentAction(
            jobs,
            CreateSuggestion(FirstAgentId));

        Assert.False(action.IsEnabled);
        Assert.Equal(
            PrepareSuggestedJobToolErrors.ToolReservationMissing.Code,
            action.DisabledReasonCode);
        Assert.Equal(
            PrepareSuggestedJobToolErrors.ToolReservationMissing.Message,
            action.DisabledReasonMessage);
    }

    [Fact]
    public void Non_suggested_assignment_has_no_manual_tool_action()
    {
        JobSystem jobs = CreateAvailableJob();
        Assert.True(jobs.Claim(JobId, FirstAgentId, ToolStackId, tick: 2).IsSuccess);
        JobAssignmentReport report = new JobAssignmentReport(
            tick: 2,
            assignments: new[]
            {
                new JobAssignment(
                    JobId,
                    FirstAgentId,
                    score: 500,
                    toolPreparation: JobToolPreparationOutcome.Switched,
                    toolStackId: ToolStackId),
            },
            failures: Array.Empty<JobAssignmentFailure>());

        JobOverlayViewModel model = Present(jobs, report);

        Assert.Empty(model.Actions);
    }

    private static JobActionViewModel PresentAction(
        JobSystem jobs,
        JobAssignmentReport report)
    {
        return Assert.Single(Present(jobs, report).Actions);
    }

    private static JobOverlayViewModel Present(
        JobSystem jobs,
        JobAssignmentReport report)
    {
        InMemoryJobRepository repository = new InMemoryJobRepository(jobs);
        JobOverlayPresenter presenter = new JobOverlayPresenter(
            new GetJobsHandler(repository),
            new GetJobReservationsHandler(repository));
        return Assert.Single(presenter.Load(report));
    }

    private static JobAssignmentReport CreateSuggestion(EntityId agentId)
    {
        return new JobAssignmentReport(
            tick: 2,
            assignments: new[]
            {
                new JobAssignment(
                    JobId,
                    agentId,
                    score: 500,
                    toolPreparation: JobToolPreparationOutcome.Suggested,
                    toolStackId: ToolStackId),
            },
            failures: Array.Empty<JobAssignmentFailure>());
    }

    private static JobSystem CreateAvailableJob()
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
        return jobs;
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
