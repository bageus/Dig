using System;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ExcavationTemplateInstanceTests
{
    [Fact]
    public void Instance_captures_stable_mask_entrances_unlock_and_provenance()
    {
        CaveRoomPlan plan = CreatePlan(CaveRoomPresetKind.Small);
        CaveRoomTemplatePlacementUnlock unlock = CaptureUnlock(
            CaveRoomPresetKind.Small,
            maximumStoneworkUnits: 0);

        ExcavationTemplateInstance instance = new ExcavationTemplateInstanceFactory().Create(
            "template-instance.0001",
            plan,
            unlock,
            "cave.arch.rounded");

        Assert.Equal("template-instance.0001", instance.Id);
        Assert.Equal(plan.Preset.Id, instance.TemplateId);
        Assert.Equal(plan.Preset.Version, instance.TemplateVersion);
        Assert.Equal(plan.VolumeCells.OrderBy(cell => cell), instance.OrderedMask);
        Assert.Equal(new CellId(8, 9, 0), instance.LeftEntrance.Cell);
        Assert.Equal(new CellId(12, 9, 0), instance.RightEntrance.Cell);
        Assert.Equal("X", instance.OrientationAxis);
        Assert.False(instance.AllowsMirror);
        Assert.Equal("cave.arch.rounded", instance.StyleId);
        Assert.Same(unlock, instance.Unlock);
    }

    [Fact]
    public void Progress_is_independent_of_worker_completion_order_and_idempotent()
    {
        CaveRoomPlan plan = CreatePlan(CaveRoomPresetKind.Small);
        CaveRoomTemplateInstanceFactory factory = new CaveRoomTemplateInstanceFactory();
        CaveRoomTemplatePlacementUnlock unlock = CaptureUnlock(
            CaveRoomPresetKind.Small,
            maximumStoneworkUnits: 0);
        ExcavationTemplateInstance forward = factory.Create("forward", plan, unlock, "style");
        ExcavationTemplateInstance reverse = factory.Create("reverse", plan, unlock, "style");

        foreach (CellId cell in plan.VolumeCells)
        {
            Assert.True(forward.MarkExcavated(cell));
            Assert.False(forward.MarkExcavated(cell));
        }

        foreach (CellId cell in plan.VolumeCells.Reverse())
        {
            Assert.True(reverse.MarkExcavated(cell));
        }

        Assert.Equal(1d, forward.Progress);
        Assert.Equal(forward.Progress, reverse.Progress);
        Assert.Equal(
            ExcavationTemplateLifecycleState.Completed,
            forward.LifecycleState);
        Assert.Equal(
            ExcavationTemplateLifecycleState.Completed,
            reverse.LifecycleState);
    }

    [Fact]
    public void Cancellation_preserves_excavated_cells_and_cancels_only_pending_targets()
    {
        CaveRoomPlan plan = CreatePlan(CaveRoomPresetKind.Small);
        ExcavationTemplateInstance instance = new ExcavationTemplateInstanceFactory().Create(
            "cancel",
            plan,
            CaptureUnlock(CaveRoomPresetKind.Small, 0),
            "style");
        CellId excavated = plan.VolumeCells[0];
        instance.MarkExcavated(excavated);

        instance.CancelPending();

        Assert.Equal(
            ExcavationTemplateLifecycleState.Cancelled,
            instance.LifecycleState);
        Assert.Equal(
            ExcavationTemplateCellState.Excavated,
            instance.CellProgress.Single(value => value.Cell == excavated).State);
        Assert.All(
            instance.CellProgress.Where(value => value.Cell != excavated),
            value => Assert.Equal(
                ExcavationTemplateCellState.Cancelled,
                value.State));
    }

    [Fact]
    public void Unlock_snapshot_must_match_the_planned_template()
    {
        CaveRoomPlan plan = CreatePlan(CaveRoomPresetKind.Small);
        CaveRoomTemplatePlacementUnlock wrongUnlock = CaptureUnlock(
            CaveRoomPresetKind.Medium,
            maximumStoneworkUnits: 2_000);

        Assert.Throws<ArgumentException>(() =>
            new ExcavationTemplateInstanceFactory().Create(
                "invalid",
                plan,
                wrongUnlock,
                "style"));
    }

    private static CaveRoomTemplatePlacementUnlock CaptureUnlock(
        CaveRoomPresetKind kind,
        int maximumStoneworkUnits)
    {
        CaveRoomTemplateUnlockState state = new CaveRoomTemplateUnlockEvaluator()
            .Evaluate(new[]
            {
                new CaveRoomTemplateCandidate(
                    "resident.builder",
                    maximumStoneworkUnits,
                    isEligible: true),
            })
            .Get(kind);
        return CaveRoomTemplatePlacementUnlock.Capture(state);
    }

    private static CaveRoomPlan CreatePlan(CaveRoomPresetKind kind)
    {
        CaveRoomPreset preset = CaveRoomPresetCatalog.Get(kind);
        CellId anchor = new CellId(10, 9, 0);
        CellId[] volume = Enumerable.Range(0, preset.Depth)
            .SelectMany(z => new[]
            {
                new CellId(8, 9, z),
                new CellId(9, 9, z),
                new CellId(10, 9, z),
                new CellId(11, 9, z),
                new CellId(12, 9, z),
            })
            .ToArray();
        return CaveRoomPlan.CreateSnapshot(
            preset,
            anchor,
            volume.Where(cell => cell.Z == 0 && cell != anchor),
            volume,
            Array.Empty<CellId>());
    }
}

}
