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
    errors.extend(require_fragments(
        renderer,
        texts.get(renderer, ""),
        "resident selection snapshot",
        (
            "SelectedAgentIds => _selectedSnapshot",
            "visual.SetSelected(_selectedIds.Contains(model.Id))",
            "agentVisual.SetSelected(_selectedIds.Contains(model.Id))",
        ),
    ))
    errors.extend(require_fragments(
        selection,
        texts.get(selection, ""),
        "ordered group selection restoration",
        (
            "RestoreSelection",
            "_selectedIds.Clear()",
            "_selectionOrder.Clear()",
            "PublishSelectionSnapshot();",
        ),
    ))
    errors.extend(require_fragments(
        loop,
        texts.get(loop, ""),
        "selection and direct ownership across simulation ticks",
        (
            "IReadOnlyList<string> selectedAgentIds",
            "primarySelectedAgentId",
            "EnforceDirectMovementOwnership(nextTick)",
            "AgentRenderer.Render(agents, movementDuration)",
            "RestoreSelection(",
        ),
    ))
    errors.extend(require_fragments(
        movement,
        texts.get(movement, ""),
        "validate-before-interrupt direct movement",
        (
            "ValidateResidentThroughTunnel",
            "ValidateResidentsThroughTunnel",
            "TerrainSession.InterruptForDirectMovement",
            "MoveResidentThroughTunnel",
            "MoveResidentsThroughTunnel",
        ),
    ))
    errors.extend(require_fragments(
        direct,
        texts.get(direct, ""),
        "persistent direct movement ownership",
        (
            "InterruptForDirectMovement",
            "EnforceDirectMovementOwnership",
            "ReleaseJobAssignmentCommand",
            "RemoveAllRoutePlans",
            "_directMovementAgents.Add",
            "IsAvailableForAutomaticWork",
        ),
    ))
    errors.extend(require_fragments(
        designations,
        texts.get(designations, ""),
        "Dig candidate direct-order suppression",
        (
            "IsAvailableForAutomaticWork(agent)",
            "agent.CellZ == 0",
        ),
    ))
    errors.extend(require_fragments(
        pointer_hits,
        texts.get(pointer_hits, ""),
        "ordered pointer hit stack",
        (
            "Physics.RaycastAll",
            "Array.Sort(hits, ComparePointerHits)",
            "left.distance.CompareTo(right.distance)",
        ),
    ))
    errors.extend(require_fragments(
        targets,
        texts.get(targets, ""),
        "excavation and movement target priority",
        (
            "GetPointerHits()",
            "ResolveExcavationTarget",
            "TryResolveTunnelDestination",
            "DigSelectedResidentTarget.Excavation",
            "DigSelectedResidentTarget.Movement",
        ),
    ))
    errors.extend(require_fragments(
        movement_input,
        texts.get(movement_input, ""),
        "all tunnel destination surfaces",
        (
            "_tunnelRenderer.TryGetCell",
            "_caveRoomFloorRenderer.TryGetCell",
            "_renderer.TryGetWalkSurface",
            "MoveResidentsThroughTunnel",
        ),
    ))
    errors.extend(require_fragments(
        multi_worker,
        texts.get(multi_worker, ""),
        "parallel connected excavation groups",
        (
            "AssignExcavationClusterToResidents",
            "workerCount",
            "List<EntityId>[] buckets",
            "AssignNextManualExcavation",
            "RollbackManualGroups",
        ),
    ))
    return errors
