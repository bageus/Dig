using System;
using Dig.Domain.Agents;
using Dig.Application.Farming;
using Dig.Application.Saving;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class FarmSaveRoundTripTests
{
    [Fact]
    public void Adapter_and_json_preserve_complete_farm_runtime_state()
    {
        EntityId farmId = EntityId.Parse("73100000000000000000000000000001");
        FarmState state = new FarmState(FarmMode.Grubs);
        state.Deliver(FarmDeliveryKind.Grub, 3, tick: 10);
        state.Deliver(FarmDeliveryKind.MushroomFeed, 2, tick: 10);
        state.Advance(FarmOperationPolicy.GrubReproductionTicks + 10);
        state.SwitchMode(FarmMode.Hamsters, tick: 20_000);
        InMemoryFarmRepository source = new InMemoryFarmRepository();
        source.Save(farmId, state);
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = SaveFormat.CurrentVersion,
            Farms = FarmSaveAdapter.Encode(source),
        };
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();

        SaveGameDocument decodedDocument = codec.Deserialize(codec.Serialize(document));
        Result<InMemoryFarmRepository> decoded =
            FarmSaveAdapter.Decode(decodedDocument.Farms);

        Assert.True(decoded.IsSuccess, decoded.Error?.ToString());
        FarmSnapshot expected = state.CreateSnapshot();
        FarmSnapshot actual = decoded.Value.Get(farmId)!.CreateSnapshot();
        Assert.Equal(expected.Mode, actual.Mode);
        Assert.Equal(expected.MushroomSeedEstablished, actual.MushroomSeedEstablished);
        Assert.Equal(expected.MushroomSlotsOccupied, actual.MushroomSlotsOccupied);
        Assert.Equal(expected.ResidualMushrooms, actual.ResidualMushrooms);
        Assert.Equal(expected.HamsterCount, actual.HamsterCount);
        Assert.Equal(expected.GrubCount, actual.GrubCount);
        Assert.Equal(expected.FeedCount, actual.FeedCount);
        Assert.Equal(expected.NextReproductionTick, actual.NextReproductionTick);
        Assert.Equal(expected.NextFeedConsumptionTick, actual.NextFeedConsumptionTick);
        Assert.Equal(expected.EscapingHamsterCount, actual.EscapingHamsterCount);
        Assert.Equal(expected.EscapingGrubCount, actual.EscapingGrubCount);
        Assert.Equal(expected.NextEscapeTick, actual.NextEscapeTick);
    }

    [Fact]
    public void Missing_farm_section_restores_empty_backward_compatible_state()
    {
        Result<InMemoryFarmRepository> decoded = FarmSaveAdapter.Decode(null);

        Assert.True(decoded.IsSuccess);
        Assert.Empty(decoded.Value.GetFarmIds());
    }

    [Fact]
    public void Malformed_farm_and_reservation_ids_are_rejected_without_throwing()
    {
        FarmSaveData invalidFarm = new FarmSaveData();
        invalidFarm.Farms.Add(new FarmStateSaveData
        {
            BuildingId = "not-an-entity-id",
            Mode = (int)FarmMode.Mushrooms,
        });
        FarmSaveData invalidReservation = new FarmSaveData();
        invalidReservation.Reservations.Add(new FarmLogisticsReservationSaveData
        {
            JobId = "not-an-entity-id",
            BuildingId = EntityId.New().ToString(),
            Kind = (int)FarmDeliveryKind.MushroomSeed,
            Quantity = 1,
            Direction = (int)FarmLogisticsDirection.Incoming,
        });

        Result<InMemoryFarmRepository> farms = FarmSaveAdapter.Decode(invalidFarm);
        Result<FarmLogisticsReservations> reservations =
            FarmSaveAdapter.DecodeReservations(invalidReservation);

        Assert.True(farms.IsFailure);
        Assert.Equal(SaveErrors.InvalidDocument, farms.Error);
        Assert.True(reservations.IsFailure);
        Assert.Equal(SaveErrors.InvalidDocument, reservations.Error);
    }

    [Fact]
    public void Builder_json_and_loader_restore_farms_into_loaded_game_state()
    {
        MaterialId ground = new MaterialId("terrain.farm-save-ground");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(
                ground,
                "Farm save ground",
                isSolid: true,
                hardness: 1,
                isMineable: true,
                outputProfile: null),
        });
        ItemCatalog items = new ItemCatalog(new[]
        {
            new ItemDefinition(
                new ItemId("item.farm-save-fixture"),
                "Farm save fixture",
                maximumStackSize: 1,
                isTool: false),
            new ItemDefinition(
                FarmItemCatalog.Default.Hamster,
                "Farm save hamster",
                maximumStackSize: 1,
                isTool: false),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(2, 2, 4),
            chunkSize: 2,
            materials,
            ground).Value;
        EntityId farmId = EntityId.Parse("73100000000000000000000000000002");
        FarmState farm = new FarmState(FarmMode.Hamsters);
        farm.Deliver(FarmDeliveryKind.Hamster, 4, tick: 5);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, 2, tick: 5);
        farm.SwitchMode(FarmMode.Grubs, tick: 6);
        InMemoryFarmRepository farms = new InMemoryFarmRepository();
        farms.Save(farmId, farm);
        EntityId farmJobId = EntityId.Parse("73100000000000000000000000000003");
        FarmLogisticsReservations farmReservations = new FarmLogisticsReservations();
        Assert.True(farmReservations.TryReserveOutgoing(
            farmJobId,
            farmId,
            FarmDeliveryKind.Hamster,
            collectableQuantity: 1,
            quantity: 1));
        JobSystem jobSystem = new JobSystem();
        EntityId sourceStackId = EntityId.Parse("73200000000000000000000000000004");
        Assert.True(jobSystem.Add(new HaulJobDefinition(
            farmJobId,
            sourceStackId,
            FarmItemCatalog.Default.Hamster,
            quantity: 1,
            ItemLocation.InWorld(new CellId(0, 0, 1)),
            priority: 650,
            createdTick: 5,
            JobRetryPolicy.Default)).IsSuccess);
        Assert.True(jobSystem.MakeAvailable(farmJobId, tick: 5).IsSuccess);
        JobDefinitionSaveRegistry jobs = new JobDefinitionSaveRegistry(
            new IJobDefinitionSaveCodec[]
            {
                new DigJobDefinitionSaveCodec(),
                new HaulJobDefinitionSaveCodec(),
            });
        SaveGameDocument document = new SaveGameBuilder(jobs).Build(
            new SaveGameContext(
                new SaveMetadataData
                {
                    SlotId = "farm-round-trip",
                    DisplayName = "Farm round trip",
                    SavedAtUtc = "2026-08-16T00:00:00Z",
                    SimulationTick = 6,
                    WorldSeed = 731,
                    GeneratorVersion = 1,
                },
                world,
                new InventoryState(items),
                jobSystem,
                new BuildingsState(),
                Array.Empty<AgentState>(),
                farms: farms,
                farmLogisticsReservations: farmReservations));
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();

        Result<LoadedGameState> loaded = new SaveGameLoader(
            new SaveMigrationPipeline(Array.Empty<ISaveMigration>()),
            jobs).Load(codec.Deserialize(codec.Serialize(document)), materials, items);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        FarmSnapshot expected = farm.CreateSnapshot();
        FarmSnapshot actual = loaded.Value.Farms.Get(farmId)!.CreateSnapshot();
        Assert.Equal(expected.Mode, actual.Mode);
        Assert.Equal(expected.FeedCount, actual.FeedCount);
        Assert.Equal(expected.EscapingHamsterCount, actual.EscapingHamsterCount);
        Assert.Equal(expected.NextEscapeTick, actual.NextEscapeTick);
        FarmLogisticsReservation restored = Assert.Single(
            loaded.Value.FarmLogisticsReservations.GetAll());
        Assert.Equal(farmJobId, restored.JobId);
        Assert.Equal(farmId, restored.BuildingId);
        Assert.Equal(FarmDeliveryKind.Hamster, restored.Kind);
        Assert.Equal(FarmLogisticsDirection.Outgoing, restored.Direction);
    }

    [Fact]
    public void Reservation_without_matching_active_job_is_rejected()
    {
        EntityId farmId = EntityId.Parse("73100000000000000000000000000005");
        EntityId jobId = EntityId.Parse("73100000000000000000000000000006");
        InMemoryFarmRepository farms = new InMemoryFarmRepository();
        farms.Save(farmId, new FarmState(FarmMode.Hamsters));
        FarmLogisticsReservations reservations = new FarmLogisticsReservations();
        Assert.True(reservations.TryReserveOutgoing(
            jobId,
            farmId,
            FarmDeliveryKind.Hamster,
            collectableQuantity: 1,
            quantity: 1));

        Result integrity = FarmSaveAdapter.ValidateReservations(
            reservations,
            farms,
            new JobSystem());

        Assert.True(integrity.IsFailure);
        Assert.Equal(SaveErrors.InvalidDocument, integrity.Error);
    }
}

}
