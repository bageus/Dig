using System;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Saving;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class HaulingTransitSaveContinuationTests
{
    private static readonly MaterialId RockId = new MaterialId("terrain.rock");
    private static readonly ItemId OreId = new ItemId("ore.iron");
    private static readonly EntityId ResidentId = Id(1);
    private static readonly EntityId SourceStackId = Id(2);
    private static readonly EntityId JobId = Id(3);
    private static readonly EntityId StorageId = Id(4);

    [Fact]
    public void Carried_haul_round_trip_continues_to_exactly_once_deposit()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(RockId, isSolid: true, hardness: 100),
        });
        ItemCatalog items = new ItemCatalog(new[]
        {
            new ItemDefinition(OreId, "Ore", 100, false),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(4, 4),
            chunkSize: 2,
            materials,
            RockId,
            explored: true).Value;
        InventoryState inventory = new InventoryState(items);
        Assert.True(inventory.AddStack(
            SourceStackId,
            OreId,
            quantity: 10,
            ItemLocation.InWorld(new CellId(1, 1)),
            tick: 0).IsSuccess);
        Assert.True(inventory.ReserveQuantity(
            SourceStackId,
            JobId,
            quantity: 4,
            tick: 1).IsSuccess);
        Assert.True(inventory.ReserveResidentSlotCapacity(
            JobId,
            ResidentId,
            OreId,
            quantity: 4,
            tick: 1).IsSuccess);
        JobSystem jobs = new JobSystem();
        Assert.True(jobs.Add(new HaulJobDefinition(
            JobId,
            SourceStackId,
            OreId,
            quantity: 4,
            StorageId,
            priority: 500,
            createdTick: 1,
            JobRetryPolicy.Default)).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, tick: 1).IsSuccess);
        Assert.True(jobs.Claim(JobId, ResidentId, tick: 2).IsSuccess);
        StorageState storage = new StorageState();
        Assert.True(storage.AddZone(new StorageZoneDefinition(
            StorageId,
            "Ore storage",
            priority: 500,
            capacity: 20,
            new StorageFilter(
                acceptsAll: false,
                allowedItems: new[] { OreId }),
            new CellId(2, 1))).IsSuccess);
        Assert.True(storage.ReserveIncoming(
            StorageId,
            JobId,
            items.Get(OreId),
            quantity: 4,
            occupiedQuantity: 0,
            tick: 2).IsSuccess);
        InMemoryInventoryRepository inventoryRepository =
            new InMemoryInventoryRepository(inventory);
        InMemoryJobRepository jobRepository = new InMemoryJobRepository(jobs);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        Assert.True(jobs.Start(JobId, tick: 3).IsSuccess);
        Result acquired = new AcquireHaulingItemHandler(
            inventoryRepository,
            jobRepository,
            journal).Handle(new AcquireHaulingItemCommand(
                JobId,
                Id(40),
                tick: 4));
        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());
        Assert.Equal(JobStageKind.TravelToDestination, jobs.Get(JobId)!.Stage);
        Assert.Equal(4, ReservedResidentOre(inventory));

        JobDefinitionSaveRegistry registry = Registry();
        SaveGameBuilder builder = new SaveGameBuilder(registry);
        Result<LoadedGameState> carriedLoad = Load(
            builder.Build(Context(world, inventory, jobs, storage, tick: 4)),
            registry,
            materials,
            items);

        Assert.True(carriedLoad.IsSuccess, carriedLoad.Error?.ToString());
        LoadedGameState carried = carriedLoad.Value;
        Assert.Equal(JobStageKind.TravelToDestination,
            carried.Jobs.Get(JobId)!.Stage);
        Assert.Equal(4, ReservedResidentOre(carried.Inventory));
        Assert.Equal(4, carried.Storage.GetReservation(JobId)!.Value.Quantity);

        InMemoryInventoryRepository restoredInventory =
            new InMemoryInventoryRepository(carried.Inventory);
        InMemoryStorageRepository restoredStorage =
            new InMemoryStorageRepository(carried.Storage);
        InMemoryJobRepository restoredJobs = new InMemoryJobRepository(carried.Jobs);
        Assert.True(new AdvanceJobHandler(restoredJobs, journal).Handle(
            new AdvanceJobCommand(JobId, tick: 6)).IsSuccess);
        CompleteHaulingJobHandler complete = new CompleteHaulingJobHandler(
            restoredInventory,
            restoredStorage,
            restoredJobs,
            journal,
            AgentSkillGrantTestFactory.Create(ResidentId, journal));
        Assert.True(complete.Handle(new CompleteHaulingJobCommand(
            JobId,
            Id(41),
            tick: 7)).IsSuccess);
        Assert.True(complete.Handle(new CompleteHaulingJobCommand(
            JobId,
            Id(41),
            tick: 8)).IsSuccess);
        AssertCompleted(carried);

        Result<LoadedGameState> completedLoad = Load(
            builder.Build(Context(
                carried.World,
                carried.Inventory,
                carried.Jobs,
                carried.Storage,
                tick: 8)),
            registry,
            materials,
            items);
        Assert.True(completedLoad.IsSuccess, completedLoad.Error?.ToString());
        LoadedGameState completed = completedLoad.Value;
        InMemoryExecutionJournal replayJournal = new InMemoryExecutionJournal();
        CompleteHaulingJobHandler replay = new CompleteHaulingJobHandler(
            new InMemoryInventoryRepository(completed.Inventory),
            new InMemoryStorageRepository(completed.Storage),
            new InMemoryJobRepository(completed.Jobs),
            replayJournal,
            AgentSkillGrantTestFactory.Create(ResidentId, replayJournal));

        Assert.True(replay.Handle(new CompleteHaulingJobCommand(
            JobId,
            Id(41),
            tick: 9)).IsSuccess);
        AssertCompleted(completed);
    }

    private static void AssertCompleted(LoadedGameState state)
    {
        Assert.Equal(JobStatus.Completed, state.Jobs.Get(JobId)!.Status);
        Assert.Equal(10, state.Inventory.GetTotal(OreId));
        Assert.Equal(4, state.Inventory.GetQuantityAt(
            OreId,
            ItemLocation.InStorage(StorageId)));
        Assert.Empty(state.Storage.GetReservations());
        Assert.Equal(0, ReservedResidentOre(state.Inventory));
    }

    private static int ReservedResidentOre(InventoryState inventory)
    {
        return inventory.CreateSnapshot().Stacks
            .Where(stack => stack.ItemId == OreId)
            .Where(stack => stack.Location.Kind == ItemLocationKind.AgentInventory)
            .Where(stack => stack.Location.OwnerId == ResidentId)
            .Sum(stack => stack.ReservedQuantity);
    }

    private static Result<LoadedGameState> Load(
        SaveGameDocument document,
        JobDefinitionSaveRegistry registry,
        MaterialCatalog materials,
        ItemCatalog items)
    {
        return new SaveGameLoader(
            new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
            registry).Load(document, materials, items);
    }

    private static SaveGameContext Context(
        WorldState world,
        InventoryState inventory,
        JobSystem jobs,
        StorageState storage,
        long tick)
    {
        return new SaveGameContext(
            new SaveMetadataData
            {
                SlotId = "hauling-transit",
                DisplayName = "Hauling transit",
                SavedAtUtc = "2026-07-19T09:00:00Z",
                SimulationTick = tick,
                WorldSeed = 42,
                GeneratorVersion = 1,
            },
            world,
            inventory,
            jobs,
            new BuildingsState(),
            Array.Empty<AgentState>(),
            storage: storage);
    }

    private static JobDefinitionSaveRegistry Registry()
    {
        return new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
        {
            new HaulJobDefinitionSaveCodec(),
        });
    }

    private static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}

}
