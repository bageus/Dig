using System.Linq;
using Dig.Application.Buildings;
using Dig.Application.Jobs;
using Dig.Application.Saving;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxPackingSaveTests
{
    private static readonly MaterialId Air = new MaterialId("air");

    [Fact]
    public void Active_packing_round_trip_continues_to_one_output_box()
    {
        BuildingBoxPackingHarness original = new BuildingBoxPackingHarness();
        Assert.True(original.Start().IsSuccess);
        original.AssignAndAdvanceToWork();
        Assert.True(original.AddWork(amount: 1).IsSuccess);

        SaveGameBuilder builder = new SaveGameBuilder(CreateRegistry());
        SaveGameDocument document = builder.Build(new SaveGameContext(
            Metadata(),
            original.Assembly.WorldRepository.Get(),
            original.Inventory,
            original.Jobs,
            original.Buildings));
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();
        SaveGameDocument decoded = codec.Deserialize(codec.Serialize(document));
        SaveGameLoader loader = new SaveGameLoader(CreateMigrations(), CreateRegistry());

        Result<LoadedGameState> result = loader.Load(
            decoded,
            CreateMaterials(),
            original.Inventory.Catalog,
            original.Assembly.Catalog);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        LoadedGameState loaded = result.Value;
        BuildingSnapshot restored = loaded.Buildings.Get(original.BuildingId)!;
        JobSnapshot restoredJob = loaded.Jobs.Get(original.PackingJobId)!;
        Assert.Equal(BuildingPackingCommitState.Active, restored.PackingPlan!.CommitState);
        Assert.Equal(1, restored.PackingPlan.CompletedWork);
        Assert.Equal(JobStatus.InProgress, restoredJob.Status);
        Assert.Equal(JobStageKind.PerformWork, restoredJob.Stage);
        Assert.Equal(original.WorkerId, restoredJob.AssignedAgentId);
        Assert.Equal(new[]
        {
            ReservationKey.ForJob(original.PackingJobId),
            ReservationKey.ForAgent(original.WorkerId),
            ReservationKey.ForPosition(restored.WorkPosition),
            ReservationKey.ForDestination(original.BuildingId),
        }, loaded.Jobs.GetReservations().Select(item => item.Key).ToArray());
        Assert.All(
            loaded.Jobs.GetReservations(),
            item => Assert.Equal(original.WorkerId, item.AgentId));
        Assert.Null(loaded.Inventory.GetStack(original.OutputStackId));

        CompleteLoadedPacking(loaded, original);
        Assert.True(original.AddWork(amount: 1).IsSuccess);
        original.AdvanceToFinalize();
        Assert.True(original.Complete().IsSuccess);

        BuildingSnapshot completed = loaded.Buildings.Get(original.BuildingId)!;
        Assert.Equal(BuildingStatus.Removed, completed.Status);
        Assert.Equal(BuildingPackingCommitState.Completed, completed.PackingPlan!.CommitState);
        Assert.Equal(JobStatus.Completed, loaded.Jobs.Get(original.PackingJobId)!.Status);
        Assert.Empty(loaded.Jobs.GetReservations());
        ItemStackSnapshot output = loaded.Inventory.GetStack(original.OutputStackId)!;
        Assert.Equal(original.BoxItemId, output.ItemId);
        Assert.Equal(1, output.Quantity);
        Assert.Equal(ItemLocation.InWorld(completed.Origin), output.Location);

        Assert.Equal(
            original.Buildings.Get(original.BuildingId)!.Status,
            completed.Status);
        Assert.Equal(
            original.Buildings.Get(original.BuildingId)!.PackingPlan!.CommitState,
            completed.PackingPlan.CommitState);
        Assert.Equal(original.Inventory.GetTotal(original.BoxItemId),
            loaded.Inventory.GetTotal(original.BoxItemId));
    }

    [Fact]
    public void Active_packing_with_premature_output_stack_is_rejected()
    {
        BuildingBoxPackingHarness harness = new BuildingBoxPackingHarness();
        Assert.True(harness.Start().IsSuccess);
        SaveGameDocument document = new SaveGameBuilder(CreateRegistry()).Build(
            new SaveGameContext(
                Metadata(),
                harness.Assembly.WorldRepository.Get(),
                harness.Inventory,
                harness.Jobs,
                harness.Buildings));
        document.Inventory.Stacks.Add(new ItemStackSaveData
        {
            StackId = harness.OutputStackId.ToString(),
            ItemId = harness.BoxItemId.ToString(),
            Quantity = 1,
            Location = new ItemLocationSaveData
            {
                Kind = (int)ItemLocationKind.World,
                CellX = 3,
                CellY = 3,
            },
        });

        Result<LoadedGameState> result = new SaveGameLoader(
            CreateMigrations(),
            CreateRegistry()).Load(
                document,
                CreateMaterials(),
                harness.Inventory.Catalog,
                harness.Assembly.Catalog);

        Assert.True(result.IsFailure);
        Assert.Equal(SaveErrors.InvalidDocument, result.Error);
    }

    private static void CompleteLoadedPacking(
        LoadedGameState loaded,
        BuildingBoxPackingHarness source)
    {
        InMemoryBuildingsRepository buildings = new InMemoryBuildingsRepository(
            loaded.Buildings);
        InMemoryInventoryRepository inventory = new InMemoryInventoryRepository(
            loaded.Inventory);
        InMemoryJobRepository jobs = new InMemoryJobRepository(loaded.Jobs);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        long tick = 500;

        Assert.True(new AddBuildingBoxPackingWorkHandler(
            buildings,
            jobs,
            journal).Handle(new AddBuildingBoxPackingWorkCommand(
                source.BuildingId,
                source.PackingJobId,
                workAmount: 1,
                tick: tick++)).IsSuccess);
        Assert.True(new AdvanceJobHandler(jobs, journal).Handle(
            new AdvanceJobCommand(source.PackingJobId, tick++)).IsSuccess);
        Assert.Equal(JobStageKind.Finalize, loaded.Jobs.Get(source.PackingJobId)!.Stage);
        Assert.True(new CompleteBuildingBoxPackingHandler(
            buildings,
            inventory,
            jobs,
            journal,
            AgentSkillGrantTestFactory.Create(source.WorkerId, journal))
            .Handle(new CompleteBuildingBoxPackingCommand(
                source.BuildingId,
                source.PackingJobId,
                tick)).IsSuccess);
    }

    private static JobDefinitionSaveRegistry CreateRegistry()
    {
        return new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
        {
            new BuildingBoxAssemblyJobSaveCodec(),
            new BuildingBoxPackingJobSaveCodec(),
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
            SlotId = "packing-active",
            DisplayName = "Packing active",
            SavedAtUtc = "2026-07-15T22:40:00Z",
            SimulationTick = 200,
            WorldSeed = 1,
            GeneratorVersion = 1,
        };
    }
}
}
