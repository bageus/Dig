using System;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ExcavationTemplateInstanceSaveSnapshotTests
{
    [Fact]
    public void Partial_instance_round_trips_exact_identity_xyz_unlock_and_progress()
    {
        ExcavationTemplateInstance source = CreateInstance("partial", CaveRoomPresetKind.Medium, 2_000);
        CellId[] excavated = source.OrderedMask.Where((_, index) => index % 3 == 0).ToArray();
        foreach (CellId cell in excavated.Reverse())
        {
            Assert.True(source.MarkExcavated(cell));
        }

        ExcavationTemplateInstanceSaveSnapshot snapshot =
            ExcavationTemplateInstanceSaveSnapshot.Capture(source);
        ExcavationTemplateInstance restored = snapshot.Restore();

        Assert.Equal(source.Id, restored.Id);
        Assert.Equal(source.TemplateId, restored.TemplateId);
        Assert.Equal(source.TemplateVersion, restored.TemplateVersion);
        Assert.Equal(source.Anchor, restored.Anchor);
        Assert.Equal(source.OrientationAxis, restored.OrientationAxis);
        Assert.Equal(source.AllowsMirror, restored.AllowsMirror);
        Assert.Equal(source.StyleId, restored.StyleId);
        Assert.Equal(source.LeftEntrance.Cell, restored.LeftEntrance.Cell);
        Assert.Equal(source.RightEntrance.Cell, restored.RightEntrance.Cell);
        Assert.Equal(source.OrderedMask, restored.OrderedMask);
        Assert.Equal(source.Unlock.RequiredStoneworkUnits, restored.Unlock.RequiredStoneworkUnits);
        Assert.Equal(source.Unlock.MaximumStoneworkUnits, restored.Unlock.MaximumStoneworkUnits);
        Assert.Equal(source.Unlock.QualifyingResidentId, restored.Unlock.QualifyingResidentId);
        Assert.Equal(ExcavationTemplateLifecycleState.Active, restored.LifecycleState);
        Assert.Equal(
            source.CellProgress.Select(value => (value.Cell, value.State)),
            restored.CellProgress.Select(value => (value.Cell, value.State)));
    }

    [Fact]
    public void Cancelled_instance_round_trips_without_restoring_cancelled_cells_as_pending()
    {
        ExcavationTemplateInstance source = CreateInstance("cancelled", CaveRoomPresetKind.Small, 0);
        source.MarkExcavated(source.OrderedMask[0]);
        source.CancelPending();

        ExcavationTemplateInstance restored =
            ExcavationTemplateInstanceSaveSnapshot.Capture(source).Restore();

        Assert.Equal(ExcavationTemplateLifecycleState.Cancelled, restored.LifecycleState);
        Assert.Equal(
            ExcavationTemplateCellState.Excavated,
            restored.CellProgress.Single(value => value.Cell == source.OrderedMask[0]).State);
        Assert.All(
            restored.CellProgress.Where(value => value.Cell != source.OrderedMask[0]),
            value => Assert.Equal(ExcavationTemplateCellState.Cancelled, value.State));
    }

    [Fact]
    public void Completed_instance_round_trips_as_completed()
    {
        ExcavationTemplateInstance source = CreateInstance("completed", CaveRoomPresetKind.Small, 0);
        foreach (CellId cell in source.OrderedMask)
        {
            source.MarkExcavated(cell);
        }

        ExcavationTemplateInstance restored =
            ExcavationTemplateInstanceSaveSnapshot.Capture(source).Restore();

        Assert.Equal(ExcavationTemplateLifecycleState.Completed, restored.LifecycleState);
        Assert.Equal(1d, restored.Progress);
        Assert.All(
            restored.CellProgress,
            value => Assert.Equal(ExcavationTemplateCellState.Excavated, value.State));
    }

    [Fact]
    public void Unknown_definition_fails_before_returning_a_partial_instance()
    {
        ExcavationTemplateInstance source = CreateInstance("unknown", CaveRoomPresetKind.Small, 0);
        ExcavationTemplateInstanceSaveSnapshot valid =
            ExcavationTemplateInstanceSaveSnapshot.Capture(source);
        ExcavationTemplateInstanceSaveSnapshot unknown = new ExcavationTemplateInstanceSaveSnapshot(
            valid.FormatVersion,
            valid.InstanceId,
            "excavation.template.missing",
            valid.TemplateVersion,
            valid.Anchor,
            valid.OrientationAxis,
            valid.AllowsMirror,
            valid.StyleId,
            valid.LeftEntrance,
            valid.RightEntrance,
            valid.RequiredStoneworkUnits,
            valid.MaximumStoneworkUnits,
            valid.QualifyingResidentId,
            valid.LifecycleState,
            valid.Cells);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => unknown.Restore());
        Assert.Contains("Unknown excavation template definition", error.Message);
    }

    [Fact]
    public void Inconsistent_lifecycle_is_rejected_atomically()
    {
        ExcavationTemplateInstance source = CreateInstance("invalid-state", CaveRoomPresetKind.Small, 0);
        ExcavationTemplateInstanceSaveSnapshot valid =
            ExcavationTemplateInstanceSaveSnapshot.Capture(source);
        ExcavationTemplateInstanceSaveSnapshot invalid = new ExcavationTemplateInstanceSaveSnapshot(
            valid.FormatVersion,
            valid.InstanceId,
            valid.TemplateId,
            valid.TemplateVersion,
            valid.Anchor,
            valid.OrientationAxis,
            valid.AllowsMirror,
            valid.StyleId,
            valid.LeftEntrance,
            valid.RightEntrance,
            valid.RequiredStoneworkUnits,
            valid.MaximumStoneworkUnits,
            valid.QualifyingResidentId,
            ExcavationTemplateLifecycleState.Completed,
            valid.Cells);

        Assert.Throws<ArgumentException>(() => invalid.Restore());
        Assert.Equal(ExcavationTemplateLifecycleState.Active, source.LifecycleState);
        Assert.All(
            source.CellProgress,
            value => Assert.Equal(ExcavationTemplateCellState.Pending, value.State));
    }

    private static ExcavationTemplateInstance CreateInstance(
        string id,
        CaveRoomPresetKind kind,
        int maximumStoneworkUnits)
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
        CaveRoomPlan plan = CaveRoomPlan.CreateSnapshot(
            preset,
            anchor,
            volume.Where(cell => cell.Z == 0 && cell != anchor),
            volume,
            Array.Empty<CellId>());
        CaveRoomTemplateUnlockState unlockState = new CaveRoomTemplateUnlockEvaluator()
            .Evaluate(new[]
            {
                new CaveRoomTemplateCandidate(
                    "resident.builder",
                    maximumStoneworkUnits,
                    isEligible: true),
            })
            .Get(kind);

        return new ExcavationTemplateInstanceFactory().Create(
            id,
            plan,
            CaveRoomTemplatePlacementUnlock.Capture(unlockState),
            "cave.arch.rounded");
    }
}

}
