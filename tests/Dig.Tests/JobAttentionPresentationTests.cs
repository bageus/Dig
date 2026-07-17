using System;
using Dig.Presentation.Jobs;
using Xunit;

namespace Dig.Tests
{

public sealed class JobAttentionPresentationTests
{
    [Fact]
    public void Projection_filters_ready_Jobs_orders_attention_and_bounds_rows()
    {
        JobOverlayViewModel[] jobs =
        {
            CreateJob("00000000000000000000000000000005", priority: 900, waiting: false),
            CreateJob("00000000000000000000000000000003", priority: 700, waiting: true),
            CreateJob("00000000000000000000000000000002", priority: 900, waiting: true),
            CreateJob("00000000000000000000000000000001", priority: 900, waiting: true),
            CreateJob("00000000000000000000000000000004", priority: 500, waiting: true),
        };

        JobAttentionSummaryViewModel summary = JobAttentionProjection.Project(
            jobs,
            maximumItems: 2);

        Assert.True(summary.HasAttention);
        Assert.Equal(4, summary.TotalCount);
        Assert.Equal(2, summary.Items.Count);
        Assert.Equal(2, summary.HiddenCount);
        Assert.Equal("00000000000000000000000000000001", summary.Items[0].JobId);
        Assert.Equal("00000000000000000000000000000002", summary.Items[1].JobId);
    }

    [Fact]
    public void Projection_preserves_compact_readiness_and_worker_details()
    {
        JobOverlayViewModel job = CreateJob(
            "00000000000000000000000000000001",
            priority: 700,
            waiting: true,
            assignedAgentId: "10000000000000000000000000000001");

        JobAttentionItemViewModel item = Assert.Single(
            JobAttentionProjection.Project(new[] { job }).Items);

        Assert.Equal(job.Id, item.JobId);
        Assert.Equal(job.Description, item.Description);
        Assert.Equal("Waiting for tool decision", item.ReadinessLabel);
        Assert.Equal("jobs.waiting_for_tool_decision", item.ReasonCode);
        Assert.Equal(job.AssignedAgentId, item.AssignedAgentId);
    }

    [Fact]
    public void Projection_returns_empty_summary_when_all_Jobs_are_ready()
    {
        JobAttentionSummaryViewModel summary = JobAttentionProjection.Project(new[]
        {
            CreateJob("00000000000000000000000000000001", priority: 700, waiting: false),
        });

        Assert.False(summary.HasAttention);
        Assert.Equal(0, summary.TotalCount);
        Assert.Empty(summary.Items);
        Assert.Equal(0, summary.HiddenCount);
    }

    [Fact]
    public void Projection_rejects_non_positive_row_limit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => JobAttentionProjection.Project(
                Array.Empty<JobOverlayViewModel>(),
                maximumItems: 0));
    }

    private static JobOverlayViewModel CreateJob(
        string id,
        int priority,
        bool waiting,
        string? assignedAgentId = null)
    {
        JobExecutionReadinessViewModel readiness = waiting
            ? new JobExecutionReadinessViewModel(
                JobExecutionReadinessKind.WaitingForToolDecision,
                "Waiting for tool decision",
                "jobs.waiting_for_tool_decision",
                "Choose whether to equip the suggested tool or proceed without it.")
            : new JobExecutionReadinessViewModel(
                JobExecutionReadinessKind.Ready,
                "Ready");
        return new JobOverlayViewModel(
            id,
            $"Job {id.Substring(id.Length - 2)}",
            status: "Claimed",
            stage: "TravelToTarget",
            priority,
            assignedAgentId,
            targetX: 1,
            targetY: 2,
            retryCount: 0,
            nextRetryTick: 0,
            reason: null,
            reservations: Array.Empty<JobReservationViewModel>(),
            executionReadiness: readiness);
    }
}
}