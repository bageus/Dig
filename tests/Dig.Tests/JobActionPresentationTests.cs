using System;
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

public sealed class JobActionPresentationTests
{
    private static readonly EntityId JobId = Id(1);
    private static readonly EntityId FirstAgentId = Id(2);
    private static readonly EntityId SecondAgentId = Id(3);
    private static readonly EntityId ToolStackId = Id(4);

    [Fact]
    public void Suggested_tool_actions_are_enabled_for_matching_active_reservation()
    {
        JobSystem jobs = CreateAvailableJob();
        Assert.True(jobs.Claim(JobId, FirstAgentId, ToolStackId, tick: 2).IsSuccess);

        IReadOnlyList<JobActionViewModel> actions = PresentActions(
            jobs,
            CreateSuggestion(FirstAgentId));

        JobActionViewModel prepare = Action(actions, JobActionKind.PrepareSuggestedTool);
        Assert.Equal("Equip suggested tool", prepare.Label);
        Assert.True(prepare.IsEnabled);
        Assert.Null(prepare.DisabledReasonCode);
        JobActionViewModel bypass = Action(actions, JobActionKind.BypassSuggestedTool);
        Assert.Equal("Proceed without suggested tool", bypass.Label);
        Assert.True(bypass.IsEnabled);
        Assert.Null(bypass.DisabledReasonCode);
    }

    [Fact]
    public void Suggested_tool_actions_report_invalid_status()
    {
        JobSystem jobs = CreateAvailableJob();

        IReadOnlyList<JobActionViewModel> actions = PresentActions(
            jobs,
            CreateSuggestion(FirstAgentId));

        foreach (JobActionViewModel action in actions)
        {
            Assert.False(action.IsEnabled);
            Assert.Equal(JobErrors.InvalidStatus.Code, action.DisabledReasonCode);
            Assert.Equal(JobErrors.InvalidStatus.Message, action.DisabledReasonMessage);
        }
    }

    [Fact]
    public void Stale_resident_disables_prepare_but_keeps_bypass_available()
    {
        JobSystem jobs = CreateAvailableJob();
        Assert.True(jobs.Claim(JobId, SecondAgentId, ToolStackId, tick: 2).IsSuccess);

        IReadOnlyList<JobActionViewModel> actions = PresentActions(
            jobs,
            CreateSuggestion(FirstAgentId));

        JobActionViewModel prepare = Action(actions, JobActionKind.PrepareSuggestedTool);
        Assert.False(prepare.IsEnabled);
        Assert.Equal(
            PrepareSuggestedJobToolErrors.SuggestionStale.Code,
            prepare.DisabledReasonCode);
        JobActionViewModel bypass = Action(actions, JobActionKind.BypassSuggestedTool);
        Assert.True(bypass.IsEnabled);
    }

    [Fact]
    public void Missing_tool_reservation_disables_prepare_but_keeps_bypass_available()
    {
        JobSystem jobs = CreateAvailableJob();
        Assert.True(jobs.Claim(JobId, FirstAgentId, tick: 2).IsSuccess);

        IReadOnlyList<JobActionViewModel> actions = PresentActions(
            jobs,
            CreateSuggestion(FirstAgentId));

        JobActionViewModel prepare = Action(actions, JobActionKind.PrepareSuggestedTool);
        Assert.False(prepare.IsEnabled);
        Assert.Equal(
            PrepareSuggestedJobToolErrors.ToolReservationMissing.Code,
            prepare.DisabledReasonCode);
        JobActionViewModel bypass = Action(actions, JobActionKind.BypassSuggestedTool);
        Assert.True(bypass.IsEnabled);
    }

    [Theory]
    [InlineData(JobToolPreparationOutcome.Switched)]
    [InlineData(JobToolPreparationOutcome.Bypassed)]
    [InlineData(JobToolPreparationOutcome.AlreadyEquipped)]
    public void Resolved_assignment_has_no_manual_tool_actions(
        JobToolPreparationOutcome outcome)
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
                    toolPreparation: outcome,
                    toolStackId: ToolStackId),
            },
            failures: Array.Empty<JobAssignmentFailure>());

        JobOverlayViewModel model = Present(jobs, report);

        Assert.Empty(model.Actions);
    }

    private static JobActionViewModel Action(
        IReadOnlyList<JobActionViewModel> actions,
        JobActionKind kind)
    {
        return Assert.Single(actions.Where(value => value.Kind == kind));
    }

    private static IReadOnlyList<JobActionViewModel> PresentActions(
        JobSystem jobs,
        JobAssignmentReport report)
    {
        IReadOnlyList<JobActionViewModel> actions = Present(jobs, report).Actions;
        Assert.Equal(2, actions.Count);
        return actions;
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
