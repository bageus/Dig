using System;
using Dig.Application.Ecology;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialMovementPlannerTests
{
    [Fact]
    public void PlannerAcceptsDiagonalAndDepthOnlyCandidatesDeterministically()
    {
        LivingMaterialSnapshot grub = Creature(LivingMaterialSpecies.Grub);
        LivingMaterialMovementPlanner planner = new LivingMaterialMovementPlanner();
        CellId current = grub.Cell!.Value;
        CellId diagonal = new CellId(current.X + 1, current.Y, current.Z + 1);
        CellId depth = new CellId(current.X, current.Y, current.Z + 1);

        LivingMaterialMovementDecision diagonalDecision = planner.Plan(
            grub,
            new[] { diagonal },
            Array.Empty<CellId>(),
            worldSeed: 99);
        LivingMaterialMovementDecision depthDecision = planner.Plan(
            grub,
            new[] { depth },
            Array.Empty<CellId>(),
            worldSeed: 99);

        Assert.True(diagonalDecision.CanMove);
        Assert.Equal(diagonal, diagonalDecision.Target);
        Assert.True(depthDecision.CanMove);
        Assert.Equal(depth, depthDecision.Target);
        Assert.NotEqual(0, depthDecision.NextDirection);
    }

    [Fact]
    public void HamsterSelectsCandidateFartherFromNearbyResidentAcrossDepth()
    {
        LivingMaterialSnapshot hamster = Creature(LivingMaterialSpecies.Hamster);
        LivingMaterialMovementPlanner planner = new LivingMaterialMovementPlanner();
        CellId current = hamster.Cell!.Value;
        CellId toward = new CellId(current.X, current.Y, current.Z - 1);
        CellId away = new CellId(current.X, current.Y, current.Z + 1);

        LivingMaterialMovementDecision decision = planner.Plan(
            hamster,
            new[] { toward, away },
            new[] { toward },
            worldSeed: 99);

        Assert.True(decision.CanMove);
        Assert.Equal(away, decision.Target);
    }

    private static LivingMaterialSnapshot Creature(LivingMaterialSpecies species)
    {
        LivingMaterialEcologyState state = new LivingMaterialEcologyState(99);
        EntityId id = EntityId.Parse(species == LivingMaterialSpecies.Hamster
            ? "31000000000000000000000000000001"
            : "31000000000000000000000000000002");
        CellId cell = new CellId(4, 2, 1);
        LivingMaterialPlaneKey plane = new LivingMaterialPlaneKey(new CellId(1, 2, 0));
        Assert.True(state.Register(id, id, species, cell, plane, 0).IsSuccess);
        return state.Get(id)!;
    }
}

}
