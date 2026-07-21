from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_cave_room_runtime_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
) -> list[str]:
    room_session = runtime_root / "DigWorldSession.CaveRooms.cs"
    protection = runtime_root / "DigWorldSession.CaveRoomProtection.cs"
    room_interaction = runtime_root / "DigWorldInteraction.CaveRooms.cs"
    room_runtime = runtime_root / "DigAgentSimulationDriverBase.CaveRooms.cs"
    room_floor = runtime_root / "DigCaveRoomFloorRenderer.cs"
    tunnel_renderer = runtime_root / "DigTunnelDemoRenderer.cs"
    world_renderer = runtime_root / "DigWorldRenderer.cs"
    depth_adapter = runtime_root / "DigWorldRenderer.DepthTerrain.cs"
    agent_session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    bootstrap = runtime_root / "DigUnityBootstrap.cs"
    errors: list[str] = []
    errors.extend(require_fragments(
        room_session,
        texts.get(room_session, ""),
        "completed cave room tracking and immutability",
        (
            "_caveRoomPlans",
            "LoadCompletedCaveRoomPlans",
            "GetCompletedCaveRoomPlans",
            "FrontExcavationCells.All",
            "!value.IsSolid",
            "IReadOnlyList<CaveRoomPlan> completed",
            "completed);",
        ),
    ))
    protection_text = texts.get(protection, "")
    errors.extend(require_fragments(
        protection,
        protection_text,
        "natural cave outer perimeter protection",
        (
            "NaturalCaveShellProtectionPolicy",
            "InitializeNaturalCaveProtection",
            "_naturalCaveProtectedCells",
            "IsNaturalCaveProtected",
            "LoadAllProtectedCells",
        ),
    ))
    for fragment in (
        "CaveRoomShellProtectionPolicy",
        "SynchronizeCompletedCaveRoomProtection",
        "_caveRoomProtectedCells",
    ):
        if fragment in protection_text:
            errors.append(f"{protection}: player room protection remains: {fragment!r}")
    errors.extend(require_fragments(
        room_interaction,
        texts.get(room_interaction, ""),
        "tick-bound cave room activation",
        (
            "SetCaveRoomRenderers",
            "RefreshCompletedCaveRooms",
            "bool force = false",
            "_lastCaveRoomRuntimeTick",
            "LoadCompletedCaveRoomPlans",
            "RefreshCaveRoomRuntime",
        ),
    ))
    runtime_text = texts.get(room_runtime, "")
    errors.extend(require_fragments(
        room_runtime,
        runtime_text,
        "ordinary player room and spatial depth runtime activation",
        (
            "_activatedCaveRooms",
            "_terrainExcavatedVolume",
            "DesignateTunnelDepth",
            "DesignateSpatialExcavation(",
            "CompleteSpatialExcavation",
            "CompleteTunnelDepthExcavation(commit.Target)",
            "CompleteSpatialExcavationJob(",
            "TunnelDepthExcavations",
            "RefreshCaveRoomRuntime",
            "RefreshTerrainDepthVolume",
            "CreateFloorCells",
            "ExpandTunnelVolume",
            "AddRoomFloor",
        ),
    ))
    for forbidden in (
        "AgentSession.ExcavateTunnelDepth",
        "internal TunnelDepthExcavationPlanResult ExcavateTunnelDepth",
    ):
        if forbidden in runtime_text:
            errors.append(f"{room_runtime}: instant depth mutation remains: {forbidden!r}")
    if "SynchronizeCompletedCaveRoomProtection" in runtime_text:
        errors.append(f"{room_runtime}: player-dug rooms must not become protected")
    room_floor_text = texts.get(room_floor, "")
    errors.extend(require_fragments(
        room_floor,
        room_floor_text,
        "completed cave room dig-only interaction proxies",
        (
            "AddRoomFloor",
            "plan.Preset.BaseWidth",
            "plan.Preset.Depth",
            "DigTunnelCellVisual",
            "FloorWorldPosition",
            "isVerticalTunnel: false",
            "SetDigInteractionActive",
            "collider.enabled = _digInteractionActive",
        ),
    ))
    for fragment in ("_backWalls", "ReplaceBackWall", "Cave room back wall", "_backWallMaterial"):
        if fragment in room_floor_text:
            errors.append(f"{room_floor}: obsolete room wall visual remains: {fragment!r}")
    for fragment in ("GameObject.CreatePrimitive", "new Material(", "GetComponent<Renderer>"):
        if fragment in room_floor_text:
            errors.append(f"{room_floor}: visible per-cell room floor remains: {fragment!r}")

    tunnel_text = texts.get(tunnel_renderer, "")
    errors.extend(require_fragments(
        tunnel_renderer,
        tunnel_text,
        "renderer-free movement surfaces and exact dig proxies",
        (
            "Freeform Tunnel Movement Surfaces",
            "Tunnel Dig Cell Proxies",
            "TryGetMovementTarget",
            "SetDigInteractionActive",
            "RebuildMovementSurfaces",
        ),
    ))
    for fragment in (
        "CreateCaveShell", "CreateShellPart", "Cave back wall",
        "_caveMaterial", "_verticalMaterial", "Walkable plane",
        "GameObject.CreatePrimitive",
    ):
        if fragment in tunnel_text:
            errors.append(f"{tunnel_renderer}: obsolete tunnel visual remains: {fragment!r}")

    errors.extend(require_fragments(
        world_renderer,
        texts.get(world_renderer, ""),
        "natural cave cutaway preserves the protected Z0 ceiling",
        (
            "for (int x = layout.CaveMinX; x <= layout.CaveMaxX; x++)",
            "for (int y = layout.CaveCeilingY + 1; y <= layout.CaveFloorY; y++)",
            "_tunnelCutaway.Add(new Vector2Int(x, y));",
        ),
    ))
    errors.extend(require_fragments(
        depth_adapter,
        texts.get(depth_adapter, ""),
        "dynamic cave room terrain meshes",
        ("SetTerrainDepthVolume", "TerrainDepthVolumePresenter", "RefreshChunkedTerrain();"),
    ))
    errors.extend(require_fragments(
        agent_session,
        texts.get(agent_session, ""),
        "dynamic cave room navigation and deferred depth opening",
        (
            "ExpandTunnelVolume",
            "WithAdditionalOpenCells",
            "_tunnelJournal",
            "CompleteTunnelDepthExcavation",
        ),
    ))
    errors.extend(require_fragments(
        bootstrap,
        texts.get(bootstrap, ""),
        "completed cave room composition",
        (
            "DigCaveRoomFloorRenderer",
            "SetCaveRoomRenderers",
            "caveRoomFloorRenderer",
            "worldRenderer.SetTerrainDepthVolume(",
        ),
    ))
    return errors
