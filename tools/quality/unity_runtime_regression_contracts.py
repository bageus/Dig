from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_runtime_regression_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
) -> list[str]:
    renderer = runtime_root / "DigAgentRenderer.cs"
    selection = runtime_root / "DigAgentRenderer.SelectionPersistence.cs"
    agent_session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    loop = runtime_root / "DigAgentSimulationDriverBase.Loop.cs"
    movement = runtime_root / "DigAgentSimulationDriverBase.TunnelMovement.cs"
    direct = runtime_root / "DigTerrainWorkDirectMovement.cs"
    designations = runtime_root / "DigTerrainWorkDesignations.cs"
    world_input = runtime_root / "DigWorldInteraction.cs"
    priority_input = runtime_root / "DigWorldInteraction.ResidentCommandPriority.cs"
    excavation = runtime_root / "DigWorldInteraction.Excavation.cs"
    depth = runtime_root / "DigWorldInteraction.TunnelDepthExcavation.cs"
    pointer_hits = runtime_root / "DigWorldInteraction.PointerHits.cs"
    targets = runtime_root / "DigWorldInteraction.SelectedResidentTargets.cs"
    movement_input = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    marquee = runtime_root / "DigWorldInteraction.MarqueeSelection.cs"
    navigation_sync = runtime_root / "DigAgentSimulationDriverBase.NavigationSync.cs"
    tunnel_renderer = runtime_root / "DigTunnelDemoRenderer.cs"
    job_visual = runtime_root / "DigJobVisual.cs"
    multi_worker = runtime_root / "DigTerrainWorkManualExcavation.MultiWorker.cs"

    errors: list[str] = []
    renderer_text = "\n".join(
        text for path, text in texts.items()
        if path.name.startswith("DigAgentRenderer") and path.suffix == ".cs"
    )
    errors.extend(require_fragments(
        renderer,
        renderer_text,
        "resident selection snapshot",
        (
            "SelectedAgentIds => _selectedSnapshot",
            "visual.SetSelected(_selectedIds.Contains(model.Id))",
            "agentVisual.SetSelected(_selectedIds.Contains(model.Id))",
        ),
    ))
    checks = (
        (selection, "ordered group selection restoration", (
            "RestoreSelection", "_selectedIds.Clear()", "_selectionOrder.Clear()",
            "PublishSelectionSnapshot();")),
        (agent_session, "single manual movement order state", (
            "Dictionary<EntityId, ManualTunnelMovementOrder>",
            "ActiveManualTunnelResidentIds", "TryAdvanceManualTunnelMovement(",
            "CanTraverseStep(", "agent.MoveTo(next, _tick)",
            "ConsumeManualTunnelMovementWarning")),
        (loop, "selection and manual ownership across simulation ticks", (
            "IReadOnlyList<string> selectedAgentIds", "primarySelectedAgentId",
            "AgentSession.ActiveManualTunnelResidentIds",
            "InterruptForManualMovement(",
            "AgentRenderer.Render(agents, movementDuration)", "RestoreSelection(")),
        (movement, "interrupt-before-plan manual movement", (
            "SynchronizeTunnelInteractionTargets(tunnelRenderer)",
            "TerrainSession.InterruptForManualMovement",
            "PlanAgentTunnelRouteReport", "PlanAgentsTunnelRoutesReport",
            "MoveResidentThroughTunnel", "MoveResidentsThroughTunnel")),
        (direct, "stateless work interruption", (
            "BindManualMovementSource", "InterruptForManualMovement",
            "ReleaseJobAssignmentCommand", "RemoveAllRoutePlans",
            "IsAvailableForAutomaticWork")),
        (designations, "Dig candidate manual-order suppression", (
            "IsAvailableForAutomaticWork(agent)", "agent.CellZ == 0")),
        (world_input, "resident commands before modal work input", (
            "TryHandlePriorityResidentPointerInput()",
            "TryHandleResidentMarqueeSelection()",
            "TryHandleTunnelDepthExcavation()",
            "TryHandleExcavationStroke()")),
        (priority_input, "resident selection and movement priority", (
            "Input.GetMouseButtonDown(0)",
            "TryResolveAgentHit(hits, out DigAgentVisual agent)",
            "ToggleResidentSelection(agent)",
            "ContextWorldTargetKind.Resident",
            "TryApplyTunnelMove(hits, leftButton: true)",
            "DisableExcavationDrawing();",
            "DisableCaveRoomPlanning();")),
        (excavation, "excavation tool pointer fallthrough", (
            "if (!CanActivateExcavationDrawing)",
            "ResetExcavationStroke();",
            "return false;",
            "!IsTerminalJobStatus(job.Model.Status)")),
        (depth, "depth tool pointer fallthrough", (
            "if (!CanActivateExcavationDrawing)",
            "return false;")),
        (pointer_hits, "resident selection through tunnel targets", (
            "Physics.RaycastAll", "Array.Sort(hits, ComparePointerHits)",
            "TryProjectResidentBounds", "blockedByForegroundObject",
            "SynchronizeTunnelInteractionTargets();")),
        (targets, "screen-disambiguated movement and excavation priority", (
            "ResolveSelectedResidentTarget(GetPointerHits())",
            "ResolveMovementPointerDistance", "return bestMovement",
            "DigSelectedResidentTarget.Excavation")),
        (movement_input, "explicit LMB tunnel destinations and active job markers", (
            "TryApplyTunnelMove(RaycastHit[] hits",
            "TryAssignExplicitSpatialExcavation(hits, residentIds)",
            "IsTerminalJobStatus(job.Model.Status)",
            "job.Model.TargetZ.Value <= 0",
            "TryResolveTunnelDestination(hit, out _, out _)",
            "ResolveSelectedResidentTarget(hits)",
            "_tunnelRenderer.TryGetCell", "_caveRoomFloorRenderer.TryGetCell",
            "MoveResidentsThroughTunnel")),
        (marquee, "selected movement bypasses marquee", (
            "TryResolveSelectedResidentMovementTarget(hits, out _)",
            "_marqueeStartHits",
            "TryApplyTunnelMove(_marqueeStartHits, leftButton: true)")),
        (navigation_sync, "new excavation collider synchronization", (
            "SynchronizeTunnelInteractionTargets(",
            "tunnelRenderer.Initialize(AgentSession.TunnelVolume)")),
        (tunnel_renderer, "incremental tunnel hit targets", (
            "_cells.Count == volume.Cells.Count",
            "GetComponentInParent<DigTunnelCellVisual>()")),
        (job_visual, "terminal jobs are presentation-only", (
            "_interactionCollider = GetComponent<Collider>();",
            "_interactionCollider.enabled = !IsTerminalStatus(model.Status)",
            "string.Equals(status, \"Completed\", StringComparison.Ordinal)",
            "string.Equals(status, \"Cancelled\", StringComparison.Ordinal)",
            "string.Equals(status, \"Failed\", StringComparison.Ordinal)")),
        (multi_worker, "parallel connected excavation groups", (
            "AssignExcavationClusterToResidents", "workerCount",
            "List<EntityId>[] buckets", "AssignNextManualExcavation",
            "RollbackManualGroups")),
    )
    for path, name, fragments in checks:
        errors.extend(require_fragments(path, texts.get(path, ""), name, fragments))

    forbidden_by_path = {
        renderer: ("AnimateRoute(",),
        agent_session: (
            "_manualTunnelOrders",
            "MoveAgentThroughTunnelCommandHandler",
            "MoveAgentsThroughTunnelCommandHandler",
        ),
        movement: (
            "ValidateResidentThroughTunnel",
            "ValidateResidentsThroughTunnel",
            "AnimateRoute(",
        ),
        direct: (
            "_directMovementAgents",
            "EnforceDirectMovementOwnership",
            "ReleaseDirectMovementControl",
            "InterruptForDirectMovement",
        ),
        pointer_hits: ("firstWalkableDistance",),
    }
    for path, fragments in forbidden_by_path.items():
        text = texts.get(path, "") if path != renderer else renderer_text
        for fragment in fragments:
            if fragment in text:
                errors.append(f"{path}: obsolete movement fragment remains: {fragment!r}")

    world_input_text = texts.get(world_input, "")
    priority_index = world_input_text.find("TryHandlePriorityResidentPointerInput()")
    modal_index = world_input_text.find("TryHandleResidentMarqueeSelection()")
    if priority_index < 0 or modal_index < 0 or priority_index >= modal_index:
        errors.append(f"{world_input}: resident commands must run before modal work tools")

    priority_text = texts.get(priority_input, "")
    resident_index = priority_text.find("TryResolveAgentHit(hits")
    movement_priority_index = priority_text.find("TryApplyTunnelMove(hits, leftButton: true)")
    if resident_index < 0 or movement_priority_index < 0 or resident_index >= movement_priority_index:
        errors.append(f"{priority_input}: resident selection must win over movement and work")

    target_text = texts.get(targets, "")
    movement_index = target_text.find("return bestMovement")
    excavation_index = target_text.find("DigSelectedResidentTarget.Excavation")
    if movement_index < 0 or excavation_index < 0 or movement_index >= excavation_index:
        errors.append(f"{targets}: open movement must resolve before excavation")

    movement_input_text = texts.get(movement_input, "")
    explicit_index = movement_input_text.find(
        "TryAssignExplicitSpatialExcavation(hits, residentIds)"
    )
    target_index = movement_input_text.find("ResolveSelectedResidentTarget(hits)")
    if explicit_index < 0 or target_index < 0 or explicit_index >= target_index:
        errors.append(
            f"{movement_input}: explicit job markers must be checked before ordinary movement"
        )
    return errors
