from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]
RejectFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_excavation_contracts(
    root: Path,
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
    reject_fragments: RejectFragments,
) -> list[str]:
    del root
    rock_path = runtime_root / "DigRockVolumeRenderer.cs"
    projection_path = runtime_root / "DigTunnelProjection.cs"
    tunnel_renderer_path = runtime_root / "DigTunnelDemoRenderer.cs"
    world_renderer_path = runtime_root / "DigWorldRenderer.cs"
    interaction_path = runtime_root / "DigWorldInteraction.cs"
    drawing_path = runtime_root / "DigWorldInteraction.Excavation.cs"
    designations_path = runtime_root / "DigTerrainWorkDesignations.cs"
    manual_path = runtime_root / "DigTerrainWorkManualExcavation.cs"
    driver_path = runtime_root / "DigAgentSimulationDriverBase.Excavation.cs"
    session_path = runtime_root / "DigAgentSession.TunnelMovement.cs"
    hud_path = runtime_root / "DigHudOverlay.Excavation.cs"
    bootstrap_path = runtime_root / "DigUnityBootstrap.cs"
    errors: list[str] = []
    errors.extend(require_fragments(
        rock_path,
        texts.get(rock_path, ""),
        "solid rock volume",
        (
            "for (int z = 1; z < volume.Depth; z++)",
            "IsSolidRock",
            "volume.IsOpen(cell)",
            "MeshFilter",
            "IndexFormat.UInt32",
        ),
    ))
    errors.extend(require_fragments(
        projection_path,
        texts.get(projection_path, ""),
        "embedded walk-surface projection",
        (
            "RockCellHalfExtent",
            "FloorThickness",
            "FloorWorldPosition",
            "WalkSurfaceY",
            "ResidentHalfHeight",
            "ResidentFootSink",
        ),
    ))
    errors.extend(require_fragments(
        tunnel_renderer_path,
        texts.get(tunnel_renderer_path, ""),
        "layered floor rendering",
        (
            "if (!vertical && cell.Z == 0)",
            "DigTunnelProjection.FloorWorldPosition(cell)",
            "DigTunnelProjection.FloorThickness",
            "DigTunnelProjection.FloorDepth",
        ),
    ))
    errors.extend(require_fragments(
        world_renderer_path,
        texts.get(world_renderer_path, ""),
        "dynamic Z0 floor rendering",
        (
            "_walkSurfaceCells",
            "cell.Y + 1",
            "DigTunnelProjection.FloorWorldPosition",
            "renderable && !hiddenByCutaway",
        ),
    ))
    errors.extend(require_fragments(
        drawing_path,
        texts.get(drawing_path, ""),
        "unified Z0 excavation drawing",
        (
            "DigExcavationDrawingMode.Tunnel",
            "DigExcavationDrawingMode.Delete",
            "ExcavationStrokePlanner",
            "Input.GetMouseButton(0)",
            "TryHandleExcavationStroke",
            "AssignExcavationCluster",
            "new SpatialCellId(selected.CellX, selected.CellY, 0)",
        ),
    ))
    errors.extend(reject_fragments(
        drawing_path,
        texts.get(drawing_path, ""),
        "separate horizontal and vertical tools",
        (
            "DigExcavationDrawingMode.Horizontal",
            "DigExcavationDrawingMode.Vertical",
        ),
    ))
    interaction = texts.get(interaction_path, "")
    errors.extend(require_fragments(
        interaction_path,
        interaction,
        "explicit excavation routing",
        (
            "TryHandleExcavationStroke()",
            "TryAssignSelectedResidentToExcavation(hit, left)",
            "excavationTool: ExcavationToolKind.None",
            "hud.SetExcavationControls(this)",
        ),
    ))
    errors.extend(reject_fragments(
        interaction_path,
        interaction,
        "implicit RMB excavation",
        ("ExcavationToolKind.Tunnel",),
    ))
    errors.extend(require_fragments(
        designations_path,
        texts.get(designations_path, ""),
        "frontier-only automatic excavation",
        (
            "IsExcavationFrontier",
            "NoCandidates",
            "agent.CellZ == 0",
            "IsManualExcavationJob(job.Id)",
        ),
    ))
    errors.extend(require_fragments(
        manual_path,
        texts.get(manual_path, ""),
        "manual excavation cluster",
        (
            "radius: 4",
            "AssignSpecificJobCommand",
            "ContinueManualExcavation",
            "AssignNextManualExcavation",
        ),
    ))
    errors.extend(require_fragments(
        driver_path,
        texts.get(driver_path, ""),
        "excavation application adapter",
        (
            "ApplyExcavationDesignation",
            "ReleaseManualTunnelOrder(residentId)",
            "SynchronizeDesignations",
            "RefreshExcavationPresentation",
        ),
    ))
    errors.extend(require_fragments(
        session_path,
        texts.get(session_path, ""),
        "manual movement release",
        ("ReleaseManualTunnelOrder", "_manualTunnelOrders.Remove"),
    ))
    errors.extend(require_fragments(
        hud_path,
        texts.get(hud_path, ""),
        "excavation HUD controls",
        ("Tunnel", "Delete", "P-", "P+"),
    ))
    errors.extend(reject_fragments(
        hud_path,
        texts.get(hud_path, ""),
        "separate axis buttons",
        (
            'GUILayout.Button("Horizontal"',
            'GUILayout.Button("Vertical"',
        ),
    ))
    errors.extend(require_fragments(
        bootstrap_path,
        texts.get(bootstrap_path, ""),
        "rock volume composition",
        ("DigRockVolumeRenderer", "rockRenderer.Initialize"),
    ))
    return errors
