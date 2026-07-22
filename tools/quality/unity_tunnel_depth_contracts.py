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
    session = runtime_root / "DigAgentSession.TunnelTopology.cs"
    runtime = runtime_root / "DigAgentSimulationDriverBase.CaveRooms.cs"
    loop = runtime_root / "DigAgentSimulationDriverBase.Loop.cs"
    spatial_runtime = runtime_root / "DigTerrainSpatialExcavation.cs"
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
            "designate one deeper layer",
            "if (_excavationMode == DigExcavationDrawingMode.Depth)",
        ),
    ))
    depth_text = texts.get(depth_input, "")
    errors.extend(require_fragments(
        depth_input,
        depth_text,
        "one-layer spatial tunnel depth interaction",
        (
            "Input.GetMouseButtonDown(0)",
            "ResolveTunnelDepthSource",
            "TryGetWalkSurface",
            "TryGetCell",
            "_caveRoomFloorRenderer.TryGetCell",
            "tunnelCell.Cell.Z > selected.Value.Z",
            "DesignateTunnelDepth",
            "Depth excavation designated",
            "A worker must reach the open face",
        ),
    ))
    for forbidden in (
        "tunnelCell.CanExcavateDepth",
        "!tunnelCell.IsVerticalTunnel",
        "ExcavateTunnelDepth",
        "RefreshCompletedCaveRooms(force: true)",
        "The new deepest tunnel cell is selected for the next step",
    ):
        if forbidden in depth_text:
            errors.append(
                f"{depth_input}: stale instant depth behavior remains: {forbidden!r}"
            )
    errors.extend(require_fragments(
        session,
        texts.get(session, ""),
        "authoritative deferred tunnel depth ownership",
        (
            "TunnelDepthExcavationPolicy",
            "_tunnelDepthExcavations",
            "TunnelDepthExcavations",
            "PlanTunnelDepthExcavation",
            "CompleteTunnelDepthExcavation",
            "SynchronizeNavigation(world, plannedVerticalCells)",
        ),
    ))
    errors.extend(require_fragments(
        runtime,
        texts.get(runtime, ""),
        "tunnel depth job designation and runtime refresh",
        (
            "AgentSession.PlanTunnelDepthExcavation(source)",
            "TerrainSession.DesignateSpatialExcavation(",
            "CompleteSpatialExcavation",
            "renderer.Initialize(AgentSession.TunnelVolume)",
            "AgentSession.TunnelDepthExcavations",
        ),
    ))
    errors.extend(require_fragments(
        loop,
        texts.get(loop, ""),
        "spatial excavation simulation loop",
        (
            "PlanSpatialExcavationMovement",
            "SetSpatialWorkMovementTargets",
            "AdvanceSpatialExcavationWork",
            "LoadSpatialExcavationsToFinalize",
            "CompleteSpatialExcavation(commits[index])",
        ),
    ))
    errors.extend(require_fragments(
        spatial_runtime,
        texts.get(spatial_runtime, ""),
        "spatial dig stages and finalization",
        (
            "SpatialDigJobDefinition",
            "JobStageKind.TravelToTarget",
            "JobStageKind.PerformWork",
            "JobStageKind.Finalize",
            "CompleteSpatialExcavationJob",
        ),
    ))
    errors.extend(require_fragments(
        hud,
        texts.get(hud, ""),
        "tunnel depth controls",
        (
            'GUILayout.Button("Depth"',
            "DigExcavationDrawingMode.Depth",
            "create a Z+1 Dig job, up to Z=3",
        ),
    ))
    return errors
