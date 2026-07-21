using System.Collections.Generic;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Production;
using Dig.Presentation.Rendering;
using Xunit;

namespace Dig.Tests
{

public sealed class PresentationDomainEffectProjectorTests
{
    private static readonly EntityId BuildingId =
        EntityId.Parse("91000000000000000000000000000001");
    private static readonly EntityId AgentId =
        EntityId.Parse("91000000000000000000000000000002");
    private static readonly EntityId OrderId =
        EntityId.Parse("91000000000000000000000000000003");

    [Fact]
    public void Authoritative_gameplay_events_project_to_bounded_effect_facts()
    {
        IDomainEvent[] events =
        {
            new BuildingConstructionProgressed(10, BuildingId, 2, 4),
            new ProductionWorkApplied(11, OrderId, BuildingId, 3, 6, 8),
            new CombatAttackResolved(12, AttackResolution()),
            new CombatStatusTicked(13, new CombatStatusDamage(
                AgentId,
                new CombatStatusId("status.burning"),
                ticksApplied: 1,
                damage: 4,
                expired: false)),
            new AgentExternalEffectApplied(
                14,
                AgentId,
                "status.poison",
                new NeedDelta(0, 0, 0, -10),
                previousHealth: 100,
                currentHealth: 90),
        };
        Dictionary<EntityId, PresentationEffectLocation> locations = new()
        {
            [BuildingId] = new PresentationEffectLocation(2d, 0d, 3d),
            [AgentId] = new PresentationEffectLocation(5d, 1d, 7d),
        };

        IReadOnlyList<PresentationEffectFact> facts =
            new PresentationDomainEffectProjector().Project(
                events,
                id => locations.TryGetValue(id, out PresentationEffectLocation value)
                    ? value
                    : (PresentationEffectLocation?)null);

        Assert.Collection(
            facts,
            fact => Assert.Equal(PresentationEffectKind.ConstructionProgress, fact.Kind),
            fact => Assert.Equal(PresentationEffectKind.ProductionPulse, fact.Kind),
            fact => Assert.Equal(PresentationEffectKind.CombatImpact, fact.Kind),
            fact => Assert.Equal(PresentationEffectKind.StatusPulse, fact.Kind),
            fact => Assert.Equal(PresentationEffectKind.StatusPulse, fact.Kind));
        Assert.All(facts, fact => Assert.InRange(fact.Magnitude, 0d, 1d));
        Assert.Equal(2d, facts[0].WorldX);
        Assert.Equal(5d, facts[2].WorldX);
    }

    [Fact]
    public void Combat_external_damage_is_not_projected_twice()
    {
        AgentExternalEffectApplied duplicate = new AgentExternalEffectApplied(
            20,
            AgentId,
            "combat:attack-1",
            new NeedDelta(0, 0, 0, -5),
            previousHealth: 100,
            currentHealth: 95);

        IReadOnlyList<PresentationEffectFact> facts =
            new PresentationDomainEffectProjector().Project(
                new IDomainEvent[] { duplicate },
                _ => new PresentationEffectLocation(1d, 2d, 3d));

        Assert.Empty(facts);
    }

    private static CombatAttackResolution AttackResolution()
    {
        return new CombatAttackResolution(
            new CombatActionId("attack-1"),
            EntityId.Parse("91000000000000000000000000000004"),
            AgentId,
            new WeaponProfileId("weapon.pickaxe"),
            CombatAttackOutcome.Hit,
            distance: 1,
            hitChance: 8000,
            hitRoll: 100,
            blockRoll: 9000,
            damage: 12,
            appliedStatusId: new CombatStatusId("status.burning"),
            wasAlreadyProcessed: false);
    }
}
}
