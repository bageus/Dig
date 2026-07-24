using System;
using System.Collections.Generic;
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

public sealed class PackableBuildingExecutionLoadRecoveryTests
{
    private static readonly MaterialId Air = new MaterialId("air");
    private static readonly EntityId StaleWorker =
        EntityId.Parse("87000000000000000000000000000001");

    [Fact]
    public void Load_rebinds_active_execution_to_authoritative_job_worker()
    {
        BuildingBoxHarness harness = CreateActiveAssembly();
        PackableBuildingExecutionRegistry executions = CreateExecution(
            harness,
            StaleWorker,
            beginClock: true);
        SaveGameDocument document = BuildDocument(harness, executions);
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();

        Result<LoadedGameState> loaded = new SaveGameLoader(
            CreateMigrations(),
            CreateRegistry()).Load(
                codec.Deserialize(codec.Serialize(document)),
                CreateMaterials(),
                harness.Inventory.Catalog,
                harness.Catalog);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        PackableBuildingExecutionSnapshot restored = Assert.Single(
            loaded.Value.PackableBuildingExecutions.CreateSnapshot());
        Assert.Equal(PackableBuildingExecutionStatus.Active, restored.Status);
        Assert.Equal(harness.WorkerId, restored.ActiveWorkerId);
        Assert.Equal(1, restored.CompletedIterations);
        Assert.Equal(new[] { StaleWorker }, restored.CompletedByWorkers);
        Assert.Null(restored.IterationClock);
    }

    [Fact]
    public void Autosave_keeps_packable_execution_section()
    {
        BuildingBoxHarness harness = CreateActiveAssembly();
        PackableBuildingExecutionRegistry executions = CreateExecution(
            harness,
            harness.WorkerId,
            beginClock: true);
        RecordingStore store = new RecordingStore();
        SaveGameService service = new SaveGameService(
            new SaveGameBuilder(CreateRegistry()),
            new SaveGameLoader(CreateMigrations(), CreateRegistry()),
            store);

        SaveGameDocument saved = service.Autosave(new SaveGameContext(
            Metadata(),
            harness.WorldRepository.Get(),
            harness.Inventory,
            harness.Jobs,
            harness.Buildings,
            Array.Empty<AgentState>(),
            Array.Empty<TerrainDepositInstance>(),
            executions));

        Assert.Equal(SaveSlotNames.Autosave, saved.Metadata.SlotId);
        Assert.Single(saved.PackableBuildingExecutions.Executions);
        Assert.Same(saved, store.Saved);
    }

    private static BuildingBoxHarness CreateActiveAssembly()
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
        return harness;
    }

    private static PackableBuildingExecutionRegistry CreateExecution(
        BuildingBoxHarness harness,
        EntityId worker,
        bool beginClock)
    {
        PackableBuildingExecutionRegistry executions =
            new PackableBuildingExecutionRegistry();
        Assert.True(executions.GetOrCreate(
            harness.JobId,
            harness.BuildingId,
            new BuildingDefinitionId("workshop.box"),
            PackableBuildingOperationKind.Unpack,
            totalIterations: 3).IsSuccess);
        Assert.True(executions.StartOrResume(harness.JobId, worker).IsSuccess);
        Assert.True(executions.CompleteIteration(harness.JobId, worker).IsSuccess);
        Assert.True(executions.Interrupt(harness.JobId).IsSuccess);
        Assert.True(executions.StartOrResume(harness.JobId, worker).IsSuccess);
        if (beginClock)
        {
            Assert.True(executions.BeginIteration(
                harness.JobId,
                worker,
                startTick: 100,
                durationSeconds: 500).IsSuccess);
        }

        return executions;
    }

    private static SaveGameDocument BuildDocument(
        BuildingBoxHarness harness,
        PackableBuildingExecutionRegistry executions)
    {
        return new SaveGameBuilder(CreateRegistry()).Build(new SaveGameContext(
            Metadata(),
            harness.WorldRepository.Get(),
            harness.Inventory,
            harness.Jobs,
            harness.Buildings,
            Array.Empty<AgentState>(),
            Array.Empty<TerrainDepositInstance>(),
            executions));
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
            SlotId = "recovery",
            DisplayName = "Recovery",
            SavedAtUtc = "2026-07-24T17:30:00Z",
            SimulationTick = 150,
            WorldSeed = 1,
            GeneratorVersion = 1,
        };
    }

    private sealed class RecordingStore : ISaveSlotStore
    {
        public SaveGameDocument? Saved { get; private set; }

        public void Save(string slotId, SaveGameDocument document)
        {
            Saved = document;
        }

        public SaveGameDocument Load(string slotId)
        {
            return Saved ?? throw new InvalidOperationException();
        }

        public IReadOnlyList<SaveSlotInfo> List()
        {
            return Array.Empty<SaveSlotInfo>();
        }
    }
}

}