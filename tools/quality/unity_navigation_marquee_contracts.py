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
    agent_renderer = runtime_root / "DigAgentRenderer.cs"
    excavation = runtime_root / "DigWorldInteraction.Excavation.cs"
    world_session = runtime_root / "DigWorldSession.TunnelNavigation.cs"
    agent_session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    agent_session_base = runtime_root / "DigAgentSession.cs"
    spatial_agent_movement = runtime_root / "DigAgentSession.SpatialWorkMovement.cs"
    navigation_sync = runtime_root / "DigAgentSimulationDriverBase.NavigationSync.cs"
    movement = runtime_root / "DigAgentSimulationDriverBase.TunnelMovement.cs"
    loop = runtime_root / "DigAgentSimulationDriverBase.Loop.cs"
    spatial_driver = runtime_root / "DigAgentSimulationDriverBase.SpatialExcavation.cs"
    spatial_runtime = runtime_root / "DigTerrainSpatialExcavation.cs"
    direct_control = runtime_root / "DigTerrainWorkDirectMovement.cs"
    designations = runtime_root / "DigTerrainWorkDesignations.cs"
    manual_excavation = runtime_root / "DigTerrainWorkManualExcavation.cs"
    multi_worker = runtime_root / "DigTerrainWorkManualExcavation.MultiWorker.cs"

    errors: list[str] = []
    interaction_text = texts.get(interaction, "")
    errors.extend(require_fragments(
        interaction,
        interaction_text,
        "resident selection, authoritative movement and forced work ordering",
        (
            "RaycastHit[] hits = GetPointerHits();",
            "TryResolveAgentHit(hits",
            "_agentRenderer!.SelectedCount > 0",
            "TryApplyTunnelMove(hit, leftButton: true)",
            "TryAssignSelectedResidentToExcavation(hit, left)",
            "TryApplyTunnelMove(hit, left)",
            "_agentRenderer!.SelectedCount == 0",
        ),
    ))
    resident_pick_index = interaction_text.find("TryResolveAgentHit(hits")
    direct_move_index = interaction_text.find(
        "TryApplyTunnelMove(hit, leftButton: true)"
    )
    forced_work_index = interaction_text.find(
        "TryAssignSelectedResidentToExcavation(hit, left)"
    )
    if (
        resident_pick_index < 0
        or direct_move_index < 0
        or forced_work_index < 0
        or resident_pick_index >= direct_move_index
        or direct_move_index >= forced_work_index
    ):
        errors.append(
            f"{interaction}: expected resident pick before authoritative movement "
            "and movement before forced work"
        )

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

    target_text = texts.get(targets, "")
    errors.extend(require_fragments(
        targets,
        target_text,
        "open layered movement priority over designated work",
        (
            "CellId? excavationCandidate",
            "GetComponentInParent<DigAgentVisual>()",
            "TryResolveTunnelDestination(",
            "visibleMovement",
            "hiddenMovement",
            "return visibleMovement",
            "DigSelectedResidentTarget.Excavation(excavationCandidate.Value)",
        ),
    ))
    movement_index = target_text.find("return visibleMovement")
    excavation_index = target_text.find(
        "DigSelectedResidentTarget.Excavation(excavationCandidate.Value)"
    )
    if movement_index < 0 or excavation_index < 0 or movement_index >= excavation_index:
        errors.append(f"{targets}: open movement must win over designated excavation")

    movement_text = texts.get(movement_input, "")
    errors.extend(require_fragments(
        movement_input,
        movement_text,
        "dedicated walkable destinations and spatial work assignment",
        (
            "_tunnelRenderer.TryGetCell",
            "_caveRoomFloorRenderer.TryGetCell",
            "TryAssignSpatialExcavation(",
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
            "Move every selected dwarf to an open Z=0 tunnel cell",
            "AssignExcavationCluster",
        ),
    ))
    errors.extend(require_fragments(
        world_session,
        texts.get(world_session, ""),
        "vertical tunnel ownership",
        ("_plannedVerticalTunnelCells", "SetVerticalTunnelPlan"),
    ))

    agent_session_text = texts.get(agent_session, "")
    errors.extend(require_fragments(
        agent_session,
        agent_session_text,
        "single authoritative manual tunnel order source",
        (
            "Dictionary<EntityId, ManualTunnelMovementOrder>",
            "ActiveManualTunnelResidentIds",
            "PlanAgentTunnelRouteCommandHandler",
            "TryAdvanceManualTunnelMovement(",
            "TunnelVolume.CanTraverseStep(",
            "agent.MoveTo(next, _tick)",
            "EnsureOccupiedCellsOpen(",
            "ConsumeManualTunnelMovementWarning",
            "SynchronizeFrontNavigation",
            "WithSynchronizedFrontLayer",
            "PlanTunnelDepthExcavation",
            "CompleteTunnelDepthExcavation",
        ),
    ))
    for forbidden in (
        "_manualTunnelOrders",
        "MoveAgentThroughTunnelCommandHandler",
        "MoveAgentsThroughTunnelCommandHandler",
        "ReleaseManualTunnelOrder",
    ):
        if forbidden in agent_session_text:
            errors.append(f"{agent_session}: obsolete tunnel order fragment remains: {forbidden!r}")

    errors.extend(require_fragments(
        agent_session_base,
        texts.get(agent_session_base, ""),
        "manual movement before all work movement",
        (
            "TryAdvanceManualTunnelMovement(agent",
            "TryAdvanceSpatialWorkMovement(agent",
        ),
    ))
    base_text = texts.get(agent_session_base, "")
    if base_text.find("TryAdvanceManualTunnelMovement(agent") >= base_text.find(
        "TryAdvanceSpatialWorkMovement(agent"
    ):
        errors.append(f"{agent_session_base}: manual movement must precede spatial work")

    errors.extend(require_fragments(
        spatial_agent_movement,
        texts.get(spatial_agent_movement, ""),
        "spatial work path movement",
        ("FindPath", "agent.SpatialPosition", "agent.MoveTo(next, _tick)"),
    ))
    errors.extend(require_fragments(
        spatial_driver,
        texts.get(spatial_driver, ""),
        "selected resident spatial assignment",
        ("TryAssignSpatialExcavation", "CancelManualTunnelMovement"),
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

    movement_driver_text = texts.get(movement, "")
    errors.extend(require_fragments(
        movement,
        movement_driver_text,
        "interrupt-before-plan manual movement",
        (
            "TerrainSession.InterruptForManualMovement",
            "PlanAgentTunnelRouteReport",
            "PlanAgentsTunnelRoutesReport",
            "AgentSession.MoveResidentThroughTunnel",
            "AgentSession.MoveResidentsThroughTunnel",
        ),
    ))
    for forbidden in (
        "ValidateResidentThroughTunnel",
        "ValidateResidentsThroughTunnel",
        "AnimateRoute(",
    ):
        if forbidden in movement_driver_text:
            errors.append(f"{movement}: obsolete validate/animate fragment remains: {forbidden!r}")

    direct_text = texts.get(direct_control, "")
    errors.extend(require_fragments(
        direct_control,
        direct_text,
        "stateless work interruption bound to authoritative movement",
        (
            "BindManualMovementSource",
            "InterruptForManualMovement",
            "IsAvailableForAutomaticWork",
            "ReleaseJobAssignmentCommand",
            "RemoveAllRoutePlans",
        ),
    ))
    for forbidden in (
        "_directMovementAgents",
        "EnforceDirectMovementOwnership",
        "ReleaseDirectMovementControl",
        "InterruptForDirectMovement",
    ):
        if forbidden in direct_text:
            errors.append(f"{direct_control}: legacy direct ownership remains: {forbidden!r}")

    loop_text = texts.get(loop, "")
    errors.extend(require_fragments(
        loop,
        loop_text,
        "manual work interruption and nonfatal route warnings",
        (
            "AgentSession.ActiveManualTunnelResidentIds",
            "TerrainSession!.InterruptForManualMovement(",
            "ConsumeManualTunnelMovementWarning()",
            "Manual movement cancelled:",
        ),
    ))

    errors.extend(require_fragments(
        designations,
        texts.get(designations, ""),
        "automatic work suppression and pending manual retries",
        ("IsAvailableForAutomaticWork(agent)", "RetryPendingManualExcavations(tick)"),
    ))
    manual_text = texts.get(manual_excavation, "")
    errors.extend(require_fragments(
        manual_excavation,
        manual_text,
        "persistent explicit work without stealing another assignment",
        (
            "IsOwnedByOtherResident",
            "job.AssignedAgentId == agentId",
            "RetryPendingManualExcavations",
            "IsWaitingForExcavationFront",
        ),
    ))
    if "ReleaseDirectMovementControl" in manual_text:
        errors.append(f"{manual_excavation}: legacy direct-control release remains")

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
    errors.extend(check_runtime_regression_contracts(runtime_root, texts, require_fragments))
    return errors
