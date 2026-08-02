using System;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.World;

namespace Dig.Application.Combat
{

public sealed partial class CombatSpatialExecutionHandler
{
    private Result<CombatSpatialExecutionReport> AcquireTarget(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        CombatIntentSnapshot intent,
        AgentState actor)
    {
        EntityId? requested = intent.TargetEntityId;
        AgentState? target = requested.HasValue ? _agents.Get(requested.Value) : null;
        if (!IsValidHostile(actor, target))
        {
            target = intent.Source == CombatIntentSource.PlayerOrder
                ? null
                : FindNearestThreat(actor, null);
        }
        else if (intent.Source != CombatIntentSource.PlayerOrder
            && !IsVisible(actor.Position, target!.Position))
        {
            target = FindNearestThreat(actor, target.Id);
        }

        if (target is null)
        {
            string reason = intent.Source == CombatIntentSource.PlayerOrder
                ? "target_unavailable"
                : "enemy_target_out_of_sight";
            return FinishForTargetLoss(command, combat, intent, reason);
        }

        CellId lastKnownCell = IsVisible(actor.Position, target.Position)
            ? target.Position
            : intent.TargetCell ?? target.Position;
        CombatExecutionSnapshot execution = combat.GetActiveExecution(actor.Id)!;
        Result targetSet = combat.SetExecutionTarget(
            execution.ExecutionId,
            target.Id,
            lastKnownCell,
            command.Tick,
            "target_acquired");
        if (targetSet.IsFailure)
        {
            return Result<CombatSpatialExecutionReport>.Failure(targetSet.Error!);
        }

        Result retargeted = intent.TargetEntityId == target.Id
            ? Result.Success()
            : combat.RetargetIntent(
                intent.IntentId,
                target.Id,
                target.Position,
                command.Tick,
                "nearest_hostile_retargeted");
        if (retargeted.IsFailure)
        {
            return Result<CombatSpatialExecutionReport>.Failure(retargeted.Error!);
        }

        combat.AdvanceExecutionStage(
            execution.ExecutionId,
            CombatExecutionStage.SelectEquipment,
            command.Tick,
            command.Tick,
            "target_acquired");
        SaveCombat(combat);
        return Report(combat.GetActiveExecution(actor.Id)!, false, null, "target_acquired");
    }

    private Result<CombatSpatialExecutionReport> Reevaluate(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        CombatIntentSnapshot intent,
        AgentState actor)
    {
        CombatExecutionSnapshot execution = combat.GetActiveExecution(actor.Id)!;
        AgentState? target = execution.TargetEntityId.HasValue
            ? _agents.Get(execution.TargetEntityId.Value)
            : null;
        if (!IsValidHostile(actor, target))
        {
            return TryRetargetOrFinish(
                command,
                combat,
                intent,
                actor,
                execution.TargetEntityId,
                "target_dead_or_lost");
        }

        bool visible = IsVisible(actor.Position, target!.Position);
        if (!visible && intent.Source != CombatIntentSource.PlayerOrder)
        {
            return FinishForTargetLoss(
                command,
                combat,
                intent,
                "enemy_target_out_of_sight");
        }

        if (!visible)
        {
            return PursueLastKnownOrFinish(
                command,
                combat,
                intent,
                actor,
                target);
        }

        bool retreat = !intent.IsPersistent
            && ShouldRetreat(actor, target, command.Tick);
        if (retreat)
        {
            combat.AdvanceExecutionStage(
                execution.ExecutionId,
                CombatExecutionStage.Retreat,
                command.Tick,
                command.Tick,
                "tactical_retreat");
        }
        else
        {
            combat.SetExecutionTarget(
                execution.ExecutionId,
                target.Id,
                target.Position,
                command.Tick,
                "target_reconfirmed");
            combat.AdvanceExecutionStage(
                execution.ExecutionId,
                CombatExecutionStage.SelectEngagementCell,
                command.Tick,
                command.Tick,
                "target_reconfirmed");
        }

        SaveCombat(combat);
        return Report(
            combat.GetActiveExecution(actor.Id)!,
            false,
            null,
            retreat ? "tactical_retreat" : "target_reconfirmed");
    }

    private Result<CombatSpatialExecutionReport> PursueLastKnownOrFinish(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        CombatIntentSnapshot intent,
        AgentState actor,
        AgentState target)
    {
        CombatExecutionSnapshot execution = combat.GetActiveExecution(actor.Id)!;
        if (!execution.LastKnownTargetCell.HasValue
            || actor.Position == execution.LastKnownTargetCell.Value)
        {
            return TryRetargetOrFinish(
                command,
                combat,
                intent,
                actor,
                target.Id,
                "target_lost");
        }

        combat.SetExecutionEngagement(
            execution.ExecutionId,
            execution.LastKnownTargetCell.Value,
            command.Tick,
            "last_known_target_cell_selected");
        combat.AdvanceExecutionStage(
            execution.ExecutionId,
            CombatExecutionStage.Approach,
            command.Tick,
            command.Tick,
            "pursuing_last_known_target_cell");
        SaveCombat(combat);
        return Report(
            combat.GetActiveExecution(actor.Id)!,
            false,
            null,
            "pursuing_last_known_target_cell");
    }

    private Result<CombatSpatialExecutionReport> TryRetargetOrFinish(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        CombatIntentSnapshot intent,
        AgentState actor,
        EntityId? excludedTarget,
        string finishReason)
    {
        if (intent.Source != CombatIntentSource.PlayerOrder)
        {
            AgentState? replacement = FindNearestThreat(actor, excludedTarget);
            if (replacement is not null)
            {
                CombatExecutionSnapshot execution = combat.GetActiveExecution(actor.Id)!;
                combat.RetargetIntent(
                    intent.IntentId,
                    replacement.Id,
                    replacement.Position,
                    command.Tick,
                    "nearest_hostile_retargeted");
                combat.SetExecutionTarget(
                    execution.ExecutionId,
                    replacement.Id,
                    replacement.Position,
                    command.Tick,
                    "nearest_hostile_retargeted");
                combat.AdvanceExecutionStage(
                    execution.ExecutionId,
                    CombatExecutionStage.SelectEquipment,
                    command.Tick,
                    command.Tick,
                    "retargeted");
                SaveCombat(combat);
                return Report(
                    combat.GetActiveExecution(actor.Id)!,
                    false,
                    null,
                    "retargeted");
            }
        }

        return FinishForTargetLoss(command, combat, intent, finishReason);
    }

    private bool ShouldRetreat(AgentState actor, AgentState target, long tick)
    {
        int ownStrength = Math.Max(
            1,
            actor.CreateSnapshot(tick).Needs.Health.Points);
        int targetStrength = Math.Max(
            1,
            target.CreateSnapshot(tick).Needs.Health.Points);
        CombatTacticalDecision decision = CombatTacticalEvaluator.Evaluate(
            _policy.TacticalPolicy,
            ownStrength,
            ownStrength,
            targetStrength,
            CombatSpatialMath.Distance3D(actor.Position, target.Position),
            int.MaxValue);
        return decision.Intent == CombatIntentKind.Retreat;
    }

    private bool IsValidHostile(AgentState actor, AgentState? target)
    {
        if (target is null || !target.IsAlive || target.Id == actor.Id)
        {
            return false;
        }

        FactionState factions = _factions.Get();
        FactionId? actorFaction = factions.GetMemberFaction(actor.Id);
        FactionId? targetFaction = factions.GetMemberFaction(target.Id);
        return actorFaction.HasValue
            && targetFaction.HasValue
            && factions.AreHostile(actorFaction.Value, targetFaction.Value);
    }

    private AgentState? FindNearestThreat(AgentState actor, EntityId? excluded)
    {
        FactionState factions = _factions.Get();
        FactionId? faction = factions.GetMemberFaction(actor.Id);
        if (!faction.HasValue)
        {
            return null;
        }

        return _agents.GetAll()
            .Where(candidate => candidate.Id != actor.Id
                && candidate.IsAlive
                && (!excluded.HasValue || candidate.Id != excluded.Value))
            .Where(candidate =>
            {
                FactionId? other = factions.GetMemberFaction(candidate.Id);
                return other.HasValue
                    && factions.AreHostile(faction.Value, other.Value);
            })
            .Where(candidate => IsVisible(actor.Position, candidate.Position))
            .OrderBy(candidate => CombatSpatialMath.Distance3D(
                actor.Position,
                candidate.Position))
            .ThenBy(candidate => candidate.Id.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private bool IsVisible(CellId source, CellId target) =>
        CombatSpatialMath.Distance3D(source, target) <= _policy.SightRange
        && CombatLineOfSightResolver.HasLineOfSight(
            source,
            target,
            cell => !_volume.IsOpen(cell));

    private Result<CombatSpatialExecutionReport> FinishForTargetLoss(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        CombatIntentSnapshot intent,
        string reason)
    {
        CombatExecutionSnapshot execution = combat.GetActiveExecution(command.ActorId)!;
        combat.CompleteExecution(execution.ExecutionId, command.Tick, reason);
        combat.CompleteIntent(intent.IntentId, command.Tick);
        SaveCombat(combat);
        return Report(
            combat.GetExecution(execution.ExecutionId)!,
            false,
            null,
            reason);
    }
}
}
