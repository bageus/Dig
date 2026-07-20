using System;
using System.Collections.Generic;
using Dig.Presentation.Creatures;
using Xunit;

namespace Dig.Tests
{
public sealed class CreatureVisualPresenterTests
{
    private readonly CreatureVisualPresenter _presenter = new CreatureVisualPresenter();

    [Fact]
    public void Species_variants_can_share_one_family_rig()
    {
        CreatureAppearanceViewModel poison = _presenter.PresentAppearance(
            Snapshot("plant-a", "enemy.plant.poison"));
        CreatureAppearanceViewModel fire = _presenter.PresentAppearance(
            Snapshot("plant-b", "enemy.plant.fire"));
        Assert.Equal(CreatureVisualFamily.Plant, poison.Family);
        Assert.Equal(poison.RigId, fire.RigId);
        Assert.NotEqual(poison.VariantId, fire.VariantId);
    }

    [Fact]
    public void Lifecycle_stage_selects_a_distinct_variant()
    {
        CreatureAppearanceViewModel child = _presenter.PresentAppearance(
            Snapshot("vuker", "enemy.vuker", CreatureLifecycleVisualStage.Child));
        CreatureAppearanceViewModel adult = _presenter.PresentAppearance(
            Snapshot("vuker", "enemy.vuker", CreatureLifecycleVisualStage.Adult));
        Assert.Contains("child", child.VariantId);
        Assert.Contains("adult", adult.VariantId);
        Assert.Equal(child.RigId, adult.RigId);
    }

    [Fact]
    public void Tamed_and_hostile_markers_differ_by_shape()
    {
        CreatureAppearanceViewModel tamed = _presenter.PresentAppearance(
            Snapshot("guard", "enemy.vuker",
                disposition: CreatureDisposition.Tamed));
        CreatureAppearanceViewModel hostile = _presenter.PresentAppearance(
            Snapshot("enemy", "enemy.vuker",
                disposition: CreatureDisposition.Hostile));
        Assert.Equal(CreatureMarkerShape.Shield, tamed.MarkerShape);
        Assert.Equal(CreatureMarkerShape.Spikes, hostile.MarkerShape);
        Assert.NotEqual(tamed.MarkerShape, hostile.MarkerShape);
    }

    [Fact]
    public void Unknown_species_uses_explicit_fallback()
    {
        CreatureAppearanceViewModel appearance = _presenter.PresentAppearance(
            Snapshot("unknown", "mod.creature.unknown"));
        Assert.True(appearance.IsFallback);
        Assert.Equal("creature.rig.fallback", appearance.RigId);
        Assert.Equal(CreatureVisualFamily.SmallCreature, appearance.Family);
    }

    [Fact]
    public void Death_and_hit_override_attack_and_movement()
    {
        CreatureActionVisualViewModel death = _presenter.PresentAction(
            Snapshot("dead", "enemy.goblin", isAlive: false,
                isMoving: true, isAttacking: true, showImpact: true));
        CreatureActionVisualViewModel hit = _presenter.PresentAction(
            Snapshot("hit", "enemy.goblin", isMoving: true,
                isAttacking: true, showImpact: true));
        Assert.Equal(CreatureActionVisualState.Death, death.State);
        Assert.Equal(1d, death.NormalizedProgress);
        Assert.Equal(CreatureActionVisualState.Hit, hit.State);
    }

    [Fact]
    public void Special_growth_and_move_projection_is_deterministic()
    {
        CreatureActionVisualViewModel special = _presenter.PresentAction(
            Snapshot("special", "enemy.demon.swallower",
                isMoving: true, isGrowing: true, isSpecialAction: true));
        CreatureActionVisualViewModel growth = _presenter.PresentAction(
            Snapshot("growth", "enemy.vuker",
                isMoving: true, isGrowing: true));
        CreatureActionVisualViewModel move = _presenter.PresentAction(
            Snapshot("move", "creature.hamster", isMoving: true));
        Assert.Equal(CreatureActionVisualState.Special, special.State);
        Assert.Equal(CreatureActionVisualState.Growth, growth.State);
        Assert.Equal(CreatureActionVisualState.Move, move.State);
    }

    [Fact]
    public void Lod_policy_freezes_hidden_creatures()
    {
        CreatureLodViewModel near = _presenter.PresentLod(8d, isVisible: true);
        CreatureLodViewModel far = _presenter.PresentLod(60d, isVisible: true);
        CreatureLodViewModel hidden = _presenter.PresentLod(10d, isVisible: false);
        Assert.Equal(CreatureAnimationUpdatePolicy.EveryFrame, near.AnimationPolicy);
        Assert.Equal(12, far.UpdateIntervalFrames);
        Assert.Equal(CreatureAnimationUpdatePolicy.Frozen, hidden.AnimationPolicy);
        Assert.False(hidden.RenderBody);
    }

    [Fact]
    public void Reconciliation_updates_existing_roots_without_recreating_them()
    {
        CreatureRenderReconciliationPlan plan = CreatureRenderReconciliationPlan.Create(
            new[] { "keep", "remove" },
            new[]
            {
                Snapshot("keep", "creature.hamster"),
                Snapshot("create", "enemy.spider"),
            },
            populationCap: 8);
        Assert.Equal(new[] { "create" }, plan.CreateIds);
        Assert.Equal(new[] { "keep" }, plan.UpdateIds);
        Assert.Equal(new[] { "remove" }, plan.RemoveIds);
    }

    [Fact]
    public void Reconciliation_enforces_population_cap_and_unique_ids()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CreatureRenderReconciliationPlan.Create(
                Array.Empty<string>(),
                new[]
                {
                    Snapshot("a", "creature.hamster"),
                    Snapshot("b", "creature.larva"),
                },
                populationCap: 1));
        Assert.Throws<InvalidOperationException>(() =>
            CreatureRenderReconciliationPlan.Create(
                Array.Empty<string>(),
                new[]
                {
                    Snapshot("same", "creature.hamster"),
                    Snapshot("same", "creature.larva"),
                },
                populationCap: 4));
    }

    private static CreatureVisualSnapshot Snapshot(
        string creatureId,
        string speciesId,
        CreatureLifecycleVisualStage stage = CreatureLifecycleVisualStage.Adult,
        CreatureDisposition disposition = CreatureDisposition.Neutral,
        bool isAlive = true,
        bool isMoving = false,
        bool isAttacking = false,
        bool showImpact = false,
        bool isGrowing = false,
        bool isSpecialAction = false)
    {
        return new CreatureVisualSnapshot(
            creatureId,
            speciesId,
            stage,
            disposition,
            isAlive,
            cellX: 1,
            cellY: 1,
            cellZ: 0,
            isMoving,
            isAttacking,
            showImpact,
            isGrowing,
            isSpecialAction,
            actionProgress: 0.5d,
            version: 1);
    }
}
}