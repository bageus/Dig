using System;
using Dig.Application.Saving;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class BarrelSaveRoundTripTests
{
    private static readonly MaterialId Air = new MaterialId("terrain.air");
    private static readonly BarrelDefinitionId DefinitionId =
        new BarrelDefinitionId("world.barrel.wooden");
    private static readonly ItemId Stone = new ItemId("material.stone");
    private static readonly ItemId Ore = new ItemId("ore.iron");
    private static readonly EntityId BarrelId = Id("e1000000000000000000000000000001");
    private static readonly EntityId JobId = Id("e2000000000000000000000000000001");
    private static readonly EntityId WorkerId = Id("e3000000000000000000000000000001");
    private static readonly CellId BarrelCell = new CellId(3, 3, 1);
    private static readonly CellId WorkCell = new CellId(2, 3, 1);

    [Fact]
    public void Active_attack_round_trip_preserves_contents_version_and_job()
    {
        MaterialCatalog materials = Materials();
        ItemCatalog items = Items();
        BarrelCatalog barrelCatalog = Barrels();
        WorldState world = WorldState.CreateFilled(
            new WorldSize(8, 8), 4, materials, Air, explored: true).Value;
        InventoryState inventory = new InventoryState(items);
        BarrelState barrels = new BarrelState(barrelCatalog);
        Assert.True(barrels.Add(
            BarrelId,
            DefinitionId,
            BarrelCell,
            Ore,
            tick: 0).IsSuccess);
        JobSystem jobs = new JobSystem();
        BarrelAttackJobDefinition definition = new BarrelAttackJobDefinition(
            JobId,
            BarrelId,
            BarrelCell,
            WorkCell,
            barrelVersion: 0,
            contentsGeneration: 0,
            priority: 900,
            createdTick: 1,
            retryPolicy: JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, 1).IsSuccess);
        Assert.True(jobs.Claim(JobId, WorkerId, 1).IsSuccess);
        Assert.True(jobs.Start(JobId, 1).IsSuccess);

        SaveGameDocument document = Builder().Build(new SaveGameContext(
            Metadata(),
            world,
            inventory,
            jobs,
            new BuildingsState(),
            Array.Empty<AgentState>(),
            barrels: barrels));
        DataContractJsonSaveCodec json = new DataContractJsonSaveCodec();
        SaveGameDocument decoded = json.Deserialize(json.Serialize(document));
        Result<LoadedGameState> loaded = Loader().Load(
            decoded,
            materials,
            items,
            buildingCatalog: null,
            terrainDepositCatalog: null,
            mushroomCatalog: null,
            barrelCatalog: barrelCatalog);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        BarrelSnapshot restored = loaded.Value.Barrels.Get(BarrelId)!;
        Assert.Equal(BarrelLifecycle.Supported, restored.Lifecycle);
        Assert.Equal(Ore, restored.ContentsItemId);
        Assert.False(restored.ContentsMaterialized);
        Assert.Equal(0, restored.Version);
        Assert.Equal(JobStatus.InProgress, loaded.Value.Jobs.Get(JobId)!.Status);
        Assert.Contains(
            loaded.Value.Jobs.GetReservations(),
            value => value.Key == ReservationKey.ForPosition(WorkCell));
    }

    [Fact]
    public void Version_six_migration_adds_empty_barrel_section()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 6,
            Barrels = null!,
        };
        SaveMigrationPipeline pipeline = new SaveMigrationPipeline(new ISaveMigration[]
        {
            new SaveVersionSixBuildingProductionMigration(),
            new SaveVersionSevenWorldExcavationProgressMigration(),
            new SaveVersionEightAgentRuntimeMigration(),
            new SaveVersionNineCombatSpatialMigration(),
            new SaveVersionTenTerrainDepositContractMigration(),
            new SaveVersionElevenLivingMaterialsMigration(),
            new SaveVersionTwelveTerrainOutputContractMigration(),
            new SaveVersionThirteenVukerEcologyMigration(),
            new SaveVersionFourteenTunnelInfrastructureMigration(),
            new SaveVersionFifteenRoomInfrastructureMigration(),
            new SaveVersionSixteenTunnelManualInfrastructureMigration(),
            new SaveVersionSeventeenExplorationMigration(),
            new SaveVersionEighteenStorageMigration(),
        });

        Result<SaveMigrationReport> migrated = pipeline.Apply(document);

        Assert.True(migrated.IsSuccess, migrated.Error?.ToString());
        Assert.Equal(SaveFormat.CurrentVersion, document.FormatVersion);
        Assert.NotNull(document.Barrels);
        Assert.Empty(document.Barrels.Barrels);
    }

    private static SaveGameBuilder Builder() => new SaveGameBuilder(
        new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
        {
            new BarrelAttackJobSaveCodec(),
        }));

    private static SaveGameLoader Loader() => new SaveGameLoader(
        new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
        new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
        {
            new BarrelAttackJobSaveCodec(),
        }));

    private static SaveMetadataData Metadata() => new SaveMetadataData
    {
        SlotId = "barrel-active",
        DisplayName = "Barrel checkpoint",
        SavedAtUtc = "2026-07-28T10:00:00Z",
        SimulationTick = 2,
        WorldSeed = 42,
        GeneratorVersion = 1,
    };

    private static MaterialCatalog Materials() => new MaterialCatalog(new[]
    {
        new MaterialDefinition(Air, isSolid: false, hardness: 0),
    });

    private static ItemCatalog Items() => new ItemCatalog(new[]
    {
        new ItemDefinition(Stone, "Stone", 100, isTool: false),
        new ItemDefinition(Ore, "Iron ore", 100, isTool: false),
    });

    private static BarrelCatalog Barrels() => new BarrelCatalog(new[]
    {
        new BarrelDefinition(DefinitionId, new[] { Stone, Ore }),
    });

    private static EntityId Id(string value) => EntityId.Parse(value);
}

}
