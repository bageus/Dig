using System;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainWorkMultiOutputCompletionTests
{
    private static readonly MaterialId Rock = new MaterialId("terrain.multi");
    private static readonly MaterialId Air = new MaterialId("terrain.air");
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly ItemId IronOre = new ItemId("ore.iron");
    private static readonly CellId Target = new CellId(3, 1, 2);
    private static readonly EntityId JobId = Id("79000000000000000000000000000001");
    private static readonly EntityId WorkerId = Id("79000000000000000000000000000002");
    private static readonly EntityId OutputBase = Id("79000000000000000000000000000100");

    [Fact]
    public void Completion_atomically_creates_multi_output_world_units_and_ledger()
    {
        WorldState world = CreateWorld();
        InventoryState inventory = CreateInventory();
        JobSystem jobs = CreateFinalizingJob();
        MiningOutputCommitState commits = new MiningOutputCommitState();
        MiningOutputPlan plan = ResolvePlan(world);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        CompleteTerrainWorkCommandHandler handler = CreateHandler(
            world,
            inventory,
            jobs,
            commits,
            journal);

        Result<TerrainWorkCompletionResult> result = handler.Handle(
            CompleteTerrainWorkCommand.FromPlan(
                JobId,
                OutputBase,
                plan,
                Air,
                tick: 10));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.False(world.GetCell(Target).Value.IsSolid);
        Assert.Equal(JobStatus.Completed, jobs.Get(JobId)!.Status);
        Assert.Equal(2, result.Value.Outputs.Count);
        Assert.Equal(3, result.Value.TotalOutputQuantity);
        Assert.Equal(3, inventory.CreateSnapshot().Stacks.Count);
        Assert.All(inventory.CreateSnapshot().Stacks, stack =>
        {
            Assert.Equal(1, stack.Quantity);
            Assert.Equal(ItemLocation.InWorld(Target), stack.Location);
        });
        Assert.Equal(2, inventory.GetTotal(Stone));
        Assert.Equal(1, inventory.GetTotal(IronOre));

        MiningOutputCommit commit = Assert.Single(commits.Snapshot());
        Assert.Equal(Target, commit.Cell);
        Assert.Equal(plan.SourceId, commit.SourceId);
        Assert.Equal(plan.SourceVersion, commit.SourceVersion);
        Assert.Equal(2, commit.Outputs.Count);
        Assert.Equal(3, commit.StackIds.Count);
    }

    [Fact]
    public void Conflicting_derived_unit_id_rejects_entire_batch_before_world_commit()
    {
        WorldState world = CreateWorld();
        InventoryState inventory = CreateInventory();
        JobSystem jobs = CreateFinalizingJob();
        MiningOutputCommitState commits = new MiningOutputCommitState();
        MiningOutputPlan plan = ResolvePlan(world);
        EntityId conflictingSecondId = Id("79000000000000000000000000000101");
        Assert.True(inventory.AddUnit(
            conflictingSecondId,
            Stone,
            ItemLocation.InWorld(new CellId(1, 1, 0)),
            tick: 1).IsSuccess);
        long worldVersion = world.Version;
        long inventoryVersion = inventory.Version;
        long jobVersion = jobs.Get(JobId)!.Version;
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();

        Result<TerrainWorkCompletionResult> result = CreateHandler(
            world,
            inventory,
            jobs,
            commits,
            journal).Handle(CompleteTerrainWorkCommand.FromPlan(
                JobId,
                OutputBase,
                plan,
                Air,
                tick: 10));

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.StackAlreadyExists, result.Error);
        Assert.Equal(worldVersion, world.Version);
        Assert.Equal(inventoryVersion, inventory.Version);
        Assert.Equal(jobVersion, jobs.Get(JobId)!.Version);
        Assert.True(world.GetCell(Target).Value.IsSolid);
        Assert.Empty(commits.Snapshot());
        Assert.Empty(journal.Events);
    }

    private static CompleteTerrainWorkCommandHandler CreateHandler(
        WorldState world,
        InventoryState inventory,
        JobSystem jobs,
        MiningOutputCommitState commits,
        InMemoryExecutionJournal journal)
    {
        return new CompleteTerrainWorkCommandHandler(
            new InMemoryJobRepository(jobs),
            new InMemoryWorldRepository(world),
            new InMemoryInventoryRepository(inventory),
            journal,
            AgentSkillGrantTestFactory.Create(WorkerId, journal),
            commits);
    }

    private static MiningOutputPlan ResolvePlan(WorldState world)
    {
        return new MiningOutputResolver().Resolve(
            worldSeed: 31,
            generatorVersion: 2,
            Target,
            world.Materials.Get(Rock)!,
            world.TerrainDeposits);
    }

    private static WorldState CreateWorld()
    {
        TerrainOutputProfile profile = new TerrainOutputProfile(
            "terrain-output.multi",
            version: 3,
            new[]
            {
                new TerrainOutputEntry(Stone, 1_000, 2, 2),
                new TerrainOutputEntry(IronOre, 1_000, 1, 1),
            });
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, "Multi rock", true, 100, true, profile),
            new MaterialDefinition(Air, "Air", false, 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(6, 4, 4),
            chunkSize: 2,
            materials,
            Rock,
            explored: true).Value;
        Assert.True(world.SetDigDesignation(Target, true, tick: 1).IsSuccess);
        world.DequeueUncommittedEvents();
        return world;
    }

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Stone, "Stone", 20, false),
            new ItemDefinition(IronOre, "Iron ore", 20, false),
        }));
    }

    private static JobSystem CreateFinalizingJob()
    {
        JobSystem jobs = new JobSystem();
        Assert.True(jobs.Add(new DigJobDefinition(
            JobId,
            new DigJobTarget(Target),
            1,
            0,
            JobRetryPolicy.Default)).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, 1).IsSuccess);
        Assert.True(jobs.Claim(JobId, WorkerId, 2).IsSuccess);
        Assert.True(jobs.Start(JobId, 3).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, 4).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, 5).IsSuccess);
        return jobs;
    }

    private static EntityId Id(string value) => EntityId.Parse(value);
}

}
