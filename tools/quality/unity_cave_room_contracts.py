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
    room_interaction = runtime_root / "DigWorldInteraction.CaveRooms.cs"
    room_runtime = runtime_root / "DigAgentSimulationDriverBase.CaveRooms.cs"
    room_floor = runtime_root / "DigCaveRoomFloorRenderer.cs"
    tunnel_renderer = runtime_root / "DigTunnelDemoRenderer.cs"
    depth_adapter = runtime_root / "DigWorldRenderer.DepthTerrain.cs"
    agent_session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    bootstrap = runtime_root / "DigUnityBootstrap.cs"
    errors: list[str] = []
    errors.extend(require_fragments(
        room_session,
        texts.get(room_session, ""),
        "completed cave room tracking and expansion",
        (
            "_caveRoomPlans",
            "LoadCompletedCaveRoomPlans",
            "GetCompletedCaveRoomPlans",
            "FrontExcavationCells.All",
            "!value.IsSolid",
            "GetCompletedCaveRoomPlans(snapshot)",
        ),
    ))
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
    errors.extend(require_fragments(
        room_runtime,
        texts.get(room_runtime, ""),
        "completed cave room and depth runtime activation",
        (
            "_activatedCaveRooms",
            "_terrainExcavatedVolume",
            "ExcavateTunnelDepth",
            "TunnelDepthExcavations",
            "RefreshCaveRoomRuntime",
            "RefreshTerrainDepthVolume",
            "CreateFloorCells",
            "ExpandTunnelVolume",
            "AddRoomFloor",
        ),
    ))
    errors.extend(require_fragments(
        room_floor,
        texts.get(room_floor, ""),
        "completed cave room floor and back wall rendering",
        (
            "AddRoomFloor",
            "plan.Preset.BaseWidth",
            "plan.Preset.Depth",
            "DigTunnelCellVisual",
            "FloorWorldPosition",
            "isVerticalTunnel: false",
            "material: _materials[z]!",
            "_backWalls",
            "ReplaceBackWall",
            "Cave room back wall",
            "DepthSpacing * 0.55f",
            "Destroy(collider)",
        ),
    ))
    errors.extend(require_fragments(
        tunnel_renderer,
        texts.get(tunnel_renderer, ""),
        "optional generated cave back wall",
        (
            "layout.CaveHasBackWall",
            "Cave back wall",
        ),
    ))
    errors.extend(require_fragments(
        depth_adapter,
        texts.get(depth_adapter, ""),
        "dynamic cave room terrain meshes",
        (
            "SetTerrainDepthVolume",
            "TerrainDepthVolumePresenter",
            "RefreshChunkedTerrain();",
        ),
    ))
    errors.extend(require_fragments(
        agent_session,
        texts.get(agent_session, ""),
        "dynamic cave room navigation",
        (
            "ExpandTunnelVolume",
            "WithAdditionalOpenCells",
            "_tunnelJournal",
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
