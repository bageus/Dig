using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Combat
{

public sealed partial class CombatSpatialExecutionHandler
{
    private Result<CombatSpatialExecutionReport> SelectEquipment(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        AgentState actor)
    {
        CombatExecutionSnapshot execution = combat.GetActiveExecution(actor.Id)!;
        if (!execution.TargetEntityId.HasValue)
            return Result<CombatSpatialExecutionReport>.Failure(
                CombatSpatialApplicationErrors.TargetUnavailable);

        Result<CombatEquipmentSelection> selection = _equipment.Select(
            actor.Id, execution.TargetEntityId.Value);
        if (selection.IsFailure)
            return Block(command, combat, execution,
                selection.Error ?? CombatSpatialApplicationErrors.EquipmentUnavailable);

        combat.SetExecutionEquipment(execution.ExecutionId,
            selection.Value.WeaponProfileId, command.Tick, "equipment_selected");
        combat.AdvanceExecutionStage(execution.ExecutionId,
            CombatExecutionStage.SelectEngagementCell,
            command.Tick, command.Tick, "equipment_selected");
        SaveCombat(combat);
        return Report(combat.GetActiveExecution(actor.Id)!, false, null,
            "equipment_selected");
    }

    private Result<CombatSpatialExecutionReport> SelectEngagement(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        AgentState actor)
    {
        CombatExecutionSnapshot execution = combat.GetActiveExecution(actor.Id)!;
        AgentState? target = execution.TargetEntityId.HasValue
            ? _agents.Get(execution.TargetEntityId.Value) : null;
        if (!IsValidHostile(actor, target))
        {
            combat.AdvanceExecutionStage(execution.ExecutionId,
                CombatExecutionStage.AcquireTarget,
                command.Tick, command.Tick, "target_requires_reacquire");
            SaveCombat(combat);
            return Report(combat.GetActiveExecution(actor.Id)!, false, null,
                "target_requires_reacquire");
        }

        CombatIntentSnapshot intent = combat.GetActiveIntent(actor.Id)!;
        if (!IsVisible(actor.Position, target!.Position) && !intent.IsPersistent)
            return PursueLastKnownOrFinish(command, combat, intent, actor, target);

        if (!execution.WeaponProfileId.HasValue)
            return Result<CombatSpatialExecutionReport>.Failure(
                CombatSpatialApplicationErrors.EquipmentUnavailable);

        WeaponProfile weapon = combat.Weapons.Get(execution.WeaponProfileId.Value);
        CombatEngagementCandidate? selected = CombatEngagementResolver.Select(
            weapon, BuildEngagementCandidates(actor, target, combat));
        if (!selected.HasValue)
            return Block(command, combat, execution,
                CombatSpatialApplicationErrors.EngagementUnavailable);

        combat.SetExecutionTarget(execution.ExecutionId, target.Id, target.Position,
            command.Tick, "target_position_updated");
        combat.SetExecutionEngagement(execution.ExecutionId, selected.Value.Cell,
            command.Tick, "engagement_selected");
        CombatExecutionStage next = actor.Position == selected.Value.Cell
            ? CombatExecutionStage.FaceTarget : CombatExecutionStage.Approach;
        combat.AdvanceExecutionStage(execution.ExecutionId, next,
            command.Tick, command.Tick,
            next == CombatExecutionStage.FaceTarget
                ? "engagement_reached" : "approach_required");
        SaveCombat(combat);
        return Report(combat.GetActiveExecution(actor.Id)!, false, null,
            "engagement_selected");
    }

    private List<CombatEngagementCandidate> BuildEngagementCandidates(
        AgentState actor, AgentState target, CombatState combat)
    {
        List<CombatEngagementCandidate> values =
            new List<CombatEngagementCandidate>();
        foreach (CellId cell in _volume.SupportedCells)
        {
            TunnelPathResult path = _volume.FindPath(actor.Position, cell);
            int distance = CombatSpatialMath.Distance3D(cell, target.Position);
            bool edge = _volume.ClassifyTraversal(cell, target.Position)
                != TunnelTraversalKind.Invalid;
            bool line = CombatLineOfSightResolver.HasLineOfSight(
                cell, target.Position, candidate => !_volume.IsOpen(candidate));
            values.Add(new CombatEngagementCandidate(
                cell, distance,
                path.Succeeded ? path.Path!.Cells.Count - 1 : int.MaxValue,
                combat.GetSoftClaimCount(cell, actor.Id), path.Succeeded,
                _volume.HasFullActorSupport(cell), edge, line));
        }
        return values;
    }

    private Result<CombatSpatialExecutionReport> Approach(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        AgentState actor)
    {
        CombatExecutionSnapshot execution = combat.GetActiveExecution(actor.Id)!;
        if (!execution.EngagementCell.HasValue)
            return Block(command, combat, execution,
                CombatSpatialApplicationErrors.EngagementUnavailable);

        if (IsPursuingLastKnown(execution, actor)
            && execution.TargetEntityId.HasValue)
        {
            AgentState? target = _agents.Get(execution.TargetEntityId.Value);
            if (target is not null && IsVisible(actor.Position, target.Position))
                return Advance(combat, execution, CombatExecutionStage.Reevaluate,
                    command.Tick, command.Tick, "target_sight_restored");
        }

        TunnelPathResult path = _volume.FindPath(
            actor.Position, execution.EngagementCell.Value);
        if (!path.Succeeded || path.Path!.Cells.Count < 1)
            return Block(command, combat, execution,
                CombatSpatialApplicationErrors.EngagementUnavailable);

        if (path.Path.Cells.Count == 1)
        {
            CombatExecutionStage arrived = IsPursuingLastKnown(execution, actor)
                ? CombatExecutionStage.Reevaluate : CombatExecutionStage.FaceTarget;
            return Advance(combat, execution, arrived,
                command.Tick, command.Tick,
                arrived == CombatExecutionStage.Reevaluate
                    ? "last_known_target_cell_reached" : "engagement_reached");
        }

        Result moved = _moveHandler.Handle(new MoveAgentCommand(
            actor.Id, path.Path.Cells[1], command.Tick));
        if (moved.IsFailure) return Block(command, combat, execution, moved.Error!);

        AgentState movedActor = _agents.Get(actor.Id)!;
        CombatExecutionStage next = path.Path.Cells.Count == 2
            ? (IsPursuingLastKnown(execution, movedActor)
                ? CombatExecutionStage.Reevaluate : CombatExecutionStage.FaceTarget)
            : CombatExecutionStage.Approach;
        combat.AdvanceExecutionStage(execution.ExecutionId, next,
            command.Tick, command.Tick,
            next == CombatExecutionStage.Reevaluate
                ? "last_known_target_cell_reached"
                : next == CombatExecutionStage.FaceTarget
                    ? "engagement_reached" : "approach_advanced");
        SaveCombat(combat);
        return Report(combat.GetActiveExecution(actor.Id)!, true, null,
            "approach_advanced");
    }

    private Result<CombatSpatialExecutionReport> Retreat(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        AgentState actor)
    {
        IReadOnlyList<AgentState> threats = GetHostileAgents(actor);
        int current = MinimumThreatDistance(actor.Position, threats);
        List<CombatRetreatCandidate> candidates =
            new List<CombatRetreatCandidate>();
        FactionState factions = _factions.Get();
        FactionId? actorFaction = factions.GetMemberFaction(actor.Id);
        foreach (CellId cell in _volume.SupportedCells)
        {
            TunnelPathResult path = _volume.FindPath(actor.Position, cell);
            FactionId? owner = factions.GetTerritoryOwner(cell);
            candidates.Add(new CombatRetreatCandidate(
                cell, MinimumThreatDistance(cell, threats),
                path.Succeeded ? path.Path!.Cells.Count - 1 : int.MaxValue,
                path.Succeeded, _volume.HasFullActorSupport(cell),
                actorFaction.HasValue && owner == actorFaction));
        }

        CombatRetreatCandidate? selected = CombatRetreatResolver.Select(
            current, candidates);
        CombatExecutionSnapshot execution = combat.GetActiveExecution(actor.Id)!;
        if (!selected.HasValue)
            return Block(command, combat, execution,
                CombatSpatialApplicationErrors.RetreatUnavailable);

        TunnelPathResult route = _volume.FindPath(actor.Position, selected.Value.Cell);
        if (route.Path!.Cells.Count > 1)
        {
            Result moved = _moveHandler.Handle(new MoveAgentCommand(
                actor.Id, route.Path.Cells[1], command.Tick));
            if (moved.IsFailure) return Block(command, combat, execution, moved.Error!);
            if (route.Path.Cells.Count > 2)
                return Report(execution, true, null, "retreat_advanced");
        }

        combat.CompleteExecution(execution.ExecutionId,
            command.Tick, "retreat_reached");
        CombatIntentSnapshot? intent = combat.GetActiveIntent(actor.Id);
        if (intent is not null) combat.CompleteIntent(intent.IntentId, command.Tick);
        SaveCombat(combat);
        return Report(combat.GetExecution(execution.ExecutionId)!, true, null,
            "retreat_reached");
    }

    private bool IsPursuingLastKnown(
        CombatExecutionSnapshot execution, AgentState actor)
    {
        if (!execution.EngagementCell.HasValue
            || !execution.LastKnownTargetCell.HasValue
            || execution.EngagementCell.Value != execution.LastKnownTargetCell.Value
            || !execution.TargetEntityId.HasValue)
            return false;
        AgentState? target = _agents.Get(execution.TargetEntityId.Value);
        return target == null || !IsVisible(actor.Position, target.Position);
    }

    private IReadOnlyList<AgentState> GetHostileAgents(AgentState actor) =>
        _agents.GetAll().Where(candidate => IsValidHostile(actor, candidate)).ToArray();

    private static int MinimumThreatDistance(
        CellId cell, IReadOnlyList<AgentState> threats) =>
        threats.Count == 0 ? int.MaxValue : threats.Min(threat =>
            CombatSpatialMath.Distance3D(cell, threat.Position));
}
}
