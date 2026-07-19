from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_navigation_and_marquee_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
) -> list[str]:
    interaction = runtime_root / "DigWorldInteraction.cs"
    marquee = runtime_root / "DigWorldInteraction.MarqueeSelection.cs"
    marquee_renderer = runtime_root / "DigSelectionMarqueeRenderer.cs"
    excavation = runtime_root / "DigWorldInteraction.Excavation.cs"
    world_session = runtime_root / "DigWorldSession.TunnelNavigation.cs"
    agent_session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    navigation_sync = runtime_root / "DigAgentSimulationDriverBase.NavigationSync.cs"
    movement = runtime_root / "DigAgentSimulationDriverBase.TunnelMovement.cs"
    movement_input = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    pointer_hits = runtime_root / "DigWorldInteraction.PointerHits.cs"
    selected_targets = runtime_root / "DigWorldInteraction.SelectedResidentTargets.cs"
    room_floor = runtime_root / "DigCaveRoomFloorRenderer.cs"
    agent_renderer = runtime_root / "DigAgentRenderer.cs"
    selection_persistence = runtime_root / "DigAgentRenderer.SelectionPersistence.cs"
    simulation_loop = runtime_root / "DigAgentSimulationDriverBase.Loop.cs"
    direct_control = runtime_root / "DigTerrainWorkDirectMovement.cs"
    designations = runtime_root / "DigTerrainWorkDesignations.cs"
    manual_excavation = runtime_root / "DigTerrainWorkManualExcavation.cs"
    multi_worker = runtime_root / "DigTerrainWorkManualExcavation.MultiWorker.cs"
    errors: list[str] = []
    errors.extend(require_fragments(
        interaction,
        texts.get(interaction, ""),
        "marquee input wiring",
        (
            "InitializeResidentMarquee();",
            "TryHandleResidentMarqueeSelection()",
            "CancelResidentMarquee();",
        ),
    ))
    errors.extend(require_fragments(
        marquee,
        texts.get(marquee, ""),
        "resident marquee selection",
        (
            "MarqueeThresholdPixels",
            "GetComponentsInChildren<DigAgentVisual>",
            "WorldToScreenPoint",
            "CreateScreenRect",
            "IsMarqueeBlockingTarget",
            "SelectResidentsInsideMarquee",
            "TryApplyTunnelMove(_marqueeStartHit, leftButton: true)",
        ),
    ))
    errors.extend(require_fragments(
        marquee_renderer,
        texts.get(marquee_renderer, ""),
        "marquee rectangle rendering",
        (
            "private void OnGUI()",
            "GUI.DrawTexture",
            "Texture2D.whiteTexture",
            "Screen.height",
        ),
    ))
    errors.extend(require_fragments(
        excavation,
        texts.get(excavation, ""),
        "vertical tunnel planning",
        (
            "ExcavationStrokeAxis.Vertical",
            "SetTunnelPlan",
            "_excavationAnchor.Value",
        ),
    ))
    errors.extend(require_fragments(
        world_session,
        texts.get(world_session, ""),
        "tunnel plan ownership",
        (
            "_plannedTunnelCells",
            "_plannedVerticalTunnelCells",
            "PlannedTunnelCells",
            "PlannedVerticalTunnelCells",
            "IsPlannedHorizontalTunnel",
            "SetTunnelPlan",
        ),
    ))
    errors.extend(require_fragments(
        agent_session,
        texts.get(agent_session, ""),
        "front navigation synchronization",
        (
            "SynchronizeFrontNavigation",
            "WithSynchronizedFrontLayer",
            "CreateTunnelMovementHandlers();",
        ),
    ))
    errors.extend(require_fragments(
        navigation_sync,
        texts.get(navigation_sync, ""),
        "authoritative world navigation refresh",
        (
            "SynchronizeExcavatedTunnelNavigation",
            "WorldSession.LoadSnapshot()",
            "WorldSession.PlannedVerticalTunnelCells",
        ),
    ))
    errors.extend(require_fragments(
        movement,
        texts.get(movement, ""),
        "validated single and group direct movement",
        (
            "SynchronizeExcavatedTunnelNavigation();",
            "ValidateResidentThroughTunnel",
            "ValidateResidentsThroughTunnel",
            "MoveResidentThroughTunnel",
            "MoveResidentsThroughTunnel",
            "TerrainSession.InterruptForDirectMovement",
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
        selected_targets,
        texts.get(selected_targets, ""),
        "selected resident target priority",
        (
            "ResolveSelectedResidentTarget",
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
        "all walkable destination renderers",
        (
            "TryResolveTunnelDestination",
            "_tunnelRenderer.TryGetCell",
            "_caveRoomFloorRenderer.TryGetCell",
            "_renderer.TryGetWalkSurface",
            "MoveResidentsThroughTunnel",
        ),
    ))
    errors.extend(require_fragments(
        room_floor,
        texts.get(room_floor, ""),
        "completed room destination lookup",
        (
            "internal bool TryGetCell",
            "GetComponent<DigTunnelCellVisual>()",
            "_cells.Contains(cell.Cell)",
        ),
    ))
    errors.extend(require_fragments(
        agent_renderer,
        texts.get(agent_renderer, ""),
        "selection material persistence",
        (
            "visual.SetSelected(_selectedIds.Contains(model.Id))",
            "agentVisual.SetSelected(_selectedIds.Contains(model.Id))",
            "SelectedAgentIds => _selectedSnapshot",
        ),
    ))
    errors.extend(require_fragments(
        selection_persistence,
        texts.get(selection_persistence, ""),
        "ordered group selection restoration",
        (
            "RestoreSelection",
            "_selectedIds.Clear()",
            "_selectionOrder.Clear()",
            "PublishSelectionSnapshot();",
        ),
    ))
    errors.extend(require_fragments(
        simulation_loop,
        texts.get(simulation_loop, ""),
        "selection persistence across simulation ticks",
        (
            "IReadOnlyList<string> selectedAgentIds",
            "primarySelectedAgentId",
            "AgentRenderer.Render(agents, movementDuration)",
            "RestoreSelection(",
        ),
    ))
    errors.extend(require_fragments(
        direct_control,
        texts.get(direct_control, ""),
        "direct movement job ownership",
        (
            "InterruptForDirectMovement",
            "EnforceDirectMovementOwnership",
            "ReleaseJobAssignmentCommand",
            "RemoveAllRoutePlans",
            "_directMovementAgents.Add",
            "ClearManualGroupForAgent",
            "IsAvailableForAutomaticWork",
        ),
    ))
    errors.extend(require_fragments(
        designations,
        texts.get(designations, ""),
        "directly controlled candidate suppression",
        (
            "IsAvailableForAutomaticWork(agent)",
            "CreateDynamicCandidates",
        ),
    ))
    errors.extend(require_fragments(
        manual_excavation,
        texts.get(manual_excavation, ""),
        "explicit return to work ownership",
        ("ReleaseDirectMovementControl(residentId);",),
    ))
    errors.extend(require_fragments(
        multi_worker,
        texts.get(multi_worker, ""),
        "parallel connected excavation assignment",
        (
            "AssignExcavationClusterToResidents",
            "workerCount",
            "List<EntityId>[] buckets",
            "AssignNextManualExcavation",
            "RollbackManualGroups",
        ),
    ))
    return errors
