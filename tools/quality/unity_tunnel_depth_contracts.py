from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_tunnel_depth_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
) -> list[str]:
    interaction = runtime_root / "DigWorldInteraction.cs"
    drawing = runtime_root / "DigWorldInteraction.Excavation.cs"
    depth_input = runtime_root / "DigWorldInteraction.TunnelDepthExcavation.cs"
    movement_input = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    spatial_movement = runtime_root / "DigAgentSession.SpatialWorkMovement.cs"
    runtime = runtime_root / "DigAgentSimulationDriverBase.CaveRooms.cs"
    loop = runtime_root / "DigAgentSimulationDriverBase.Loop.cs"
    spatial_jobs = runtime_root / "DigTerrainSpatialExcavation.cs"
    hud = runtime_root / "DigHudOverlay.Excavation.cs"
    errors: list[str] = []
    errors.extend(require_fragments(
        interaction,
        texts.get(interaction, ""),
        "tunnel depth input priority",
        (
            "TryHandleCaveRoomPlacement()",
            "TryHandleTunnelDepthExcavation()",
            "TryHandleExcavationStroke()",
        ),
    ))
    errors.extend(require_fragments(
        drawing,
        texts.get(drawing, ""),
        "single-click depth mode",
        (
            "Depth = 3",
            "DigExcavationDrawingMode.Depth",
            "excavate one deeper layer",
            "if (_excavationMode == DigExcavationDrawingMode.Depth)",
        ),
    ))
    depth_text = texts.get(depth_input, "")
    errors.extend(require_fragments(
        depth_input,
        depth_text,
        "one-layer spatial depth designation",
        (
            "Input.GetMouseButtonDown(0)",
            "ResolveTunnelDepthSource",
            "TryGetWalkSurface",
            "_caveRoomFloorRenderer.TryGetCell",
            "DesignateTunnelDepth(source.Value)",
            "Depth excavation designated",
            "A worker must reach the open face",
        ),
    ))
    for forbidden in (
        "!tunnelCell.IsVerticalTunnel",
        "RefreshCompletedCaveRooms(force: true)",
        "Tunnel depth excavated at",
    ):
        if forbidden in depth_text:
            errors.append(f"{depth_input}: forbidden instant/orientation contract {forbidden!r}")

    errors.extend(require_fragments(
        movement_input,
        texts.get(movement_input, ""),
        "selected resident spatial assignment",
        (
            "TryAssignSpatialExcavation(",
            "Existing workers keep their excavation jobs",
            "MoveResidentsThroughTunnel",
        ),
    ))
    errors.extend(require_fragments(
        session,
        texts.get(session, ""),
        "plan-before-finalize tunnel ownership",
        (
            "PlanTunnelDepthExcavation",
            "CompleteTunnelDepthExcavation",
            "_tunnelDepthExcavations.Add(target)",
            "ExpandTunnelVolume(new[] { target })",
        ),
    ))
    errors.extend(require_fragments(
        spatial_movement,
        texts.get(spatial_movement, ""),
        "one-cell spatial worker movement",
        (
            "FindPath(",
            "path.Path.Cells[1]",
            "agent.MoveTo(next, _tick)",
        ),
    ))
    errors.extend(require_fragments(
        spatial_jobs,
        texts.get(spatial_jobs, ""),
        "spatial excavation job stages",
        (
            "SpatialDigJobDefinition",
            "PlanSpatialExcavationMovement",
            "AdvanceSpatialExcavationWork",
            "LoadSpatialExcavationsToFinalize",
            "CompleteSpatialExcavationJob",
            "ScheduleActivity.Work",
        ),
    ))
    errors.extend(require_fragments(
        loop,
        texts.get(loop, ""),
        "simulation-driven spatial excavation",
        (
            "SetSpatialWorkMovementTargets",
            "AdvanceSpatialExcavationWork",
            "LoadSpatialExcavationsToFinalize",
            "CompleteSpatialExcavation(commits[index])",
        ),
    ))
    errors.extend(require_fragments(
        runtime,
        texts.get(runtime, ""),
        "finalize-only terrain opening",
        (
            "DesignateSpatialExcavation(",
            "CompleteTunnelDepthExcavation(commit.Target)",
            "CompleteSpatialExcavationJob(",
            "renderer.Initialize(AgentSession.TunnelVolume)",
        ),
    ))
    errors.extend(require_fragments(
        hud,
        texts.get(hud, ""),
        "tunnel depth controls",
        (
            'GUILayout.Button("Depth"',
            "DigExcavationDrawingMode.Depth",
            "each click opens only Z+1, up to Z=3",
        ),
    ))
    return errors
