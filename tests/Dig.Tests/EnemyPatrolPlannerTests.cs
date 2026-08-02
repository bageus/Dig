using System;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class EnemyPatrolPlannerTests
{
    private static readonly EntityId EnemyId = EntityId.Parse(
        "ca000000000000000000000000000011");

    [Fact]
    public void Cave_monster_patrol_is_slow_deterministic_and_flat()
    {
        EnemyCombatDefinition cave = CaveEncounterCombatContent.CaveMonster;
        CellId anchor = new CellId(1, 1, 0);
        CellId[] plane =
        {
            anchor,
            new CellId(0, 1, 0),
            new CellId(2, 1, 0),
            new CellId(1, 1, 1),
            new CellId(0, 1, 1),
            new CellId(2, 1, 1),
        };
        TunnelNavigationVolume volume = new TunnelNavigationVolume(
            width: 3,
            height: 3,
            depth: 2,
            openCells: plane,
            verticalCells: Array.Empty<CellId>(),
            supportedCells: plane);
        EnemyPatrolPlanner planner = new EnemyPatrolPlanner();

        EnemyPatrolDecision early = planner.Plan(
            cave, EnemyId, anchor, anchor, volume, worldSeed: 77UL, tick: 3);
        EnemyPatrolDecision first = planner.Plan(
            cave, EnemyId, anchor, anchor, volume, worldSeed: 77UL, tick: 4);
        EnemyPatrolDecision replay = planner.Plan(
            cave, EnemyId, anchor, anchor, volume, worldSeed: 77UL, tick: 4);

        Assert.False(early.ShouldMove);
        Assert.True(first.ShouldMove);
        Assert.Equal(first.Target, replay.Target);
        Assert.Equal(anchor.Y, first.Target.Y);
        Assert.NotEqual(anchor, first.Target);
        Assert.True(volume.HasFullActorSupport(first.Target));
        Assert.InRange(Math.Abs(first.Target.X - anchor.X), 0, 1);
        Assert.InRange(Math.Abs(first.Target.Z - anchor.Z), 0, 1);
    }

    [Fact]
    public void Stationary_vine_cannot_receive_a_patrol_profile()
    {
        Assert.Throws<ArgumentException>(() => new EnemyCombatDefinition(
            "enemy.test.patrolling_vine",
            "Patrolling vine",
            maximumHealth: 1_000,
            minimumGroupSize: 1,
            maximumGroupSize: 1,
            EnemyTraversalCapability.Stationary,
            CaveEncounterCombatContent.CaveMonsterBiteProfileId,
            EnemyAttachmentSurface.CaveWall,
            patrolWanderRadius: 1,
            patrolIntervalTicks: 4));
    }
}
}
