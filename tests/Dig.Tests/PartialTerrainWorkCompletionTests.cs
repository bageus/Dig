using Dig.Application.Agents;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class PartialTerrainWorkCompletionTests
{
    private static readonly MaterialId Rock = new MaterialId("test.rock");
    private static readonly MaterialId Air = new MaterialId("test.air");
    private static readonly CellId Target = new CellId(3, 1);
    private static readonly EntityId JobId =
        EntityId.Parse("20000000000000000000000000000001");
    private static readonly EntityId WorkerId =
        EntityId.Parse("10000000000000000000000000000001");

    [Fact]
    public void Partial_room_target_completes_without_opening_or_output()
    {
        WorldState world = CreateWorld();
        ExcavationQuarter required = ExcavationQuarter.UpperRight
            | ExcavationQuarter.LowerRight;
        Assert.True(world.CommitExcavationQuarter(
            Target,
            ExcavationQuarter.UpperRight,
            ExcavationCutPattern.VerticalColumns,
            Air,
            tick: 5).IsSuccess);
        Assert.True(world.CommitExcavationQuarter(
            Target,
            ExcavationQuarter.LowerRight,
            ExcavationCutPattern.VerticalColumns,
            Air,
            tick: 6).IsSuccess);
        JobSystem jobs = CreateFinalizingJob();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        CompletePartialTerrainWorkCommandHandler handler =
            new CompletePartialTerrainWorkCommandHandler(
                new InMemoryJobRepository(jobs),
                new InMemoryWorldRepository(world),
                journal,
                AgentSkillGrantTestFactory.Create(WorkerId, journal));

        Result result = handler.Handle(new CompletePartialTerrainWorkCommand(
            JobId,
            required,
            tick: 10));

        Assert.True(result.IsSuccess);
        CellSnapshot cell = world.GetCell(Target).Value;
        Assert.True(cell.IsSolid);
        Assert.Equal(Rock, cell.State.MaterialId);
        Assert.Equal(CellDesignation.None, cell.State.Designation);
        Assert.Equal(required, cell.State.CompletedExcavationQuarters);
        Assert.Equal(JobStatus.Completed, jobs.Get(JobId)!.Status);
        Assert.Empty(jobs.GetReservations());
    }

    [Fact]
    public void Partial_completion_rejects_missing_required_quarter()
    {
        WorldState world = CreateWorld();
        Assert.True(world.CommitExcavationQuarter(
            Target,
            ExcavationQuarter.UpperRight,
            ExcavationCutPattern.VerticalColumns,
            Air,
            tick: 5).IsSuccess);
        JobSystem jobs = CreateFinalizingJob();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        CompletePartialTerrainWorkCommandHandler handler =
            new CompletePartialTerrainWorkCommandHandler(
                new InMemoryJobRepository(jobs),
                new InMemoryWorldRepository(world),
                journal,
                AgentSkillGrantTestFactory.Create(WorkerId, journal));

        Result result = handler.Handle(new CompletePartialTerrainWorkCommand(
            JobId,
            ExcavationQuarter.UpperRight | ExcavationQuarter.LowerRight,
            tick: 10));

        Assert.True(result.IsFailure);
        Assert.Equal(CellDesignation.Dig, world.GetCell(Target).Value.State.Designation);
        Assert.Equal(JobStatus.InProgress, jobs.Get(JobId)!.Status);
    }

    private static JobSystem CreateFinalizingJob()
    {
        JobSystem jobs = new JobSystem();
        Assert.True(jobs.Add(new DigJobDefinition(
            JobId,
            new DigJobTarget(Target),
            priority: 500,
            createdTick: 0,
            JobRetryPolicy.Default)).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, tick: 0).IsSuccess);
        Assert.True(jobs.Claim(JobId, WorkerId, tick: 1).IsSuccess);
        Assert.True(jobs.Start(JobId, tick: 2).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 3).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 4).IsSuccess);
        return jobs;
    }

    private static WorldState CreateWorld()
    {
        WorldState world = WorldState.CreateFilled(
            new WorldSize(6, 4),
            chunkSize: 2,
            new MaterialCatalog(new[]
            {
                new MaterialDefinition(Rock, isSolid: true, hardness: 100),
                new MaterialDefinition(Air, isSolid: false, hardness: 0),
            }),
            Rock,
            explored: true).Value;
        Assert.True(world.SetDigDesignation(
            Target,
            designated: true,
            tick: 1).IsSuccess);
        world.DequeueUncommittedEvents();
        return world;
    }
}

}
