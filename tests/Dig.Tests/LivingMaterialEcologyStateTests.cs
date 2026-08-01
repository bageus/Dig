using System;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialEcologyStateTests
{
    [Fact]
    public void ProfilesMatchApprovedCadenceAndRadius()
    {
        Assert.Equal(96, LivingMaterialEcologyProfiles.EcologyStepsPerDay);
        Assert.Equal(4, LivingMaterialEcologyProfiles.EcologyStepsPerSimulationTick);
        Assert.Equal(6, LivingMaterialEcologyProfiles.Hamster.WanderRadius);
        Assert.Equal(800, LivingMaterialEcologyProfiles.Hamster.MovementCreditPerEcologyStep);
        Assert.Equal(4, LivingMaterialEcologyProfiles.Grub.WanderRadius);
        Assert.Equal(650, LivingMaterialEcologyProfiles.Grub.MovementCreditPerEcologyStep);
        Assert.Equal(10, LivingMaterialEcologyProfiles.PopulationCapPerPlane);
    }

    [Fact]
    public void HamsterReleaseIsDormantForExactlyOneEcologyStep()
    {
        LivingMaterialEcologyState state = new LivingMaterialEcologyState(42);
        EntityId id = Id(1);
        CellId cell = new CellId(4, 2, 0);
        LivingMaterialPlaneKey plane = new LivingMaterialPlaneKey(new CellId(1, 2, 0));
        Assert.True(state.Register(id, id, LivingMaterialSpecies.Hamster, null, plane, 0).IsSuccess);
        Assert.True(state.Release(id, cell, plane, 0).IsSuccess);
        Assert.Equal(LivingMaterialActivity.ReleaseDormant, state.Get(id)!.Activity);

        Assert.True(state.AdvanceOneEcologyStep(1).IsSuccess);

        LivingMaterialSnapshot snapshot = state.Get(id)!;
        Assert.Equal(LivingMaterialActivity.Moving, snapshot.Activity);
        Assert.Equal(0, snapshot.ActivityStepsRemaining);
        Assert.Equal(0, snapshot.MovementCredit);
        Assert.True(state.AdvanceOneEcologyStep(1).IsSuccess);
        Assert.Equal(800, state.Get(id)!.MovementCredit);
    }

    [Theory]
    [InlineData(LivingMaterialSpecies.Hamster, 4, 3200, false)]
    [InlineData(LivingMaterialSpecies.Hamster, 5, 4000, true)]
    [InlineData(LivingMaterialSpecies.Grub, 6, 3900, false)]
    [InlineData(LivingMaterialSpecies.Grub, 7, 4000, true)]
    public void FixedPointMovementCreditIsDeterministic(
        LivingMaterialSpecies species,
        int steps,
        int expectedCredit,
        bool due)
    {
        LivingMaterialEcologyState state = CreateFreeState(species, out EntityId id);
        for (int index = 0; index < steps; index++)
        {
            Assert.True(state.AdvanceOneEcologyStep(index + 1).IsSuccess);
        }

        LivingMaterialSnapshot snapshot = state.Get(id)!;
        Assert.Equal(expectedCredit, snapshot.MovementCredit);
        Assert.Equal(due, snapshot.IsMovementDue);
    }

    [Fact]
    public void HamsterEventuallySearchesAndSleepsWithinApprovedBands()
    {
        LivingMaterialEcologyState state = CreateFreeState(
            LivingMaterialSpecies.Hamster,
            out EntityId id);
        CellId left = new CellId(5, 2, 0);
        CellId right = new CellId(6, 2, 0);
        LivingMaterialPlaneKey plane = state.Get(id)!.PlaneKey;
        bool searched = false;
        bool slept = false;

        for (int movement = 0; movement < 40; movement++)
        {
            while (!state.Get(id)!.IsMovementDue)
            {
                Assert.True(state.AdvanceOneEcologyStep(movement + 1).IsSuccess);
            }

            LivingMaterialSnapshot before = state.Get(id)!;
            CellId target = before.Cell == left ? right : left;
            int direction = target.X > before.Cell!.Value.X ? 1 : -1;
            Assert.True(state.CommitMovement(id, target, plane, direction, movement + 1).IsSuccess);
            LivingMaterialSnapshot after = state.Get(id)!;
            searched |= after.Activity == LivingMaterialActivity.HamsterSearching;
            slept |= after.Activity == LivingMaterialActivity.HamsterSleeping;
            while (after.ActivityStepsRemaining > 0)
            {
                Assert.True(state.AdvanceOneEcologyStep(movement + 1).IsSuccess);
                after = state.Get(id)!;
            }
        }

        Assert.True(searched);
        Assert.True(slept);
    }

    [Fact]
    public void ReproductionCommitCreatesNewbornWithTwoCycleBudget()
    {
        LivingMaterialEcologyState state = CreateFreeState(
            LivingMaterialSpecies.Grub,
            out EntityId parentId);
        for (int index = 0; index < 96; index++)
        {
            Assert.True(state.AdvanceOneEcologyStep(index + 1).IsSuccess);
        }

        CellId cell = state.Get(parentId)!.Cell!.Value;
        Result<LivingMaterialReproductionPlan> plan = state.PlanReproduction(parentId, cell);
        Assert.True(plan.IsSuccess);
        Assert.True(state.CommitReproduction(plan.Value, 24).IsSuccess);

        LivingMaterialSnapshot parent = state.Get(parentId)!;
        LivingMaterialSnapshot newborn = state.Get(plan.Value.OffspringId)!;
        Assert.Equal(1, parent.ReproductionCyclesCompleted);
        Assert.Equal(0, newborn.ReproductionCyclesCompleted);
        Assert.Equal(state.EcologyStep + 96, newborn.NextReproductionStep);
        Assert.Equal(2, LivingMaterialEcologyProfiles.MaximumSuccessfulCycles);
    }

    [Fact]
    public void MovementAcceptsDiagonalAndDepthStepsButRejectsHeightAndLongJumps()
    {
        LivingMaterialEcologyState state = CreateFreeState(
            LivingMaterialSpecies.Grub,
            out EntityId id);
        LivingMaterialPlaneKey plane = state.Get(id)!.PlaneKey;
        AdvanceUntilMovementDue(state, id);

        Result diagonal = state.CommitMovement(
            id,
            new CellId(6, 2, 1),
            plane,
            direction: 1,
            tick: 1);
        Assert.True(diagonal.IsSuccess);

        AdvanceUntilMovementDue(state, id);
        Result depth = state.CommitMovement(
            id,
            new CellId(6, 2, 2),
            plane,
            direction: 1,
            tick: 2);
        Assert.True(depth.IsSuccess);

        AdvanceUntilMovementDue(state, id);
        Assert.Equal(LivingMaterialErrors.InvalidMovement, state.CommitMovement(
            id,
            new CellId(6, 3, 2),
            plane,
            direction: 1,
            tick: 3).Error);
        Assert.Equal(LivingMaterialErrors.InvalidMovement, state.CommitMovement(
            id,
            new CellId(8, 2, 2),
            plane,
            direction: 1,
            tick: 3).Error);
    }

    [Fact]
    public void MovementRegionRebindPreservesCreditActivityAndDeterministicSequence()
    {
        LivingMaterialEcologyState state = CreateFreeState(
            LivingMaterialSpecies.Hamster,
            out EntityId id);
        AdvanceUntilMovementDue(state, id);
        LivingMaterialSnapshot before = state.Get(id)!;
        LivingMaterialPlaneKey merged = new LivingMaterialPlaneKey(new CellId(0, 2, 0));

        Result rebound = state.RebindMovementRegion(
            id,
            before.Cell!.Value,
            before.AnchorCell,
            merged,
            tick: 4);

        Assert.True(rebound.IsSuccess);
        LivingMaterialSnapshot after = state.Get(id)!;
        Assert.Equal(merged, after.PlaneKey);
        Assert.Equal(before.AnchorCell, after.AnchorCell);
        Assert.Equal(before.Activity, after.Activity);
        Assert.Equal(before.MovementCredit, after.MovementCredit);
        Assert.Equal(before.Direction, after.Direction);
        Assert.Equal(before.DeterministicSequence, after.DeterministicSequence);
    }

    [Fact]
    public void SnapshotRestorePreservesNextDeterministicResult()
    {
        LivingMaterialEcologyState state = CreateFreeState(
            LivingMaterialSpecies.Hamster,
            out EntityId id);
        for (int index = 0; index < 5; index++)
        {
            Assert.True(state.AdvanceOneEcologyStep(index + 1).IsSuccess);
        }

        LivingMaterialEcologySnapshot snapshot = state.CaptureSnapshot();
        Result<LivingMaterialEcologyState> restored = LivingMaterialEcologyState.Restore(snapshot);
        Assert.True(restored.IsSuccess);
        Assert.Equal(state.Get(id)!.MovementCredit, restored.Value.Get(id)!.MovementCredit);
        Assert.Equal(state.Get(id)!.Direction, restored.Value.Get(id)!.Direction);
        Assert.Equal(state.Get(id)!.DeterministicSequence, restored.Value.Get(id)!.DeterministicSequence);
    }

    private static void AdvanceUntilMovementDue(
        LivingMaterialEcologyState state,
        EntityId id)
    {
        long tick = 1;
        while (!state.Get(id)!.IsMovementDue)
        {
            Assert.True(state.AdvanceOneEcologyStep(tick++).IsSuccess);
        }
    }

    private static LivingMaterialEcologyState CreateFreeState(
        LivingMaterialSpecies species,
        out EntityId id)
    {
        LivingMaterialEcologyState state = new LivingMaterialEcologyState(1234);
        id = Id(species == LivingMaterialSpecies.Hamster ? 10 : 20);
        CellId cell = new CellId(5, 2, 0);
        LivingMaterialPlaneKey plane = new LivingMaterialPlaneKey(new CellId(1, 2, 0));
        Assert.True(state.Register(id, id, species, cell, plane, 0).IsSuccess);
        return state;
    }

    private static EntityId Id(int suffix) => EntityId.Parse(
        "1000000000000000000000000000" + suffix.ToString("D4"));
}

}
