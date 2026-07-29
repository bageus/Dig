using System.Collections.Generic;
using Dig.Application.Jobs;
using Xunit;

namespace Dig.Tests
{

public sealed class ExcavationCadenceResolverTests
{
    private readonly ExcavationCadenceResolver _resolver =
        new ExcavationCadenceResolver(
            ExcavationCadenceProfile.CreateLegacyDeterministic());

    [Theory]
    [InlineData(0, 9)]
    [InlineData(10, 9)]
    [InlineData(11, 6)]
    [InlineData(20, 6)]
    [InlineData(21, 3)]
    [InlineData(50, 3)]
    [InlineData(51, 2)]
    [InlineData(70, 2)]
    [InlineData(71, 1)]
    [InlineData(100, 1)]
    public void Existing_skill_thresholds_produce_deterministic_intervals(
        int skill,
        int expected)
    {
        ExcavationCadenceDecision decision = _resolver.Resolve(
            materialHardness: 120,
            miningSkill: skill,
            equipmentIntervalTicks: 3,
            TerrainWorkPosture.Standing,
            tick: 1);

        Assert.Equal(expected, decision.IntervalTicks);
        Assert.Equal(ExcavationFlavorCue.None, decision.FlavorCue);
    }

    [Fact]
    public void Hardness_and_tool_interval_compose_without_random_swings()
    {
        ExcavationCadenceDecision bareHands = _resolver.Resolve(
            materialHardness: 240,
            miningSkill: 21,
            equipmentIntervalTicks: 3,
            TerrainWorkPosture.Standing,
            tick: 5);
        ExcavationCadenceDecision pickaxe = _resolver.Resolve(
            materialHardness: 240,
            miningSkill: 21,
            equipmentIntervalTicks: 1,
            TerrainWorkPosture.Standing,
            tick: 5);

        Assert.Equal(6, bareHands.IntervalTicks);
        Assert.Equal(2, pickaxe.IntervalTicks);
    }

    [Fact]
    public void Posture_ratio_is_data_driven()
    {
        ExcavationCadenceRatio neutral = new ExcavationCadenceRatio(1, 1);
        ExcavationCadenceProfile profile = new ExcavationCadenceProfile(
            referenceHardness: 120,
            skillBands: new[]
            {
                new ExcavationSkillCadenceBand(0, 100, neutral),
            },
            postureRatios: new Dictionary<TerrainWorkPosture, ExcavationCadenceRatio>
            {
                [TerrainWorkPosture.Standing] = neutral,
                [TerrainWorkPosture.DepthBraced] = new ExcavationCadenceRatio(5, 4),
                [TerrainWorkPosture.Climbing] = new ExcavationCadenceRatio(3, 2),
            });
        ExcavationCadenceResolver resolver = new ExcavationCadenceResolver(profile);

        Assert.Equal(3, resolver.Resolve(120, 50, 3,
            TerrainWorkPosture.Standing, 1).IntervalTicks);
        Assert.Equal(4, resolver.Resolve(120, 50, 3,
            TerrainWorkPosture.DepthBraced, 1).IntervalTicks);
        Assert.Equal(5, resolver.Resolve(120, 50, 3,
            TerrainWorkPosture.Climbing, 1).IntervalTicks);
    }

    [Fact]
    public void Due_ticks_are_fixed_tick_deterministic()
    {
        ExcavationCadenceDecision decision = _resolver.Resolve(
            120, 11, 3, TerrainWorkPosture.Standing, tick: 0);

        Assert.True(ExcavationCadenceResolver.IsDue(0, decision));
        Assert.False(ExcavationCadenceResolver.IsDue(5, decision));
        Assert.True(ExcavationCadenceResolver.IsDue(6, decision));
    }

    [Fact]
    public void Flavor_cue_is_optional_and_never_changes_interval()
    {
        ExcavationCadenceRatio neutral = new ExcavationCadenceRatio(1, 1);
        ExcavationCadenceProfile profile = new ExcavationCadenceProfile(
            120,
            new[] { new ExcavationSkillCadenceBand(0, 100, neutral) },
            new Dictionary<TerrainWorkPosture, ExcavationCadenceRatio>
            {
                [TerrainWorkPosture.Standing] = neutral,
                [TerrainWorkPosture.DepthBraced] = neutral,
                [TerrainWorkPosture.Climbing] = neutral,
            },
            lowSkillFlavorIntervalTicks: 4);
        ExcavationCadenceResolver resolver = new ExcavationCadenceResolver(profile);

        ExcavationCadenceDecision cue = resolver.Resolve(
            120, 10, 3, TerrainWorkPosture.Standing, tick: 8);
        ExcavationCadenceDecision none = resolver.Resolve(
            120, 10, 3, TerrainWorkPosture.Standing, tick: 9);

        Assert.Equal(cue.IntervalTicks, none.IntervalTicks);
        Assert.Equal(ExcavationFlavorCue.ClumsySwing, cue.FlavorCue);
        Assert.Equal(ExcavationFlavorCue.None, none.FlavorCue);
    }
}

}
