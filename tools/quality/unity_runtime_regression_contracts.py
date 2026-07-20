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
        (loop, "selection and direct ownership across simulation ticks", (
            "IReadOnlyList<string> selectedAgentIds", "primarySelectedAgentId",
            "EnforceDirectMovementOwnership(nextTick)",
            "AgentRenderer.Render(agents, movementDuration)", "RestoreSelection(")),
        (movement, "validate-before-interrupt direct movement", (
            "ValidateResidentThroughTunnel", "ValidateResidentsThroughTunnel",
            "TerrainSession.InterruptForDirectMovement", "MoveResidentThroughTunnel",
            "MoveResidentsThroughTunnel")),
        (direct, "persistent direct movement ownership", (
            "InterruptForDirectMovement", "EnforceDirectMovementOwnership",
            "ReleaseJobAssignmentCommand", "RemoveAllRoutePlans",
            "_directMovementAgents.Add", "IsAvailableForAutomaticWork")),
        (designations, "Dig candidate direct-order suppression", (
            "IsAvailableForAutomaticWork(agent)", "agent.CellZ == 0")),
        (pointer_hits, "ordered pointer hit stack", (
            "Physics.RaycastAll", "Array.Sort(hits, ComparePointerHits)",
            "left.distance.CompareTo(right.distance)")),
        (targets, "excavation and movement target priority", (
            "GetPointerHits()", "ResolveExcavationTarget", "TryResolveTunnelDestination",
            "DigSelectedResidentTarget.Excavation", "DigSelectedResidentTarget.Movement")),
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
    return errors
