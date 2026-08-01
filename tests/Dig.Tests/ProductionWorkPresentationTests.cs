using Dig.Application.Jobs;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Jobs;
using Xunit;

namespace Dig.Tests
{

public sealed class ProductionWorkPresentationTests
{
    [Fact]
    public void Presenter_marks_production_work_and_maps_workstation_target()
    {
        EntityId jobId = EntityId.Parse("20000000000000000000000000000006");
        EntityId orderId = EntityId.Parse("21000000000000000000000000000006");
        EntityId buildingId = EntityId.Parse("22000000000000000000000000000006");
        EntityId agentId = EntityId.Parse("30000000000000000000000000000006");
        CellId work = new CellId(5, 4, 1);
        JobSystem jobs = new JobSystem();
        ProductionWorkJobDefinition definition = new ProductionWorkJobDefinition(
            jobId,
            orderId,
            buildingId,
            new RecipeId("recipe.presentation.production"),
            work,
            priority: 700,
            createdTick: 2,
            JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(jobId, tick: 2).IsSuccess);
        Assert.True(jobs.Claim(jobId, agentId, tick: 3).IsSuccess);
        Assert.True(jobs.Start(jobId, tick: 4).IsSuccess);
        Assert.True(jobs.AdvanceStage(jobId, tick: 5).IsSuccess);
        InMemoryJobRepository repository = new InMemoryJobRepository(jobs);

        JobOverlayViewModel model = Assert.Single(new JobOverlayPresenter(
            new GetJobsHandler(repository),
            new GetJobReservationsHandler(repository)).Load());

        Assert.True(model.IsProductionWork);
        Assert.Equal(work.X, model.TargetX);
        Assert.Equal(work.Y, model.TargetY);
        Assert.Equal(work.Z, model.TargetZ);
        Assert.Equal("PerformWork", model.Stage);
        Assert.Equal(agentId.ToString(), model.AssignedAgentId);
    }
}

}
