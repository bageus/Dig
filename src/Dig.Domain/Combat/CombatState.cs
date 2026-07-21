using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Runtime;

namespace Dig.Domain.Combat
{

public static class CombatErrors
{
    public static readonly DomainError CombatantDead = new DomainError(
        "combat.combatant.dead",
        "Both combatants must be alive when an attack is resolved.");

    public static readonly DomainError TargetNotHostile = new DomainError(
        "combat.target.not_hostile",
        "The target faction is not hostile to the attacker faction.");

    public static readonly DomainError TargetOutOfRange = new DomainError(
        "combat.target.out_of_range",
        "The target is outside the weapon range.");

    public static readonly DomainError AttackOnCooldown = new DomainError(
        "combat.attack.cooldown",
        "The attacker cannot use this weapon again yet.");
}

public sealed class CombatStateSnapshot
{
    public CombatStateSnapshot(
        long version,
        int processedActionCount,
        IReadOnlyList<CombatStatusSnapshot> statuses)
    {
        Version = version;
        ProcessedActionCount = processedActionCount;
        Statuses = statuses;
    }

    public long Version { get; }
    public int ProcessedActionCount { get; }
    public IReadOnlyList<CombatStatusSnapshot> Statuses { get; }
}

public sealed partial class CombatState : AggregateRoot
{
    private readonly Dictionary<CombatActionId, CombatAttackResolution> _resolutions =
        new Dictionary<CombatActionId, CombatAttackResolution>();
    private readonly Dictionary<EntityId, long> _lastAttackTicks =
        new Dictionary<EntityId, long>();
    private readonly Dictionary<CombatStatusKey, CombatStatusState> _statuses =
        new Dictionary<CombatStatusKey, CombatStatusState>();

    public CombatState(WeaponCatalog weapons)
    {
        Weapons = weapons ?? throw new ArgumentNullException(nameof(weapons));
    }

    public WeaponCatalog Weapons { get; }
    public long Version { get; private set; }

    public bool HasResolvedAttack(CombatActionId actionId) =>
        _resolutions.ContainsKey(actionId);

    public Result<CombatAttackResolution> ResolveAttack(
        CombatAttackRequest request,
        CombatantSnapshot attacker,
        CombatantSnapshot target,
        FactionState factions)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (attacker is null)
        {
            throw new ArgumentNullException(nameof(attacker));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (factions is null)
        {
            throw new ArgumentNullException(nameof(factions));
        }

        if (attacker.Id != request.AttackerId || target.Id != request.TargetId)
        {
            throw new ArgumentException("Combatant snapshots must match the attack request.");
        }

        if (_resolutions.TryGetValue(
            request.ActionId,
            out CombatAttackResolution? existing))
        {
            return Result<CombatAttackResolution>.Success(existing.AsReplay());
        }

        if (!attacker.IsAlive || !target.IsAlive)
        {
            return Result<CombatAttackResolution>.Failure(CombatErrors.CombatantDead);
        }

        if (!factions.AreHostile(attacker.FactionId, target.FactionId))
        {
            return Result<CombatAttackResolution>.Failure(CombatErrors.TargetNotHostile);
        }

        WeaponProfile weapon = Weapons.Get(request.WeaponProfileId);
        if (_lastAttackTicks.TryGetValue(attacker.Id, out long lastAttackTick)
            && request.Tick < checked(lastAttackTick + weapon.CooldownTicks))
        {
            return Result<CombatAttackResolution>.Failure(CombatErrors.AttackOnCooldown);
        }

        int distance = CalculateDistance(attacker.Position, target.Position);
        if (distance < weapon.MinimumRange || distance > weapon.MaximumRange)
        {
            return Result<CombatAttackResolution>.Failure(CombatErrors.TargetOutOfRange);
        }

        DeterministicRandomStream random = CreateActionRandom(
            request.WorldSeed,
            request.ActionId);
        int hitRoll = random.NextInt(10_000);
        int blockRoll = random.NextInt(10_000);
        int statusRoll = random.NextInt(10_000);
        int hitChance = Math.Max(
            0,
            Math.Min(10_000, weapon.Accuracy + attacker.AccuracyModifier - target.Evasion));
        CombatAttackOutcome outcome;
        int damage = 0;
        CombatStatusId? appliedStatusId = null;
        if (hitRoll >= hitChance)
        {
            outcome = CombatAttackOutcome.Miss;
        }
        else
        {
            int effectiveArmor = checked(
                target.Armor * (10_000 - weapon.ArmorPenetration) / 10_000);
            int afterArmor = Math.Max(0, weapon.BaseDamage - effectiveArmor);
            bool blocked = blockRoll < target.BlockChance;
            damage = Math.Max(0, afterArmor - (blocked ? target.BlockValue : 0));
            outcome = blocked ? CombatAttackOutcome.Blocked : CombatAttackOutcome.Hit;

            CombatStatusDefinition? status = weapon.StatusOnHit;
            if (damage > 0
                && status is not null
                && statusRoll < status.ApplicationChance)
            {
                appliedStatusId = status.Id;
                ApplyOrRefreshStatus(target.Id, status, request.ActionId, request.Tick);
            }
        }

        CombatAttackResolution resolution = new CombatAttackResolution(
            request.ActionId,
            attacker.Id,
            target.Id,
            weapon.Id,
            outcome,
            distance,
            hitChance,
            hitRoll,
            blockRoll,
            damage,
            appliedStatusId,
            wasAlreadyProcessed: false);
        _resolutions.Add(request.ActionId, resolution);
        _lastAttackTicks[attacker.Id] = request.Tick;
        Version = checked(Version + 1);
        Raise(new CombatAttackResolved(request.Tick, resolution));
        return Result<CombatAttackResolution>.Success(resolution);
    }

    public IReadOnlyList<CombatStatusDamage> AdvanceStatuses(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        List<CombatStatusDamage> damage = new List<CombatStatusDamage>();
        CombatStatusKey[] keys = _statuses.Keys.OrderBy(key => key).ToArray();
        foreach (CombatStatusKey key in keys)
        {
            CombatStatusDamage? applied = _statuses[key].Advance(tick);
            if (!applied.HasValue)
            {
                continue;
            }

            damage.Add(applied.Value);
            Raise(new CombatStatusTicked(tick, applied.Value));
            if (applied.Value.Expired)
            {
                _statuses.Remove(key);
            }
        }

        if (damage.Count > 0)
        {
            Version = checked(Version + 1);
        }

        return new ReadOnlyCollection<CombatStatusDamage>(damage);
    }

    public CombatStateSnapshot CreateSnapshot()
    {
        CombatStatusSnapshot[] statuses = _statuses
            .OrderBy(item => item.Key)
            .Select(item => item.Value.CreateSnapshot())
            .ToArray();
        return new CombatStateSnapshot(
            Version,
            _resolutions.Count,
            new ReadOnlyCollection<CombatStatusSnapshot>(statuses));
    }

    private void ApplyOrRefreshStatus(
        EntityId targetId,
        CombatStatusDefinition definition,
        CombatActionId sourceActionId,
        long tick)
    {
        CombatStatusKey key = new CombatStatusKey(targetId, definition.Id);
        if (_statuses.TryGetValue(key, out CombatStatusState? status))
        {
            status.Refresh(definition, sourceActionId, tick);
            return;
        }

        _statuses.Add(
            key,
            new CombatStatusState(targetId, definition, sourceActionId, tick));
    }

    private static DeterministicRandomStream CreateActionRandom(
        ulong worldSeed,
        CombatActionId actionId)
    {
        RandomStreamCatalog catalog = new RandomStreamCatalog(worldSeed);
        return catalog.GetOrCreate("combat.attack." + actionId);
    }

    private static int CalculateDistance(
        Dig.Domain.World.CellId first,
        Dig.Domain.World.CellId second)
    {
        return checked(Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y));
    }

    private readonly struct CombatStatusKey : IEquatable<CombatStatusKey>, IComparable<CombatStatusKey>
    {
        public CombatStatusKey(EntityId targetId, CombatStatusId statusId)
        {
            TargetId = targetId;
            StatusId = statusId;
        }

        public EntityId TargetId { get; }
        public CombatStatusId StatusId { get; }

        public int CompareTo(CombatStatusKey other)
        {
            int target = string.Compare(
                TargetId.ToString(),
                other.TargetId.ToString(),
                StringComparison.Ordinal);
            return target != 0 ? target : StatusId.CompareTo(other.StatusId);
        }

        public bool Equals(CombatStatusKey other)
        {
            return TargetId == other.TargetId && StatusId == other.StatusId;
        }

        public override bool Equals(object? obj)
        {
            return obj is CombatStatusKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TargetId, StatusId);
        }
    }
}
}
