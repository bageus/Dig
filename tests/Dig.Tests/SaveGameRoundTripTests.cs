using System;
using System.IO;
using System.Linq;
using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class SaveGameRoundTripTests
{
    private static readonly MaterialId Rock = new MaterialId("terrain.rock");
    private static readonly MaterialId Air = new MaterialId("terrain.air");
    private static readonly ItemId Ore = new ItemId("ore.test");
    private static readonly EntityId StackId = Id("f1000000000000000000000000000001");
    private static readonly EntityId JobId = Id("f2000000000000000000000000000002");
    private static readonly EntityId WorkerId = Id("f3000000000000000000000000000003");
    private static readonly CellId Target = new CellId(1, 1);

    [Fact]
    public void Round_trip_restores_world_inventory_jobs_and_next_transition()
    {
        MaterialCatalog materials = CreateMaterials();
        ItemCatalog items = CreateItems();
        SaveGameContext context = CreateContext(materials, items, "manual-1");
        SaveGameBuilder builder = CreateBuilder();
        SaveGameDocument document = builder.Build(context);
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();
        byte[] firstBytes = codec.Serialize(document);
        SaveGameDocument decoded = codec.Deserialize(firstBytes);
        SaveGameLoader loader = CreateLoader();

        Result<LoadedGameState> loadedResult = loader.Load(decoded, materials, items);

        Assert.True(loadedResult.IsSuccess);
        LoadedGameState loaded = loadedResult.Value;
        Assert.Equal(context.Metadata.SimulationTick, loaded.Metadata.SimulationTick);
        AssertWorldEqual(context.World.CreateSnapshot(), loaded.World.CreateSnapshot());
        AssertInventoryEqual(
            context.Inventory.CreateSnapshot(),
            loaded.Inventory.CreateSnapshot());
        AssertJobsEqual(context.Jobs, loaded.Jobs);

        Assert.True(context.Jobs.Start(JobId, tick: 11).IsSuccess);
        Assert.True(loaded.Jobs.Start(JobId, tick: 11).IsSuccess);
        Assert.Equal(
            context.Jobs.Get(JobId)!.Stage,
            loaded.Jobs.Get(JobId)!.Stage);
        Assert.Equal(
            context.Jobs.Get(JobId)!.Status,
            loaded.Jobs.Get(JobId)!.Status);

        SaveGameDocument rebuilt = builder.Build(new SaveGameContext(
            loaded.Metadata,
            loaded.World,
            loaded.Inventory,
            loaded.Jobs));
        byte[] secondBytes = codec.Serialize(rebuilt);
        Assert.Equal(firstBytes, secondBytes);
    }

    [Fact]
    public void File_store_supports_manual_and_autosave_slots()
    {
        string directory = CreateTempDirectory();
        try
        {
            MaterialCatalog materials = CreateMaterials();
            ItemCatalog items = CreateItems();
            FileSaveSlotStore store = new FileSaveSlotStore(
                directory,
                new DataContractJsonSaveCodec());
            SaveGameService service = new SaveGameService(
                CreateBuilder(),
                CreateLoader(),
                store);
            SaveGameContext context = CreateContext(materials, items, "manual-1");

            service.Save(context);
            service.Autosave(context);
            var slots = service.ListSlots();

            Assert.Equal(2, slots.Count);
            Assert.Contains(slots, item => item.SlotId == "manual-1" && !item.IsCorrupted);
            Assert.Contains(slots, item => item.SlotId == SaveSlotNames.Autosave && !item.IsCorrupted);
            Assert.True(service.Load("manual-1", materials, items).IsSuccess);
            Assert.True(service.Load(SaveSlotNames.Autosave, materials, items).IsSuccess);
            Assert.False(File.Exists(Path.Combine(directory, "manual-1.digsave.tmp")));
            Assert.False(File.Exists(Path.Combine(directory, "manual-1.digsave.bak")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static SaveGameContext CreateContext(
        MaterialCatalog materials,
        ItemCatalog items,
        string slotId)
    {
        WorldState world = WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            Rock,
            explored: true).Value;
        Assert.True(world.SetDigDesignation(Target, designated: true, tick: 5).IsSuccess);

        InventoryState inventory = new InventoryState(items);
        Assert.True(inventory.AddStack(
            StackId,
            Ore,
            quantity: 10,
            ItemLocation.InWorld(new CellId(2, 1)),
            tick: 6).IsSuccess);
        Assert.True(inventory.ReserveQuantity(
            StackId,
            JobId,
            quantity: 3,
            tick: 7).IsSuccess);

        JobSystem jobs = new JobSystem();
        DigJobDefinition definition = new DigJobDefinition(
            JobId,
            new DigJobTarget(Target),
            priority: 20,
            createdTick: 7,
            new JobRetryPolicy(maximumRetries: 3, retryDelayTicks: 2));
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, tick: 7).IsSuccess);
        Assert.True(jobs.Claim(JobId, WorkerId, tick: 8).IsSuccess);

        return new SaveGameContext(
            new SaveMetadataData
            {
                SlotId = slotId,
                DisplayName = "Digging checkpoint",
                SavedAtUtc = "2026-07-15T15:00:00Z",
                SimulationTick = 10,
                WorldSeed = 42UL,
                GeneratorVersion = 1,
            },
            world,
            inventory,
            jobs);
    }

    private static SaveGameBuilder CreateBuilder()
    {
        return new SaveGameBuilder(new JobDefinitionSaveRegistry(new[]
        {
            new DigJobDefinitionSaveCodec(),
        }));
    }

    private static SaveGameLoader CreateLoader()
    {
        return new SaveGameLoader(
            new SaveMigrationPipeline(new ISaveMigration[]
            {
                new LegacySaveVersionZeroMigration(),
            }),
            new JobDefinitionSaveRegistry(new[]
            {
                new DigJobDefinitionSaveCodec(),
            }));
    }

    private static MaterialCatalog CreateMaterials()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
    }

    private static ItemCatalog CreateItems()
    {
        return new ItemCatalog(new[]
        {
            new ItemDefinition(Ore, "Test Ore", maximumStackSize: 100, isTool: false),
        });
    }

    private static void AssertWorldEqual(WorldSnapshot expected, WorldSnapshot actual)
    {
        Assert.Equal(expected.Size.Width, actual.Size.Width);
        Assert.Equal(expected.Size.Height, actual.Size.Height);
        Assert.Equal(expected.ChunkSize, actual.ChunkSize);
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.Chunks.Count, actual.Chunks.Count);
        for (int index = 0; index < expected.Chunks.Count; index++)
        {
            Assert.Equal(expected.Chunks[index].Id, actual.Chunks[index].Id);
            Assert.Equal(expected.Chunks[index].ChunkVersion, actual.Chunks[index].ChunkVersion);
            Assert.Equal(
                expected.Chunks[index].Cells.Select(item => item.State).ToArray(),
                actual.Chunks[index].Cells.Select(item => item.State).ToArray());
        }
    }

    private static void AssertInventoryEqual(
        InventorySnapshot expected,
        InventorySnapshot actual)
    {
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.Stacks.Count, actual.Stacks.Count);
        Assert.Equal(expected.Stacks[0].StackId, actual.Stacks[0].StackId);
        Assert.Equal(expected.Stacks[0].ItemId, actual.Stacks[0].ItemId);
        Assert.Equal(expected.Stacks[0].Quantity, actual.Stacks[0].Quantity);
        Assert.Equal(expected.Stacks[0].Location, actual.Stacks[0].Location);
        Assert.Equal(
            expected.Stacks[0].Reservations.Select(item => item.JobId).ToArray(),
            actual.Stacks[0].Reservations.Select(item => item.JobId).ToArray());
        Assert.Equal(
            expected.Stacks[0].ReservedQuantity,
            actual.Stacks[0].ReservedQuantity);
    }

    private static void AssertJobsEqual(JobSystem expected, JobSystem actual)
    {
        JobSnapshot expectedJob = expected.Get(JobId)!;
        JobSnapshot actualJob = actual.Get(JobId)!;
        Assert.Equal(expectedJob.Status, actualJob.Status);
        Assert.Equal(expectedJob.Stage, actualJob.Stage);
        Assert.Equal(expectedJob.AssignedAgentId, actualJob.AssignedAgentId);
        Assert.Equal(expectedJob.RetryCount, actualJob.RetryCount);
        Assert.Equal(expectedJob.NextRetryTick, actualJob.NextRetryTick);
        Assert.Equal(expectedJob.Version, actualJob.Version);
        Assert.Equal(
            expected.GetReservations().Select(item => item.Key).ToArray(),
            actual.GetReservations().Select(item => item.Key).ToArray());
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "dig-save-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static EntityId Id(string value)
    {
        return EntityId.Parse(value);
    }
}
}
