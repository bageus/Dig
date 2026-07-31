using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class CaveRoomReapplyAndMediumPreviewPlayModeTests
{
    [TestCase(CaveRoomPresetKind.Small)]
    [TestCase(CaveRoomPresetKind.Medium)]
    [TestCase(CaveRoomPresetKind.Large)]
    [TestCase(CaveRoomPresetKind.Tall)]
    public void Pointer_on_each_front_silhouette_cell_resolves_the_same_anchor(
        CaveRoomPresetKind kind)
    {
        CaveRoomPreset preset = CaveRoomPresetCatalog.Get(kind);
        CellId entrance = new CellId(18, 12, CellId.MinimumDepth);
        for (int level = 0; level < preset.Height; level++)
        {
            CaveRoomRowProfile row = CaveRoomPlanner.ResolveRowProfile(
                preset,
                entrance.X,
                level);
            foreach (int x in row.RequiredQuartersByX.Keys)
            {
                CellId pointer = new CellId(
                    x,
                    entrance.Y - level,
                    CellId.MinimumDepth);
                Assert.That(
                    CaveRoomPlacementCandidateResolver.Resolve(preset, pointer),
                    Does.Contain(entrance));
            }
        }
    }

    [Test]
    public void Erased_incomplete_room_can_be_reapplied_without_redesignating_open_target()
    {
        DigWorldSession world = DigWorldSession.CreateDemo(20, 14, 5);
        CaveRoomPlanResult planned = FindPlan(
            world,
            CaveRoomPresetKind.Small,
            width: 20,
            height: 14);
        Assert.That(planned.Succeeded, Is.True, planned.Detail);
        CaveRoomPlan original = planned.Plan!;
        Assert.That(world.ApplyCaveRoomPlan(original).IsSuccess, Is.True);
        CaveRoomExcavationTarget completed = original.ExcavationTargets
            .First(value => value.IsFullCell);
        Assert.That(world.ExcavateSpatialCell(completed.Cell).IsSuccess, Is.True);

        CellId eraseTarget = original.ExcavationCells
            .First(value => value != completed.Cell);
        CellId[] erased = world.ExpandExcavationEraseCells(new[] { eraseTarget })
            .ToArray();
        for (int index = 0; index < erased.Length; index++)
        {
            Result<CellSnapshot> cell = world.Repository.Get().GetCell(erased[index]);
            if (cell.IsSuccess
                && cell.Value.IsSolid
                && cell.Value.State.Designation == CellDesignation.Dig)
            {
                Assert.That(world.SetDesignation(erased[index], active: false).IsSuccess, Is.True);
            }
        }

        world.CommitExcavationErase(erased);
        CaveRoomPlanResult resumed = world.PlanCaveRoom(
            original.Preset.Kind,
            original.Entrance);

        Assert.That(resumed.Succeeded, Is.True, resumed.Detail);
        CaveRoomPlan resumedPlan = resumed.Plan!;
        Assert.That(resumedPlan.ExcavationCells, Has.No.Member(completed.Cell));
        Assert.That(world.ApplyCaveRoomPlan(resumedPlan).IsSuccess, Is.True);
        Result<CellSnapshot> remaining = world.Repository.Get().GetCell(eraseTarget);
        Assert.That(remaining.IsSuccess, Is.True);
        Assert.That(remaining.Value.State.Designation, Is.EqualTo(CellDesignation.Dig));
    }


    [TestCase(CaveRoomPresetKind.Small)]
    [TestCase(CaveRoomPresetKind.Medium)]
    [TestCase(CaveRoomPresetKind.Large)]
    [TestCase(CaveRoomPresetKind.Tall)]
    public void Confirmed_room_plan_leaves_authoritative_dig_designations(
        CaveRoomPresetKind kind)
    {
        const int width = 48;
        const int height = 28;
        DigWorldSession world = DigWorldSession.CreateDemo(width, height, 5);
        CaveRoomPlanResult planned = FindPlan(world, kind, width, height);

        Assert.That(planned.Succeeded, Is.True, planned.Detail);
        Assert.That(world.ApplyCaveRoomPlan(planned.Plan!).IsSuccess, Is.True);
        Assert.That(
            planned.Plan!.ExcavationCells.Any(cell =>
            {
                Result<CellSnapshot> snapshot = world.Repository.Get().GetCell(cell);
                return snapshot.IsSuccess
                    && snapshot.Value.State.Designation == CellDesignation.Dig;
            }),
            Is.True,
            $"{kind} did not leave any Dig designation after confirmation.");
    }

    private static CaveRoomPlanResult FindPlan(
        DigWorldSession world,
        CaveRoomPresetKind kind,
        int width,
        int height)
    {
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                CaveRoomPlanResult result = world.PlanCaveRoom(
                    kind,
                    new CellId(x, y, CellId.MinimumDepth));
                if (result.Succeeded)
                {
                    return result;
                }
            }
        }

        return world.PlanCaveRoom(
            kind,
            new CellId(width / 2, height / 2, CellId.MinimumDepth));
    }
}

}
