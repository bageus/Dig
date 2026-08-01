using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Ecology;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class LivingMaterialMovementPlayModeTests
{
    [Test]
    public void DropDormancyAllowsDiagonalAndDepthMovementButRejectsHeightChange()
    {
        LivingMaterialEcologyState state = new LivingMaterialEcologyState(42);
        EntityId hamsterId = Id(10);
        CellId dropped = new CellId(5, 3, 0);
        LivingMaterialPlaneKey plane = new LivingMaterialPlaneKey(
            new CellId(2, 3, 0));
        Assert.That(state.Register(
            hamsterId,
            hamsterId,
            LivingMaterialSpecies.Hamster,
            worldCell: null,
            plane,
            tick: 0).IsSuccess, Is.True);
        Assert.That(state.Release(hamsterId, dropped, plane, tick: 0).IsSuccess, Is.True);
        Assert.That(state.Get(hamsterId)!.Activity,
            Is.EqualTo(LivingMaterialActivity.ReleaseDormant));

        Assert.That(state.AdvanceOneEcologyStep(1).IsSuccess, Is.True);
        Assert.That(state.Get(hamsterId)!.Activity,
            Is.EqualTo(LivingMaterialActivity.Moving));
        AdvanceUntilMovementDue(state, hamsterId, tick: 2);

        Result diagonal = state.CommitMovement(
            hamsterId,
            new CellId(6, 3, 1),
            plane,
            direction: 1,
            tick: 8);
        Assert.That(diagonal.IsSuccess, Is.True);
        AdvanceUntilMovementDue(state, hamsterId, tick: 9);

        Result depth = state.CommitMovement(
            hamsterId,
            new CellId(6, 3, 2),
            plane,
            direction: 1,
            tick: 15);
        Assert.That(depth.IsSuccess, Is.True);
        AdvanceUntilMovementDue(state, hamsterId, tick: 16);

        Result vertical = state.CommitMovement(
            hamsterId,
            new CellId(6, 2, 2),
            plane,
            direction: 1,
            tick: 22);

        Assert.That(vertical.IsFailure, Is.True);
        Assert.That(vertical.Error, Is.EqualTo(LivingMaterialErrors.InvalidMovement));
        Assert.That(state.Get(hamsterId)!.Cell, Is.EqualTo(new CellId(6, 3, 2)));
    }

    [Test]
    public void NavigationResolverAllowsDiagonalAndDepthWithoutCornerCutOrHeightChange()
    {
        CellId from = new CellId(3, 2, 0);
        CellId sideX = new CellId(4, 2, 0);
        CellId sideZ = new CellId(3, 2, 1);
        CellId diagonal = new CellId(4, 2, 1);
        CellId vertical = new CellId(3, 3, 0);
        LivingMaterialPlaneResolver open = Resolver(
            from,
            sideX,
            sideZ,
            diagonal,
            vertical);
        Assert.That(open.TryResolve(from, out LivingMaterialPlane plane), Is.True);
        LivingMaterialSnapshot creature = Snapshot(Id(11), from, plane.Key);

        IReadOnlyList<CellId> candidates = open.GetMovementCandidates(creature);
        Assert.That(candidates, Has.Member(sideZ));
        Assert.That(candidates, Has.Member(diagonal));
        Assert.That(candidates, Has.No.Member(vertical));

        LivingMaterialPlaneResolver blocked = Resolver(from, sideX, diagonal);
        Assert.That(blocked.TryResolve(from, out LivingMaterialPlane blockedPlane), Is.True);
        LivingMaterialSnapshot blockedCreature = Snapshot(Id(12), from, blockedPlane.Key);
        Assert.That(blocked.GetMovementCandidates(blockedCreature),
            Has.No.Member(diagonal));
    }

    private static LivingMaterialPlaneResolver Resolver(params CellId[] openCells)
    {
        MaterialId air = new MaterialId("air");
        MaterialId stone = new MaterialId("stone");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(air, isSolid: false, hardness: 0),
            new MaterialDefinition(stone, isSolid: true, hardness: 10),
        });
        Result<WorldState> created = WorldState.CreateFilled(
            new WorldSize(8, 6),
            4,
            materials,
            stone,
            explored: true);
        Assert.That(created.IsSuccess, Is.True);
        List<TerrainChange> changes = openCells
            .Distinct()
            .Select(value => new TerrainChange(
                value,
                new CellState(
                    air,
                    CellDesignation.None,
                    isExplored: true,
                    damage: 0,
                    temperature: 20)))
            .ToList();
        Assert.That(created.Value.ApplyTerrainChanges(changes, tick: 1).IsSuccess, Is.True);
        NavigationMap map = new NavigationMap(TraversalProfile.CreateFreeMover());
        Assert.That(map.Rebuild(
            created.Value.CreateSnapshot(),
            Array.Empty<TraversalLink>()).IsSuccess, Is.True);
        Result<NavigationSnapshot> snapshot = map.GetSnapshot();
        Assert.That(snapshot.IsSuccess, Is.True);
        return new LivingMaterialPlaneResolver(snapshot.Value);
    }

    private static LivingMaterialSnapshot Snapshot(
        EntityId id,
        CellId cell,
        LivingMaterialPlaneKey planeKey)
    {
        return new LivingMaterialSnapshot(
            id,
            id,
            LivingMaterialSpecies.Grub,
            LivingMaterialContainment.Free,
            cell,
            cell,
            planeKey,
            direction: 1,
            activity: LivingMaterialActivity.Moving,
            activityStepsRemaining: 0,
            movementCredit: 0,
            successfulMovementSteps: 0,
            nextSearchAtStep: int.MaxValue,
            nextSleepAtStep: int.MaxValue,
            reproductionCyclesCompleted: 0,
            nextReproductionStep: 96,
            deterministicSequence: 0,
            blockedReason: null,
            version: 1);
    }

    private static void AdvanceUntilMovementDue(
        LivingMaterialEcologyState state,
        EntityId creatureId,
        long tick)
    {
        while (!state.Get(creatureId)!.IsMovementDue)
        {
            Assert.That(state.AdvanceOneEcologyStep(tick++).IsSuccess, Is.True);
        }
    }

    private static EntityId Id(int suffix) => EntityId.Parse(
        "7600000000000000000000000000" + suffix.ToString("D4"));
}

}
