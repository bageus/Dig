using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class CaveEncounterCombatContentTests
{
    private static readonly EntityId AttackerId = EntityId.Parse(
        "ca000000000000000000000000000001");
    private static readonly EntityId TargetId = EntityId.Parse(
        "ca000000000000000000000000000002");
    private static readonly FactionId Residents = new FactionId("faction.residents");
    private static readonly FactionId Hostiles = new FactionId("faction.hostiles");

    [Fact]
    public void Enemy_hierarchy_uses_approved_group_and_traversal_profiles()
    {
        Assert.Equal(4, CaveEncounterCombatContent.EnemyDefinitions.Count);

        EnemyCombatDefinition cave = CaveEncounterCombatContent.CaveMonster;
        Assert.Equal("Пещерный монстр", cave.DisplayName);
        Assert.Equal(7_000, cave.MaximumHealth);
        Assert.Equal(2, cave.MinimumGroupSize);
        Assert.Equal(2, cave.MaximumGroupSize);
        Assert.True(cave.Traversal.HasFlag(
            EnemyTraversalCapability.SupportedWalk));
        Assert.True(cave.Traversal.HasFlag(
            EnemyTraversalCapability.VerticalClimb));
        Assert.True(cave.Traversal.HasFlag(
            EnemyTraversalCapability.DepthTraverse));
        Assert.True(cave.HasPatrol);
        Assert.Equal(6, cave.PatrolWanderRadius);
        Assert.Equal(4, cave.PatrolIntervalTicks);
        Assert.Equal(6, cave.SightRange);
        Assert.True(cave.RetainsAggroUntilTargetUnavailable);

        EnemyCombatDefinition vine = Find(
            CaveEncounterCombatContent.PredatoryVineSpeciesId);
        Assert.Equal(EnemyTraversalCapability.Stationary, vine.Traversal);
        Assert.False(vine.Traversal.HasFlag(
            EnemyTraversalCapability.DepthTraverse));
        Assert.False(vine.Traversal.HasFlag(
            EnemyTraversalCapability.VerticalClimb));
        Assert.False(vine.Traversal.HasFlag(
            EnemyTraversalCapability.SupportedWalk));
        Assert.Equal(
            EnemyAttachmentSurface.HorizontalTunnel
                | EnemyAttachmentSurface.CaveFloor
                | EnemyAttachmentSurface.CaveWall,
            vine.AttachmentSurfaces);
        Assert.False(vine.AttachmentSurfaces.HasFlag(
            EnemyAttachmentSurface.CaveCeiling));
        Assert.False(vine.HasPatrol);

        EnemyCombatDefinition swallower = Find(
            CaveEncounterCombatContent.SwallowerSpeciesId);
        Assert.True(swallower.Traversal.HasFlag(
            EnemyTraversalCapability.DepthTraverse));
        Assert.False(swallower.Traversal.HasFlag(
            EnemyTraversalCapability.VerticalClimb));
        Assert.Equal(2, swallower.MinimumGroupSize);
        Assert.Equal(3, swallower.MaximumGroupSize);

        EnemyCombatDefinition spider = Find(
            CaveEncounterCombatContent.SpiderSpeciesId);
        Assert.True(spider.Traversal.HasFlag(
            EnemyTraversalCapability.VerticalClimb));
        Assert.True(spider.Traversal.HasFlag(
            EnemyTraversalCapability.DepthTraverse));
        Assert.True(spider.Traversal.HasFlag(
            EnemyTraversalCapability.WallClimb));
        Assert.True(spider.Traversal.HasFlag(
            EnemyTraversalCapability.CeilingAmbush));
    }

    [Fact]
    public void Stationary_enemy_requires_an_explicit_attachment_surface()
    {
        Assert.Throws<System.ArgumentException>(() => new EnemyCombatDefinition(
            "enemy.test.stationary",
            "Stationary test",
            maximumHealth: 1_000,
            minimumGroupSize: 1,
            maximumGroupSize: 1,
            EnemyTraversalCapability.Stationary,
            CaveEncounterCombatContent.CaveMonsterBiteProfileId));
    }

    [Fact]
    public void Resident_weapon_selection_is_data_driven_for_club_and_slingshot()
    {
        Assert.Collection(
            CaveEncounterCombatContent.ResidentWeaponDefinitions,
            club =>
            {
                Assert.Equal(CombatEquipmentContent.ClubItemId, club.ItemId);
                Assert.Equal(CaveEncounterCombatContent.ClubProfileId, club.ProfileId);
                Assert.Equal(10, club.SelectionPriority);
                Assert.Same(
                    club,
                    CaveEncounterCombatContent.FindResidentWeapon(club.ItemId));
            },
            slingshot =>
            {
                Assert.Equal(WorkshopProductionContent.SlingshotItemId, slingshot.ItemId);
                Assert.Equal(CaveEncounterCombatContent.SlingshotProfileId, slingshot.ProfileId);
                Assert.Equal(20, slingshot.SelectionPriority);
                Assert.Same(
                    slingshot,
                    CaveEncounterCombatContent.FindResidentWeapon(slingshot.ItemId));
            });

        Assert.Null(CaveEncounterCombatContent.FindResidentWeapon(
            new Dig.Domain.Inventory.ItemId("weapon.unknown")));
    }

    [Fact]
    public void Starting_profiles_use_the_four_tick_melee_cycle()
    {
        WeaponCatalog catalog = new WeaponCatalog(
            CaveEncounterCombatContent.CreateWeaponProfiles());

        AssertProfile(
            catalog.Get(CaveEncounterCombatContent.UnarmedProfileId),
            accuracy: 6_000,
            damage: 500,
            cooldown: CaveEncounterCombatContent.BaseMeleeCycleTicks,
            AgentSkillCatalog.UnarmedCombat);
        AssertProfile(
            catalog.Get(CaveEncounterCombatContent.ClubProfileId),
            accuracy: 6_500,
            damage: 850,
            cooldown: CaveEncounterCombatContent.BaseMeleeCycleTicks,
            AgentSkillCatalog.OneHandedCombat);

        WeaponProfile bite = catalog.Get(
            CaveEncounterCombatContent.CaveMonsterBiteProfileId);
        Assert.Equal(7_000, bite.Accuracy);
        Assert.Equal(650, bite.BaseDamage);
        Assert.Equal(CaveEncounterCombatContent.BaseMeleeCycleTicks, bite.CooldownTicks);
        Assert.Null(bite.SkillProfile);
    }

    [Fact]
    public void Skill_scaling_reaches_bounded_accuracy_damage_and_defense_caps()
    {
        CombatSkillScalingPolicy scaling =
            CombatSkillScalingPolicy.CreateCaveEncounter();

        Assert.Equal(0, scaling.ResolveAccuracyModifier(0));
        Assert.Equal(10_000, scaling.ResolveDamageMultiplier(0));
        Assert.Equal(0, scaling.ResolveDefenseReduction(0));
        Assert.Equal(2_500, scaling.ResolveAccuracyModifier(
            AgentSkillCatalog.IndividualMaximumUnits));
        Assert.Equal(14_000, scaling.ResolveDamageMultiplier(
            AgentSkillCatalog.IndividualMaximumUnits));
        Assert.Equal(3_000, scaling.ResolveDefenseReduction(
            AgentSkillCatalog.IndividualMaximumUnits));
    }

    [Fact]
    public void Damage_multiplier_and_defense_reduction_compose_deterministically()
    {
        WeaponProfileId weaponId = new WeaponProfileId("combat.test.scaled");
        CombatState combat = new CombatState(new WeaponCatalog(new[]
        {
            new WeaponProfile(
                weaponId,
                minimumRange: 1,
                maximumRange: 1,
                accuracy: 10_000,
                baseDamage: 1_000,
                armorPenetration: 0,
                cooldownTicks: 1),
        }));
        FactionState factions = CreateFactions();

        CombatAttackResolution result = combat.ResolveAttack(
            new CombatAttackRequest(
                new CombatActionId("scaled-damage"),
                AttackerId,
                TargetId,
                weaponId,
                worldSeed: 9UL,
                tick: 1),
            Combatant(
                AttackerId,
                Residents,
                new CellId(0, 0),
                damageMultiplier: 14_000,
                damageReduction: 0),
            Combatant(
                TargetId,
                Hostiles,
                new CellId(1, 0),
                damageMultiplier: 10_000,
                damageReduction: 3_000),
            factions).Value;

        Assert.Equal(CombatAttackOutcome.Hit, result.Outcome);
        Assert.Equal(980, result.Damage);
    }

    [Fact]
    public void Weapon_hit_chance_never_exceeds_ninety_five_percent()
    {
        WeaponCatalog catalog = new WeaponCatalog(
            CaveEncounterCombatContent.CreateWeaponProfiles());
        CombatState combat = new CombatState(catalog);

        CombatAttackResolution result = combat.ResolveAttack(
            new CombatAttackRequest(
                new CombatActionId("accuracy-cap"),
                AttackerId,
                TargetId,
                CaveEncounterCombatContent.ClubProfileId,
                worldSeed: 19UL,
                tick: 1),
            Combatant(
                AttackerId,
                Residents,
                new CellId(0, 0),
                damageMultiplier: 10_000,
                damageReduction: 0,
                accuracyModifier: 5_000),
            Combatant(
                TargetId,
                Hostiles,
                new CellId(1, 0),
                damageMultiplier: 10_000,
                damageReduction: 0),
            CreateFactions()).Value;

        Assert.Equal(9_500, result.HitChance);
    }

    private static EnemyCombatDefinition Find(string speciesId)
    {
        return CaveEncounterCombatContent.EnemyDefinitions.Single(
            value => value.SpeciesId == speciesId);
    }

    private static void AssertProfile(
        WeaponProfile profile,
        int accuracy,
        int damage,
        long cooldown,
        AgentSkillId skillId)
    {
        Assert.Equal(accuracy, profile.Accuracy);
        Assert.Equal(damage, profile.BaseDamage);
        Assert.Equal(cooldown, profile.CooldownTicks);
        Assert.Equal(9_500, profile.MaximumHitChance);
        Assert.Equal(skillId, profile.SkillProfile!.SkillId);
        Assert.Equal(25, profile.SkillProfile.HitGrantUnits);
    }

    private static CombatantSnapshot Combatant(
        EntityId id,
        FactionId faction,
        CellId cell,
        int damageMultiplier,
        int damageReduction,
        int accuracyModifier = 0)
    {
        return new CombatantSnapshot(
            id,
            faction,
            cell,
            isAlive: true,
            new NeedValue(10_000),
            accuracyModifier,
            evasion: 0,
            armor: 0,
            blockChance: 0,
            blockValue: 0,
            damageMultiplier,
            damageReduction);
    }

    private static FactionState CreateFactions()
    {
        FactionState factions = new FactionState(
            new FactionCatalog(new[]
            {
                new FactionDefinition(Residents, "Residents", -10_000),
                new FactionDefinition(Hostiles, "Hostiles", -10_000),
            }),
            new FactionDiplomacyPolicy(-5_000, 3_000, 8_000, 1_000));
        factions.AssignMember(AttackerId, Residents);
        factions.AssignMember(TargetId, Hostiles);
        return factions;
    }
}

}
