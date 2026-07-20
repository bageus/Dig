using System;
using Dig.Presentation.Rendering;
using Xunit;

namespace Dig.Tests
{
public sealed class RenderBudgetPlanTests
{
    [Fact]
    public void Critical_effects_win_before_nearer_ambient_effects()
    {
        EffectSpawnRequest[] effects =
        {
            Effect("ambient", VfxPriority.Ambient, 0d, 8),
            Effect("critical", VfxPriority.Critical, 100d, 8),
        };
        RenderBudgetPlan plan = RenderBudgetPlan.Create(effects,
            Array.Empty<LightRequest>(), new RenderFrameBudget(1, 16, 0, 0),
            0d, 0d, 0d);
        Assert.Single(plan.Effects);
        Assert.Equal("critical", plan.Effects[0].RequestId);
        Assert.Equal(1, plan.DroppedEffects);
    }

    [Fact]
    public void Particle_budget_is_never_exceeded()
    {
        EffectSpawnRequest[] effects =
        {
            Effect("a", VfxPriority.Important, 1d, 12),
            Effect("b", VfxPriority.Important, 2d, 12),
            Effect("c", VfxPriority.Important, 3d, 4),
        };
        RenderBudgetPlan plan = RenderBudgetPlan.Create(effects,
            Array.Empty<LightRequest>(), new RenderFrameBudget(3, 16, 0, 0),
            0d, 0d, 0d);
        Assert.Equal(16, plan.ParticleCount);
        Assert.Equal("a", plan.Effects[0].RequestId);
        Assert.Equal("c", plan.Effects[1].RequestId);
    }

    [Fact]
    public void Stable_id_breaks_equal_priority_and_distance_ties()
    {
        EffectSpawnRequest[] effects =
        {
            Effect("z", VfxPriority.Normal, 1d, 1),
            Effect("a", VfxPriority.Normal, -1d, 1),
        };
        RenderBudgetPlan plan = RenderBudgetPlan.Create(effects,
            Array.Empty<LightRequest>(), new RenderFrameBudget(2, 16, 0, 0),
            0d, 0d, 0d);
        Assert.Equal("a", plan.Effects[0].RequestId);
        Assert.Equal("z", plan.Effects[1].RequestId);
    }

    [Fact]
    public void Light_and_shadow_budgets_are_independent()
    {
        LightRequest[] lights =
        {
            Light("critical", RealtimeLightPriority.Critical, 5d, true),
            Light("focus", RealtimeLightPriority.Focused, 1d, true),
            Light("normal", RealtimeLightPriority.Normal, 0d, false),
        };
        RenderBudgetPlan plan = RenderBudgetPlan.Create(
            Array.Empty<EffectSpawnRequest>(), lights,
            new RenderFrameBudget(1, 16, 2, 1), 0d, 0d, 0d);
        Assert.Equal(2, plan.Lights.Count);
        Assert.True(plan.Lights[0].ShadowsEnabled);
        Assert.False(plan.Lights[1].ShadowsEnabled);
        Assert.Equal("critical", plan.Lights[0].Request.RequestId);
        Assert.Equal(1, plan.DroppedLights);
    }

    [Fact]
    public void Duplicate_request_ids_are_rejected_before_rendering()
    {
        EffectSpawnRequest duplicate = Effect("same", VfxPriority.Normal, 0d, 1);
        Assert.Throws<InvalidOperationException>(() => RenderBudgetPlan.Create(
            new[] { duplicate, duplicate }, Array.Empty<LightRequest>(),
            RenderFrameBudget.Default, 0d, 0d, 0d));
    }

    private static EffectSpawnRequest Effect(string id, VfxPriority priority,
        double x, int particles)
    {
        return new EffectSpawnRequest(id, "vfx.test", VfxCategory.Ambient,
            priority, x, 0d, 0d, 1d, particles, 1d, 1L);
    }

    private static LightRequest Light(string id, RealtimeLightPriority priority,
        double x, bool shadows)
    {
        return new LightRequest(id, RealtimeLightKind.Point, priority,
            x, 0d, 0d, 8d, 1d, 1d, 0.8d, 0.6d, shadows, 1L);
    }
}
}
