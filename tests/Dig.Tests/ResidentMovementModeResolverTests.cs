using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentMovementModeResolverTests
{
    private static readonly EntityId ResidentId =
        EntityId.Parse("40000000000000000000000000000042");

    [Fact]
    public void Vertical_traversal_overrides_carrying_and_fast_modes()
    {
        ResidentMovementModeResolution resolution = Resolver().Resolve(Request(
            traversal: TunnelTraversalKind.VerticalClimb,
            repeated: true,
            carriesBox: true,
            hoverboard: true,
            intent: AgentIntentKind.Flee));

        Assert.Equal(ResidentMovementMode.Climbing, resolution.Mode);
        Assert.Equal(ResidentMovementModeReason.VerticalTraversal, resolution.Reason);
    }

    [Fact]
    public void Building_box_disables_fast_and_personal_mobility()
    {
        ResidentMovementModeResolution resolution = Resolver(
            automaticMobilityMinimumRemainingSteps: 2).Resolve(Request(
                repeated: true,
                remainingSteps: 10,
                carriesBox: true,
                hoverboard: true));

        Assert.Equal(ResidentMovementMode.Carrying, resolution.Mode);
        Assert.Equal(ResidentMobilityKind.Hoverboard, resolution.Mobility);
        Assert.Equal(ResidentMovementModeReason.BuildingBoxCarried, resolution.Reason);
    }

    [Fact]
    public void Hoverboard_has_priority_and_repeat_activates_personal_mobility()
    {
        ResidentMovementModeResolution resolution = Resolver().Resolve(Request(
            repeated: true,
            rideHamster: true,
            hoverboard: true));

        Assert.Equal(ResidentMovementMode.Mobility, resolution.Mode);
        Assert.Equal(ResidentMobilityKind.Hoverboard, resolution.Mobility);
        Assert.Equal(ResidentMovementModeReason.HoverboardAvailable, resolution.Reason);
    }

    [Fact]
    public void Fleeing_wins_over_forced_fast_without_personal_mobility()
    {
        ResidentMovementModeResolution resolution = Resolver().Resolve(Request(
            repeated: true,
            intent: AgentIntentKind.Flee));

        Assert.Equal(ResidentMovementMode.Fleeing, resolution.Mode);
        Assert.Equal(ResidentMovementModeReason.FleeIntent, resolution.Reason);
    }

    [Fact]
    public void Repeated_manual_destination_uses_forced_fast_mode()
    {
        ResidentMovementModeResolution resolution = Resolver().Resolve(Request(
            repeated: true));

        Assert.Equal(ResidentMovementMode.ForcedFast, resolution.Mode);
        Assert.Equal(
            ResidentMovementModeReason.RepeatedManualCommand,
            resolution.Reason);
    }

    [Fact]
    public void Critical_alertness_uses_tired_mode()
    {
        ResidentMovementModeResolution resolution = Resolver().Resolve(Request(
            alertness: 2_000));

        Assert.Equal(ResidentMovementMode.Tired, resolution.Mode);
        Assert.Equal(ResidentMovementModeReason.CriticalAlertness, resolution.Reason);
    }

    [Fact]
    public void Custom_data_definition_controls_cadence_and_visual_duration()
    {
        ResidentMovementModeCatalog catalog = new ResidentMovementModeCatalog(
            Enum.GetValues(typeof(ResidentMovementMode))
                .Cast<ResidentMovementMode>()
                .Select(mode => new ResidentMovementModeDefinition(
                    mode,
                    speedMultiplier: mode == ResidentMovementMode.Tired ? 0.5d : 1d,
                    transitionDurationMultiplier:
                        mode == ResidentMovementMode.Tired ? 1.75d : 1d))
                .ToArray());
        ResidentMovementModeResolver resolver = new ResidentMovementModeResolver(
            new ResidentMovementModePolicy(2_000, null, catalog));

        ResidentMovementModeResolution resolution = resolver.Resolve(Request(
            alertness: 1_000,
            inventorySpeed: 0.8d));

        Assert.Equal(0.4d, resolution.EffectiveSpeedMultiplier, 8);
        Assert.Equal(0.4d, resolution.AuthoritativeCadenceMultiplier, 8);
        Assert.Equal(1.75d, resolution.TransitionDurationMultiplier, 8);
    }

    [Fact]
    public void Default_policy_does_not_invent_legacy_autodistance_threshold()
    {
        ResidentMovementModeResolution resolution = Resolver().Resolve(Request(
            remainingSteps: 100,
            hoverboard: true));

        Assert.Equal(ResidentMobilityKind.Hoverboard, resolution.Mobility);
        Assert.Equal(ResidentMovementMode.Normal, resolution.Mode);
    }

    private static ResidentMovementModeResolver Resolver(
        int? automaticMobilityMinimumRemainingSteps = null)
    {
        return new ResidentMovementModeResolver(new ResidentMovementModePolicy(
            2_000,
            automaticMobilityMinimumRemainingSteps,
            ResidentMovementModeCatalog.CreateNeutralDefaults()));
    }

    private static ResidentMovementModeRequest Request(
        int alertness = 8_000,
        AgentIntentKind intent = AgentIntentKind.Work,
        TunnelTraversalKind traversal = TunnelTraversalKind.SupportedWalk,
        bool repeated = false,
        int remainingSteps = 1,
        double inventorySpeed = 1d,
        bool carriesBox = false,
        bool rideHamster = false,
        bool hoverboard = false)
    {
        return new ResidentMovementModeRequest(
            ResidentId,
            alertness,
            intent,
            ResidentMovementCommandSource.Manual,
            traversal,
            repeated,
            remainingSteps,
            inventorySpeed,
            carriesBox,
            rideHamster,
            hoverboard);
    }
}

}
