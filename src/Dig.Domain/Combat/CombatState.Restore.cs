using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;

namespace Dig.Domain.Combat
{

public readonly struct CombatCooldownSnapshot
{
    public CombatCooldownSnapshot(EntityId actorId, long lastAttackTick)
    {
        if (actorId.IsEmpty || lastAttackTick < 0)
        {
            throw new ArgumentException("Combat cooldown snapshot is invalid.");
        }

        ActorId = actorId;
        LastAttackTick = lastAttackTick;
    }

    public EntityId ActorId { get; }
    public long LastAttackTick { get; }
}

public sealed partial class CombatState
{
    public Result RestoreRuntime(
        long version,
        IReadOnlyCollection<CombatIntentSnapshot> intents,
        IReadOnlyCollection<CombatExecutionSnapshot> executions,
        IReadOnlyCollection<CombatAttackResolution> resolutions,
        IReadOnlyCollection<CombatCooldownSnapshot> cooldowns,
        IReadOnlyCollection<CombatStatusSnapshot> statuses)
    {
        if (version < 0 || intents is null || executions is null
            || resolutions is null || cooldowns is null || statuses is null)
        {
            return Result.Failure(new DomainError(
                "combat.restore.invalid",
                "The combat runtime snapshot is invalid."));
        }

        _intents.Clear();
        _activeIntents.Clear();
        _executions.Clear();
        _activeExecutions.Clear();
        _resolutions.Clear();
        _lastAttackTicks.Clear();
        _statuses.Clear();

        foreach (CombatIntentSnapshot intent in intents.OrderBy(value => value.IntentId))
        {
            CombatIntentRequest request = new CombatIntentRequest(
                intent.IntentId,
                intent.ActorId,
                intent.Kind,
                intent.Source,
                intent.CreatedTick,
                intent.ExpiresTick,
                intent.TargetEntityId,
                intent.TargetCell);
            CombatIntentRecord record = new CombatIntentRecord(request);
            if (intent.Status != CombatIntentStatus.Active)
            {
                record.Finish(
                    intent.Status,
                    intent.FinishedTick ?? intent.CreatedTick,
                    intent.FinishReason ?? "restored_terminal");
            }

            _intents.Add(intent.IntentId, record);
            if (intent.Status == CombatIntentStatus.Active)
            {
                if (_activeIntents.ContainsKey(intent.ActorId))
                {
                    return Result.Failure(new DomainError(
                        "combat.restore.duplicate_active_intent",
                        "An actor cannot restore multiple active combat intents."));
                }

                _activeIntents.Add(intent.ActorId, intent.IntentId);
            }
        }

        foreach (CombatExecutionSnapshot execution in executions.OrderBy(value => value.ExecutionId))
        {
            bool weaponKnown = !execution.WeaponProfileId.HasValue
                || Weapons.Profiles.Any(value => value.Id == execution.WeaponProfileId.Value);
            if (!_intents.ContainsKey(execution.IntentId) || !weaponKnown)
            {
                return Result.Failure(new DomainError(
                    "combat.restore.reference_invalid",
                    "The combat execution references an unknown intent or weapon."));
            }

            CombatExecutionRecord record = CombatExecutionRecord.Restore(execution);
            _executions.Add(execution.ExecutionId, record);
            if (!execution.IsTerminal)
            {
                if (_activeExecutions.ContainsKey(execution.ActorId))
                {
                    return Result.Failure(new DomainError(
                        "combat.restore.duplicate_active_execution",
                        "An actor cannot restore multiple active combat executions."));
                }

                _activeExecutions.Add(execution.ActorId, execution.ExecutionId);
            }
        }

        foreach (CombatAttackResolution resolution in resolutions.OrderBy(value => value.ActionId))
        {
            Weapons.Get(resolution.WeaponProfileId);
            _resolutions.Add(resolution.ActionId, resolution);
        }

        foreach (CombatCooldownSnapshot cooldown in cooldowns.OrderBy(value => value.ActorId.ToString(), StringComparer.Ordinal))
        {
            _lastAttackTicks.Add(cooldown.ActorId, cooldown.LastAttackTick);
        }

        foreach (CombatStatusSnapshot status in statuses
            .OrderBy(value => value.TargetId.ToString(), StringComparer.Ordinal)
            .ThenBy(value => value.StatusId))
        {
            CombatStatusKey key = new CombatStatusKey(status.TargetId, status.StatusId);
            _statuses.Add(key, CombatStatusState.Restore(status));
        }

        Version = version;
        DequeueUncommittedEvents();
        return Result.Success();
    }
}
}
