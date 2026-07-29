using System.Linq;
using Dig.Domain.Combat;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class CombatSpatialResolverTests
{
    [Fact]
    public void Distance_and_line_of_sight_include_depth()
    {
        CellId source = new CellId(0, 0, 0);
        CellId target = new CellId(2, 1, 2);
        CellId[] trace = CombatLineOfSightResolver.Trace(source, target).ToArray();

        Assert.Equal(5, CombatSpatialMath.Distance3D(source, target));
        Assert.Equal(target, trace[^1]);
        Assert.True(CombatLineOfSightResolver.HasLineOfSight(
            source,
            target,
            _ => false));
        Assert.False(CombatLineOfSightResolver.HasLineOfSight(
            source,
            target,
            cell => cell == trace[0]));
    }

    [Fact]
    public void Engagement_prefers_soft_claim_then_route_cost()
    {
        WeaponProfile weapon = Weapon(CombatAttackSpatialMode.Melee, 1, 1);
        CombatEngagementCandidate selected = CombatEngagementResolver.Select(
            weapon,
            new[]
            {
                Candidate(new CellId(1, 0, 0), route: 1, claims: 2, edge: true, line: true),
                Candidate(new CellId(0, 1, 0), route: 4, claims: 0, edge: true, line: true),
                Candidate(new CellId(0, 0, 1), route: 2, claims: 0, edge: true, line: true),
            })!.Value;

        Assert.Equal(new CellId(0, 0, 1), selected.Cell);
    }

    [Fact]
    public void Ranged_candidate_requires_line_of_sight()
    {
        WeaponProfile weapon = Weapon(CombatAttackSpatialMode.Ranged, 2, 4);
        CombatEngagementCandidate selected = CombatEngagementResolver.Select(
            weapon,
            new[]
            {
                Candidate(new CellId(0, 0, 0), 1, 0, false, false, distance: 3),
                Candidate(new CellId(1, 0, 0), 3, 0, false, true, distance: 2),
            })!.Value;

        Assert.Equal(new CellId(1, 0, 0), selected.Cell);
    }

    [Fact]
    public void Retreat_maximizes_threat_distance_then_territory_and_route()
    {
        CombatRetreatCandidate selected = CombatRetreatResolver.Select(
            2,
            new[]
            {
                new CombatRetreatCandidate(new CellId(3, 0, 0), 4, 1, true, true, false),
                new CombatRetreatCandidate(new CellId(0, 3, 0), 4, 3, true, true, true),
                new CombatRetreatCandidate(new CellId(0, 0, 3), 4, 2, true, true, true),
            })!.Value;

        Assert.Equal(new CellId(0, 0, 3), selected.Cell);
    }

    private static CombatEngagementCandidate Candidate(
        CellId cell,
        int route,
        int claims,
        bool edge,
        bool line,
        int distance = 1) =>
        new CombatEngagementCandidate(
            cell,
            distance,
            route,
            claims,
            true,
            true,
            edge,
            line);

    private static WeaponProfile Weapon(
        CombatAttackSpatialMode mode,
        int minimumRange,
        int maximumRange) =>
        new WeaponProfile(
            new WeaponProfileId("weapon.spatial.test"),
            minimumRange,
            maximumRange,
            accuracy: 10_000,
            baseDamage: 100,
            armorPenetration: 0,
            cooldownTicks: 1,
            spatialMode: mode);
}
}
