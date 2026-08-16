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
        JobDefinitionSaveRegistry jobs = new JobDefinitionSaveRegistry(
            Array.Empty<IJobDefinitionSaveCodec>());
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
                new JobSystem(),
                new BuildingsState(),
                Array.Empty<AgentState>(),
                farms: farms));
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
    }
}

}
