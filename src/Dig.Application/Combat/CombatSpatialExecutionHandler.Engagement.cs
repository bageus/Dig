using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Combat;
using Dig.Domain.Core;
using Dig.Domain.Factions;
using Dig.Domain.Inventory;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Combat
{

public sealed partial class CombatSpatialExecutionHandler
{
    private const double CombatRunCellsPerTick = 1.25d;
    private const double CombatClimbCellsPerTick = 0.5d;

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
            && IsAtCombatSurfacePose(actor, target, weapon, selected.Value.Cell)
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

        int budget = int.MaxValue;
        int consumed = 0;
        bool movedAny = false;
        while (true)
        {
            actor = _agents.Get(actor.Id)!;
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
                return CompleteApproach(command, combat, execution, actor, movedAny);

            CellId nextCell = path.Path.Cells[1];
            budget = Math.Min(budget,
                ResolveCombatMovementBudget(command.Tick, actor.Position, nextCell));
            if (consumed >= budget)
                return Report(combat.GetActiveExecution(actor.Id)!, movedAny, null,
                    movedAny ? "approach_advanced" : "approach_waiting_cadence");

            Result moved = _moveHandler.Handle(new MoveAgentCommand(
                actor.Id, nextCell, command.Tick));
            if (moved.IsFailure)
                return Block(command, combat, execution, moved.Error!);
            consumed = checked(consumed + 1);
            movedAny = true;
        }
    }

    private Result<CombatSpatialExecutionReport> CompleteApproach(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        CombatExecutionSnapshot execution,
        AgentState actor,
        bool moved)
    {
        Result<CombatSpatialExecutionReport>? surfaceApproach =
            CompleteCombatSurfaceApproach(command, combat, execution, actor, moved);
        if (surfaceApproach != null)
            return surfaceApproach;

        CombatExecutionStage arrived = IsPursuingLastKnown(execution, actor)
            ? CombatExecutionStage.Reevaluate : CombatExecutionStage.FaceTarget;
        combat.AdvanceExecutionStage(execution.ExecutionId, arrived,
            command.Tick, command.Tick,
            arrived == CombatExecutionStage.Reevaluate
                ? "last_known_target_cell_reached" : "engagement_reached");
        SaveCombat(combat);
        return Report(combat.GetActiveExecution(actor.Id)!, moved, null,
            arrived == CombatExecutionStage.Reevaluate
                ? "last_known_target_cell_reached" : "engagement_reached");
    }

    private Result<CombatSpatialExecutionReport> Retreat(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        AgentState actor)
    {
        CombatExecutionSnapshot execution = combat.GetActiveExecution(actor.Id)!;
        int budget = int.MaxValue;
        int consumed = 0;
        bool movedAny = false;
        while (true)
        {
            actor = _agents.Get(actor.Id)!;
            CombatRetreatCandidate? selected = SelectRetreat(actor);
            if (!selected.HasValue)
                return Block(command, combat, execution,
                    CombatSpatialApplicationErrors.RetreatUnavailable);

            TunnelPathResult route = _volume.FindPath(actor.Position, selected.Value.Cell);
            if (!route.Succeeded || route.Path == null)
                return Block(command, combat, execution,
                    CombatSpatialApplicationErrors.RetreatUnavailable);
            if (route.Path.Cells.Count == 1)
                return CompleteRetreat(command, combat, execution, movedAny);

            CellId nextCell = route.Path.Cells[1];
            budget = Math.Min(budget,
                ResolveCombatMovementBudget(command.Tick, actor.Position, nextCell));
            if (consumed >= budget)
                return Report(execution, movedAny, null,
                    movedAny ? "retreat_advanced" : "retreat_waiting_cadence");

            Result moved = _moveHandler.Handle(new MoveAgentCommand(
                actor.Id, nextCell, command.Tick));
            if (moved.IsFailure)
                return Block(command, combat, execution, moved.Error!);
            consumed = checked(consumed + 1);
            movedAny = true;
        }
    }

    private CombatRetreatCandidate? SelectRetreat(AgentState actor)
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
        return CombatRetreatResolver.Select(current, candidates);
    }

    private Result<CombatSpatialExecutionReport> CompleteRetreat(
        AdvanceCombatSpatialExecutionCommand command,
        CombatState combat,
        CombatExecutionSnapshot execution,
        bool moved)
    {
        combat.CompleteExecution(execution.ExecutionId,
            command.Tick, "retreat_reached");
        CombatIntentSnapshot? intent = combat.GetActiveIntent(execution.ActorId);
        if (intent is not null) combat.CompleteIntent(intent.IntentId, command.Tick);
        SaveCombat(combat);
        return Report(combat.GetExecution(execution.ExecutionId)!, moved, null,
            "retreat_reached");
    }

    private int ResolveCombatMovementBudget(long tick, CellId from, CellId to)
    {
        TunnelTraversalKind traversal = _volume.ClassifyTraversal(from, to);
        double speed = traversal == TunnelTraversalKind.VerticalClimb
            || traversal == TunnelTraversalKind.ShaftGapTraverse
            ? CombatClimbCellsPerTick
            : CombatRunCellsPerTick;
        return ResidentInventoryMovementCadence.ResolveStepCount(tick, speed);
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
