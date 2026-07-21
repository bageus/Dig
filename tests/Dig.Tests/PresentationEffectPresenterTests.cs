using System;
using System.Collections.Generic;
using Dig.Presentation.Rendering;
using Xunit;

namespace Dig.Tests
{
public sealed class PresentationEffectPresenterTests
{
    private readonly PresentationEffectPresenter _presenter =
        new PresentationEffectPresenter();

    [Fact]
    public void Empty_input_returns_canonical_empty_frame()
    {
        PresentationEffectFrame frame = _presenter.Present(
            Array.Empty<PresentationEffectFact>());

        Assert.Same(PresentationEffectFrame.Empty, frame);
        Assert.Empty(frame.Effects);
        Assert.Empty(frame.Lights);
    }

    [Fact]
    public void Input_order_does_not_change_projected_order()
    {
        PresentationEffectFact ambient = Fact(
            "event-b", PresentationEffectKind.AmbientDust, 0.2d);
        PresentationEffectFact combat = Fact(
            "event-a", PresentationEffectKind.CombatImpact, 0.9d);

        PresentationEffectFrame first = _presenter.Present(
            new[] { ambient, combat });
        PresentationEffectFrame second = _presenter.Present(
            new[] { combat, ambient });

        Assert.Equal(first.Effects.Count, second.Effects.Count);
        for (int index = 0; index < first.Effects.Count; index++)
        {
            Assert.Equal(first.Effects[index].RequestId,
                second.Effects[index].RequestId);
            Assert.Equal(first.Effects[index].EffectId,
                second.Effects[index].EffectId);
            Assert.Equal(first.Effects[index].ParticleBudget,
                second.Effects[index].ParticleBudget);
        }
        Assert.Equal("event-a:effect", first.Effects[0].RequestId);
        Assert.Equal(VfxPriority.Critical, first.Effects[0].Priority);
    }

    [Theory]
    [InlineData(PresentationEffectKind.ExcavationImpact, VfxCategory.Excavation)]
    [InlineData(PresentationEffectKind.DepositReveal, VfxCategory.Deposit)]
    [InlineData(PresentationEffectKind.ConstructionProgress, VfxCategory.Construction)]
    [InlineData(PresentationEffectKind.ProductionPulse, VfxCategory.Production)]
    [InlineData(PresentationEffectKind.StatusPulse, VfxCategory.Status)]
    [InlineData(PresentationEffectKind.CombatImpact, VfxCategory.Combat)]
    [InlineData(PresentationEffectKind.AmbientDust, VfxCategory.Ambient)]
    public void Discrete_kinds_map_to_expected_categories(
        PresentationEffectKind kind, VfxCategory category)
    {
        PresentationEffectFrame frame = _presenter.Present(
            new[] { Fact("event", kind, 0.5d) });

        EffectSpawnRequest effect = Assert.Single(frame.Effects);
        Assert.Equal(category, effect.Category);
        Assert.Empty(frame.Lights);
    }

    [Theory]
    [InlineData(PresentationEffectKind.LavaGlow, true)]
    [InlineData(PresentationEffectKind.CrystalGlow, false)]
    [InlineData(PresentationEffectKind.CampfireGlow, true)]
    [InlineData(PresentationEffectKind.ProductionBuildingGlow, false)]
    public void Emissive_kinds_create_effect_and_light(
        PresentationEffectKind kind, bool castsShadows)
    {
        PresentationEffectFrame frame = _presenter.Present(
            new[] { Fact("glow", kind, 0.8d) });

        Assert.Single(frame.Effects);
        LightRequest light = Assert.Single(frame.Lights);
        Assert.Equal(castsShadows, light.CastsShadows);
        Assert.StartsWith("glow:light.", light.RequestId);
    }

    [Fact]
    public void Magnitude_changes_visual_budget_without_changing_identity()
    {
        PresentationEffectFrame low = _presenter.Present(new[]
        {
            Fact("impact", PresentationEffectKind.CombatImpact, 0.1d),
        });
        PresentationEffectFrame high = _presenter.Present(new[]
        {
            Fact("impact", PresentationEffectKind.CombatImpact, 1.0d),
        });

        Assert.Equal(low.Effects[0].RequestId, high.Effects[0].RequestId);
        Assert.Equal(low.Effects[0].EffectId, high.Effects[0].EffectId);
        Assert.True(high.Effects[0].ParticleBudget
            > low.Effects[0].ParticleBudget);
        Assert.True(high.Effects[0].Scale > low.Effects[0].Scale);
    }

    [Fact]
    public void Duplicate_stable_event_ids_are_rejected()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => _presenter.Present(new List<PresentationEffectFact>
            {
                Fact("duplicate", PresentationEffectKind.ExcavationImpact, 0.3d),
                Fact("duplicate", PresentationEffectKind.CombatImpact, 0.7d),
            }));

        Assert.Contains("Duplicate presentation effect id", exception.Message);
    }

    private static PresentationEffectFact Fact(string id,
        PresentationEffectKind kind, double magnitude)
    {
        return new PresentationEffectFact(id, kind,
            worldX: 3d, worldY: 4d, worldZ: 2d,
            magnitude: magnitude, version: 7L);
    }
}
}