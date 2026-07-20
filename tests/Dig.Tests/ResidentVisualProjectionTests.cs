using System;
using Dig.Presentation.Agents;
using Xunit;

namespace Dig.Tests
{
public sealed class ResidentVisualProjectionTests
{
    private readonly ResidentVisualPresenter _presenter = new ResidentVisualPresenter();

    [Fact]
    public void Same_identity_restores_same_variant()
    {
        ResidentAppearanceViewModel first = _presenter.PresentAppearance(
            "resident.ada", ResidentBodyVariant.Feminine,
            ResidentAgeVisualBand.Old, ResidentHeadwearRole.Miner);
        ResidentAppearanceViewModel second = _presenter.PresentAppearance(
            "resident.ada", ResidentBodyVariant.Feminine,
            ResidentAgeVisualBand.Old, ResidentHeadwearRole.Miner);
        Assert.Equal(first.Version, second.Version);
        Assert.Equal(first.ClothingPaletteIndex, second.ClothingPaletteIndex);
        Assert.Equal(first.HairPaletteIndex, second.HairPaletteIndex);
        Assert.Equal(first.HairVariant, second.HairVariant);
        Assert.Equal(first.FaceVariant, second.FaceVariant);
    }

    [Theory]
    [InlineData("Dig tunnel", ResidentActionVisualState.Dig)]
    [InlineData("Construct building", ResidentActionVisualState.Build)]
    [InlineData("Pickup item", ResidentActionVisualState.Pickup)]
    [InlineData("Deliver item", ResidentActionVisualState.Drop)]
    public void Intent_maps_to_presentation_action(
        string intent,
        ResidentActionVisualState expected)
    {
        ResidentActionVisualViewModel visual = _presenter.PresentAction(
            Agent(intent),
            isMoving: false,
            isCarrying: false);
        Assert.Equal(expected, visual.State);
    }

    [Fact]
    public void Movement_wins_and_keeps_authoritative_progress()
    {
        ResidentActionVisualViewModel visual = _presenter.PresentAction(
            Agent("Dig tunnel", elapsed: 3, required: 10),
            isMoving: true,
            isCarrying: false);
        Assert.Equal(ResidentActionVisualState.Walk, visual.State);
        Assert.Equal(0.3d, visual.NormalizedProgress, 6);
    }

    [Fact]
    public void Death_is_terminal_even_during_movement_or_impact()
    {
        ResidentActionVisualViewModel visual = _presenter.PresentAction(
            Agent("Wait", isAlive: false),
            isMoving: true,
            isCarrying: true,
            showImpact: true);
        Assert.Equal(ResidentActionVisualState.Death, visual.State);
        Assert.Equal(1d, visual.NormalizedProgress);
        Assert.False(visual.IsLooping);
    }

    [Fact]
    public void Carry_is_used_when_no_specific_action_exists()
    {
        ResidentActionVisualViewModel visual = _presenter.PresentAction(
            Agent("Wait"),
            isMoving: false,
            isCarrying: true);
        Assert.Equal(ResidentActionVisualState.Carry, visual.State);
        Assert.True(visual.IsLooping);
    }

    private static AgentViewModel Agent(
        string intent,
        bool isAlive = true,
        int elapsed = 0,
        int required = 0)
    {
        return new AgentViewModel(
            id: "resident.test",
            name: "Test",
            version: 12,
            isAlive: isAlive,
            cellX: 1,
            cellY: 1,
            nutrition: 80,
            alertness: 75,
            mood: 70,
            health: isAlive ? 100 : 0,
            scheduledActivity: "Work",
            activeIntent: intent,
            actionElapsedTicks: elapsed,
            actionRequiredTicks: required,
            decisionReason: "test",
            decisionExplanation: "test",
            utilityOptions: Array.Empty<AgentUtilityOptionViewModel>());
    }
}
}