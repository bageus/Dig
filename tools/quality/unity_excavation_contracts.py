from __future__ import annotations

from pathlib import Path
from typing import Callable

from unity_cave_room_contracts import check_cave_room_runtime_contracts
from unity_depth_terrain_contracts import check_depth_terrain_contracts

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
    projection_path = runtime_root / "DigTunnelProjection.cs"
    tunnel_renderer_path = runtime_root / "DigTunnelDemoRenderer.cs"
    world_renderer_path = runtime_root / "DigWorldRenderer.cs"
    protection_path = runtime_root / "DigWorldRenderer.Protection.cs"
    cell_visual_path = runtime_root / "DigCellVisual.cs"
    movement_path = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    world_session_path = runtime_root / "DigWorldSession.cs"
    room_session_path = runtime_root / "DigWorldSession.CaveRooms.cs"
    room_protection_path = runtime_root / "DigWorldSession.CaveRoomProtection.cs"
    interaction_path = runtime_root / "DigWorldInteraction.cs"
    drawing_path = runtime_root / "DigWorldInteraction.Excavation.cs"
    drawing_defaults_path = runtime_root / "DigWorldInteraction.ExcavationDefaults.cs"
    room_interaction_path = runtime_root / "DigWorldInteraction.CaveRooms.cs"
    room_preview_path = runtime_root / "DigCaveRoomPreviewRenderer.cs"
    designations_path = runtime_root / "DigTerrainWorkDesignations.cs"
    manual_path = runtime_root / "DigTerrainWorkManualExcavation.cs"
    driver_path = runtime_root / "DigAgentSimulationDriverBase.Excavation.cs"
    room_driver_path = runtime_root / "DigAgentSimulationDriverBase.CaveRooms.cs"
    session_path = runtime_root / "DigAgentSession.TunnelMovement.cs"
    hud_path = runtime_root / "DigHudOverlay.Excavation.cs"
    bootstrap_path = runtime_root / "DigUnityBootstrap.cs"
    errors: list[str] = []

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

    tunnel_renderer = texts.get(tunnel_renderer_path, "")
    errors.extend(require_fragments(
        tunnel_renderer_path,
        tunnel_renderer,
        "depth-floor rendering without shaft markers",
        (
            "if (vertical || cell.Z == 0",
            "DigTunnelProjection.FloorWorldPosition(cell)",
            "DigTunnelProjection.FloorThickness",
            "DigTunnelProjection.FloorDepth",
            "Layered Tunnel Floors",
        ),
    ))
    errors.extend(reject_fragments(
        tunnel_renderer_path,
        tunnel_renderer,
        "synthetic initial shaft and cave frame visuals",
        (
            '"Shaft {cell}"',
            "Cave ceiling",
            "Cave back wall",
            "CreateCaveShell",
            "_verticalMaterial",
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
            "partial class DigWorldSession",
            "ExcavationBoundaryPolicy",
            "topRockY: layout.SurfaceY + 1",
            "ProtectedCells => LoadAllProtectedCells()",
            "world.excavation.protected_rock",
            "_boundaryPolicy.IsProtected(cell) || IsCaveRoomProtected(cell)",
        ),
    ))
    errors.extend(require_fragments(
        room_protection_path,
        texts.get(room_protection_path, ""),
        "completed cave room perimeter protection",
        (
            "CaveRoomShellProtectionPolicy",
            "SynchronizeCompletedCaveRoomProtection",
            "_caveRoomProtectedCells",
            "LoadAllProtectedCells",
        ),
    ))
    errors.extend(require_fragments(
        room_driver_path,
        texts.get(room_driver_path, ""),
        "completed cave room protection activation",
        (
            "WorldSession!.SynchronizeCompletedCaveRoomProtection(completedPlans);",
            "WorldRenderer!.SetProtectedCells(WorldSession.ProtectedCells);",
        ),
    ))
    errors.extend(require_fragments(
        room_session_path,
        texts.get(room_session_path, ""),
        "cave room designation transaction",
        (
            "CaveRoomPlanner",
            "PlanCaveRoom",
            "ApplyCaveRoomPlan",
            "FrontExcavationCells",
            "RollBackDesignations",
        ),
    ))

    drawing = texts.get(drawing_path, "")
    errors.extend(require_fragments(
        drawing_path,
        drawing,
        "unified Z0 excavation drawing",
        (
            "DigExcavationDrawingMode.Tunnel",
            "DigExcavationDrawingMode.Delete",
            "ExcavationStrokePlanner",
            "Input.GetMouseButton(0)",
            "TryHandleExcavationStroke",
            "AssignExcavationCluster",
            "GetSelectedModels()",
            "new SpatialCellId(resident.CellX, resident.CellY, 0)",
            "_session!.IsProtected(target)",
            "_renderer!.HighlightRejected(target)",
            "DisableCaveRoomPlanning",
        ),
    ))
    errors.extend(reject_fragments(
        drawing_path,
        drawing,
        "separate horizontal and vertical tools",
        (
            "DigExcavationDrawingMode.Horizontal",
            "DigExcavationDrawingMode.Vertical",
        ),
    ))
    errors.extend(require_fragments(
        drawing_defaults_path,
        texts.get(drawing_defaults_path, ""),
        "base tunnel palette",
        (
            "EnsureDefaultExcavationDrawingMode",
            "DigExcavationDrawingMode.Tunnel",
            "CanSelectExcavationCells",
        ),
    ))
    errors.extend(require_fragments(
        room_interaction_path,
        texts.get(room_interaction_path, ""),
        "cave room hover and placement",
        (
            "CaveRoomPresetKind?",
            "SetCaveRoomPlanningPreset",
            "UpdateCaveRoomPreview",
            "PlanCaveRoom",
            "TryHandleCaveRoomPlacement",
            "ApplyCaveRoomPlan",
        ),
    ))
    errors.extend(require_fragments(
        room_preview_path,
        texts.get(room_preview_path, ""),
        "volumetric trapezoid preview",
        (
            "EdgeCount = 12",
            "CreateCorners",
            "preset.BaseWidth",
            "preset.TopWidth",
            "preset.Depth - 1",
            "_validMaterial",
            "_invalidMaterial",
        ),
    ))

    interaction = texts.get(interaction_path, "")
    errors.extend(require_fragments(
        interaction_path,
        interaction,
        "explicit excavation routing",
        (
            "UpdateCaveRoomPreview()",
            "TryHandleCaveRoomPlacement()",
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
            "ApplyCaveRoomPlan",
            "ReleaseManualTunnelOrder(residentIds[index])",
            "AssignExcavationClusterToResidents",
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

    hud = texts.get(hud_path, "")
    errors.extend(require_fragments(
        hud_path,
        hud,
        "legacy excavation adapter controls",
        (
            "EnsureDefaultExcavationDrawingMode",
            'GUILayout.Button("Tunnel"',
            'GUILayout.Button("Depth"',
            'GUILayout.Button("Delete"',
        ),
    ))
    errors.extend(reject_fragments(
        hud_path,
        hud,
        "removed excavation HUD controls",
        (
            'GUILayout.Button("Off"',
            'GUILayout.Button("P-"',
            'GUILayout.Button("P+"',
        ),
    ))
    errors.extend(require_fragments(
        bootstrap_path,
        texts.get(bootstrap_path, ""),
        "room preview composition",
        (
            "worldRenderer.SetProtectedCells(worldSession.ProtectedCells)",
            "DigCaveRoomPreviewRenderer",
            "SetCaveRoomRenderers",
        ),
    ))
    errors.extend(check_depth_terrain_contracts(
        runtime_root,
        texts,
        require_fragments,
        reject_fragments,
    ))
    errors.extend(check_cave_room_runtime_contracts(
        runtime_root,
        texts,
        require_fragments,
    ))
    return errors
