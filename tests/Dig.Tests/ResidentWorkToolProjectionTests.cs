using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Jobs;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentWorkToolProjectionTests
{
    [Fact]
    public void Excavation_projects_pickaxe_only_during_perform_work()
    {
        EntityId jobId = Id(1);
        DigJobDefinition definition = new DigJobDefinition(
            jobId,
            new DigJobTarget(new CellId(5, 4)),
            priority: 700,
            createdTick: 1,
            JobRetryPolicy.Default);

        Assert.Equal(
            ResidentWorkToolVisualKind.None,
            Project(definition, advanceCount: 0).WorkToolVisualKind);
        Assert.Equal(
            ResidentWorkToolVisualKind.Pickaxe,
            Project(definition, advanceCount: 1).WorkToolVisualKind);
    }

    [Fact]
    public void Spatial_excavation_projects_pickaxe_at_exact_depth()
    {
        CellId target = new CellId(6, 5, 2);
        SpatialDigJobDefinition definition = new SpatialDigJobDefinition(
            Id(2),
            new SpatialDigJobTarget(target, new CellId(5, 5, 2)),
            priority: 700,
            createdTick: 1,
            JobRetryPolicy.Default);

        JobOverlayViewModel model = Project(definition, advanceCount: 1);

        Assert.Equal(ResidentWorkToolVisualKind.Pickaxe, model.WorkToolVisualKind);
        Assert.Equal(target.Z, model.TargetZ);
    }

    [Fact]
    public void Mushroom_chop_projects_axe()
    {
        MushroomChopJobDefinition definition = new MushroomChopJobDefinition(
            Id(3),
            Id(30),
            new CellId(7, 5, 1),
            new CellId(6, 5, 1),
            growthGeneration: 1,
            requiredSwings: 3,
            priority: 900,
            createdTick: 1,
            JobRetryPolicy.Default);

        Assert.Equal(
            ResidentWorkToolVisualKind.Axe,
            Project(definition, advanceCount: 1).WorkToolVisualKind);
    }

    [Fact]
    public void Construction_projects_hammer_but_repair_does_not_invent_one()
    {
        CellId work = new CellId(7, 7, 1);
        BuildingWorkJobDefinition construction = new BuildingWorkJobDefinition(
            Id(31),
            Id(32),
            BuildingWorkKind.Construction,
            work,
            priority: 600,
            createdTick: 1,
            JobRetryPolicy.Default);
        BuildingWorkJobDefinition repair = new BuildingWorkJobDefinition(
            Id(33),
            Id(34),
            BuildingWorkKind.Repair,
            work,
            priority: 600,
            createdTick: 1,
            JobRetryPolicy.Default);

        JobOverlayViewModel constructionModel = Project(construction, advanceCount: 1);
        JobOverlayViewModel repairModel = Project(repair, advanceCount: 1);

        Assert.Equal(
            ResidentWorkToolVisualKind.Hammer,
            constructionModel.WorkToolVisualKind);
        Assert.Equal(
            ResidentWorkToolVisualKind.None,
            repairModel.WorkToolVisualKind);
        Assert.Equal(work.Z, constructionModel.TargetZ);
    }

    [Fact]
    public void Building_box_assembly_projects_hammer_and_work_position()
    {
        CellId work = new CellId(8, 6, 2);
        BuildingBoxAssemblyJobDefinition definition =
            new BuildingBoxAssemblyJobDefinition(
                Id(4),
                Id(40),
                Id(41),
                new CellId(9, 6, 2),
                work,
                priority: 600,
                createdTick: 1,
                JobRetryPolicy.Default);

        JobOverlayViewModel model = Project(definition, advanceCount: 3);

        Assert.Equal(ResidentWorkToolVisualKind.Hammer, model.WorkToolVisualKind);
        Assert.Equal(work.X, model.TargetX);
        Assert.Equal(work.Y, model.TargetY);
        Assert.Equal(work.Z, model.TargetZ);
    }

    [Fact]
    public void Building_box_packing_projects_hammer_and_work_position()
    {
        CellId work = new CellId(10, 6, 3);
        BuildingBoxPackingJobDefinition definition =
            new BuildingBoxPackingJobDefinition(
                Id(5),
                Id(50),
                Id(51),
                work,
                priority: 600,
                createdTick: 1,
                JobRetryPolicy.Default);

        JobOverlayViewModel model = Project(definition, advanceCount: 1);

        Assert.Equal(ResidentWorkToolVisualKind.Hammer, model.WorkToolVisualKind);
        Assert.Equal(work.X, model.TargetX);
        Assert.Equal(work.Y, model.TargetY);
        Assert.Equal(work.Z, model.TargetZ);
    }

    private static JobOverlayViewModel Project(
        JobDefinition definition,
        int advanceCount)
    {
        JobSystem jobs = new JobSystem();
        EntityId workerId = Id(100 + advanceCount);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(definition.Id, tick: 2).IsSuccess);
        Assert.True(jobs.Claim(definition.Id, workerId, tick: 3).IsSuccess);
        Assert.True(jobs.Start(definition.Id, tick: 4).IsSuccess);
        for (int index = 0; index < advanceCount; index++)
        {
            Assert.True(jobs.AdvanceStage(definition.Id, tick: 5 + index).IsSuccess);
        }

        InMemoryJobRepository repository = new InMemoryJobRepository(jobs);
        return Assert.Single(new JobOverlayPresenter(
            new GetJobsHandler(repository),
            new GetJobReservationsHandler(repository)).Load());
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
