using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.World;

namespace Dig.Domain.Combat
{

public enum CombatAttackOutcome
{
    Miss = 0,
    Blocked = 1,
    Hit = 2,
}

public sealed class CombatStatusDefinition
{
    public CombatStatusDefinition(
        CombatStatusId id,
        int applicationChance,
        int durationTicks,
        int damagePerTick)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Status id cannot be empty.", nameof(id));
        }

        ValidateChance(applicationChance, nameof(applicationChance));
        if (durationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationTicks));
        }

        if (damagePerTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damagePerTick));
        }

        Id = id;
        ApplicationChance = applicationChance;
        DurationTicks = durationTicks;
        DamagePerTick = damagePerTick;
    }

    public CombatStatusId Id { get; }
    public int ApplicationChance { get; }
    public int DurationTicks { get; }
    public int DamagePerTick { get; }

    private static void ValidateChance(int chance, string parameterName)
    {
        if (chance < 0 || chance > 10_000)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public sealed class WeaponProfile
{
    public WeaponProfile(
        WeaponProfileId id,
        int minimumRange,
        int maximumRange,
        int accuracy,
        int baseDamage,
        int armorPenetration,
        long cooldownTicks,
        CombatStatusDefinition? statusOnHit = null)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Weapon profile id cannot be empty.", nameof(id));
        }

        if (minimumRange < 0 || maximumRange < minimumRange)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumRange));
        }

        ValidateChance(accuracy, nameof(accuracy));
        ValidateChance(armorPenetration, nameof(armorPenetration));
        if (baseDamage <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseDamage));
        }

        if (cooldownTicks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cooldownTicks));
        }

        Id = id;
        MinimumRange = minimumRange;
        MaximumRange = maximumRange;
        Accuracy = accuracy;
        BaseDamage = baseDamage;
        ArmorPenetration = armorPenetration;
        CooldownTicks = cooldownTicks;
        StatusOnHit = statusOnHit;
    }

    public WeaponProfileId Id { get; }
    public int MinimumRange { get; }
    public int MaximumRange { get; }
    public int Accuracy { get; }
    public int BaseDamage { get; }
    public int ArmorPenetration { get; }
    public long CooldownTicks { get; }
    public CombatStatusDefinition? StatusOnHit { get; }

    private static void ValidateChance(int chance, string parameterName)
    {
        if (chance < 0 || chance > 10_000)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public sealed class WeaponCatalog
{
    private readonly Dictionary<WeaponProfileId, WeaponProfile> _profiles;

    public WeaponCatalog(IEnumerable<WeaponProfile> profiles)
    {
        if (profiles is null)
        {
            throw new ArgumentNullException(nameof(profiles));
        }

        WeaponProfile[] values = profiles.OrderBy(item => item.Id).ToArray();
        if (values.Length == 0)
        {
            throw new ArgumentException("Weapon catalog cannot be empty.", nameof(profiles));
        }

        if (values.Select(item => item.Id).Distinct().Count() != values.Length)
        {
            throw new ArgumentException("Weapon profile ids must be unique.", nameof(profiles));
        }

        _profiles = values.ToDictionary(item => item.Id);
        Profiles = new ReadOnlyCollection<WeaponProfile>(values);
    }

    public IReadOnlyList<WeaponProfile> Profiles { get; }

    public WeaponProfile Get(WeaponProfileId id)
    {
        return _profiles.TryGetValue(id, out WeaponProfile? profile)
            ? profile
            : throw new KeyNotFoundException($"Weapon profile '{id}' is not registered.");
    }
}

public sealed class CombatantSnapshot
{
    public CombatantSnapshot(
        EntityId id,
        FactionId factionId,
        CellId position,
        bool isAlive,
        NeedValue health,
        int accuracyModifier,
        int evasion,
        int armor,
        int blockChance,
        int blockValue)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("Combatant id cannot be empty.", nameof(id));
        }

        if (factionId.IsEmpty)
        {
            throw new ArgumentException("Faction id cannot be empty.", nameof(factionId));
        }

        ValidateSignedModifier(accuracyModifier, nameof(accuracyModifier));
        ValidateChance(evasion, nameof(evasion));
        ValidateChance(blockChance, nameof(blockChance));
        if (armor < 0 || armor > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(armor));
        }

        if (blockValue < 0 || blockValue > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(blockValue));
        }

        Id = id;
        FactionId = factionId;
        Position = position;
        IsAlive = isAlive;
        Health = health;
        AccuracyModifier = accuracyModifier;
        Evasion = evasion;
        Armor = armor;
        BlockChance = blockChance;
        BlockValue = blockValue;
    }

    public EntityId Id { get; }
    public FactionId FactionId { get; }
    public CellId Position { get; }
    public bool IsAlive { get; }
    public NeedValue Health { get; }
    public int AccuracyModifier { get; }
    public int Evasion { get; }
    public int Armor { get; }
    public int BlockChance { get; }
    public int BlockValue { get; }

    private static void ValidateChance(int value, string parameterName)
    {
        if (value < 0 || value > 10_000)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateSignedModifier(int value, string parameterName)
    {
        if (value < -10_000 || value > 10_000)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
}
