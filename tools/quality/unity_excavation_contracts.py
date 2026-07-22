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
    projection = runtime_root / "DigTunnelProjection.cs"
    tunnel_renderer = runtime_root / "DigTunnelDemoRenderer.cs"
    world_renderer = runtime_root / "DigWorldRenderer.cs"
    protection = runtime_root / "DigWorldRenderer.Protection.cs"
    cell_visual = runtime_root / "DigCellVisual.cs"
    movement = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    world_session = runtime_root / "DigWorldSession.cs"
    room_session = runtime_root / "DigWorldSession.CaveRooms.cs"
    room_protection = runtime_root / "DigWorldSession.CaveRoomProtection.cs"
    interaction = runtime_root / "DigWorldInteraction.cs"
    drawing = runtime_root / "DigWorldInteraction.Excavation.cs"
    drawing_defaults = runtime_root / "DigWorldInteraction.ExcavationDefaults.cs"
    room_interaction = runtime_root / "DigWorldInteraction.CaveRooms.cs"
    room_preview = runtime_root / "DigCaveRoomPreviewRenderer.cs"
    designations = runtime_root / "DigTerrainWorkDesignations.cs"
    manual = runtime_root / "DigTerrainWorkManualExcavation.cs"
    driver = runtime_root / "DigAgentSimulationDriverBase.Excavation.cs"
    room_driver = runtime_root / "DigAgentSimulationDriverBase.CaveRooms.cs"
    session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    hud = runtime_root / "DigHudOverlay.Excavation.cs"
    bootstrap = runtime_root / "DigUnityBootstrap.cs"
    errors: list[str] = []

    errors.extend(require_fragments(
        projection,
        texts.get(projection, ""),
        "embedded walk-surface projection",
        (
            "RockCellHalfExtent",
            "FloorThickness",
            "FloorWorldPosition",
            "WalkSurfaceY",
            "ResidentFootSink",
            "WalkSurfaceY(cellY) - ResidentFootSink",
        ),
    ))
    errors.extend(reject_fragments(
        projection,
        texts.get(projection, ""),
        "resident root lifted above its authored foot anchor",
        ("+ ResidentHalfHeight",),
    ))
    tunnel_text = texts.get(tunnel_renderer, "")
    errors.extend(require_fragments(
        tunnel_renderer,
        tunnel_text,
        "separate freeform movement surfaces and dig-only cell proxies",
        (
            "Freeform Tunnel Movement Surfaces",
            "Tunnel Dig Cell Proxies",
            "SetDigInteractionActive",
            "collider.enabled = !_digInteractionActive",
            "collider.enabled = _digInteractionActive",
            "DigTunnelProjection.CellWorldPosition(cell)",
            "TryGetMovementTarget",
            "internal bool TryGetCell(",
            "CellId cell,",
        ),
    ))
    errors.extend(reject_fragments(
        tunnel_renderer,
        tunnel_text,
        "visible shaft or synthetic cave geometry",
        (
            '"Shaft {cell}"',
            "CreateCaveShell",
            "Cave ceiling",
            "Cave back wall",
            "_verticalMaterial",
        ),
    ))
    errors.extend(require_fragments(
        world_renderer,
        texts.get(world_renderer, ""),
        "dynamic Z0 floor rendering",
        ("_walkSurfaceCells", "TryGetWalkSurface", "ApplyProtectedVisual"),
    ))
    errors.extend(require_fragments(
        movement,
        texts.get(movement, ""),
        "direct layered movement",
        ("_tunnelRenderer.TryGetCell", "CellId destination", "MoveResidentThroughTunnel"),
    ))
    errors.extend(require_fragments(
        protection,
        texts.get(protection, ""),
        "protected rock presentation",
        ("SetProtectedCells", "HighlightRejected", "SetRejected(true)", "visual.Model.IsSolid"),
    ))
    errors.extend(require_fragments(
        cell_visual,
        texts.get(cell_visual, ""),
        "rejected cell feedback",
        ("SetRejected", "Color.red", "_rejected"),
    ))
    errors.extend(require_fragments(
        world_session,
        texts.get(world_session, ""),
        "boundary and natural cave protection",
        (
            "ExcavationBoundaryPolicy",
            "InitializeNaturalCaveProtection(layout)",
            "ProtectedCells => LoadAllProtectedCells()",
            "_boundaryPolicy.IsProtected(cell) || IsNaturalCaveProtected(cell)",
        ),
    ))
    room_protection_text = texts.get(room_protection, "")
    errors.extend(require_fragments(
        room_protection,
        room_protection_text,
        "natural cave perimeter",
        (
            "NaturalCaveShellProtectionPolicy",
            "InitializeNaturalCaveProtection",
            "_naturalCaveProtectedCells",
            "LoadAllProtectedCells",
        ),
    ))
    errors.extend(reject_fragments(
        room_protection,
        room_protection_text,
        "player-dug room protection",
        (
            "CaveRoomShellProtectionPolicy",
            "SynchronizeCompletedCaveRoomProtection",
            "_caveRoomProtectedCells",
        ),
    ))
    errors.extend(reject_fragments(
        room_driver,
        texts.get(room_driver, ""),
        "player-dug room protection activation",
        ("SynchronizeCompletedCaveRoomProtection",),
    ))
    errors.extend(require_fragments(
        room_session,
        texts.get(room_session, ""),
        "cave room designation transaction",
        (
            "CaveRoomPlanner",
            "PlanCaveRoom",
            "ApplyCaveRoomPlan",
            "FrontExcavationCells",
            "RollBackDesignations",
        ),
    ))

    drawing_text = texts.get(drawing, "")
    errors.extend(require_fragments(
        drawing,
        drawing_text,
        "unified Z0 excavation drawing",
        (
            "DigExcavationDrawingMode.Tunnel",
            "DigExcavationDrawingMode.Delete",
            "ExcavationStrokePlanner",
            "TryHandleExcavationStroke",
            "AssignExcavationCluster",
            "_session!.IsProtected(target)",
            "_renderer!.HighlightRejected(target)",
        ),
    ))
    errors.extend(reject_fragments(
        drawing,
        drawing_text,
        "separate horizontal and vertical tools",
        ("DigExcavationDrawingMode.Horizontal", "DigExcavationDrawingMode.Vertical"),
    ))
    errors.extend(require_fragments(
        drawing_defaults,
        texts.get(drawing_defaults, ""),
        "base tunnel palette",
        (
            "EnsureDefaultExcavationDrawingMode",
            "DigExcavationDrawingMode.Tunnel",
            "CanSelectExcavationCells",
        ),
    ))
    errors.extend(require_fragments(
        room_interaction,
        texts.get(room_interaction, ""),
        "cave room hover and placement",
        (
            "CaveRoomPresetKind?",
            "SetCaveRoomPlanningPreset",
            "UpdateCaveRoomPreview",
            "TryHandleCaveRoomPlacement",
        ),
    ))
    errors.extend(require_fragments(
        room_preview,
        texts.get(room_preview, ""),
        "volumetric trapezoid preview",
        (
            "EdgeCount = 12",
            "CreateCorners",
            "preset.BaseWidth",
            "preset.TopWidth",
            "preset.Depth - 1",
        ),
    ))
    errors.extend(require_fragments(
        interaction,
        texts.get(interaction, ""),
        "explicit excavation routing",
        (
            "TryHandleCaveRoomPlacement()",
            "TryHandleTunnelDepthExcavation()",
            "TryHandleExcavationStroke()",
        ),
    ))
    errors.extend(require_fragments(
        designations,
        texts.get(designations, ""),
        "frontier-only automatic excavation",
        (
            "IsExcavationFrontier",
            "NoCandidates",
            "Math.Abs(agent.CellZ - target.Z)",
            "IsManualExcavationJob(job.Id)",
        ),
    ))
    errors.extend(require_fragments(
        manual,
        texts.get(manual, ""),
        "manual excavation cluster",
        ("radius: 4", "AssignSpecificJobCommand", "ContinueManualExcavation", "AssignNextManualExcavation"),
    ))
    errors.extend(require_fragments(
        driver,
        texts.get(driver, ""),
        "excavation application adapter",
        (
            "ApplyExcavationDesignation",
            "ApplyCaveRoomPlan",
            "AssignExcavationClusterToResidents",
            "SynchronizeDesignations",
            "CancelManualTunnelMovement",
        ),
    ))

    session_text = texts.get(session, "")
    errors.extend(require_fragments(
        session,
        session_text,
        "authoritative manual movement cancellation",
        ("CancelManualTunnelMovement", "_manualTunnelMovements.Remove"),
    ))
    errors.extend(reject_fragments(
        session,
        session_text,
        "legacy manual movement release",
        ("ReleaseManualTunnelOrder", "_manualTunnelOrders.Remove"),
    ))

    errors.extend(require_fragments(
        hud,
        texts.get(hud, ""),
        "legacy excavation adapter controls",
        ('GUILayout.Button("Tunnel"', 'GUILayout.Button("Depth"', 'GUILayout.Button("Delete"'),
    ))
    errors.extend(require_fragments(
        bootstrap,
        texts.get(bootstrap, ""),
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
