using System;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainWorkCompletionNoOutputTests
{
    private static readonly MaterialId Rock = new MaterialId("test.rock");
    private static readonly MaterialId Air = new MaterialId("test.air");
    private static readonly CellId Target = new CellId(3, 1);
    private static readonly EntityId JobId =
        EntityId.Parse("40000000000000000000000000000009");

    [Fact]
    public void Ordinary_rock_completes_without_creating_an_inventory_stack()
    {
        WorldState world = CreateWorld();
        InventoryState inventory = new InventoryState(
            new ItemCatalog(Array.Empty<ItemDefinition>()));
        JobSystem jobs = CreateFinalizingJob();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        CompleteTerrainWorkCommandHandler handler = new CompleteTerrainWorkCommandHandler(
            new InMemoryJobRepository(jobs),
            new InMemoryWorldRepository(world),
            new InMemoryInventoryRepository(inventory),
            journal);
        long inventoryVersion = inventory.Version;

        Result<TerrainWorkCompletionResult> result = handler.Handle(
            CompleteTerrainWorkCommand.WithoutOutput(JobId, Air, tick: 10));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.ProducedOutput);
        Assert.Equal(0, result.Value.OutputQuantity);
        Assert.False(world.GetCell(Target).Value.IsSolid);
        Assert.Equal(inventoryVersion, inventory.Version);
        Assert.Empty(inventory.CreateSnapshot().Stacks);
        Assert.Equal(JobStatus.Completed, jobs.Get(JobId)!.Status);
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(6, 4),
            chunkSize: 2,
            materials,
            Rock,
            explored: true).Value;
        Assert.True(world.SetDigDesignation(Target, designated: true, tick: 1).IsSuccess);
        world.DequeueUncommittedEvents();
        return world;
    }

    private static JobSystem CreateFinalizingJob()
    {
        JobSystem jobs = new JobSystem();
        DigJobDefinition definition = new DigJobDefinition(
            JobId,
            new DigJobTarget(Target),
            priority: 500,
            createdTick: 0,
            JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, tick: 0).IsSuccess);
        Assert.True(jobs.Claim(
            JobId,
            EntityId.Parse("10000000000000000000000000000009"),
            tick: 1).IsSuccess);
        Assert.True(jobs.Start(JobId, tick: 2).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 3).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 4).IsSuccess);
        return jobs;
    }
}

}