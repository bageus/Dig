from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_authoritative_movement_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
) -> list[str]:
    interaction = runtime_root / "DigWorldInteraction.cs"
    targets = runtime_root / "DigWorldInteraction.SelectedResidentTargets.cs"
    movement_input = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    excavation = runtime_root / "DigWorldInteraction.Excavation.cs"
    agent_session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    agent_session_base = runtime_root / "DigAgentSession.cs"
    spatial_agent_movement = runtime_root / "DigAgentSession.SpatialWorkMovement.cs"
    movement_driver = runtime_root / "DigAgentSimulationDriverBase.TunnelMovement.cs"
    spatial_driver = runtime_root / "DigAgentSimulationDriverBase.SpatialExcavation.cs"
    navigation_sync = runtime_root / "DigAgentSimulationDriverBase.NavigationSync.cs"
    tunnel_renderer = runtime_root / "DigTunnelDemoRenderer.cs"
    loop = runtime_root / "DigAgentSimulationDriverBase.Loop.cs"
    direct_control = runtime_root / "DigTerrainWorkDirectMovement.cs"
    designations = runtime_root / "DigTerrainWorkDesignations.cs"
    manual_excavation = runtime_root / "DigTerrainWorkManualExcavation.cs"

    errors: list[str] = []
    interaction_text = texts.get(interaction, "")
    errors.extend(require_fragments(
        interaction,
        interaction_text,
        "resident selection and authoritative movement ordering",
        (
            "TryResolveAgentHit(hits",
            "_agentRenderer!.SelectedCount > 0",
            "TryApplyTunnelMove(hit, leftButton: true)",
            "TryAssignSelectedResidentToExcavation(hit, left)",
        ),
    ))
    resident_index = interaction_text.find("TryResolveAgentHit(hits")
    movement_index = interaction_text.find(
        "TryApplyTunnelMove(hit, leftButton: true)"
    )
    work_index = interaction_text.find(
        "TryAssignSelectedResidentToExcavation(hit, left)"
    )
    if (
        resident_index < 0
        or movement_index < 0
        or work_index < 0
        or resident_index >= movement_index
        or movement_index >= work_index
    ):
        errors.append(
            f"{interaction}: resident pick must precede manual movement, "
            "which must precede forced work"
        )

    target_text = texts.get(targets, "")
    errors.extend(require_fragments(
        targets,
        target_text,
        "screen-disambiguated Z0-Z3 tunnel target priority",
        (
            "ResolveSelectedResidentTarget(GetPointerHits())",
            "HashSet<SpatialCellId> seenMovementCells",
            "ResolveMovementPointerDistance(",
            "bestScreenDistance",
            "return bestMovement",
            "DigSelectedResidentTarget.Excavation(excavationCandidate.Value)",
        ),
    ))
    resolved_movement_index = target_text.find("return bestMovement")
    excavation_index = target_text.find(
        "DigSelectedResidentTarget.Excavation(excavationCandidate.Value)"
    )
    if (
        resolved_movement_index < 0
        or excavation_index < 0
        or resolved_movement_index >= excavation_index
    ):
        errors.append(f"{targets}: open movement must win over designated excavation")

    movement_input_text = texts.get(movement_input, "")
    errors.extend(require_fragments(
        movement_input,
        movement_input_text,
        "explicit layered movement destinations",
        (
            "TryApplyTunnelMove(RaycastHit[] hits",
            "ResolveSelectedResidentTarget(hits)",
            "_tunnelRenderer.TryGetCell",
            "_caveRoomFloorRenderer.TryGetCell",
            "MoveResidentsThroughTunnel",
        ),
    ))
    if "TryAssignSpatialExcavation(" in movement_input_text:
        errors.append(
            f"{movement_input}: ordinary selected-resident LMB must not assign spatial work"
        )

    errors.extend(require_fragments(
        navigation_sync,
        texts.get(navigation_sync, ""),
        "authoritative world-to-hit-target synchronization",
        (
            "SynchronizeTunnelInteractionTargets(",
            "SynchronizeExcavatedTunnelNavigation();",
            "tunnelRenderer.Initialize(AgentSession.TunnelVolume)",
        ),
    ))
    errors.extend(require_fragments(
        tunnel_renderer,
        texts.get(tunnel_renderer, ""),
        "incremental newly excavated tunnel targets",
        (
            "_cells.Count == volume.Cells.Count",
            "foreach (SpatialCellId cell in volume.Cells)",
            "GetComponentInParent<DigTunnelCellVisual>()",
        ),
    ))

    errors.extend(require_fragments(
        excavation,
        texts.get(excavation, ""),
        "explicit return to Z0 before front-layer work",
        (
            "Move every selected dwarf to an open Z=0 tunnel cell",
            "AssignExcavationCluster",
        ),
    ))

    session_text = texts.get(agent_session, "")
    errors.extend(require_fragments(
        agent_session,
        session_text,
        "single authoritative manual movement order source",
        (
            "Dictionary<EntityId, ManualTunnelMovementOrder>",
            "ActiveManualTunnelResidentIds",
            "PlanAgentTunnelRouteCommandHandler",
            "TryAdvanceManualTunnelMovement(",
            "TunnelVolume.CanTraverseStep(",
            "agent.MoveTo(next, _tick)",
            "EnsureOccupiedCellsOpen(",
            "ConsumeManualTunnelMovementWarning",
        ),
    ))
    reject_fragments = (
        "_manualTunnelOrders",
        "MoveAgentThroughTunnelCommandHandler",
        "MoveAgentsThroughTunnelCommandHandler",
        "ReleaseManualTunnelOrder",
    )
    for fragment in reject_fragments:
        if fragment in session_text:
            errors.append(
                f"{agent_session}: obsolete tunnel movement fragment remains: {fragment!r}"
            )

    base_text = texts.get(agent_session_base, "")
    errors.extend(require_fragments(
        agent_session_base,
        base_text,
        "manual movement before work and local movement failures",
        (
            "TryAdvanceManualTunnelMovement(agent",
            "TryAdvanceSpatialWorkMovement(agent",
            "CancelManualMovementWithWarning(agent.Id, spatialMovement.Error!)",
            "CancelManualMovementWithWarning(agent.Id, result.Error!)",
            "return Result.Success();",
        ),
    ))
    manual_index = base_text.find("TryAdvanceManualTunnelMovement(agent")
    spatial_index = base_text.find("TryAdvanceSpatialWorkMovement(agent")
    if manual_index < 0 or spatial_index < 0 or manual_index >= spatial_index:
        errors.append(f"{agent_session_base}: manual movement must precede spatial work")

    errors.extend(require_fragments(
        spatial_agent_movement,
        texts.get(spatial_agent_movement, ""),
        "authoritative spatial work steps",
        ("FindPath", "agent.SpatialPosition", "agent.MoveTo(next, _tick)"),
    ))

    driver_text = texts.get(movement_driver, "")
    errors.extend(require_fragments(
        movement_driver,
        driver_text,
        "interrupt-before-plan manual movement",
        (
            "SynchronizeTunnelInteractionTargets(tunnelRenderer)",
            "TerrainSession.InterruptForManualMovement",
            "PlanAgentTunnelRouteReport",
            "PlanAgentsTunnelRoutesReport",
            "AgentSession.MoveResidentThroughTunnel",
            "AgentSession.MoveResidentsThroughTunnel",
        ),
    ))
    for fragment in (
        "ValidateResidentThroughTunnel",
        "ValidateResidentsThroughTunnel",
        "AnimateRoute(",
    ):
        if fragment in driver_text:
            errors.append(
                f"{movement_driver}: obsolete validate/animate fragment remains: {fragment!r}"
            )

    errors.extend(require_fragments(
        spatial_driver,
        texts.get(spatial_driver, ""),
        "manual route cancellation when assigning spatial work",
        ("TryAssignSpatialExcavation", "CancelManualTunnelMovement"),
    ))

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
    for fragment in (
        "_directMovementAgents",
        "EnforceDirectMovementOwnership",
        "ReleaseDirectMovementControl",
        "InterruptForDirectMovement",
    ):
        if fragment in direct_text:
            errors.append(
                f"{direct_control}: legacy direct ownership remains: {fragment!r}"
            )

    errors.extend(require_fragments(
        loop,
        texts.get(loop, ""),
        "nonfatal per-resident movement warnings",
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
        "automatic work suppression during manual movement",
        ("IsAvailableForAutomaticWork(agent)",),
    ))
    if "ReleaseDirectMovementControl" in texts.get(manual_excavation, ""):
        errors.append(
            f"{manual_excavation}: legacy direct-control release remains"
        )
    return errors
