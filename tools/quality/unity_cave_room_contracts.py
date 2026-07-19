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
    rock_renderer = runtime_root / "DigRockVolumeRenderer.cs"
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
            "GetCompletedCaveRoomPlans(snapshot)",
            "completedPlans: completed",
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
            "ExcavateTunnelDepth",
            "TunnelDepthExcavations",
            "RefreshCaveRoomRuntime",
            "CreateFloorCells",
            "ExpandTunnelVolume",
            "SetExcavatedCells",
            "AddRoomFloor",
            "SynchronizeCompletedCaveRoomProtection",
        ),
    ))
    room_floor_text = texts.get(room_floor, "")
    errors.extend(require_fragments(
        room_floor,
        room_floor_text,
        "ordinary completed cave room floor rendering",
        (
            "AddRoomFloor",
            "plan.Preset.BaseWidth",
            "plan.Preset.Depth",
            "DigTunnelCellVisual",
            "FloorWorldPosition",
            "isVerticalTunnel: false",
            "material: _materials[z]!",
        ),
    ))
    for fragment in (
        "_backWalls",
        "ReplaceBackWall",
        "Cave room back wall",
        "_backWallMaterial",
    ):
        if fragment in room_floor_text:
            errors.append(
                f"{room_floor.relative_to(runtime_root.parent.parent.parent.parent.parent)}: "
                f"synthetic completed-room back wall is forbidden: {fragment!r}"
            )

    tunnel_text = texts.get(tunnel_renderer, "")
    errors.extend(require_fragments(
        tunnel_renderer,
        tunnel_text,
        "depth-floor-only tunnel visuals",
        (
            "if (vertical || cell.Z == 0",
            "Walkable plane",
            "FloorWorldPosition",
            "Layered Tunnel Floors",
        ),
    ))
    for fragment in (
        "CreateCaveShell",
        "CreateShellPart",
        "Cave back wall",
        "_caveMaterial",
        "_verticalMaterial",
        '"Shaft {cell}"',
    ):
        if fragment in tunnel_text:
            errors.append(
                f"{tunnel_renderer}: synthetic shaft or cave-shell visual is forbidden: "
                f"{fragment!r}"
            )

    errors.extend(require_fragments(
        rock_renderer,
        texts.get(rock_renderer, ""),
        "dynamic cave room rock meshes",
        (
            "SetExcavatedCells",
            "_excavatedCells.SetEquals",
            "Rebuild",
            "_excavatedCells.Contains(cell)",
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
            "rockRenderer",
        ),
    ))
    return errors
