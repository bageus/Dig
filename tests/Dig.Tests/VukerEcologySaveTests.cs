using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class VukerEcologySaveTests
{
    [Fact]
    public void AdapterRoundTripPreservesPairCooldownChildAndTameOwner()
    {
        VukerRegionKey region = new VukerRegionKey(new CellId(0, 1, 0));
        VukerEcologyState state = new VukerEcologyState(7711);
        Assert.True(state.RegisterAdult(
            Id(1), region, new CellId(0, 1, 0), VukerDisposition.Wild, 0).IsSuccess);
        Assert.True(state.RegisterAdult(
            Id(2), region, new CellId(1, 1, 0), VukerDisposition.Wild, 0).IsSuccess);
        state.Advance(0);
        VukerPairSnapshot pair = Assert.Single(state.GetPairs());
        long birthTick = pair.NextBirthTick;
        pair = Assert.Single(state.Advance(birthTick));
        EntityId childId = state.CreateDeterministicChildId(pair.PairId, 0);
        EntityId residentId = Id(90);
        Assert.True(state.CommitBirth(
            pair.PairId,
            childId,
            region,
            new CellId(2, 1, 0),
            birthTick).IsSuccess);
        Assert.True(state.ReserveKidnap(childId, residentId, birthTick + 1).IsSuccess);
        Assert.True(state.CommitTame(childId, residentId, birthTick + 2).IsSuccess);

        VukerEcologySaveData saved = VukerEcologySaveAdapter.Encode(state);
        Result<VukerEcologyState> restored = VukerEcologySaveAdapter.Decode(saved, 999);

        Assert.True(restored.IsSuccess, restored.Error?.ToString());
        VukerIndividualSnapshot child = restored.Value.GetIndividual(childId)!;
        VukerPairSnapshot restoredPair = restored.Value.GetPair(pair.PairId)!;
        Assert.Equal(VukerDisposition.Tamed, child.Disposition);
        Assert.Equal(residentId, child.TamedByResidentId);
        Assert.Equal(1, restoredPair.SuccessfulCycles);
        Assert.Equal(state.CurrentTick, restored.Value.CurrentTick);
        Assert.Equal(state.WorldSeed, restored.Value.WorldSeed);
        Assert.Equal(
            state.CreateDeterministicChildId(pair.PairId, 1),
            restored.Value.CreateDeterministicChildId(pair.PairId, 1));
    }

    [Fact]
    public void AdapterRejectsInvalidCycleCount()
    {
        VukerEcologySaveData saved = new VukerEcologySaveData
        {
            WorldSeed = 1,
            Pairs =
            {
                new VukerPairSaveData
                {
                    PairId = "pair-invalid",
                    FirstParentId = Id(1).ToString(),
                    SecondParentId = Id(2).ToString(),
                    SuccessfulCycles = 4,
                },
            },
        };

        Result<VukerEcologyState> restored = VukerEcologySaveAdapter.Decode(saved, 1);

        Assert.True(restored.IsFailure);
        Assert.Equal(VukerEcologyErrors.InvalidSnapshot, restored.Error);
    }

    [Fact]
    public void VersionThirteenMigrationAddsEmptyVukerSection()
    {
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = 13,
            Metadata = new SaveMetadataData
            {
                WorldSeed = 4455,
                SimulationTick = 99,
            },
            Vukers = null!,
        };

        new SaveVersionThirteenVukerEcologyMigration().Apply(document);

        Assert.Equal(14, document.FormatVersion);
        Assert.NotNull(document.Vukers);
        Assert.Equal((ulong)4455, document.Vukers.WorldSeed);
        Assert.Equal(99, document.Vukers.CurrentTick);
        Assert.Empty(document.Vukers.Individuals);
        Assert.Empty(document.Vukers.Pairs);
    }

    private static EntityId Id(int suffix) => EntityId.Parse(
        "e400000000000000000000000000" + suffix.ToString("D4"));
}

}
