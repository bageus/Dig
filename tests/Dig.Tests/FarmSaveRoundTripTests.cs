using Dig.Application.Farming;
using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Farming;
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
}

}
