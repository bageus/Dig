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

public sealed class TerrainDepositCompletionIntegrationTests
{
    private static readonly MaterialId Rock = new MaterialId("deposit-work.rock");
    private static readonly MaterialId Air = new MaterialId("deposit-work.air");
    private static readonly ItemId IronOre = new ItemId("ore.iron");
    private static readonly CellId Target = new CellId(3, 1, 2);
    private static readonly EntityId JobId =
        EntityId.Parse("73000000000000000000000000000001");
    private static readonly EntityId WorkerId =
        EntityId.Parse("73000000000000000000000000000002");
    private static readonly EntityId OutputId =
        EntityId.Parse("73000000000000000000000000000003");

    [Fact]
    public void Finalize_commits_deposit_output_depletion_and_open_world_once()
    {
        WorldState world = CreateWorld();
        TerrainDepositDefinition definition = new TerrainDepositDefinition(
            "deposit.iron_ore",
            "Iron ore",
            IronOre,
            maximumYield: 3,
            generationWeight: 1,
            allowedHostMaterialIds: new[] { Rock });
        world.ReplaceTerrainDeposits(new[]
        {
            new TerrainDepositInstance(
                "deposit-instance",
                Target,
                definition,
                isRevealed: true,
                remainingYield: 3,
                version: 5),
        }, generatorVersion: 4);
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                IronOre,
                "Iron ore",
                maximumStackSize: 20,
                isTool: false),
        }));
        JobSystem jobs = CreateFinalizingJob();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        MiningOutputPlan plan = new MiningOutputResolver().Resolve(
            worldSeed: 9,
            generatorVersion: 4,
            Target,
            world.Materials.Get(Rock)!,
            world.TerrainDeposits);
        CompleteTerrainWorkCommandHandler handler =
            new CompleteTerrainWorkCommandHandler(
                new InMemoryJobRepository(jobs),
                new InMemoryWorldRepository(world),
                new InMemoryInventoryRepository(inventory),
                journal,
                AgentSkillGrantTestFactory.Create(WorkerId, journal));

        Result<TerrainWorkCompletionResult> result = handler.Handle(
            new CompleteTerrainWorkCommand(
                JobId,
                OutputId,
                plan.ItemId,
                plan.Quantity,
                Air,
                tick: 10,
                plan.DepositInstanceId,
                plan.Quantity));

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.False(world.GetCell(Target).Value.IsSolid);
        Assert.True(world.TerrainDeposits.TryGet(
            Target,
            out TerrainDepositInstance depleted));
        Assert.True(depleted.IsDepleted);
        Assert.Equal(3, inventory.GetTotal(IronOre));
        Assert.Equal(
            3,
            inventory.CreateSnapshot().Stacks.Count(value =>
                value.ItemId == IronOre
                && value.Location == ItemLocation.InWorld(Target)));
        Assert.Equal(JobStatus.Completed, jobs.Get(JobId)!.Status);
        Assert.Contains(journal.Events, value => value is TerrainDepositDepleted);
    }

    [Fact]
    public void Stale_deposit_plan_fails_before_inventory_world_or_job_changes()
    {
        WorldState world = CreateWorld();
        TerrainDepositDefinition definition = new TerrainDepositDefinition(
            "deposit.iron_ore",
            "Iron ore",
            IronOre,
            maximumYield: 3,
            generationWeight: 1);
        world.ReplaceTerrainDeposits(new[]
        {
            new TerrainDepositInstance(
                "current",
                Target,
                definition,
                isRevealed: true,
                remainingYield: 3,
                version: 1),
        }, generatorVersion: 1);
        InventoryState inventory = new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(IronOre, "Iron ore", 20, isTool: false),
        }));
        JobSystem jobs = CreateFinalizingJob();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        CompleteTerrainWorkCommandHandler handler =
            new CompleteTerrainWorkCommandHandler(
                new InMemoryJobRepository(jobs),
                new InMemoryWorldRepository(world),
                new InMemoryInventoryRepository(inventory),
                journal,
                AgentSkillGrantTestFactory.Create(WorkerId, journal));
        long worldVersion = world.Version;

        Result<TerrainWorkCompletionResult> result = handler.Handle(
            new CompleteTerrainWorkCommand(
                JobId,
                OutputId,
                IronOre,
                outputQuantity: 3,
                Air,
                tick: 10,
                depositInstanceId: "stale",
                depositExpectedYield: 3));

        Assert.Equal(WorldErrors.TerrainDepositStale, result.Error);
        Assert.Equal(worldVersion, world.Version);
        Assert.True(world.GetCell(Target).Value.IsSolid);
        Assert.Equal(0, inventory.GetTotal(IronOre));
        Assert.Equal(JobStatus.InProgress, jobs.Get(JobId)!.Status);
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(6, 4, 4),
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
        Assert.True(jobs.Add(new DigJobDefinition(
            JobId,
            new DigJobTarget(Target),
            priority: 1,
            createdTick: 0,
            JobRetryPolicy.Default)).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, tick: 1).IsSuccess);
        Assert.True(jobs.Claim(JobId, WorkerId, tick: 2).IsSuccess);
        Assert.True(jobs.Start(JobId, tick: 3).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 4).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 5).IsSuccess);
        return jobs;
    }
}

}
