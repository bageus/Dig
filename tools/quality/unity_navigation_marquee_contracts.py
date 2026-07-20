from __future__ import annotations

from pathlib import Path
from typing import Callable

from unity_runtime_regression_contracts import check_runtime_regression_contracts

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_navigation_and_marquee_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
) -> list[str]:
    interaction = runtime_root / "DigWorldInteraction.cs"
    pointer_hits = runtime_root / "DigWorldInteraction.PointerHits.cs"
    targets = runtime_root / "DigWorldInteraction.SelectedResidentTargets.cs"
    movement_input = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    depth_input = runtime_root / "DigWorldInteraction.TunnelDepthExcavation.cs"
    tunnel_renderer = runtime_root / "DigTunnelDemoRenderer.cs"
    room_floor = runtime_root / "DigCaveRoomFloorRenderer.cs"
    canvas_hud = runtime_root / "DigWorldInteraction.CanvasHud.cs"
    camera = runtime_root / "DigCameraController.cs"
    marquee = runtime_root / "DigWorldInteraction.MarqueeSelection.cs"
    marquee_renderer = runtime_root / "DigSelectionMarqueeRenderer.cs"
    excavation = runtime_root / "DigWorldInteraction.Excavation.cs"
    world_session = runtime_root / "DigWorldSession.TunnelNavigation.cs"
    agent_session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    navigation_sync = runtime_root / "DigAgentSimulationDriverBase.NavigationSync.cs"
    movement = runtime_root / "DigAgentSimulationDriverBase.TunnelMovement.cs"
    direct_control = runtime_root / "DigTerrainWorkDirectMovement.cs"
    designations = runtime_root / "DigTerrainWorkDesignations.cs"
    manual_excavation = runtime_root / "DigTerrainWorkManualExcavation.cs"

    errors: list[str] = []
    interaction_text = texts.get(interaction, "")
    errors.extend(require_fragments(
        interaction,
        interaction_text,
        "selected-resident tunnel movement before global resident picking",
        (
            "RaycastHit[] hits = GetPointerHits();",
            "_agentRenderer!.SelectedCount > 0",
            "TryApplyTunnelMove(hit, leftButton: true)",
            "TryResolveAgentHit(hits",
            "TryApplyTunnelMove(hit, left)",
            "_agentRenderer!.SelectedCount == 0",
        ),
    ))
    direct_move_index = interaction_text.find(
        "TryApplyTunnelMove(hit, leftButton: true)"
    )
    resident_pick_index = interaction_text.find("TryResolveAgentHit(hits")
    if (
        direct_move_index < 0
        or resident_pick_index < 0
        or direct_move_index >= resident_pick_index
    ):
        errors.append(
            f"{interaction}: selected-resident tunnel movement must be resolved "
            "before the global all-hit resident picker"
        )
    if "TryAssignSelectedResidentToExcavation(hit, left)" in interaction_text:
        errors.append(f"{interaction}: selected-resident LMB must not assign excavation")

    errors.extend(require_fragments(
        pointer_hits,
        texts.get(pointer_hits, ""),
        "reliable resident pointer resolution",
        (
            "Physics.RaycastAll",
            "GetComponentInParent<DigAgentVisual>()",
            "TryResolveAgentNearPointer",
            "ResidentScreenPickRadius",
            "WorldToScreenPoint",
        ),
    ))
    errors.extend(require_fragments(
        targets,
        texts.get(targets, ""),
        "movement before excavation markers and child-collider residents",
        (
            "CellId? excavationCandidate",
            "GetComponentInParent<DigAgentVisual>()",
            "TryResolveTunnelDestination(",
            "return DigSelectedResidentTarget.Movement",
            "DigSelectedResidentTarget.Excavation(excavationCandidate.Value)",
        ),
    ))
    target_text = texts.get(targets, "")
    if target_text.find("return DigSelectedResidentTarget.Movement") >= target_text.find(
        "DigSelectedResidentTarget.Excavation(excavationCandidate.Value)"
    ):
        errors.append(f"{targets}: movement must win over excavation markers")

    movement_text = texts.get(movement_input, "")
    errors.extend(require_fragments(
        movement_input,
        movement_text,
        "dedicated walkable destination proxies",
        (
            "_tunnelRenderer.TryGetCell",
            "_caveRoomFloorRenderer.TryGetCell",
            "MoveResidentsThroughTunnel",
        ),
    ))
    if "TryGetWalkSurface" in movement_text:
        errors.append(f"{movement_input}: general terrain picking must remain disabled")

    errors.extend(require_fragments(
        tunnel_renderer,
        texts.get(tunnel_renderer, ""),
        "full-volume tunnel and natural cave targets",
        (
            "ConfigureInteractionCollider(target, cell, vertical, layout)",
            "IsNaturalCaveFloor",
            "SetWorldColliderBounds",
            "CaveCeilingY + 1",
        ),
    ))
    errors.extend(require_fragments(
        room_floor,
        texts.get(room_floor, ""),
        "full-volume completed-room targets",
        (
            "for (int z = 0; z < plan.Preset.Depth; z++)",
            "Cave room hit target",
            "ConfigureInteractionCollider(floor, plan, cell)",
            "floor.GetComponent<Renderer>().enabled = z > 0",
        ),
    ))

    depth_text = texts.get(depth_input, "")
    errors.extend(require_fragments(
        depth_input,
        depth_text,
        "authoritative tunnel depth input",
        (
            "ResolveTunnelDepthSource",
            "!tunnelCell.IsVerticalTunnel",
            "tunnelCell.Cell.Z > selected.Value.Z",
            "ExcavateTunnelDepth(source.Value",
        ),
    ))
    if "tunnelCell.CanExcavateDepth" in depth_text:
        errors.append(f"{depth_input}: stale depth presentation flag remains")

    errors.extend(require_fragments(
        canvas_hud,
        texts.get(canvas_hud, ""),
        "roster double-click focus",
        (
            "RegisterRosterResidentClick",
            "clickCount: RegisterRosterResidentClick(residentId)",
            "return doubleClick ? 2 : 1",
        ),
    ))
    errors.extend(require_fragments(
        camera,
        texts.get(camera, ""),
        "bounded resident focus zoom",
        ("focusZoomStep", "_distance - focusZoomStep", "Mathf.Clamp("),
    ))

    errors.extend(require_fragments(
        marquee,
        texts.get(marquee, ""),
        "resident marquee selection",
        ("MarqueeThresholdPixels", "SelectResidentsInsideMarquee", "TryApplyTunnelMove"),
    ))
    errors.extend(require_fragments(
        marquee_renderer,
        texts.get(marquee_renderer, ""),
        "marquee rendering",
        ("private void OnGUI()", "GUI.DrawTexture", "Screen.height"),
    ))
    errors.extend(require_fragments(
        excavation,
        texts.get(excavation, ""),
        "vertical tunnel planning",
        ("ExcavationStrokeAxis.Vertical", "vertical: true"),
    ))
    errors.extend(require_fragments(
        world_session,
        texts.get(world_session, ""),
        "vertical tunnel ownership",
        ("_plannedVerticalTunnelCells", "SetVerticalTunnelPlan"),
    ))
    errors.extend(require_fragments(
        agent_session,
        texts.get(agent_session, ""),
        "front navigation synchronization",
        ("SynchronizeFrontNavigation", "WithSynchronizedFrontLayer"),
    ))
    errors.extend(require_fragments(
        navigation_sync,
        texts.get(navigation_sync, ""),
        "authoritative navigation refresh",
        ("SynchronizeExcavatedTunnelNavigation", "WorldSession.LoadSnapshot()"),
    ))
    errors.extend(require_fragments(
        movement,
        texts.get(movement, ""),
        "validated direct movement",
        ("ValidateResidentThroughTunnel", "TerrainSession.InterruptForDirectMovement"),
    ))
    errors.extend(require_fragments(
        direct_control,
        texts.get(direct_control, ""),
        "direct movement ownership",
        ("InterruptForDirectMovement", "IsAvailableForAutomaticWork"),
    ))
    errors.extend(require_fragments(
        designations,
        texts.get(designations, ""),
        "automatic work suppression",
        ("IsAvailableForAutomaticWork(agent)",),
    ))
    errors.extend(require_fragments(
        manual_excavation,
        texts.get(manual_excavation, ""),
        "explicit return to work",
        ("ReleaseDirectMovementControl(residentId);",),
    ))
    errors.extend(check_runtime_regression_contracts(runtime_root, texts, require_fragments))
    return errors
