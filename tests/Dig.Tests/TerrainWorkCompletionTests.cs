using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Inventory;
using Xunit;

namespace Dig.Tests
{

public sealed class TerrainWorkCompletionTests
{
    private static readonly MaterialId Rock = new MaterialId("test.rock");
    private static readonly MaterialId Air = new MaterialId("test.air");
    private static readonly ItemId RockItem = new ItemId("test.rock.chunk");
    private static readonly CellId Target = new CellId(3, 1);

    [Fact]
    public void Completion_updates_world_inventory_and_job_once()
    {
        WorldState world = CreateWorld();
        InventoryState inventory = CreateInventory();
        JobSystem jobs = CreateFinalizingJob();
        EntityId outputId = Id("50000000000000000000000000000001");
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        CompleteTerrainWorkCommandHandler handler = CreateHandler(
            jobs,
            world,
            inventory,
            journal);

        Result<TerrainWorkCompletionResult> result = handler.Handle(
            new CompleteTerrainWorkCommand(
                JobId,
                outputId,
                RockItem,
                outputQuantity: 12,
                Air,
                tick: 10));

        Assert.True(result.IsSuccess);
        CellSnapshot cell = world.GetCell(Target).Value;
        Assert.False(cell.IsSolid);
        Assert.Equal(CellDesignation.None, cell.State.Designation);
        ItemStackSnapshot stack = Assert.IsType<ItemStackSnapshot>(
            inventory.GetStack(outputId));
        Assert.Equal(12, stack.Quantity);
        Assert.Equal(ItemLocation.InWorld(Target), stack.Location);
        Assert.Equal(JobStatus.Completed, jobs.Get(JobId)!.Status);
        Assert.Empty(jobs.GetReservations());
        Assert.Contains(journal.Events, item => item is CellChanged);
        Assert.Contains(journal.Events, item => item is JobStatusChanged);
    }

    [Fact]
    public void Rejected_output_preserves_all_authoritative_state()
    {
        WorldState world = CreateWorld();
        InventoryState inventory = CreateInventory();
        JobSystem jobs = CreateFinalizingJob();
        EntityId outputId = Id("50000000000000000000000000000002");
        Assert.True(inventory.AddStack(
            outputId,
            RockItem,
            1,
            ItemLocation.InWorld(new CellId(1, 1)),
            tick: 1).IsSuccess);
        long worldVersion = world.Version;
        long inventoryVersion = inventory.Version;
        long jobVersion = jobs.Get(JobId)!.Version;
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        CompleteTerrainWorkCommandHandler handler = CreateHandler(
            jobs,
            world,
            inventory,
            journal);

        Result<TerrainWorkCompletionResult> result = handler.Handle(
            new CompleteTerrainWorkCommand(
                JobId,
                outputId,
                RockItem,
                outputQuantity: 12,
                Air,
                tick: 10));

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.StackAlreadyExists, result.Error);
        Assert.Equal(worldVersion, world.Version);
        Assert.Equal(inventoryVersion, inventory.Version);
        Assert.Equal(jobVersion, jobs.Get(JobId)!.Version);
        Assert.True(world.GetCell(Target).Value.IsSolid);
        Assert.Equal(JobStatus.InProgress, jobs.Get(JobId)!.Status);
        Assert.Empty(journal.Events);
    }

    [Fact]
    public void Route_planner_selects_lowest_cost_adjacent_work_cell()
    {
        WorldState world = CreateWorld();
        Assert.True(world.ApplyTerrainChanges(
            new[]
            {
                Empty(new CellId(1, 1)),
                Empty(new CellId(2, 1)),
                Empty(new CellId(3, 2)),
            },
            tick: 2).IsSuccess);
        NavigationMap map = new NavigationMap(TraversalProfile.CreateGroundedDwarf());
        Assert.True(map.Rebuild(
            world.CreateSnapshot(),
            Array.Empty<TraversalLink>()).IsSuccess);
        NavigationSnapshot navigation = map.GetSnapshot().Value;
        JobSystem jobs = CreateFinalizingJob();
        TerrainWorkRoutePlanner planner = new TerrainWorkRoutePlanner(
            new NavigationPathfinder());

        Result<TerrainWorkRoutePlan> result = planner.Plan(
            jobs.Get(JobId)!,
            new CellId(1, 1),
            navigation);

        Assert.True(result.IsSuccess);
        TerrainWorkRoutePlan plan = result.Value;
        Assert.True(plan.Succeeded);
        Assert.Equal(new CellId(2, 1), plan.WorkCell);
        Assert.Equal(
            new[] { new CellId(1, 1), new CellId(2, 1) },
            plan.PathResult.Path!.Cells);
    }

    [Fact]
    public void Inventory_presenter_returns_only_stable_world_items()
    {
        InventoryState inventory = CreateInventory();
        EntityId later = Id("60000000000000000000000000000002");
        EntityId earlier = Id("60000000000000000000000000000001");
        Assert.True(inventory.AddStack(
            later,
            RockItem,
            4,
            ItemLocation.InWorld(new CellId(4, 1)),
            tick: 1).IsSuccess);
        Assert.True(inventory.AddStack(
            earlier,
            RockItem,
            3,
            ItemLocation.InWorld(new CellId(2, 1)),
            tick: 1).IsSuccess);
        Assert.True(inventory.AddStack(
            Id("60000000000000000000000000000003"),
            RockItem,
            2,
            ItemLocation.InAgent(Id("10000000000000000000000000000001")),
            tick: 1).IsSuccess);
        InMemoryInventoryRepository repository = new InMemoryInventoryRepository(inventory);
        InventoryWorldPresenter presenter = new InventoryWorldPresenter(
            new GetInventorySnapshotQueryHandler(repository));

        var items = presenter.Load();

        Assert.Equal(new[] { earlier.ToString(), later.ToString() },
            items.Select(item => item.StackId));
        Assert.All(items, item => Assert.Equal(1, item.CellY));
    }

    private static EntityId JobId => Id("40000000000000000000000000000001");

    private static CompleteTerrainWorkCommandHandler CreateHandler(
        JobSystem jobs,
        WorldState world,
        InventoryState inventory,
        InMemoryExecutionJournal journal)
    {
        return new CompleteTerrainWorkCommandHandler(
            new InMemoryJobRepository(jobs),
            new InMemoryWorldRepository(world),
            new InMemoryInventoryRepository(inventory),
            journal);
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
            Id("10000000000000000000000000000001"),
            tick: 1).IsSuccess);
        Assert.True(jobs.Start(JobId, tick: 2).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 3).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 4).IsSuccess);
        Assert.Equal(JobStageKind.Finalize, jobs.Get(JobId)!.Stage);
        return jobs;
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

    private static InventoryState CreateInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(
                RockItem,
                "Rock chunk",
                maximumStackSize: 100,
                isTool: false),
        }));
    }

    private static TerrainChange Empty(CellId cellId)
    {
        return new TerrainChange(
            cellId,
            new CellState(
                Air,
                CellDesignation.None,
                isExplored: true,
                damage: 0,
                temperature: 20));
    }

    private static EntityId Id(string value)
    {
        return EntityId.Parse(value);
    }
}

}
