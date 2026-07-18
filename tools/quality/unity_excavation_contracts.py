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
    protection_path = runtime_root / "DigWorldRenderer.Protection.cs"
    cell_visual_path = runtime_root / "DigCellVisual.cs"
    movement_path = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    world_session_path = runtime_root / "DigWorldSession.cs"
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
        "dynamic Z0 floor rendering and movement",
        (
            "_walkSurfaceCells",
            "cell.Y + 1",
            "DigTunnelProjection.FloorWorldPosition",
            "renderable && !hiddenByCutaway",
            "TryGetWalkSurface",
            "new SpatialCellId(visual.Model.X, visual.Model.Y, 0)",
            "ApplyProtectedVisual",
        ),
    ))
    errors.extend(require_fragments(
        movement_path,
        texts.get(movement_path, ""),
        "direct layered movement",
        (
            "_tunnelRenderer.TryGetCell",
            "_renderer.TryGetWalkSurface",
            "SpatialCellId destination",
            "MoveResidentThroughTunnel",
        ),
    ))
    errors.extend(require_fragments(
        protection_path,
        texts.get(protection_path, ""),
        "protected rock presentation",
        (
            "SetProtectedCells",
            "HighlightRejected",
            "SetRejected(true)",
            "visual.Model.IsSolid",
        ),
    ))
    errors.extend(require_fragments(
        cell_visual_path,
        texts.get(cell_visual_path, ""),
        "rejected cell feedback",
        ("SetRejected", "Color.red", "_rejected"),
    ))
    errors.extend(require_fragments(
        world_session_path,
        texts.get(world_session_path, ""),
        "protected excavation boundary",
        (
            "ExcavationBoundaryPolicy",
            "topRockY: layout.SurfaceY + 1",
            "ProtectedCells",
            "world.excavation.protected_rock",
            "_boundaryPolicy.IsProtected(cell)",
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
            "_session!.IsProtected(target)",
            "_renderer!.HighlightRejected(target)",
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
        (
            "DigRockVolumeRenderer",
            "rockRenderer.Initialize",
            "worldRenderer.SetProtectedCells(worldSession.ProtectedCells)",
        ),
    ))
    return errors
