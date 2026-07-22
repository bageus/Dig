#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RUNTIME = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime"


def read(name: str) -> tuple[Path, str]:
    path = RUNTIME / name
    return path, path.read_text(encoding="utf-8-sig") if path.exists() else ""


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing terrain chunk contract {fragment!r}"
        for fragment in fragments if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden terrain chunk fragment {fragment!r}"
        for fragment in fragments if fragment in text
    ]


def main() -> int:
    errors: list[str] = []
    chunk_path, chunk = read("DigTerrainChunkRenderer.cs")
    errors.extend(require(chunk_path, chunk, (
        "DigTerrainRenderSnapshot snapshot",
        "snapshot.IsDirty(chunk.Key)",
        "DigTerrainChunkMeshBuilder.Build(",
        "CalculateSignature(",
        "RebuildCount",
        "VertexCount",
        "TriangleCount",
    )))

    builder_path, builder = read("DigTerrainRenderSnapshotBuilder.cs")
    errors.extend(require(builder_path, builder, (
        "int depth = world.Depth;",
        "AddWorldChunks(",
        "new DigTerrainChunkKey(source.X, source.Y, source.Z)",
        "DigTerrainRenderCell.FromWorld(",
        "sourceCell.Z",
        "ToCellKeys(cutawayCells)",
        "ToCellKeys(protectedCells)",
        "MarkChunkAndNeighbours",
    )))
    errors.extend(reject(builder_path, builder, (
        "TerrainDepthVolumeViewModel",
        "AddDepthChunks(",
        "z: 0",
        "ToDepthZero",
    )))

    renderer_path, renderer = read("DigWorldRenderer.cs")
    errors.extend(require(renderer_path, renderer, (
        "Dictionary<CellId, DigCellVisual>",
        "Dictionary<ChunkId, Transform>",
        "new CellId(cell.X, cell.Y, cell.Z)",
        "SelectAt(int x, int y, int z)",
        "RefreshChunkedTerrain(world)",
    )))
    errors.extend(reject(renderer_path, renderer, (
        "Dictionary<Vector2Int, DigCellVisual>",
        "new Vector2Int(cell.X, cell.Y)",
    )))

    catalog_path, catalog = read("DigWorldRenderer.VisualCatalog.cs")
    errors.extend(require(catalog_path, catalog, (
        "RefreshChunkedTerrain(WorldViewModel world)",
        "_terrainSnapshotBuilder.Build(",
        "_terrainDepositDecorations",
        "_tunnelCutaway",
        "_protectedCells",
        "EnsureTerrainChunkRenderer().Render(snapshot, terrainVisualCatalog)",
        "SetTunnelDigInteractionActive",
        "renderer.enabled = _tunnelDigInteractionActive",
        "collider.enabled = visual.gameObject.activeSelf",
        "_tunnelDigInteractionActive",
        "visual.Model.IsDesignated",
    )))
    errors.extend(reject(catalog_path, catalog, (
        "_terrainDepthVolume",
        "SetTerrainDepthVolume",
        "TerrainDepthVolumePresenter",
        "private void LateUpdate()",
        "EnsureTerrainChunkRenderer().Render(\n                _cells",
    )))

    cell_path, cell = read("DigCellVisual.cs")
    errors.extend(require(cell_path, cell, (
        "DisableInteractionCollider",
        "collider.enabled = false;",
    )))

    world_path, world = read("DigWorldRenderer.cs")
    errors.extend(require(world_path, world, (
        "RefreshChunkedTerrain(world);",
        "RefreshChunkedTerrain();",
    )))

    depth_adapter_path = RUNTIME / "DigWorldRenderer.DepthTerrain.cs"
    if depth_adapter_path.exists():
        errors.append(
            f"{depth_adapter_path.relative_to(ROOT)}: separate depth terrain adapter must remain removed"
        )

    rock_path = RUNTIME / "DigRockVolumeRenderer.cs"
    if rock_path.exists():
        errors.append(
            f"{rock_path.relative_to(ROOT)}: separate rock renderer must remain removed"
        )

    interaction_path, interaction = read("DigWorldInteraction.cs")
    errors.extend(reject(interaction_path, interaction, (
        "_renderer!.TryGetCell",
        "_renderer.TryGetCell",
        "ContextWorldTargetKind.Ground",
    )))

    movement_path, movement = read("DigWorldInteraction.TunnelMovement.cs")
    errors.extend(reject(movement_path, movement, (
        "_renderer.TryGetWalkSurface",
        "_renderer!.TryGetWalkSurface",
    )))

    room_path, room = read("DigWorldInteraction.CaveRooms.cs")
    errors.extend(require(room_path, room, (
        "SetCaveRoomPlanningPreset",
        "SetTunnelDigInteractionActive(active: true)",
        "_renderer!.TryGetCell",
    )))

    drawing_path, drawing = read("DigWorldInteraction.Excavation.cs")
    errors.extend(require(drawing_path, drawing, (
        "SetTunnelDigInteractionActive(UsesTunnelCellInteraction(mode))",
        "UsesTunnelCellInteraction",
        "_renderer!.TryGetCell",
    )))

    if errors:
        print("Unity terrain chunk contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("PASS: authoritative XYZ terrain chunks and interaction proxies")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
