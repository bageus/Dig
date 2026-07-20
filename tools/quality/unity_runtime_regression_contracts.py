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
    pointer_hits = runtime_root / "DigWorldInteraction.PointerHits.cs"
    targets = runtime_root / "DigWorldInteraction.SelectedResidentTargets.cs"
    movement_input = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
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
            "TerrainSession.InterruptForManualMovement",
            "PlanAgentTunnelRouteReport", "PlanAgentsTunnelRoutesReport",
            "MoveResidentThroughTunnel", "MoveResidentsThroughTunnel")),
        (direct, "stateless work interruption", (
            "BindManualMovementSource", "InterruptForManualMovement",
            "ReleaseJobAssignmentCommand", "RemoveAllRoutePlans",
            "IsAvailableForAutomaticWork")),
        (designations, "Dig candidate manual-order suppression", (
            "IsAvailableForAutomaticWork(agent)", "agent.CellZ == 0")),
        (pointer_hits, "ordered pointer hit stack", (
            "Physics.RaycastAll", "Array.Sort(hits, ComparePointerHits)",
            "left.distance.CompareTo(right.distance)")),
        (targets, "movement and excavation target priority", (
            "GetPointerHits()", "ResolveExcavationTarget", "TryResolveTunnelDestination",
            "return visibleMovement", "DigSelectedResidentTarget.Excavation")),
        (movement_input, "explicit tunnel destination surfaces", (
            "_tunnelRenderer.TryGetCell", "_caveRoomFloorRenderer.TryGetCell",
            "MoveResidentsThroughTunnel")),
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
    }
    for path, fragments in forbidden_by_path.items():
        text = texts.get(path, "") if path != renderer else renderer_text
        for fragment in fragments:
            if fragment in text:
                errors.append(f"{path}: obsolete movement fragment remains: {fragment!r}")

    target_text = texts.get(targets, "")
    movement_index = target_text.find("return visibleMovement")
    excavation_index = target_text.find("DigSelectedResidentTarget.Excavation")
    if movement_index < 0 or excavation_index < 0 or movement_index >= excavation_index:
        errors.append(f"{targets}: open movement must resolve before excavation")

    return errors
