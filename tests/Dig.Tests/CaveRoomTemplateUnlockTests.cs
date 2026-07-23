using System;
using System.Linq;
using Dig.Application.World;
using Xunit;

namespace Dig.Tests
{

public sealed class CaveRoomTemplateUnlockTests
{
    [Fact]
    public void Catalog_matches_authoritative_dimensions_thresholds_and_ids()
    {
        CaveRoomPreset[] presets = CaveRoomPresetCatalog.Definitions
            .OrderBy(value => value.Kind)
            .ToArray();

        Assert.Collection(
            presets,
            value => AssertPreset(value, "excavation.template.cave.small", 5, 3, 3, 2, 0),
            value => AssertPreset(value, "excavation.template.cave.medium", 8, 6, 3, 3, 2_000),
            value => AssertPreset(value, "excavation.template.cave.large", 12, 8, 5, 4, 4_000),
            value => AssertPreset(value, "excavation.template.cave.tall", 10, 6, 7, 4, 6_000));
    }

    [Theory]
    [InlineData(0, true, false, false, false)]
    [InlineData(1_999, true, false, false, false)]
    [InlineData(2_000, true, true, false, false)]
    [InlineData(4_000, true, true, true, false)]
    [InlineData(6_000, true, true, true, true)]
    public void Thresholds_are_evaluated_from_maximum_eligible_stonework(
        int stoneworkUnits,
        bool small,
        bool medium,
        bool large,
        bool tall)
    {
        CaveRoomTemplateUnlockSnapshot snapshot =
            new CaveRoomTemplateUnlockEvaluator().Evaluate(new[]
            {
                new CaveRoomTemplateCandidate("resident.b", stoneworkUnits, isEligible: true),
                new CaveRoomTemplateCandidate("resident.hidden", 10_000, isEligible: false),
            });

        Assert.Equal(small, snapshot.Get(CaveRoomPresetKind.Small).IsUnlocked);
        Assert.Equal(medium, snapshot.Get(CaveRoomPresetKind.Medium).IsUnlocked);
        Assert.Equal(large, snapshot.Get(CaveRoomPresetKind.Large).IsUnlocked);
        Assert.Equal(tall, snapshot.Get(CaveRoomPresetKind.Tall).IsUnlocked);
    }

    [Fact]
    public void Equal_skill_uses_stable_resident_id_tie_break()
    {
        CaveRoomTemplateCandidate[] candidates =
        {
            new CaveRoomTemplateCandidate("resident.z", 4_000, isEligible: true),
            new CaveRoomTemplateCandidate("resident.a", 4_000, isEligible: true),
        };

        CaveRoomTemplateUnlockSnapshot first =
            new CaveRoomTemplateUnlockEvaluator().Evaluate(candidates);
        CaveRoomTemplateUnlockSnapshot reversed =
            new CaveRoomTemplateUnlockEvaluator().Evaluate(candidates.Reverse());

        Assert.Equal("resident.a", first.QualifyingResidentId);
        Assert.Equal(first.QualifyingResidentId, reversed.QualifyingResidentId);
        Assert.Equal(first.MaximumStoneworkUnits, reversed.MaximumStoneworkUnits);
    }

    [Fact]
    public void Captured_unlock_remains_valid_after_skill_loss()
    {
        CaveRoomTemplateUnlockEvaluator evaluator = new CaveRoomTemplateUnlockEvaluator();
        CaveRoomTemplateUnlockState unlocked = evaluator.Evaluate(new[]
        {
            new CaveRoomTemplateCandidate("resident.master", 6_000, isEligible: true),
        }).Get(CaveRoomPresetKind.Tall);

        CaveRoomTemplatePlacementUnlock captured =
            CaveRoomTemplatePlacementUnlock.Capture(unlocked);
        CaveRoomTemplateUnlockState afterLoss = evaluator.Evaluate(Array.Empty<CaveRoomTemplateCandidate>())
            .Get(CaveRoomPresetKind.Tall);

        Assert.False(afterLoss.IsUnlocked);
        Assert.Equal("excavation.template.cave.tall", captured.TemplateId);
        Assert.Equal(6_000, captured.MaximumStoneworkUnits);
        Assert.Equal("resident.master", captured.QualifyingResidentId);
    }

    [Fact]
    public void Entrances_are_left_and_right_on_X_and_mirroring_is_forbidden()
    {
        CaveRoomPreset preset = CaveRoomPresetCatalog.Get(CaveRoomPresetKind.Large);

        Assert.Equal("X", preset.PassageAxis);
        Assert.False(preset.AllowsMirror);
        Assert.Equal(0, preset.GetEntranceOffsetX(CaveRoomEntranceSide.Left));
        Assert.Equal(preset.BaseWidth - 1, preset.GetEntranceOffsetX(CaveRoomEntranceSide.Right));
    }

    private static void AssertPreset(
        CaveRoomPreset preset,
        string id,
        int baseWidth,
        int topWidth,
        int height,
        int depth,
        int requiredStoneworkUnits)
    {
        Assert.Equal(id, preset.Id);
        Assert.Equal(1, preset.Version);
        Assert.Equal(baseWidth, preset.BaseWidth);
        Assert.Equal(topWidth, preset.TopWidth);
        Assert.Equal(height, preset.Height);
        Assert.Equal(depth, preset.Depth);
        Assert.Equal(requiredStoneworkUnits, preset.RequiredStoneworkUnits);
    }
}

}
