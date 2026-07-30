using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Saving;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class SaveGameServiceMiningOutputIntegrationTests
{
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly MaterialId TerrainStone = new MaterialId("terrain.stone");
    private static readonly EntityId StackId =
        EntityId.Parse("78000000000000000000000000000001");

    [Fact]
    public void Manual_and_autosave_restore_the_exactly_once_mining_output_ledger()
    {
        CellId cell = new CellId(2, 2, 1);
        InventoryState inventory = new InventoryState(CreateItemCatalog());
        Assert.True(inventory.AddStack(
            StackId,
            Stone,
            quantity: 3,
            ItemLocation.InWorld(cell),
            tick: 9).IsSuccess);
        MiningOutputPlan output = ResolveStone(cell);
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Record(output, StackId);

        SaveGameContext context = new SaveGameContext(
            Metadata("manual-ledger"),
            CreateWorld(),
            inventory,
            new JobSystem(),
            new BuildingsState(),
            Array.Empty<AgentState>(),
            miningOutputCommits: commits);
        RecordingSaveSlotStore store = new RecordingSaveSlotStore();
        SaveGameBuilder builder = new SaveGameBuilder(CreateJobRegistry());
        SaveGameService service = new SaveGameService(
            builder,
            new SaveGameLoader(
                new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
                CreateJobRegistry()),
            store);

        SaveGameDocument manual = service.Save(context);
        SaveGameDocument autosave = service.Autosave(context);

        Assert.Single(manual.MiningOutput.Commits);
        Assert.Single(autosave.MiningOutput.Commits);
        Result<LoadedGameState> loadedManual = service.Load(
            "manual-ledger",
            CreateMaterialCatalog(),
            CreateItemCatalog());
        Result<LoadedGameState> loadedAutosave = service.Load(
            SaveSlotNames.Autosave,
            CreateMaterialCatalog(),
            CreateItemCatalog());
        Assert.True(loadedManual.IsSuccess);
        Assert.True(loadedAutosave.IsSuccess);
        AssertRestoredLedger(loadedManual.Value, cell, output);
        AssertRestoredLedger(loadedAutosave.Value, cell, output);

        SaveGameDocument rebuilt = builder.Build(new SaveGameContext(
            loadedManual.Value.Metadata,
            loadedManual.Value.World,
            loadedManual.Value.Inventory,
            loadedManual.Value.Jobs,
            loadedManual.Value.Buildings,
            Array.Empty<AgentState>(),
            loadedManual.Value.TerrainDeposits,
            loadedManual.Value.PackableBuildingExecutions,
            loadedManual.Value.MiningOutput.Commits,
            terrainDepositGeneratorVersion:
                loadedManual.Value.TerrainDepositGeneratorVersion));
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();
        Assert.Equal(codec.Serialize(manual), codec.Serialize(rebuilt));
    }

    private static void AssertRestoredLedger(
        LoadedGameState loaded,
        CellId cell,
        MiningOutputPlan output)
    {
        Assert.True(loaded.MiningOutput.Commits.IsCommitted(cell));
        Assert.True(loaded.MiningOutput.Integrity.IsValid);
        Assert.Equal(3, loaded.MiningOutput.Integrity.CommittedQuantity);
        Assert.Equal(3, loaded.MiningOutput.Integrity.TrackedWorldQuantity);
        Assert.Throws<InvalidOperationException>(() =>
            loaded.MiningOutput.Commits.Validate(
                output,
                StackId,
                loaded.Inventory,
                new TerrainDepositState()));
    }

    private static SaveMetadataData Metadata(string slotId)
    {
        return new SaveMetadataData
        {
            SlotId = slotId,
            DisplayName = "Mining ledger checkpoint",
            SavedAtUtc = "2026-07-27T00:00:00Z",
            SimulationTick = 9,
            WorldSeed = 31,
            GeneratorVersion = 2,
        };
    }

    private static WorldState CreateWorld()
    {
        Result<WorldState> world = WorldState.CreateFilled(
            new WorldSize(4, 4, 4),
            chunkSize: 2,
            CreateMaterialCatalog(),
            TerrainStone);
        Assert.True(world.IsSuccess);
        return world.Value;
    }

    private static MaterialCatalog CreateMaterialCatalog()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(
                TerrainStone,
                "Stone",
                isSolid: true,
                hardness: 10,
                isMineable: true,
                outputProfile: null),
        });
    }

    private static ItemCatalog CreateItemCatalog()
    {
        return new ItemCatalog(new[]
        {
            new ItemDefinition(
                Stone,
                "Stone",
                maximumStackSize: 20,
                isTool: false),
        });
    }

    private static MiningOutputPlan ResolveStone(CellId cell)
    {
        return new MiningOutputResolver().Resolve(
            worldSeed: 31,
            generatorVersion: 2,
            cell,
            new MaterialDefinition(
                TerrainStone,
                "Stone",
                isSolid: true,
                hardness: 10,
                isMineable: true,
                outputProfile: new TerrainOutputProfile(
                    "terrain-output.stone",
                    version: 1,
                    entries: new[]
                    {
                        new TerrainOutputEntry(
                            Stone,
                            probabilityPermille: 1_000,
                            minimumQuantity: 3,
                            maximumQuantity: 3),
                    })),
            new TerrainDepositState());
    }

    private static JobDefinitionSaveRegistry CreateJobRegistry()
    {
        return new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
        {
            new EmptyJobDefinitionSaveCodec(),
        });
    }

    private sealed class RecordingSaveSlotStore : ISaveSlotStore
    {
        private readonly Dictionary<string, SaveGameDocument> _documents =
            new Dictionary<string, SaveGameDocument>(StringComparer.Ordinal);

        public void Save(string slotId, SaveGameDocument document)
        {
            _documents[slotId] = document;
        }

        public SaveGameDocument Load(string slotId)
        {
            return _documents[slotId];
        }

        public IReadOnlyList<SaveSlotInfo> List()
        {
            return _documents
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => new SaveSlotInfo(
                    value.Key,
                    value.Value.Metadata,
                    isCorrupted: false,
                    errorMessage: null))
                .ToArray();
        }
    }

    private sealed class EmptyJobDefinitionSaveCodec : IJobDefinitionSaveCodec
    {
        public string TypeId => "test.empty";

        public bool CanEncode(JobDefinition definition)
        {
            return false;
        }

        public JobDefinitionSaveData Encode(JobDefinition definition)
        {
            throw new NotSupportedException();
        }

        public JobDefinition Decode(JobDefinitionSaveData data)
        {
            throw new NotSupportedException();
        }
    }
}

}
