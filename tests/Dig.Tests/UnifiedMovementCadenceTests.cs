using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Navigation;
using Xunit;

namespace Dig.Tests
{

public sealed class UnifiedMovementCadenceTests
{
    private static readonly EntityId ResidentId = EntityId.Parse(
        "00000000000000000000000000000001");

    [Fact]
    public void Run_walk_and_climb_have_exact_four_tick_step_counts()
    {
        Assert.Equal(
            new[] { 1, 1, 1, 2 },
            Enumerable.Range(1, 4)
                .Select(tick => ResidentInventoryMovementCadence.ResolveStepCount(
                    tick,
                    1.25d)));
        Assert.Equal(
            new[] { 1, 1, 1, 1 },
            Enumerable.Range(1, 4)
                .Select(tick => ResidentInventoryMovementCadence.ResolveStepCount(
                    tick,
                    1d)));
        Assert.Equal(
            new[] { 0, 1, 0, 1 },
            Enumerable.Range(1, 4)
                .Select(tick => ResidentInventoryMovementCadence.ResolveStepCount(
                    tick,
                    0.5d)));
    }

    [Fact]
    public void Default_mode_resolver_exposes_gameplay_speeds()
    {
        ResidentMovementModeResolver resolver = new ResidentMovementModeResolver(
            ResidentMovementModePolicy.CreateDefault());

        Assert.Equal(1.25d, Resolve(resolver, 10_000, TunnelTraversalKind.SupportedWalk)
            .AuthoritativeCadenceMultiplier);
        Assert.Equal(1d, Resolve(resolver, 2_000, TunnelTraversalKind.SupportedWalk)
            .AuthoritativeCadenceMultiplier);
        Assert.Equal(0.5d, Resolve(resolver, 10_000, TunnelTraversalKind.VerticalClimb)
            .AuthoritativeCadenceMultiplier);
    }

    private static ResidentMovementModeResolution Resolve(
        ResidentMovementModeResolver resolver,
        int alertness,
        TunnelTraversalKind traversal)
    {
        return resolver.Resolve(new ResidentMovementModeRequest(
            ResidentId,
            alertness,
            AgentIntentKind.Idle,
            ResidentMovementCommandSource.Automatic,
            traversal,
            repeatedManualCommand: false,
            remainingPathSteps: 4,
            inventorySpeedMultiplier: 1d,
            carriesBuildingBox: false,
            hasRideHamster: false,
            hasHoverboard: false));
    }
}

}
