using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Jobs;
using Xunit;

namespace Dig.Tests;

public sealed class JobPresenterTests
{
    [Fact]
    public void Diagnostic_view_exposes_stage_owner_and_block_reason()
    {
        EntityId jobId = EntityId.Parse("30000000000000000000000000000001");
        EntityId agentId = EntityId.Parse("40000000000000000000000000000001");
        JobSystem jobs = new JobSystem();
        DigJobDefinition definition = new DigJobDefinition(
            jobId,
            new DigJobTarget(new CellId(5, 6)),
            priority: 500,
            createdTick: 0,
            JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(jobId, tick: 0).IsSuccess);
        Assert.True(jobs.Claim(jobId, agentId, tick: 1).IsSuccess);
        Assert.True(jobs.Start(jobId, tick: 2).IsSuccess);
        Assert.True(jobs.Block(
            jobId,
            new JobBlockReason("path_missing", "No route to target."),
            tick: 3).IsSuccess);

        JobDiagnosticView view = new JobPresenter().Present(jobs.Get(jobId)!);

        Assert.Equal(JobStatus.Blocked.ToString(), view.Status);
        Assert.Equal(JobStageKind.None.ToString(), view.Stage);
        Assert.Null(view.AssignedAgentId);
        Assert.Contains("path_missing", view.Reason!);
        Assert.Equal("Dig:5,6", view.Target);
    }
}
