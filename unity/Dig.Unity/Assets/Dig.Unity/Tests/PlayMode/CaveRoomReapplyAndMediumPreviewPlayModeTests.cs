using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class CaveRoomReapplyAndMediumPreviewPlayModeTests
{
    [Test]
    public void Medium_pointer_on_each_front_row_can_resolve_an_even_width_anchor()
    {
        CaveRoomPreset preset = CaveRoomPresetCatalog.Get(CaveRoomPresetKind.Medium);
        CellId entrance = new CellId(10, 9, CellId.MinimumDepth);
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
        CaveRoomPlanResult planned = FindPlan(world, CaveRoomPresetKind.Small);
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
        Assert.That(resumed.Plan!.ExcavationCells, Does.Not.Contain(completed.Cell));
        Assert.That(world.ApplyCaveRoomPlan(resumed.Plan).IsSuccess, Is.True);
        Result<CellSnapshot> remaining = world.Repository.Get().GetCell(eraseTarget);
        Assert.That(remaining.IsSuccess, Is.True);
        Assert.That(remaining.Value.State.Designation, Is.EqualTo(CellDesignation.Dig));
    }

    private static CaveRoomPlanResult FindPlan(
        DigWorldSession world,
        CaveRoomPresetKind kind)
    {
        for (int y = 2; y < 13; y++)
        {
            for (int x = 2; x < 18; x++)
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

        return world.PlanCaveRoom(kind, new CellId(10, 9, CellId.MinimumDepth));
    }
}

}
