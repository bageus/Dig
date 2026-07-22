using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{
public sealed class EraseExcavationBatchTests
{
    private static readonly CellId First = new CellId(1, 1);
    private static readonly CellId Second = new CellId(2, 1);
    private static readonly CellId Completed = new CellId(3, 1);

    [Fact]
    public void Batch_removes_designations_and_cancels_jobs_with_reservations_once()
    {
        WorldState world = CreateWorld(out MaterialId air);
        Assert.True(world.SetDigDesignation(First, true, 1).IsSuccess);
        Assert.True(world.SetDigDesignation(Second, true, 2).IsSuccess);
        Assert.True(world.Excavate(Completed, air, 3).IsSuccess);
        InMemoryWorldRepository worlds = new InMemoryWorldRepository(world);
        InMemoryJobRepository jobs = CreateClaimedJobs();
        long previousVersion = world.Version;
        EraseExcavationBatchHandler handler = new EraseExcavationBatchHandler(
            worlds,
            jobs,
            new InMemoryExecutionJournal());

        Result<EraseExcavationBatchReport> result = handler.Handle(
            new EraseExcavationBatchCommand(
                new[] { Second, First, First, Completed },
                tick: 5));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.DesignationCount);
        Assert.Equal(2, result.Value.CancelledJobIds.Count);
        Assert.Equal(previousVersion + 1, worlds.Get().Version);
        Assert.Equal(CellDesignation.None, worlds.Get().GetCell(First).Value.State.Designation);
        Assert.Equal(CellDesignation.None, worlds.Get().GetCell(Second).Value.State.Designation);
        Assert.False(worlds.Get().GetCell(Completed).Value.IsSolid);
        Assert.All(jobs.Get().GetAll(), job => Assert.Equal(JobStatus.Cancelled, job.Status));
        Assert.Empty(jobs.Get().GetReservations());
    }

    [Fact]
    public void Invalid_cell_rejects_entire_batch_without_world_or_job_changes()
    {
        WorldState world = CreateWorld(out _);
        Assert.True(world.SetDigDesignation(First, true, 1).IsSuccess);
        InMemoryWorldRepository worlds = new InMemoryWorldRepository(world);
        InMemoryJobRepository jobs = CreateClaimedJobs(firstOnly: true);
        long previousVersion = world.Version;
        EraseExcavationBatchHandler handler = new EraseExcavationBatchHandler(
            worlds,
            jobs,
            new InMemoryExecutionJournal());

        Result<EraseExcavationBatchReport> result = handler.Handle(
            new EraseExcavationBatchCommand(
                new[] { First, new CellId(99, 99) },
                tick: 5));

        Assert.True(result.IsFailure);
        Assert.Equal(WorldErrors.CellOutOfBounds, result.Error);
        Assert.Equal(previousVersion, worlds.Get().Version);
        Assert.Equal(CellDesignation.Dig, worlds.Get().GetCell(First).Value.State.Designation);
        Assert.Equal(JobStatus.Claimed, jobs.Get().GetAll().Single().Status);
        Assert.NotEmpty(jobs.Get().GetReservations());
    }

    private static InMemoryJobRepository CreateClaimedJobs(bool firstOnly = false)
    {
        InMemoryJobRepository repository = new InMemoryJobRepository();
        AddAndClaim(repository.Get(), Id(1), First, Id(101), tick: 3);
        if (!firstOnly)
        {
            AddAndClaim(repository.Get(), Id(2), Second, Id(102), tick: 4);
        }

        return repository;
    }

    private static void AddAndClaim(
        JobSystem jobs,
        EntityId jobId,
        CellId cell,
        EntityId residentId,
        long tick)
    {
        Assert.True(jobs.Add(new DigJobDefinition(
            jobId,
            new DigJobTarget(cell),
            priority: 750,
            createdTick: tick,
            JobRetryPolicy.Default)).IsSuccess);
        Assert.True(jobs.MakeAvailable(jobId, tick).IsSuccess);
        Assert.True(jobs.Claim(jobId, residentId, tick).IsSuccess);
    }

    private static WorldState CreateWorld(out MaterialId air)
    {
        MaterialId rock = new MaterialId("test.rock");
        air = new MaterialId("test.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, true, 100),
            new MaterialDefinition(air, false, 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(6, 4),
            chunkSize: 2,
            materials,
            rock,
            explored: true).Value;
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
