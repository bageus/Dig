from __future__ import annotations

from pathlib import Path
from typing import Callable

from unity_authoritative_movement_contracts import (
    check_authoritative_movement_contracts,
)
from unity_runtime_regression_contracts import check_runtime_regression_contracts

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_navigation_and_marquee_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
) -> list[str]:
    interaction = runtime_root / "DigWorldInteraction.cs"
    pointer_hits = runtime_root / "DigWorldInteraction.PointerHits.cs"
    tunnel_renderer = runtime_root / "DigTunnelDemoRenderer.cs"
    room_floor = runtime_root / "DigCaveRoomFloorRenderer.cs"
    depth_input = runtime_root / "DigWorldInteraction.TunnelDepthExcavation.cs"
    canvas_hud = runtime_root / "DigWorldInteraction.CanvasHud.cs"
    camera = runtime_root / "DigCameraController.cs"
    marquee = runtime_root / "DigWorldInteraction.MarqueeSelection.cs"
    marquee_renderer = runtime_root / "DigSelectionMarqueeRenderer.cs"
    agent_renderer = runtime_root / "DigAgentRenderer.cs"
    excavation = runtime_root / "DigWorldInteraction.Excavation.cs"
    world_session = runtime_root / "DigWorldSession.TunnelNavigation.cs"
    agent_session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    spatial_runtime = runtime_root / "DigTerrainSpatialExcavation.cs"
    navigation_sync = runtime_root / "DigAgentSimulationDriverBase.NavigationSync.cs"
    designations = runtime_root / "DigTerrainWorkDesignations.cs"
    multi_worker = runtime_root / "DigTerrainWorkManualExcavation.MultiWorker.cs"

    errors: list[str] = []
    errors.extend(require_fragments(
        interaction,
        texts.get(interaction, ""),
        "shared pointer hit stack",
        (
            "RaycastHit[] hits = GetPointerHits();",
            "TryResolveAgentHit(hits",
            "TryApplyTunnelMove(hit, leftButton: true)",
            "TryAssignSelectedResidentToExcavation(hit, left)",
        ),
    ))

    pointer_text = texts.get(pointer_hits, "")
    errors.extend(require_fragments(
        pointer_hits,
        pointer_text,
        "reliable resident pointer and hover resolution",
        (
            "Physics.RaycastAll",
            "GetComponentInParent<DigAgentVisual>()",
            "TryResolveAgentNearPointer",
            "ResidentScreenPickRadius",
            "WorldToScreenPoint",
            "UpdateResidentHover",
            "SetHovered(false)",
            "SetHovered(true)",
            "firstWalkableDistance",
        ),
    ))
    errors.extend(require_fragments(
        agent_renderer,
        texts.get(agent_renderer, ""),
        "child-collider resident ownership",
        ("hit.collider.GetComponentInParent<DigAgentVisual>()",),
    ))

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
        "authoritative spatial tunnel depth input",
        (
            "ResolveTunnelDepthSource",
            "_caveRoomFloorRenderer.TryGetCell",
            "tunnelCell.Cell.Z > selected.Value.Z",
            "DesignateTunnelDepth(source.Value)",
            "Depth excavation designated",
        ),
    ))
    for forbidden in (
        "tunnelCell.CanExcavateDepth",
        "!tunnelCell.IsVerticalTunnel",
        "ExcavateTunnelDepth(source.Value",
        "RefreshCompletedCaveRooms(force: true)",
    ):
        if forbidden in depth_text:
            errors.append(
                f"{depth_input}: stale instant/orientation depth input remains: {forbidden!r}"
            )

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
        "resident marquee selection with shared pointer resolution",
        (
            "MarqueeThresholdPixels",
            "RaycastHit[] hits = GetPointerHits();",
            "IsMarqueeBlockingTarget(hits)",
            "TryResolveAgentHit(hits",
            "SelectResidentsInsideMarquee",
            "TryApplyTunnelMove",
        ),
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
        "vertical tunnel planning and explicit work",
        (
            "ExcavationStrokeAxis.Vertical",
            "vertical: true",
            "TryAssignSelectedResidentToExcavation",
            "AssignExcavationCluster",
        ),
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
        "front navigation synchronization and deferred depth commit",
        (
            "SynchronizeFrontNavigation",
            "WithSynchronizedFrontLayer",
            "PlanTunnelDepthExcavation",
            "CompleteTunnelDepthExcavation",
        ),
    ))
    errors.extend(require_fragments(
        spatial_runtime,
        texts.get(spatial_runtime, ""),
        "spatial excavation job lifecycle",
        (
            "SpatialDigJobDefinition",
            "DesignateSpatialExcavation",
            "PlanSpatialExcavationMovement",
            "AdvanceSpatialExcavationWork",
            "LoadSpatialExcavationsToFinalize",
            "CompleteSpatialExcavationJob",
        ),
    ))
    errors.extend(require_fragments(
        navigation_sync,
        texts.get(navigation_sync, ""),
        "authoritative navigation refresh",
        ("SynchronizeExcavatedTunnelNavigation", "WorldSession.LoadSnapshot()"),
    ))
    errors.extend(require_fragments(
        designations,
        texts.get(designations, ""),
        "pending manual excavation retries",
        ("RetryPendingManualExcavations(tick)",),
    ))
    errors.extend(require_fragments(
        multi_worker,
        texts.get(multi_worker, ""),
        "persistent multi-worker excavation groups",
        (
            "workerCount = Math.Min(agents.Length, orderedJobs.Count)",
            "IsWaitingForExcavationFront(assigned)",
            "IsOwnedByUnselectedResident",
        ),
    ))

    errors.extend(check_authoritative_movement_contracts(
        runtime_root,
        texts,
        require_fragments,
    ))
    errors.extend(check_runtime_regression_contracts(
        runtime_root,
        texts,
        require_fragments,
    ))
    return errors
