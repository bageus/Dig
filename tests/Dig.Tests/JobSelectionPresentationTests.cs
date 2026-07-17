using System;
using System.Collections.Generic;
using Dig.Presentation.Jobs;
using Xunit;

namespace Dig.Tests
{

public sealed class JobSelectionPresentationTests
{
    [Fact]
    public void Resolve_rebinds_selection_to_latest_immutable_model()
    {
        JobOverlayViewModel previous = CreateJob("00000000000000000000000000000001", "Claimed");
        JobOverlayViewModel updated = CreateJob(previous.Id, "InProgress");

        JobOverlayViewModel? resolved = JobSelectionProjection.Resolve(
            new[] { updated },
            previous.Id);

        Assert.Same(updated, resolved);
        Assert.Equal("InProgress", resolved!.Status);
    }

    [Fact]
    public void Resolve_clears_selection_when_job_is_no_longer_projected()
    {
        JobOverlayViewModel existing = CreateJob(
            "00000000000000000000000000000002",
            "Claimed");

        JobOverlayViewModel? resolved = JobSelectionProjection.Resolve(
            new[] { existing },
            "00000000000000000000000000000003");

        Assert.Null(resolved);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_keeps_empty_selection_empty(string? selectedJobId)
    {
        JobOverlayViewModel existing = CreateJob(
            "00000000000000000000000000000004",
            "Claimed");

        Assert.Null(JobSelectionProjection.Resolve(
            new[] { existing },
            selectedJobId));
    }

    [Fact]
    public void Resolve_rejects_null_projection_collection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            JobSelectionProjection.Resolve(null!, "job"));
    }

    private static JobOverlayViewModel CreateJob(string id, string status)
    {
        return new JobOverlayViewModel(
            id,
            "Dig target",
            status,
            "TravelToTarget",
            priority: 500,
            assignedAgentId: null,
            targetX: 4,
            targetY: 5,
            retryCount: 0,
            nextRetryTick: 0,
            reason: null,
            reservations: Array.Empty<JobReservationViewModel>());
    }
}
}
