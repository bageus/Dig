using System;
using System.IO;
using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class SaveMigrationAndCorruptionTests
{
    private static readonly MaterialId Rock = new MaterialId("terrain.rock");
    private static readonly ItemId Ore = new ItemId("ore.test");
    private static readonly EntityId StackId = Id("fa000000000000000000000000000001");
    private static readonly EntityId JobId = Id("fb000000000000000000000000000002");

    [Fact]
    public void Version_zero_fixture_migrates_in_order_and_is_idempotent()
    {
        string fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "save-v0.json");
        SaveGameDocument document = new DataContractJsonSaveCodec().Deserialize(
            File.ReadAllBytes(fixturePath));
        SaveMigrationPipeline pipeline = CreateMigrations();

        Result<SaveMigrationReport> first = pipeline.Apply(document);
        Result<SaveMigrationReport> replay = pipeline.Apply(document);

        Assert.True(first.IsSuccess);
        Assert.Equal(new[]
        {
            "save.v0_to_v1.metadata",
            "save.v1_to_v2.buildings",
            "save.v2_to_v3.packing",
            "save.v3_to_v4.agent_skills",
            "save.v4_to_v5.authoritative_xyz",
        }, first.Value.AppliedSteps);
        Assert.Equal(SaveFormat.CurrentVersion, document.FormatVersion);
        Assert.Equal(1, document.Metadata.GeneratorVersion);
        Assert.Equal("legacy", document.Metadata.DisplayName);
        Assert.NotNull(document.Buildings);
        Assert.Empty(document.Buildings.Buildings);
        Assert.Equal(4, document.World.Depth);
        Assert.NotNull(document.AgentPositions);
        Assert.Empty(document.AgentPositions.Agents);
        Assert.True(replay.IsSuccess);
        Assert.Empty(replay.Value.AppliedSteps);
    }

    [Fact]
    public void Version_three_fixture_adds_empty_agent_skills_once()
    {
        string fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "save-v3.json");
        SaveGameDocument document = new DataContractJsonSaveCodec().Deserialize(
            File.ReadAllBytes(fixturePath));
        SaveMigrationPipeline pipeline = CreateMigrations();

        Result<SaveMigrationReport> first = pipeline.Apply(document);
        Result<SaveMigrationReport> replay = pipeline.Apply(document);

        Assert.True(first.IsSuccess);
        Assert.Equal(
            new[] { "save.v3_to_v4.agent_skills", "save.v4_to_v5.authoritative_xyz" },
            first.Value.AppliedSteps);
        Assert.Equal(SaveFormat.CurrentVersion, document.FormatVersion);
        Assert.NotNull(document.AgentSkills);
        Assert.Empty(document.AgentSkills.Agents);
        Assert.Equal(4, document.World.Depth);
        Assert.NotNull(document.AgentPositions);
        Assert.Empty(document.AgentPositions.Agents);
        Assert.True(replay.IsSuccess);
        Assert.Empty(replay.Value.AppliedSteps);
    }

    [Fact]
    public void Future_version_is_rejected_without_mutation()
    {
        SaveGameDocument document = MinimalDocument("future");
        document.FormatVersion = SaveFormat.CurrentVersion + 1;

        Result<SaveMigrationReport> result = CreateMigrations().Apply(document);

        Assert.True(result.IsFailure);
        Assert.Equal(SaveErrors.UnsupportedVersion, result.Error);
        Assert.Equal(SaveFormat.CurrentVersion + 1, document.FormatVersion);
    }

    [Fact]
    public void Unknown_job_type_and_item_id_are_reported_explicitly()
    {
        MaterialCatalog materials = CreateMaterials();
        ItemCatalog items = CreateItems();
        SaveGameDocument unknownJob = CreateValidDocument(materials, items);
        unknownJob.Jobs.Jobs[0].Definition.TypeId = "job.unknown.v99";
        SaveGameDocument unknownItem = CreateValidDocument(materials, items);
        unknownItem.Inventory.Stacks[0].ItemId = "item.missing";
        SaveGameLoader loader = CreateLoader();

        Result<LoadedGameState> jobResult = loader.Load(unknownJob, materials, items);
        Result<LoadedGameState> itemResult = loader.Load(unknownItem, materials, items);

        Assert.True(jobResult.IsFailure);
        Assert.Equal(SaveErrors.UnknownJobType, jobResult.Error);
        Assert.True(itemResult.IsFailure);
        Assert.Equal("inventory.restore.unknown_item", itemResult.Error!.Code);
    }

    [Fact]
    public void Dangling_inventory_reservation_is_rejected()
    {
        MaterialCatalog materials = CreateMaterials();
        ItemCatalog items = CreateItems();
        SaveGameDocument document = CreateValidDocument(materials, items);
        document.Inventory.Stacks[0].Reservations.Add(new ItemReservationSaveData
        {
            JobId = "fc000000000000000000000000000003",
            Quantity = 1,
        });

        Result<LoadedGameState> result = CreateLoader().Load(document, materials, items);

        Assert.True(result.IsFailure);
        Assert.Equal(SaveErrors.InvalidDocument, result.Error);
    }

    [Fact]
    public void Corrupted_slot_is_visible_and_load_fails_with_controlled_reason()
    {
        string directory = CreateTempDirectory();
        try
        {
            FileSaveSlotStore store = new FileSaveSlotStore(
                directory,
                new DataContractJsonSaveCodec());
            store.Save("broken", MinimalDocument("broken"));
            File.WriteAllText(
                Path.Combine(directory, "broken.digsave"),
                "{not valid json");

            SaveSlotInfo slot = Assert.Single(store.List());
            SaveStorageException exception = Assert.Throws<SaveStorageException>(
                () => store.Load("broken"));

            Assert.True(slot.IsCorrupted);
            Assert.Null(slot.Metadata);
            Assert.Equal(SaveStorageFailureKind.Corrupted, exception.Kind);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Interrupted_atomic_replacement_recovers_previous_slot()
    {
        string directory = CreateTempDirectory();
        try
        {
            FileSaveSlotStore store = new FileSaveSlotStore(
                directory,
                new DataContractJsonSaveCodec());
            SaveGameDocument original = MinimalDocument("checkpoint");
            original.Metadata.DisplayName = "Previous complete save";
            store.Save("checkpoint", original);
            string target = Path.Combine(directory, "checkpoint.digsave");
            File.Move(target, target + ".bak");
            File.WriteAllText(target + ".tmp", "partial write");

            SaveGameDocument recovered = store.Load("checkpoint");

            Assert.Equal("Previous complete save", recovered.Metadata.DisplayName);
            Assert.True(File.Exists(target));
            Assert.False(File.Exists(target + ".tmp"));
            Assert.False(File.Exists(target + ".bak"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Overwrite_commits_new_document_without_temp_or_backup_files()
    {
        string directory = CreateTempDirectory();
        try
        {
            FileSaveSlotStore store = new FileSaveSlotStore(
                directory,
                new DataContractJsonSaveCodec());
            SaveGameDocument first = MinimalDocument("slot-1");
            first.Metadata.DisplayName = "First";
            SaveGameDocument second = MinimalDocument("slot-1");
            second.Metadata.DisplayName = "Second";

            store.Save("slot-1", first);
            store.Save("slot-1", second);
            SaveGameDocument loaded = store.Load("slot-1");

            Assert.Equal("Second", loaded.Metadata.DisplayName);
            Assert.False(File.Exists(Path.Combine(directory, "slot-1.digsave.tmp")));
            Assert.False(File.Exists(Path.Combine(directory, "slot-1.digsave.bak")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static SaveGameDocument CreateValidDocument(
        MaterialCatalog materials,
        ItemCatalog items)
    {
        WorldState world = WorldState.CreateFilled(
            new WorldSize(4, 4),
            chunkSize: 2,
            materials,
            Rock,
            explored: true).Value;
        InventoryState inventory = new InventoryState(items);
        inventory.AddStack(
            StackId,
            Ore,
            quantity: 5,
            ItemLocation.InWorld(new CellId(1, 1)),
            tick: 0);
        JobSystem jobs = new JobSystem();
        jobs.Add(new DigJobDefinition(
            JobId,
            new DigJobTarget(new CellId(2, 2)),
            priority: 1,
            createdTick: 0,
            JobRetryPolicy.Default));
        return new SaveGameBuilder(CreateRegistry()).Build(new SaveGameContext(
            Metadata("valid"),
            world,
            inventory,
            jobs));
    }

    private static SaveGameDocument MinimalDocument(string slotId)
    {
        return new SaveGameDocument
        {
            FormatVersion = SaveFormat.CurrentVersion,
            Metadata = Metadata(slotId),
            World = new WorldSaveData(),
            Inventory = new InventorySaveData(),
            Jobs = new JobsSaveData(),
        };
    }

    private static SaveMetadataData Metadata(string slotId)
    {
        return new SaveMetadataData
        {
            SlotId = slotId,
            DisplayName = slotId,
            SavedAtUtc = "2026-07-15T15:00:00Z",
            SimulationTick = 0,
            WorldSeed = 1,
            GeneratorVersion = 1,
        };
    }

    private static SaveGameLoader CreateLoader()
    {
        return new SaveGameLoader(CreateMigrations(), CreateRegistry());
    }

    private static SaveMigrationPipeline CreateMigrations()
    {
        return new SaveMigrationPipeline(new ISaveMigration[]
        {
            new LegacySaveVersionZeroMigration(),
            new SaveVersionOneBuildingsMigration(),
            new SaveVersionTwoPackingMigration(),
            new SaveVersionThreeAgentSkillsMigration(),
            new SaveVersionFourAuthoritativeCoordinatesMigration(),
        });
    }

    private static JobDefinitionSaveRegistry CreateRegistry()
    {
        return new JobDefinitionSaveRegistry(new[]
        {
            new DigJobDefinitionSaveCodec(),
        });
    }

    private static MaterialCatalog CreateMaterials()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(Rock, isSolid: true, hardness: 100),
        });
    }

    private static ItemCatalog CreateItems()
    {
        return new ItemCatalog(new[]
        {
            new ItemDefinition(Ore, "Test Ore", maximumStackSize: 100, isTool: false),
        });
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "dig-save-corruption-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static EntityId Id(string value)
    {
        return EntityId.Parse(value);
    }
}
}
