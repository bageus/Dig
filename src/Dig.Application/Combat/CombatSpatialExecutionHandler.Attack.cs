using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Navigation;

namespace Dig.Application.Combat
{

public sealed partial class CombatSpatialExecutionHandler
{
    private Result<CombatSpatialExecutionReport> ResolveAttack(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        AgentState actor)
    {
        CombatExecutionSnapshot execution = combat.GetActiveExecution(actor.Id)!;
        AgentState? target = execution.TargetEntityId.HasValue
            ? _agents.Get(execution.TargetEntityId.Value)
            : null;
        if (!IsValidHostile(actor, target))
        {
            combat.AdvanceExecutionStage(
                execution.ExecutionId,
                CombatExecutionStage.AcquireTarget,
                command.Tick,
                command.Tick,
                "target_requires_reacquire");
            SaveCombat(combat);
            return Report(
                combat.GetActiveExecution(actor.Id)!,
                false,
                null,
                "target_requires_reacquire");
        }

        if (!execution.WeaponProfileId.HasValue)
        {
            return Result<CombatSpatialExecutionReport>.Failure(
                CombatSpatialApplicationErrors.EquipmentUnavailable);
        }

        Result<CombatEquipmentSelection> selection = _equipment.Select(
            actor.Id,
            target!.Id);
        if (selection.IsFailure)
        {
            return Block(command, combat, execution, selection.Error!);
        }

        if (selection.Value.WeaponProfileId != execution.WeaponProfileId.Value)
        {
            Result changed = combat.SetExecutionEquipment(
                execution.ExecutionId,
                selection.Value.WeaponProfileId,
                command.Tick,
                "equipment_changed");
            if (changed.IsFailure)
            {
                return Result<CombatSpatialExecutionReport>.Failure(changed.Error!);
            }

            Result cleared = combat.SetExecutionEngagement(
                execution.ExecutionId,
                null,
                command.Tick,
                "equipment_changed");
            if (cleared.IsFailure)
            {
                return Result<CombatSpatialExecutionReport>.Failure(cleared.Error!);
            }

            Result advanced = combat.AdvanceExecutionStage(
                execution.ExecutionId,
                CombatExecutionStage.SelectEngagementCell,
                command.Tick,
                command.Tick,
                "equipment_changed");
            if (advanced.IsFailure)
            {
                return Result<CombatSpatialExecutionReport>.Failure(advanced.Error!);
            }
            SaveCombat(combat);
            return Report(
                combat.GetActiveExecution(actor.Id)!,
                false,
                null,
                "equipment_changed");
        }

        WeaponProfile weapon = combat.Weapons.Get(selection.Value.WeaponProfileId);
        int distance = CombatSpatialMath.Distance3D(actor.Position, target.Position);
        bool spatiallyValid = distance >= weapon.MinimumRange
            && distance <= weapon.MaximumRange
            && (weapon.SpatialMode == CombatAttackSpatialMode.Melee
                ? _volume.ClassifyTraversal(actor.Position, target.Position)
                    != TunnelTraversalKind.Invalid
                : CombatLineOfSightResolver.HasLineOfSight(
                    actor.Position,
                    target.Position,
                    cell => !_volume.IsOpen(cell)));
        if (!spatiallyValid)
        {
            combat.AdvanceExecutionStage(
                execution.ExecutionId,
                CombatExecutionStage.SelectEngagementCell,
                command.Tick,
                command.Tick,
                "target_moved_out_of_engagement");
            SaveCombat(combat);
            return Report(
                combat.GetActiveExecution(actor.Id)!,
                false,
                null,
                "target_moved_out_of_engagement");
        }

        CombatActionId actionId = new CombatActionId(
            execution.ExecutionId + ":attack:" + execution.ResolvedActionCount);
        Result<CombatAttackResolution> resolved = _attackHandler.Handle(
            new ResolveCombatAttackCommand(
                actionId,
                actor.Id,
                target.Id,
                weapon.Id,
                command.WorldSeed,
                command.Tick,
                selection.Value.AttackerModifiers,
                selection.Value.TargetModifiers,
                sourceIntentId: null));
        if (resolved.IsFailure)
        {
            if (CombatErrors.AttackOnCooldown.Equals(resolved.Error))
            {
                combat.AdvanceExecutionStage(
                    execution.ExecutionId,
                    CombatExecutionStage.ResolveAttack,
                    checked(command.Tick + 1),
                    command.Tick,
                    "attack_cooldown");
                SaveCombat(combat);
                return Report(
                    combat.GetActiveExecution(actor.Id)!,
                    false,
                    null,
                    "attack_cooldown");
            }

            return Block(command, combat, execution, resolved.Error!);
        }

        long recovery = Math.Max(_policy.RecoveryTicks, weapon.CooldownTicks);
        combat = _combat.Get();
        combat.RecordExecutionAttack(
            execution.ExecutionId,
            actionId,
            checked(command.Tick + recovery),
            command.Tick);
        if (!resolved.Value.WasAlreadyProcessed)
        {
            combat.PublishAlarm(new CombatAlarmStimulus(
                actor.Id,
                target.Id,
                target.Position,
                _policy.AlarmRadius,
                command.Tick));
            IssueAlarmIntents(combat, actor, target, command.Tick);
        }

        SaveCombat(combat);
        return Report(
            combat.GetActiveExecution(actor.Id)!,
            false,
            resolved.Value,
            "attack_resolved");
    }

    private void IssueAlarmIntents(
        CombatState combat,
        AgentState attacker,
        AgentState victim,
        long tick)
    {
        var factions = _factions.Get();
        var victimFaction = factions.GetMemberFaction(victim.Id);
        if (!victimFaction.HasValue)
        {
            return;
        }

        foreach (AgentState ally in _agents.GetAll()
            .Where(candidate => candidate.IsAlive && candidate.Id != victim.Id)
            .OrderBy(candidate => candidate.Id.ToString(), StringComparer.Ordinal))
        {
            var faction = factions.GetMemberFaction(ally.Id);
            if (!faction.HasValue
                || faction.Value != victimFaction.Value
                || CombatSpatialMath.Distance3D(ally.Position, victim.Position)
                    > _policy.AlarmRadius)
            {
                continue;
            }

            CombatIntentSnapshot? active = combat.GetActiveIntent(ally.Id);
            if (active is not null
                && (active.Source == CombatIntentSource.PlayerOrder
                    || active.Kind == CombatIntentKind.Retreat))
            {
                continue;
            }

            CombatIntentId id = new CombatIntentId(
                "alarm:" + ally.Id + ":" + attacker.Id + ":" + tick);
            long expiresTick = _policy.RetainsAggro(ally.Id)
                ? long.MaxValue
                : checked(tick + Math.Max(1, _policy.SightRange));
            combat.IssueIntent(new CombatIntentRequest(
                id,
                ally.Id,
                CombatIntentKind.Attack,
                CombatIntentSource.Alarm,
                tick,
                expiresTick,
                attacker.Id,
                attacker.Position));
        }
    }

    private Result<CombatSpatialExecutionReport> RetryBlocked(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        CombatExecutionSnapshot execution)
    {
        if (execution.RetryCount > _policy.MaximumRetries)
        {
            CombatIntentSnapshot? intent = combat.GetActiveIntent(command.ActorId);
            bool persistent = intent?.IsPersistent == true;
            combat.CancelExecution(
                execution.ExecutionId,
                command.Tick,
                persistent ? "persistent_retry_replan" : "retry_exhausted");
            if (intent is not null && !persistent)
            {
                combat.CancelIntent(intent.IntentId, "retry_exhausted", command.Tick);
            }

            SaveCombat(combat);
            return Report(
                combat.GetExecution(execution.ExecutionId)!,
                false,
                null,
                persistent ? "persistent_retry_replan" : "retry_exhausted");
        }

        return Advance(
            combat,
            execution,
            CombatExecutionStage.AcquireTarget,
            command.Tick,
            command.Tick,
            "retrying");
    }

    private Result<CombatSpatialExecutionReport> Block(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        CombatExecutionSnapshot execution,
        DomainError error)
    {
        if (execution.RetryCount >= _policy.MaximumRetries)
        {
            CombatIntentSnapshot? intent = combat.GetActiveIntent(command.ActorId);
            bool persistent = intent?.IsPersistent == true;
            combat.CancelExecution(
                execution.ExecutionId,
                command.Tick,
                persistent
                    ? error.Code + ":persistent_retry_replan"
                    : error.Code + ":retry_exhausted");
            if (intent is not null && !persistent)
            {
                combat.CancelIntent(intent.IntentId, "retry_exhausted", command.Tick);
            }
        }
        else
        {
            combat.IncrementExecutionRetry(
                execution.ExecutionId,
                checked(command.Tick + _policy.RetryDelayTicks),
                command.Tick,
                error.Code);
        }

        SaveCombat(combat);
        return Report(
            combat.GetExecution(execution.ExecutionId)!,
            false,
            null,
            error.Code);
    }
}
}
