using System;
using Dig.Application.Buildings;
using Dig.Application.Saving;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class PackableBuildingExecutionSaveIntegrationTests
{
    private static readonly MaterialId Air = new MaterialId("air");

    [Fact]
    public void In_flight_unpack_iteration_survives_full_save_load()
    {
        BuildingBoxHarness harness = new BuildingBoxHarness();
        Assert.True(harness.Confirm(
            harness.BuildingId,
            harness.JobId,
            new CellId(3, 3)).IsSuccess);
        harness.AssignAndAdvanceToDeposit();
        Assert.True(harness.CommitToSite().IsSuccess);
        harness.AdvanceToPerformWork();
        Assert.True(harness.AddWork(amount: 1).IsSuccess);

        PackableBuildingExecutionRegistry executions =
            new PackableBuildingExecutionRegistry();
        Assert.True(executions.GetOrCreate(
            harness.JobId,
            harness.BuildingId,
            new BuildingDefinitionId("workshop.box"),
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3).IsSuccess);
        Assert.True(executions.StartOrResume(harness.JobId, harness.WorkerId).IsSuccess);
        Assert.True(executions.CompleteIteration(harness.JobId, harness.WorkerId).IsSuccess);
        Assert.True(executions.BeginIteration(
            harness.JobId,
            harness.WorkerId,
            startTick: 100,
            durationSeconds: 500).IsSuccess);

        SaveGameDocument document = new SaveGameBuilder(CreateRegistry()).Build(
            new SaveGameContext(
                Metadata(),
                harness.WorldRepository.Get(),
                harness.Inventory,
                harness.Jobs,
                harness.Buildings,
                Array.Empty<AgentState>(),
                Array.Empty<TerrainDepositInstance>(),
                executions));
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();
        SaveGameDocument decoded = codec.Deserialize(codec.Serialize(document));

        Result<LoadedGameState> loaded = new SaveGameLoader(
            CreateMigrations(),
            CreateRegistry()).Load(
                decoded,
                CreateMaterials(),
                harness.Inventory.Catalog,
                harness.Catalog);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        PackableBuildingExecutionSnapshot restored = Assert.Single(
            loaded.Value.PackableBuildingExecutions.CreateSnapshot());
        Assert.Equal(harness.JobId, restored.OperationId);
        Assert.Equal(harness.BuildingId, restored.PackageId);
        Assert.Equal(1, restored.CompletedIterations);
        Assert.Equal(new[] { harness.WorkerId }, restored.CompletedByWorkers);
        Assert.NotNull(restored.IterationClock);
        Assert.Equal(100, restored.IterationClock!.StartTick);
        Assert.Equal(500, restored.IterationClock.DurationSeconds);
        Assert.False(loaded.Value.PackableBuildingExecutions.IsIterationReady(
            harness.JobId,
            harness.WorkerId,
            tick: 599).Value);
        Assert.True(loaded.Value.PackableBuildingExecutions.IsIterationReady(
            harness.JobId,
            harness.WorkerId,
            tick: 600).Value);
    }

    private static JobDefinitionSaveRegistry CreateRegistry()
    {
        return new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
        {
            new BuildingBoxAssemblyJobSaveCodec(),
        });
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

    private static MaterialCatalog CreateMaterials()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
    }

    private static SaveMetadataData Metadata()
    {
        return new SaveMetadataData
        {
            SlotId = "packable-execution",
            DisplayName = "Packable execution",
            SavedAtUtc = "2026-07-24T17:20:00Z",
            SimulationTick = 150,
            WorldSeed = 1,
            GeneratorVersion = 1,
        };
    }
}

}