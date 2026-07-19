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
    marquee = runtime_root / "DigWorldInteraction.MarqueeSelection.cs"
    marquee_renderer = runtime_root / "DigSelectionMarqueeRenderer.cs"
    excavation = runtime_root / "DigWorldInteraction.Excavation.cs"
    world_session = runtime_root / "DigWorldSession.TunnelNavigation.cs"
    agent_session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    navigation_sync = runtime_root / "DigAgentSimulationDriverBase.NavigationSync.cs"
    movement = runtime_root / "DigAgentSimulationDriverBase.TunnelMovement.cs"
    movement_input = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    room_floor = runtime_root / "DigCaveRoomFloorRenderer.cs"
    agent_renderer = runtime_root / "DigAgentRenderer.cs"
    direct_control = runtime_root / "DigTerrainWorkDirectMovement.cs"
    designations = runtime_root / "DigTerrainWorkDesignations.cs"
    manual_excavation = runtime_root / "DigTerrainWorkManualExcavation.cs"
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
            "SetTunnelPlan(",
            "vertical: true",
            "_excavationAnchor.Value",
        ),
    ))
    errors.extend(require_fragments(
        world_session,
        texts.get(world_session, ""),
        "vertical tunnel plan ownership",
        (
            "_plannedVerticalTunnelCells",
            "PlannedVerticalTunnelCells",
            "SetVerticalTunnelPlan",
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
        movement_input,
        texts.get(movement_input, ""),
        "explicit tunnel destination renderers",
        (
            "_tunnelRenderer.TryGetCell",
            "_caveRoomFloorRenderer.TryGetCell",
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
        ),
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
    errors.extend(check_runtime_regression_contracts(
        runtime_root,
        texts,
        require_fragments,
    ))
    return errors
