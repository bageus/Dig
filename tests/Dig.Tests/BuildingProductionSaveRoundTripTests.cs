using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Production;
using Dig.Application.Saving;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingProductionSaveRoundTripTests
{
    private static readonly MaterialId Air = new MaterialId("terrain.air");

    [Fact]
    public void Mid_step_round_trip_continues_without_reconsuming_finished_steps()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(100);
        EntityId orderId = CampfireProductionTestHarness.Id(500);
        EntityId jobId = CampfireProductionTestHarness.Id(501);
        harness.AddBuildingStock(CampfireProductionContent.MushroomLegItemId, 2, 510);
        harness.AddBuildingStock(CampfireProductionContent.MushroomCapItemId, 1, 511);
        Assert.True(harness.Enqueue(orderId, CampfireProductionContent.TentRecipeId, 1).IsSuccess);
        Assert.True(harness.Prepare(jobId, 2).IsSuccess);
        harness.ClaimBeginAndReachWork(orderId, jobId, 3);
        Assert.True(harness.Work(orderId, jobId, elapsedTicks: 120, tick: 6).IsSuccess);
        Assert.True(harness.Supply.SetDeliveryEnabled(
            CampfireProductionTestHarness.BuildingId,
            CampfireProductionContent.MushroomCapItemId,
            enabled: false,
            tick: 7).IsSuccess);

        SaveGameDocument document = Builder().Build(new SaveGameContext(
            Metadata("mid-step"),
            CreateWorld(),
            harness.Inventory,
            harness.Jobs,
            harness.Buildings,
            Array.Empty<AgentState>(),
            production: harness.Production,
            buildingSupply: harness.Supply));
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();
        Result<LoadedGameState> loaded = Loader().Load(
            codec.Deserialize(codec.Serialize(document)),
            CreateMaterials(),
            harness.Items,
            harness.BuildingsCatalog,
            terrainDepositCatalog: null,
            mushroomCatalog: null,
            harness.Content);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        ProductionOrderSnapshot restored = loaded.Value.Production.Get(orderId)!;
        Assert.Equal(ProductionOrderStatus.InProgress, restored.Status);
        Assert.True(restored.MaterialSteps[0].Consumed);
        Assert.Equal(20, restored.MaterialSteps[1].CompletedTicks);
        Assert.Equal(1, loaded.Value.Inventory.GetTotal(
            CampfireProductionContent.MushroomLegItemId));
        Assert.False(loaded.Value.BuildingSupply.Get(
            CampfireProductionTestHarness.BuildingId,
            loaded.Value.Inventory.CreateSnapshot())!.Stocks.Single(value =>
                value.ItemId == CampfireProductionContent.MushroomCapItemId)
            .DeliveryEnabled);

        InMemoryProductionRepository production = new InMemoryProductionRepository(
            loaded.Value.Production);
        InMemoryInventoryRepository inventory = new InMemoryInventoryRepository(
            loaded.Value.Inventory);
        InMemoryJobRepository jobs = new InMemoryJobRepository(loaded.Value.Jobs);
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.True(agents.Add(AgentTestFactory.CreateAgent(
            id: CampfireProductionTestHarness.WorkerId)).IsSuccess);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        Assert.True(new ApplyProductionWorkHandler(
            production,
            inventory,
            jobs,
            agents,
            journal).Handle(new ApplyProductionWorkCommand(
                orderId,
                jobId,
                baseWork: 180,
                conditionEfficiencyBasisPoints: 10_000,
                tick: 8)).IsSuccess);

        AgentSkillGrantService skills = new AgentSkillGrantService(agents, journal);
        EntityId outputId = CampfireProductionTestHarness.Id(512);
        Assert.True(new CompleteProductionOrderHandler(
            production,
            inventory,
            jobs,
            journal,
            skills).Handle(new CompleteProductionOrderCommand(
                orderId,
                jobId,
                new[] { outputId },
                tick: 9,
                ItemLocation.InWorld(new CellId(4, 2, 0)))).IsSuccess);
        Assert.Equal(CampfireProductionContent.TentBoxItemId,
            loaded.Value.Inventory.GetStack(outputId)!.ItemId);
        Assert.Equal(1, loaded.Value.Inventory.GetTotal(
            CampfireProductionContent.TentBoxItemId));
        Assert.Equal(200, agents.Get(CampfireProductionTestHarness.WorkerId)!
            .CreateSkillProgressionSnapshot()
            .GetLevel(AgentSkillCatalog.Woodworking));
    }

    [Fact]
    public void Active_mixed_supply_and_job_definition_round_trip()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(1);
        EntityId supplyJobId = CampfireProductionTestHarness.Id(520);
        EntityId sourceId = CampfireProductionTestHarness.Id(521);
        EntityId transitId = CampfireProductionTestHarness.Id(522);
        EntityId depositId = CampfireProductionTestHarness.Id(523);
        Assert.True(harness.Inventory.AddStack(
            sourceId,
            CampfireProductionContent.StoneItemId,
            2,
            ItemLocation.InWorld(new CellId(1, 1, 0)),
            1).IsSuccess);
        Assert.True(harness.Inventory.ReserveQuantity(sourceId, supplyJobId, 2, 2).IsSuccess);
        ItemReservationAllocation allocation = new ItemReservationAllocation(
            sourceId,
            CampfireProductionContent.StoneItemId,
            2);
        BuildingSupplyJobDefinition definition = new BuildingSupplyJobDefinition(
            supplyJobId,
            CampfireProductionTestHarness.BuildingId,
            harness.Buildings.Get(CampfireProductionTestHarness.BuildingId)!.WorkPosition,
            new[] { allocation },
            new[] { transitId },
            new[] { depositId },
            priority: 400,
            createdTick: 2,
            JobRetryPolicy.Default);
        Assert.True(harness.Jobs.Add(definition).IsSuccess);
        Assert.True(harness.Jobs.MakeAvailable(supplyJobId, 2).IsSuccess);
        Assert.True(harness.Supply.ReserveIncoming(
            CampfireProductionTestHarness.BuildingId,
            supplyJobId,
            new[] { new ItemConsumptionRequest(CampfireProductionContent.StoneItemId, 2) },
            CampfireProductionContent.CreateWorkstation().StockRules.ToDictionary(
                value => value.ItemId,
                _ => 0),
            2).IsSuccess);

        SaveGameDocument document = Builder().Build(new SaveGameContext(
            Metadata("supply"),
            CreateWorld(),
            harness.Inventory,
            harness.Jobs,
            harness.Buildings,
            Array.Empty<AgentState>(),
            production: harness.Production,
            buildingSupply: harness.Supply));
        Result<LoadedGameState> loaded = Loader().Load(
            document,
            CreateMaterials(),
            harness.Items,
            harness.BuildingsCatalog,
            terrainDepositCatalog: null,
            mushroomCatalog: null,
            harness.Content);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        BuildingSupplySnapshot supply = loaded.Value.BuildingSupply.Get(
            CampfireProductionTestHarness.BuildingId,
            loaded.Value.Inventory.CreateSnapshot())!;
        Assert.Equal(supplyJobId, supply.ActiveSupplyJobId);
        Assert.Equal(2, supply.Stocks.Single(value =>
            value.ItemId == CampfireProductionContent.StoneItemId).Incoming);
        BuildingSupplyJobDefinition restored = Assert.IsType<BuildingSupplyJobDefinition>(
            loaded.Value.Jobs.Get(supplyJobId)!.Definition);
        Assert.Equal(allocation, Assert.Single(restored.Allocations));
        Assert.Equal(transitId, Assert.Single(restored.TransitStackIds));
        Assert.Equal(depositId, Assert.Single(restored.DepositStackIds));
    }

    private static SaveGameBuilder Builder()
    {
        return new SaveGameBuilder(Registry());
    }

    private static SaveGameLoader Loader()
    {
        return new SaveGameLoader(
            new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
            Registry());
    }

    private static JobDefinitionSaveRegistry Registry()
    {
        return new JobDefinitionSaveRegistry(new IJobDefinitionSaveCodec[]
        {
            new BuildingBoxAssemblyJobSaveCodec(),
            new ProductionWorkJobSaveCodec(),
            new ProductionPackageUseJobSaveCodec(),
            new BuildingSupplyJobSaveCodec(),
        });
    }

    private static MaterialCatalog CreateMaterials()
    {
        return new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
    }

    private static WorldState CreateWorld()
    {
        return WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            CreateMaterials(),
            Air,
            explored: true).Value;
    }

    private static SaveMetadataData Metadata(string slot)
    {
        return new SaveMetadataData
        {
            SlotId = slot,
            DisplayName = slot,
            SavedAtUtc = "2026-07-27T18:30:00Z",
            SimulationTick = 10,
            WorldSeed = 7,
            GeneratorVersion = 1,
        };
    }
}

}
