from __future__ import annotations

from pathlib import Path
from typing import Callable

from unity_runtime_regression_contracts import check_runtime_regression_contracts

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]
RejectFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_navigation_and_marquee_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
) -> list[str]:
    interaction = runtime_root / "DigWorldInteraction.cs"
    marquee = runtime_root / "DigWorldInteraction.MarqueeSelection.cs"
    marquee_renderer = runtime_root / "DigSelectionMarqueeRenderer.cs"
    excavation = runtime_root / "DigWorldInteraction.Excavation.cs"
    depth_input = runtime_root / "DigWorldInteraction.TunnelDepthExcavation.cs"
    pointer_hits = runtime_root / "DigWorldInteraction.PointerHits.cs"
    selected_targets = runtime_root / "DigWorldInteraction.SelectedResidentTargets.cs"
    world_session = runtime_root / "DigWorldSession.TunnelNavigation.cs"
    agent_session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    navigation_sync = runtime_root / "DigAgentSimulationDriverBase.NavigationSync.cs"
    movement = runtime_root / "DigAgentSimulationDriverBase.TunnelMovement.cs"
    movement_input = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    tunnel_renderer = runtime_root / "DigTunnelDemoRenderer.cs"
    room_floor = runtime_root / "DigCaveRoomFloorRenderer.cs"
    canvas_hud = runtime_root / "DigWorldInteraction.CanvasHud.cs"
    camera = runtime_root / "DigCameraController.cs"
    agent_renderer = runtime_root / "DigAgentRenderer.cs"
    direct_control = runtime_root / "DigTerrainWorkDirectMovement.cs"
    designations = runtime_root / "DigTerrainWorkDesignations.cs"
    manual_excavation = runtime_root / "DigTerrainWorkManualExcavation.cs"
    errors: list[str] = []
    errors.extend(require_fragments(
        interaction,
        texts.get(interaction, ""),
        "resident-first direct movement input",
        (
            "InitializeResidentMarquee();",
            "TryHandleResidentMarqueeSelection()",
            "RaycastHit[] hits = GetPointerHits();",
            "TryResolveAgentHit(hits",
            "TryApplyTunnelMove(hit, left)",
            "_agentRenderer.SelectedCount == 0",
        ),
    ))
    interaction_text = texts.get(interaction, "")
    if "TryAssignSelectedResidentToExcavation(hit, left)" in interaction_text:
        errors.append(f"{interaction}: ordinary selected-resident LMB must not assign rock excavation")
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
        ("private void OnGUI()", "GUI.DrawTexture", "Texture2D.whiteTexture", "Screen.height"),
    ))
    errors.extend(require_fragments(
        pointer_hits,
        texts.get(pointer_hits, ""),
        "ordered collider and screen-space resident picking",
        (
            "Physics.RaycastAll",
            "GetComponentInParent<DigAgentVisual>()",
            "TryResolveAgentNearPointer",
            "ResidentScreenPickRadius",
            "WorldToScreenPoint",
        ),
    ))
    errors.extend(require_fragments(
        selected_targets,
        texts.get(selected_targets, ""),
        "walkable movement before excavation markers",
        (
            "CellId? excavationCandidate",
            "TryResolveTunnelDestination(",
            "return DigSelectedResidentTarget.Movement",
            "DigSelectedResidentTarget.Excavation(excavationCandidate.Value)",
        ),
    ))
    target_text = texts.get(selected_targets, "")
    movement_index = target_text.find("return DigSelectedResidentTarget.Movement")
    excavation_index = target_text.find("DigSelectedResidentTarget.Excavation(excavationCandidate.Value)")
    if movement_index < 0 or excavation_index < 0 or movement_index >= excavation_index:
        errors.append(f"{selected_targets}: movement must win over excavation markers")
    errors.extend(require_fragments(
        excavation,
        texts.get(excavation, ""),
        "vertical tunnel planning",
        ("ExcavationStrokeAxis.Vertical", "SetTunnelPlan(", "vertical: true", "_excavationAnchor.Value"),
    ))
    errors.extend(require_fragments(
        depth_input,
        texts.get(depth_input, ""),
        "depth excavation from any open horizontal tunnel target",
        (
            "ResolveTunnelDepthSource",
            "!tunnelCell.IsVerticalTunnel",
            "tunnelCell.Cell.Z > selected.Value.Z",
            "ExcavateTunnelDepth(source.Value",
        ),
    ))
    if "tunnelCell.CanExcavateDepth" in texts.get(depth_input, ""):
        errors.append(f"{depth_input}: depth picking must not depend on stale presentation flags")
    errors.extend(require_fragments(
        tunnel_renderer,
        texts.get(tunnel_renderer, ""),
        "full-volume tunnel and natural cave hit targets",
        (
            "ConfigureInteractionCollider(target, cell, vertical, layout)",
            "IsNaturalCaveFloor",
            "SetWorldColliderBounds",
            "CaveCeilingY + 1",
            "new Vector3(0.94f, height, 0.50f)",
        ),
    ))
    errors.extend(require_fragments(
        movement_input,
        texts.get(movement_input, ""),
        "all walkable destination adapters",
        (
            "_tunnelRenderer.TryGetCell",
            "_caveRoomFloorRenderer.TryGetCell",
            "_renderer.TryGetWalkSurface",
            "MoveResidentsThroughTunnel",
        ),
    ))
    errors.extend(require_fragments(
        room_floor,
        texts.get(room_floor, ""),
        "Z0 and deep full-volume completed-room targets",
        (
            "for (int z = 0; z < plan.Preset.Depth; z++)",
            "Cave room hit target",
            "ConfigureInteractionCollider(floor, plan, cell)",
            "floor.GetComponent<Renderer>().enabled = z > 0",
            "plan.VolumeCells",
        ),
    ))
    errors.extend(require_fragments(
        canvas_hud,
        texts.get(canvas_hud, ""),
        "roster double-click focus routing",
        (
            "RegisterRosterResidentClick",
            "PointerInputSurface.ResidentRoster",
            "clickCount: RegisterRosterResidentClick(residentId)",
            "return doubleClick ? 2 : 1",
        ),
    ))
    errors.extend(require_fragments(
        camera,
        texts.get(camera, ""),
        "resident focus with a bounded zoom step",
        ("focusZoomStep", "_distance - focusZoomStep", "Mathf.Clamp(", "ApplyPose();"),
    ))
    errors.extend(require_fragments(
        world_session,
        texts.get(world_session, ""),
        "vertical tunnel plan ownership",
        ("_plannedVerticalTunnelCells", "PlannedVerticalTunnelCells", "SetVerticalTunnelPlan"),
    ))
    errors.extend(require_fragments(
        agent_session,
        texts.get(agent_session, ""),
        "front navigation synchronization",
        ("SynchronizeFrontNavigation", "WithSynchronizedFrontLayer", "CreateTunnelMovementHandlers();"),
    ))
    errors.extend(require_fragments(
        navigation_sync,
        texts.get(navigation_sync, ""),
        "authoritative world navigation refresh",
        ("SynchronizeExcavatedTunnelNavigation", "WorldSession.LoadSnapshot()", "WorldSession.PlannedVerticalTunnelCells"),
    ))
    errors.extend(require_fragments(
        movement,
        texts.get(movement, ""),
        "fresh single and group navigation with work interruption",
        (
            "SynchronizeExcavatedTunnelNavigation();",
            "MoveResidentThroughTunnel",
            "MoveResidentsThroughTunnel",
            "residentIds.Count == 1",
            "TerrainSession.InterruptForDirectMovement",
        ),
    ))
    errors.extend(require_fragments(
        agent_renderer,
        texts.get(agent_renderer, ""),
        "selection material persistence",
        ("visual.SetSelected(_selectedIds.Contains(model.Id))", "agentVisual.SetSelected(_selectedIds.Contains(model.Id))"),
    ))
    errors.extend(require_fragments(
        direct_control,
        texts.get(direct_control, ""),
        "direct movement job ownership",
        (
            "InterruptForDirectMovement",
            "ReleaseJobAssignmentCommand",
            "_routePlans.Remove",
            "_directMovementAgents.Add",
            "ClearManualGroupForAgent",
            "IsAvailableForAutomaticWork",
        ),
    ))
    errors.extend(require_fragments(
        designations,
        texts.get(designations, ""),
        "directly controlled candidate suppression",
        ("IsAvailableForAutomaticWork(agent)", "CreateDynamicCandidates"),
    ))
    errors.extend(require_fragments(
        manual_excavation,
        texts.get(manual_excavation, ""),
        "explicit return to work ownership",
        ("ReleaseDirectMovementControl(residentId);",),
    ))
    errors.extend(check_runtime_regression_contracts(runtime_root, texts, require_fragments))
    return errors
