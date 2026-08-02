using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class VukerEcologyStateTests
{
    private static readonly VukerRegionKey Region =
        new VukerRegionKey(new CellId(0, 1, 0));

    [Fact]
    public void AdultsFormStablePairAndFirstBirthIsDueAfterSevenDays()
    {
        VukerEcologyState state = CreatePair(out EntityId first, out EntityId second);

        VukerPairSnapshot pair = Assert.Single(state.GetPairs());
        Assert.Equal(first, pair.FirstParentId);
        Assert.Equal(second, pair.SecondParentId);
        Assert.Equal(VukerEcologyProfile.ReproductionCooldownTicks, pair.NextBirthTick);
        Assert.Empty(state.Advance(pair.NextBirthTick - 1));
        Assert.Equal(pair.PairId, Assert.Single(state.Advance(pair.NextBirthTick)).PairId);
    }

    [Fact]
    public void SuccessfulCycleCreatesOneChildAndRestartsSevenDayCooldown()
    {
        VukerEcologyState state = CreatePair(out _, out _);
        VukerPairSnapshot due = Assert.Single(state.Advance(
            VukerEcologyProfile.ReproductionCooldownTicks));
        EntityId childId = state.CreateDeterministicChildId(
            due.PairId,
            due.SuccessfulCycles);

        Assert.True(state.CommitBirth(
            due.PairId,
            childId,
            due.Region,
            new CellId(2, 1, 0),
            due.NextBirthTick).IsSuccess);

        VukerIndividualSnapshot child = state.GetIndividual(childId)!;
        VukerPairSnapshot pair = state.GetPair(due.PairId)!;
        Assert.Equal(VukerLifecycleStage.Child, child.Lifecycle);
        Assert.Equal(due.NextBirthTick + VukerEcologyProfile.ChildGrowthTicks,
            child.MaturityTick);
        Assert.Equal(1, pair.SuccessfulCycles);
        Assert.Equal(due.NextBirthTick + VukerEcologyProfile.ReproductionCooldownTicks,
            pair.NextBirthTick);
    }

    [Fact]
    public void ChildPatrolActorIsNotCombatEligibleUntilThreeDayMaturity()
    {
        VukerEcologyState state = CreatePair(out _, out _);
        long birthTick = VukerEcologyProfile.ReproductionCooldownTicks;
        VukerPairSnapshot pair = Assert.Single(state.Advance(birthTick));
        EntityId childId = state.CreateDeterministicChildId(pair.PairId, 0);
        Assert.True(state.CommitBirth(
            pair.PairId,
            childId,
            pair.Region,
            new CellId(2, 1, 0),
            birthTick).IsSuccess);

        Assert.False(state.IsCombatEligible(childId));
        state.Advance(birthTick + VukerEcologyProfile.ChildGrowthTicks - 1);
        Assert.False(state.IsCombatEligible(childId));
        state.Advance(birthTick + VukerEcologyProfile.ChildGrowthTicks);
        Assert.True(state.IsCombatEligible(childId));
        Assert.Equal(VukerLifecycleStage.Adult, state.GetIndividual(childId)!.Lifecycle);
    }

    [Fact]
    public void CapIsTenPerRegionAndBlockedAttemptConsumesNoCycle()
    {
        VukerEcologyState state = new VukerEcologyState(44);
        for (int index = 0; index < VukerEcologyProfile.PopulationCapPerRegion; index++)
        {
            Assert.True(state.RegisterAdult(
                Id(index + 1),
                Region,
                new CellId(index, 1, 0),
                VukerDisposition.Wild,
                0).IsSuccess);
        }

        VukerPairSnapshot pair = state.Advance(
            VukerEcologyProfile.ReproductionCooldownTicks).First();
        EntityId childId = state.CreateDeterministicChildId(pair.PairId, 0);
        Result result = state.CommitBirth(
            pair.PairId,
            childId,
            Region,
            new CellId(20, 1, 0),
            pair.NextBirthTick);

        Assert.Equal(VukerEcologyErrors.PopulationCapReached, result.Error);
        VukerPairSnapshot blocked = state.GetPair(pair.PairId)!;
        Assert.Equal(0, blocked.SuccessfulCycles);
        Assert.Equal("population_cap", blocked.BlockedReason);
        Assert.True(blocked.IsDue(pair.NextBirthTick + 1));
    }

    [Fact]
    public void ExhaustedPairDoesNotResetItsCycleBudget()
    {
        VukerEcologyState state = CreatePair(out EntityId first, out EntityId second);
        VukerPairId pairId = Assert.Single(state.GetPairs()).PairId;
        long tick = VukerEcologyProfile.ReproductionCooldownTicks;
        for (int cycle = 0;
            cycle < VukerEcologyProfile.MaximumSuccessfulCyclesPerPair;
            cycle++)
        {
            VukerPairSnapshot due = state.Advance(tick)
                .Single(value => value.PairId == pairId);
            EntityId childId = state.CreateDeterministicChildId(pairId, cycle);
            Assert.True(state.CommitBirth(
                pairId,
                childId,
                Region,
                new CellId(2 + cycle, 1, 0),
                tick).IsSuccess);
            tick += VukerEcologyProfile.ReproductionCooldownTicks;
        }

        state.Advance(tick + VukerEcologyProfile.ReproductionCooldownTicks);

        VukerPairSnapshot exhausted = state.GetPair(pairId)!;
        Assert.True(exhausted.IsActive);
        Assert.Equal("cycle_limit_reached", exhausted.TerminalReason);
        Assert.Equal(VukerEcologyProfile.MaximumSuccessfulCyclesPerPair,
            exhausted.SuccessfulCycles);
        Assert.Equal(pairId, state.GetIndividual(first)!.ActivePairId);
        Assert.Equal(pairId, state.GetIndividual(second)!.ActivePairId);
        Assert.DoesNotContain(state.GetPairs(), value =>
            value.PairId != pairId
            && (value.FirstParentId == first || value.SecondParentId == first));
    }

    [Fact]
    public void ParentDeathBreaksOldPairAndSurvivorCanFormNewPair()
    {
        VukerEcologyState state = CreatePair(out EntityId first, out EntityId second);
        VukerPairId oldPair = Assert.Single(state.GetPairs()).PairId;
        EntityId replacement = Id(3);
        Assert.True(state.RegisterAdult(
            replacement,
            Region,
            new CellId(3, 1, 0),
            VukerDisposition.Wild,
            1).IsSuccess);
        Assert.True(state.SynchronizeActor(
            second,
            Region,
            new CellId(1, 1, 0),
            isAlive: false,
            tick: 2).IsSuccess);

        state.Advance(2);

        Assert.False(state.GetPair(oldPair)!.IsActive);
        VukerPairSnapshot newPair = state.GetPairs().Single(value => value.IsActive);
        Assert.Contains(first, new[] { newPair.FirstParentId, newPair.SecondParentId });
        Assert.Contains(replacement, new[] { newPair.FirstParentId, newPair.SecondParentId });
        Assert.Equal(0, newPair.SuccessfulCycles);
    }

    [Fact]
    public void AltKidnapReservationCommitsTamedChildAndPersistsOwner()
    {
        VukerEcologyState state = CreatePair(out _, out _);
        long birthTick = VukerEcologyProfile.ReproductionCooldownTicks;
        VukerPairSnapshot pair = Assert.Single(state.Advance(birthTick));
        EntityId childId = state.CreateDeterministicChildId(pair.PairId, 0);
        EntityId residentId = Id(90);
        Assert.True(state.CommitBirth(
            pair.PairId,
            childId,
            Region,
            new CellId(2, 1, 0),
            birthTick).IsSuccess);

        Assert.True(state.ReserveKidnap(childId, residentId, birthTick + 1).IsSuccess);
        Assert.True(state.CommitTame(childId, residentId, birthTick + 2).IsSuccess);

        VukerIndividualSnapshot child = state.GetIndividual(childId)!;
        Assert.Equal(VukerDisposition.Tamed, child.Disposition);
        Assert.Equal(residentId, child.TamedByResidentId);
        Assert.Null(child.KidnapReservedBy);
        Assert.False(state.IsWildChild(childId));
        Assert.True(state.IsTamed(childId));
    }

    [Fact]
    public void SnapshotRestorePreservesNextDeterministicLifecycleResult()
    {
        VukerEcologyState state = CreatePair(out _, out _);
        long dueTick = VukerEcologyProfile.ReproductionCooldownTicks;
        VukerPairSnapshot before = Assert.Single(state.Advance(dueTick));
        Assert.True(state.RecordBirthBlocked(
            before.PairId,
            "ecology.vuker.birth_cell_blocked",
            dueTick).IsSuccess);

        Result<VukerEcologyState> restored = VukerEcologyState.Restore(
            state.CaptureSnapshot());

        Assert.True(restored.IsSuccess);
        VukerPairSnapshot after = Assert.Single(restored.Value.Advance(dueTick));
        Assert.Equal(before.PairId, after.PairId);
        Assert.Equal(before.SuccessfulCycles, after.SuccessfulCycles);
        Assert.Equal("ecology.vuker.birth_cell_blocked", after.BlockedReason);
        Assert.Equal(
            state.CreateDeterministicChildId(before.PairId, 0),
            restored.Value.CreateDeterministicChildId(after.PairId, 0));
    }

    private static VukerEcologyState CreatePair(
        out EntityId first,
        out EntityId second)
    {
        VukerEcologyState state = new VukerEcologyState(1234);
        first = Id(1);
        second = Id(2);
        Assert.True(state.RegisterAdult(
            first, Region, new CellId(0, 1, 0), VukerDisposition.Wild, 0).IsSuccess);
        Assert.True(state.RegisterAdult(
            second, Region, new CellId(1, 1, 0), VukerDisposition.Wild, 0).IsSuccess);
        state.Advance(0);
        return state;
    }

    private static EntityId Id(int suffix) => EntityId.Parse(
        "e200000000000000000000000000" + suffix.ToString("D4"));
}

}
