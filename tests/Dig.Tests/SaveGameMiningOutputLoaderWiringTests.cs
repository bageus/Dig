using Dig.Application.Saving;
using Dig.Application.World;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class SaveGameMiningOutputLoaderWiringTests
{
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly MaterialId TerrainStone = new MaterialId("terrain.stone");
    private static readonly EntityId StackId =
        EntityId.Parse("77000000000000000000000000000001");

    [Fact]
    public void Loader_restores_document_mining_output_against_loaded_inventory_and_world()
    {
        InventoryState inventory = CreateInventory();
        CellId cell = new CellId(1, 2, 0);
        Assert.True(inventory.AddStack(
            StackId,
            Stone,
            2,
            ItemLocation.InWorld(cell),
            tick: 6).IsSuccess);
        MiningOutputCommitState commits = new MiningOutputCommitState();
        commits.Record(ResolveStone(cell), StackId);

        SaveGameContext context = new SaveGameContext(
            Metadata(),
            CreateWorld(),
            inventory,
            new JobSystem(),
            new BuildingsState());
        SaveGameBuilder builder = new SaveGameBuilder(CreateJobRegistry());
        Result<SaveGameDocument> saved = builder.Build(context, commits);
        Assert.True(saved.IsSuccess);

        SaveGameLoader loader = new SaveGameLoader(
            new SaveMigrationPipeline(System.Array.Empty<ISaveMigration>()),
            CreateJobRegistry());
        Result<LoadedGameWithMiningOutput> loaded = loader.LoadWithMiningOutput(
            saved.Value,
            CreateMaterialCatalog(),
            CreateItemCatalog());

        Assert.True(loaded.IsSuccess);
        Assert.True(loaded.Value.MiningOutput.Commits.IsCommitted(cell));
        Assert.True(loaded.Value.MiningOutput.Integrity.IsValid);
        Assert.Equal(2, loaded.Value.MiningOutput.Integrity.CommittedQuantity);
        Assert.Equal(2, loaded.Value.MiningOutput.Integrity.TrackedWorldQuantity);
    }

    [Fact]
    public void Loader_rejects_mining_output_that_does_not_match_loaded_inventory()
    {
        SaveGameContext context = new SaveGameContext(
            Metadata(),
            CreateWorld(),
            CreateInventory(),
            new JobSystem(),
            new BuildingsState());
        SaveGameDocument document = new SaveGameBuilder(CreateJobRegistry()).Build(context);
        document.MiningOutput.Commits.Add(new MiningOutputCommitSaveData
        {
            X = 1,
            Y = 1,
            Z = 0,
            SourceKind = (int)MiningOutputSourceKind.Terrain,
            ItemId = Stone.ToString(),
            Quantity = 2,
            StackId = StackId.ToString(),
            HasStack = true,
        });

        Result<LoadedGameWithMiningOutput> loaded = new SaveGameLoader(
            new SaveMigrationPipeline(System.Array.Empty<ISaveMigration>()),
            CreateJobRegistry())
            .LoadWithMiningOutput(
                document,
                CreateMaterialCatalog(),
                CreateItemCatalog());

        Assert.True(loaded.IsFailure);
        Assert.Equal(MiningOutputSaveErrors.IntegrityMismatch, loaded.Error);
    }

    private static SaveMetadataData Metadata()
    {
        return new SaveMetadataData
        {
            SlotId = "slot-loader",
            DisplayName = "Loader",
            SavedAtUtc = "2026-07-24T00:00:00Z",
            SimulationTick = 6,
            WorldSeed = 23,
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

    private static InventoryState CreateInventory()
    {
        return new InventoryState(CreateItemCatalog());
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
            worldSeed: 23,
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
                            minimumQuantity: 2,
                            maximumQuantity: 2),
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

    private sealed class EmptyJobDefinitionSaveCodec : IJobDefinitionSaveCodec
    {
        public string TypeId => "test.empty";

        public bool CanEncode(JobDefinition definition)
        {
            return false;
        }

        public JobDefinitionSaveData Encode(JobDefinition definition)
        {
            throw new System.NotSupportedException();
        }

        public JobDefinition Decode(JobDefinitionSaveData data)
        {
            throw new System.NotSupportedException();
        }
    }
}

}