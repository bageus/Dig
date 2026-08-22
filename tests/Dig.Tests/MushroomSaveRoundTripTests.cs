using System;
using Dig.Application.Saving;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class MushroomSaveRoundTripTests
{
    private static readonly MaterialId Air = new MaterialId("terrain.air");
    private static readonly MushroomDefinitionId DefinitionId =
        new MushroomDefinitionId("ecology.mushroom.common");
    private static readonly ItemId Cap = new ItemId("material.mushroom_cap");
    private static readonly ItemId Leg = new ItemId("material.mushroom_leg");
    private static readonly EntityId SiteId = Id("c1000000000000000000000000000001");
    private static readonly EntityId JobId = Id("c2000000000000000000000000000001");
    private static readonly EntityId WorkerId = Id("c3000000000000000000000000000001");
    private static readonly CellId SiteCell = new CellId(3, 3, 1);
    private static readonly CellId WorkCell = new CellId(2, 3, 1);

    [Fact]
    public void Mid_chop_round_trip_preserves_paused_growth_progress_and_reservations()
    {
        MaterialCatalog materials = CreateMaterials();
        ItemCatalog items = CreateItems();
        MushroomCatalog mushroomCatalog = CreateMushroomCatalog();
        WorldState world = WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            Air,
            explored: true).Value;
        InventoryState inventory = new InventoryState(items);
        MushroomState mushrooms = new MushroomState(mushroomCatalog);
        Assert.True(mushrooms.AddSite(
            SiteId,
            DefinitionId,
            SiteCell,
            MushroomStage.Medium,
            tick: 0).IsSuccess);
        Assert.True(mushrooms.BeginChop(
            SiteId,
            JobId,
            WorkerId,
            requiredSwings: 5,
            tick: 4).IsSuccess);
        Assert.False(mushrooms.CompleteSwing(SiteId, JobId, WorkerId, tick: 5).Value);
        Assert.False(mushrooms.CompleteSwing(SiteId, JobId, WorkerId, tick: 6).Value);

        JobSystem jobs = new JobSystem();
        MushroomChopJobDefinition definition = new MushroomChopJobDefinition(
            JobId,
            SiteId,
            SiteCell,
            WorkCell,
            growthGeneration: 0,
            requiredSwings: 5,
            priority: 900,
            createdTick: 4,
            retryPolicy: JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(JobId, tick: 4).IsSuccess);
        Assert.True(jobs.Claim(JobId, WorkerId, tick: 4).IsSuccess);
        Assert.True(jobs.Start(JobId, tick: 4).IsSuccess);
        Assert.True(jobs.AdvanceStage(JobId, tick: 5).IsSuccess);

        SaveGameDocument document = CreateBuilder().Build(new SaveGameContext(
            Metadata(),
            world,
            inventory,
            jobs,
            new BuildingsState(),
            Array.Empty<AgentState>(),
            mushrooms: mushrooms));
        DataContractJsonSaveCodec json = new DataContractJsonSaveCodec();
        SaveGameDocument decoded = json.Deserialize(json.Serialize(document));
        Result<LoadedGameState> loaded = CreateLoader().Load(
            decoded,
            materials,
            items,
            buildingCatalog: null,
            terrainDepositCatalog: null,
            mushroomCatalog);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        MushroomSiteSnapshot restored = loaded.Value.Mushrooms.Get(SiteId)!;
        Assert.Equal(MushroomStage.Medium, restored.Stage);
        Assert.Equal(2, restored.CompletedSwings);
        Assert.Equal(5, restored.RequiredSwings);
        Assert.Equal(4, restored.GrowthPausedAtTick);
        Assert.Equal(JobId, restored.ActiveChopJobId);
        Assert.Equal(WorkerId, restored.ActiveWorkerId);
        Assert.Equal(JobStageKind.PerformWork, loaded.Value.Jobs.Get(JobId)!.Stage);
        Assert.Contains(
            loaded.Value.Jobs.GetReservations(),
            reservation => reservation.Key == ReservationKey.ForEcologyTarget(SiteId));
        Assert.Equal(SiteCell, Assert.Single(loaded.Value.Mushrooms.GetBuildingBlockedCells()));
    }

    [Fact]
    public void Version_five_migration_adds_empty_mushroom_section()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 5,
            Mushrooms = null!,
        };
        SaveMigrationPipeline pipeline = new SaveMigrationPipeline(new ISaveMigration[]
        {
            new SaveVersionFiveMushroomsMigration(),
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
        Assert.NotNull(document.Mushrooms);
        Assert.Empty(document.Mushrooms.Sites);
    }

    private static SaveGameBuilder CreateBuilder()
    {
        return new SaveGameBuilder(new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
        {
            new MushroomChopJobSaveCodec(),
        }));
    }

    private static SaveGameLoader CreateLoader()
    {
        return new SaveGameLoader(
            new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
            new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
            {
                new MushroomChopJobSaveCodec(),
            }));
    }

    private static SaveMetadataData Metadata()
    {
        return new SaveMetadataData
        {
            SlotId = "mushroom-mid-chop",
            DisplayName = "Mushroom checkpoint",
            SavedAtUtc = "2026-07-27T10:00:00Z",
            SimulationTick = 6,
            WorldSeed = 42,
            GeneratorVersion = 1,
        };
    }

    private static MaterialCatalog CreateMaterials()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
    }

    private static ItemCatalog CreateItems()
    {
        return new ItemCatalog(new[]
        {
            new ItemDefinition(Cap, "Mushroom cap", 100, isTool: false),
            new ItemDefinition(Leg, "Mushroom leg", 100, isTool: false),
        });
    }

    private static MushroomCatalog CreateMushroomCatalog()
    {
        return new MushroomCatalog(new[]
        {
            new MushroomDefinition(
                DefinitionId,
                stageDurationTicks: 10,
                capItemId: Cap,
                legItemId: Leg),
        });
    }

    private static EntityId Id(string value) => EntityId.Parse(value);
}

}
