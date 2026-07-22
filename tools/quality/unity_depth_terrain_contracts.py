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
    renderer_path = runtime_root / "DigWorldRenderer.cs"
    builder_path = runtime_root / "DigTerrainRenderSnapshotBuilder.cs"
    driver_path = runtime_root / "DigAgentSimulationDriverBase.CaveRooms.cs"
    bootstrap_path = runtime_root / "DigUnityBootstrap.cs"

    errors = require_fragments(
        renderer_path,
        texts.get(renderer_path, ""),
        "authoritative XYZ terrain projection",
        (
            "Dictionary<CellId, DigCellVisual>",
            "Dictionary<ChunkId, Transform>",
            "new CellId(cell.X, cell.Y, cell.Z)",
            "DigTunnelProjection.CellWorldPosition(cellId)",
            "DigTunnelProjection.FloorWorldPosition(cellId)",
            "foreach (CellId cell in volume.Cells)",
        ),
    )
    errors.extend(require_fragments(
        builder_path,
        texts.get(builder_path, ""),
        "world-derived depth terrain chunks",
        (
            "int depth = world.Depth;",
            "AddWorldChunks(",
            "new DigTerrainChunkKey(source.X, source.Y, source.Z)",
            "sourceCell.Z",
            "ToCellKeys(cutawayCells)",
            "ToCellKeys(protectedCells)",
        ),
    ))
    errors.extend(require_fragments(
        driver_path,
        texts.get(driver_path, ""),
        "authoritative World excavation refresh",
        (
            "WorldSession!.ExcavateSpatialCell(commit.Target)",
            "AgentSession!.CompleteTunnelDepthExcavation(",
            "WorldSession.LoadSnapshot()",
            "WorldRenderer!.Render(WorldSession.LoadView())",
            "WorldSession!.ActivateCaveRoomVolume(plan)",
        ),
    ))
    errors.extend(require_fragments(
        bootstrap_path,
        texts.get(bootstrap_path, ""),
        "single World terrain composition",
        (
            "worldRenderer.SetTunnelCutaway(agentSession.TunnelVolume)",
            "worldRenderer.SetTerrainDeposits(worldSession.LoadTerrainDeposits())",
            "worldRenderer.Render(world)",
            "SetCaveRoomRenderers",
        ),
    ))

    forbidden = (
        "TerrainDepthVolumePresenter",
        "TerrainDepthVolumeViewModel",
        "SetTerrainDepthVolume",
        "AddDepthChunks",
        "_terrainDepthVolume",
        "_terrainExcavatedVolume",
        "RefreshTerrainDepthVolume",
    )
    for path in (renderer_path, builder_path, driver_path, bootstrap_path):
        errors.extend(reject_fragments(
            path,
            texts.get(path, ""),
            "parallel depth terrain ownership",
            forbidden,
        ))
    return errors
