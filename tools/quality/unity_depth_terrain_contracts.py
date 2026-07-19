from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]
RejectFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_depth_terrain_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
    reject_fragments: RejectFragments,
) -> list[str]:
    adapter_path = runtime_root / "DigWorldRenderer.DepthTerrain.cs"
    builder_path = runtime_root / "DigTerrainRenderSnapshotBuilder.Depth.cs"
    session_path = runtime_root / "DigWorldSession.cs"
    driver_path = runtime_root / "DigAgentSimulationDriverBase.CaveRooms.cs"
    interaction_path = runtime_root / "DigWorldInteraction.CaveRooms.cs"
    bootstrap_path = runtime_root / "DigUnityBootstrap.cs"

    errors = require_fragments(
        adapter_path,
        texts.get(adapter_path, ""),
        "unified depth terrain adapter",
        (
            "TerrainDepthVolumePresenter",
            "SetTerrainDepthVolume",
            "RefreshChunkedTerrain();",
        ),
    )
    errors.extend(require_fragments(
        builder_path,
        texts.get(builder_path, ""),
        "depth terrain chunk projection",
        (
            "TerrainDepthVolumeViewModel",
            "AddDepthChunks",
            "CalculateDepthChunkVersion",
            "DigTerrainChunkKey",
        ),
    ))
    errors.extend(require_fragments(
        session_path,
        texts.get(session_path, ""),
        "depth terrain material facts",
        (
            "SolidMaterialId => _solidMaterialId",
            "SolidHardness => _solidHardness",
        ),
    ))
    errors.extend(require_fragments(
        driver_path,
        texts.get(driver_path, ""),
        "depth terrain excavation refresh",
        (
            "_terrainExcavatedVolume",
            "RefreshTerrainDepthVolume",
            "WorldRenderer.SetTerrainDepthVolume(",
        ),
    ))
    errors.extend(reject_fragments(
        interaction_path,
        texts.get(interaction_path, ""),
        "separate cave rock renderer",
        ("DigRockVolumeRenderer", "_caveRoomRockRenderer"),
    ))

    bootstrap = texts.get(bootstrap_path, "")
    errors.extend(require_fragments(
        bootstrap_path,
        bootstrap,
        "unified rock volume composition",
        (
            "worldRenderer.SetTerrainDepthVolume(",
            "worldSession.SolidMaterialId.ToString()",
            "Array.Empty<SpatialCellId>()",
            "SetCaveRoomRenderers",
        ),
    ))
    errors.extend(reject_fragments(
        bootstrap_path,
        bootstrap,
        "separate rock volume composition",
        ("DigRockVolumeRenderer", "rockRenderer.Initialize"),
    ))
    return errors
