using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Inventory;

namespace Dig.Domain.Content
{

[Flags]
public enum EnemyTraversalCapability
{
    None = 0,
    SupportedWalk = 1 << 0,
    VerticalClimb = 1 << 1,
    DepthTraverse = 1 << 2,
    WallClimb = 1 << 3,
    CeilingAmbush = 1 << 4,
    Stationary = 1 << 5,
}

[Flags]
public enum EnemyAttachmentSurface
{
    None = 0,
    HorizontalTunnel = 1 << 0,
    CaveFloor = 1 << 1,
    CaveWall = 1 << 2,
    CaveCeiling = 1 << 3,
}

public sealed class EnemyCombatDefinition
{
    public EnemyCombatDefinition(
        string speciesId,
        string displayName,
        int maximumHealth,
        int minimumGroupSize,
        int maximumGroupSize,
        EnemyTraversalCapability traversal,
        WeaponProfileId attackProfileId,
        EnemyAttachmentSurface attachmentSurfaces = EnemyAttachmentSurface.None,
        int patrolWanderRadius = 0,
        int patrolIntervalTicks = 0,
        int sightRange = 0,
        bool retainsAggroUntilTargetUnavailable = false)
    {
        if (string.IsNullOrWhiteSpace(speciesId)
            || string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Enemy species and display name are required.");
        }

        if (maximumHealth <= 0 || maximumHealth > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumHealth));
        }

        if (minimumGroupSize <= 0 || maximumGroupSize < minimumGroupSize)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumGroupSize));
        }

        if (attackProfileId.IsEmpty
            || traversal == EnemyTraversalCapability.None
            || !Enum.IsDefined(typeof(EnemyTraversalCapability), traversal)
                && !IsValidFlags(traversal))
        {
            throw new ArgumentException("Enemy traversal and attack profile are required.");
        }

        bool stationary = traversal.HasFlag(EnemyTraversalCapability.Stationary);
        if (stationary && traversal != EnemyTraversalCapability.Stationary)
        {
            throw new ArgumentException(
                "A stationary enemy cannot also declare voluntary movement capabilities.",
                nameof(traversal));
        }

        if (!IsValidAttachmentFlags(attachmentSurfaces))
        {
            throw new ArgumentException(
                "Enemy attachment surfaces contain an unsupported value.",
                nameof(attachmentSurfaces));
        }

        if (stationary && attachmentSurfaces == EnemyAttachmentSurface.None)
        {
            throw new ArgumentException(
                "A stationary enemy requires at least one legal attachment surface.",
                nameof(attachmentSurfaces));
        }

        if (patrolWanderRadius < 0 || patrolIntervalTicks < 0 || sightRange < 0
            || (patrolWanderRadius == 0) != (patrolIntervalTicks == 0))
        {
            throw new ArgumentOutOfRangeException(nameof(patrolWanderRadius));
        }

        if (stationary && patrolWanderRadius != 0)
        {
            throw new ArgumentException(
                "A stationary enemy cannot declare a patrol profile.",
                nameof(patrolWanderRadius));
        }

        if (retainsAggroUntilTargetUnavailable && sightRange <= 0)
        {
            throw new ArgumentException(
                "Persistent aggro requires a positive sight range.",
                nameof(sightRange));
        }

        SpeciesId = speciesId.Trim();
        DisplayName = displayName.Trim();
        MaximumHealth = maximumHealth;
        MinimumGroupSize = minimumGroupSize;
        MaximumGroupSize = maximumGroupSize;
        Traversal = traversal;
        AttackProfileId = attackProfileId;
        AttachmentSurfaces = attachmentSurfaces;
        PatrolWanderRadius = patrolWanderRadius;
        PatrolIntervalTicks = patrolIntervalTicks;
        SightRange = sightRange;
        RetainsAggroUntilTargetUnavailable = retainsAggroUntilTargetUnavailable;
    }

    public string SpeciesId { get; }
    public string DisplayName { get; }
    public int MaximumHealth { get; }
    public int MinimumGroupSize { get; }
    public int MaximumGroupSize { get; }
    public EnemyTraversalCapability Traversal { get; }
    public WeaponProfileId AttackProfileId { get; }
    public EnemyAttachmentSurface AttachmentSurfaces { get; }
    public int PatrolWanderRadius { get; }
    public int PatrolIntervalTicks { get; }
    public int SightRange { get; }
    public bool RetainsAggroUntilTargetUnavailable { get; }
    public bool HasPatrol => PatrolWanderRadius > 0;

    private static bool IsValidFlags(EnemyTraversalCapability value)
    {
        const EnemyTraversalCapability all =
            EnemyTraversalCapability.SupportedWalk
            | EnemyTraversalCapability.VerticalClimb
            | EnemyTraversalCapability.DepthTraverse
            | EnemyTraversalCapability.WallClimb
            | EnemyTraversalCapability.CeilingAmbush
            | EnemyTraversalCapability.Stationary;
        return (value & ~all) == 0;
    }

    private static bool IsValidAttachmentFlags(EnemyAttachmentSurface value)
    {
        const EnemyAttachmentSurface all =
            EnemyAttachmentSurface.HorizontalTunnel
            | EnemyAttachmentSurface.CaveFloor
            | EnemyAttachmentSurface.CaveWall
            | EnemyAttachmentSurface.CaveCeiling;
        return (value & ~all) == 0;
    }
}

public sealed class ResidentCombatWeaponDefinition
{
    public ResidentCombatWeaponDefinition(
        ItemId itemId,
        WeaponProfileId profileId,
        int selectionPriority)
    {
        if (itemId.IsEmpty || profileId.IsEmpty)
        {
            throw new ArgumentException("Weapon item and profile are required.");
        }

        if (selectionPriority < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(selectionPriority));
        }

        ItemId = itemId;
        ProfileId = profileId;
        SelectionPriority = selectionPriority;
    }

    public ItemId ItemId { get; }
    public WeaponProfileId ProfileId { get; }
    public int SelectionPriority { get; }
}

public static class CaveEncounterCombatContent
{
    public const string CaveMonsterSpeciesId = "enemy.vuker";
    public const string PredatoryVineSpeciesId = "enemy.plant.predatory_vine";
    public const string SwallowerSpeciesId = "enemy.demon.swallower";
    public const string SpiderSpeciesId = "enemy.spider";

    public static readonly WeaponProfileId UnarmedProfileId =
        new WeaponProfileId("combat.weapon.unarmed");
    public static readonly WeaponProfileId ClubProfileId =
        new WeaponProfileId("combat.weapon.club");
    public static readonly WeaponProfileId CaveMonsterBiteProfileId =
        new WeaponProfileId("combat.enemy.cave_bite");

    public static readonly CombatDefenseSkillProfile ResidentReceivedHitProfile =
        new CombatDefenseSkillProfile(
            "combat.defense.received_hit",
            defenseGrantUnits: 10);

    private static readonly IReadOnlyList<ResidentCombatWeaponDefinition>
        ResidentWeapons = new ReadOnlyCollection<ResidentCombatWeaponDefinition>(new[]
        {
            new ResidentCombatWeaponDefinition(
                CombatEquipmentContent.ClubItemId,
                ClubProfileId,
                selectionPriority: 10),
        });

    private static readonly IReadOnlyList<EnemyCombatDefinition> Enemies =
        new ReadOnlyCollection<EnemyCombatDefinition>(new[]
        {
            new EnemyCombatDefinition(
                CaveMonsterSpeciesId,
                "Пещерный монстр",
                maximumHealth: 7_000,
                minimumGroupSize: 2,
                maximumGroupSize: 2,
                EnemyTraversalCapability.SupportedWalk
                    | EnemyTraversalCapability.VerticalClimb
                    | EnemyTraversalCapability.DepthTraverse,
                CaveMonsterBiteProfileId,
                patrolWanderRadius: 6,
                patrolIntervalTicks: 4,
                sightRange: 6,
                retainsAggroUntilTargetUnavailable: true),
            new EnemyCombatDefinition(
                PredatoryVineSpeciesId,
                "Хищная лиана",
                maximumHealth: 5_000,
                minimumGroupSize: 1,
                maximumGroupSize: 1,
                EnemyTraversalCapability.Stationary,
                CaveMonsterBiteProfileId,
                EnemyAttachmentSurface.HorizontalTunnel
                    | EnemyAttachmentSurface.CaveFloor
                    | EnemyAttachmentSurface.CaveWall),
            new EnemyCombatDefinition(
                SwallowerSpeciesId,
                "Живоглот",
                maximumHealth: 8_000,
                minimumGroupSize: 2,
                maximumGroupSize: 3,
                EnemyTraversalCapability.SupportedWalk
                    | EnemyTraversalCapability.DepthTraverse,
                CaveMonsterBiteProfileId),
            new EnemyCombatDefinition(
                SpiderSpeciesId,
                "Паук",
                maximumHealth: 4_500,
                minimumGroupSize: 1,
                maximumGroupSize: 2,
                EnemyTraversalCapability.SupportedWalk
                    | EnemyTraversalCapability.VerticalClimb
                    | EnemyTraversalCapability.DepthTraverse
                    | EnemyTraversalCapability.WallClimb
                    | EnemyTraversalCapability.CeilingAmbush,
                CaveMonsterBiteProfileId),
        }.OrderBy(value => value.SpeciesId, StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<EnemyCombatDefinition> EnemyDefinitions => Enemies;

    public static IReadOnlyList<ResidentCombatWeaponDefinition>
        ResidentWeaponDefinitions => ResidentWeapons;

    public static ResidentCombatWeaponDefinition? FindResidentWeapon(ItemId itemId)
    {
        return ResidentWeapons.FirstOrDefault(value => value.ItemId == itemId);
    }

    public static EnemyCombatDefinition CaveMonster =>
        Enemies.Single(value => value.SpeciesId == CaveMonsterSpeciesId);

    public static IReadOnlyList<WeaponProfile> CreateWeaponProfiles()
    {
        return new[]
        {
            new WeaponProfile(
                UnarmedProfileId,
                minimumRange: 1,
                maximumRange: 1,
                accuracy: 6_000,
                baseDamage: 500,
                armorPenetration: 0,
                cooldownTicks: 2,
                skillProfile: new CombatSkillProfile(
                    AgentSkillCatalog.UnarmedCombat,
                    hitGrantUnits: 25),
                spatialMode: CombatAttackSpatialMode.Melee,
                maximumHitChance: 9_500),
            new WeaponProfile(
                ClubProfileId,
                minimumRange: 1,
                maximumRange: 1,
                accuracy: 6_500,
                baseDamage: 850,
                armorPenetration: 0,
                cooldownTicks: 2,
                skillProfile: new CombatSkillProfile(
                    AgentSkillCatalog.OneHandedCombat,
                    hitGrantUnits: 25),
                spatialMode: CombatAttackSpatialMode.Melee,
                maximumHitChance: 9_500),
            new WeaponProfile(
                CaveMonsterBiteProfileId,
                minimumRange: 1,
                maximumRange: 1,
                accuracy: 7_000,
                baseDamage: 650,
                armorPenetration: 0,
                cooldownTicks: 3,
                spatialMode: CombatAttackSpatialMode.Melee,
                maximumHitChance: 9_500),
        };
    }
}

}
