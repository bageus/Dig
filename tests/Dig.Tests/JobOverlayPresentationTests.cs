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
        Assert.Equal(4, model.Reservations.Count);
        Assert.Equal(
            new[] { "Agent", "Designation", "Job", "Position" },
            model.Reservations.Select(item => item.Kind));
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