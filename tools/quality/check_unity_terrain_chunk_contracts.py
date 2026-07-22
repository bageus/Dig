#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RUNTIME = (
    ROOT
    / "unity"
    / "Dig.Unity"
    / "Assets"
    / "Dig.Unity"
    / "Runtime"
)


def read(name: str) -> tuple[Path, str]:
    path = RUNTIME / name
    if not path.exists():
        return path, ""
    return path, path.read_text(encoding="utf-8-sig")


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing terrain chunk contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden terrain interaction fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []

    chunk_path, chunk = read("DigTerrainChunkRenderer.cs")
    errors.extend(require(
        chunk_path,
        chunk,
        (
            "DigTerrainRenderSnapshot snapshot",
            "snapshot.IsDirty(chunk.Key)",
            "DigTerrainChunkMeshBuilder.Build(",
            "CalculateSignature(",
            "RebuildCount",
            "VertexCount",
            "TriangleCount",
            "visual.ResolveMesh()",
        ),
    ))
    errors.extend(reject(
        chunk_path,
        chunk,
        (
            "DigCellVisual",
            "Vector2Int",
            "_cellsByChunk",
            "PrepareChunkCells",
            "MeshCollider",
            "BoxCollider",
            "AddComponent<Collider",
        ),
    ))

    mesh_path, mesh = read("DigTerrainChunkMeshBuilder.cs")
    errors.extend(require(
        mesh_path,
        mesh,
        (
            "internal static partial class DigTerrainChunkMeshBuilder",
            "DigTerrainRenderChunk chunk",
            "DigTerrainRenderSnapshot snapshot",
            "position.Offset(-1, 0, 0)",
            "position.Offset(1, 0, 0)",
            "position.Offset(0, -1, 0)",
            "position.Offset(0, 1, 0)",
            "position.Offset(0, 0, -1)",
            "position.Offset(0, 0, 1)",
            "snapshot.IsCutaway(neighbour)",
            "DigTerrainSurfaceRole.Floor",
            "DigTerrainSurfaceRole.Wall",
            "DigTerrainSurfaceRole.Ceiling",
            "DigTerrainSurfaceRole.FreshCut",
            "private const float HalfExtent = 0.5f;",
        ),
    ))
    errors.extend(reject(
        mesh_path,
        mesh,
        ("UnityEngine.Random", "MeshCollider", "BoxCollider", "DigCellVisual"),
    ))

    geometry_path, geometry = read("DigTerrainChunkMeshBuilder.Geometry.cs")
    errors.extend(require(
        geometry_path,
        geometry,
        (
            "ResolveDepthExtents",
            "DigTunnelProjection.DepthSpacing",
            "AddFace(",
            "ResolvePlaneOffset",
            "int plane = salt switch",
        ),
    ))
    errors.extend(reject(
        geometry_path,
        geometry,
        (
            "UnityEngine.Random",
            "MeshCollider",
            "BoxCollider",
            "DigCellVisual",
            "cell.X * 73856093",
        ),
    ))

    snapshot_path, snapshot = read("DigTerrainRenderSnapshot.cs")
    errors.extend(require(
        snapshot_path,
        snapshot,
        (
            "IsRenderedSolid",
            "IsDirty",
            "DigTerrainChunkKey",
            "DigTerrainCellKey",
        ),
    ))

    builder_path, builder = read("DigTerrainRenderSnapshotBuilder.cs")
    errors.extend(require(
        builder_path,
        builder,
        (
            "TerrainDepthVolumeViewModel? depthVolume",
            "WorldViewModel world",
            "DigTerrainRenderCell.FromWorld(",
            "z: 0",
            "AddDepthChunks(",
            "MarkChangedCells",
            "MarkChunkAndNeighbours",
            "chunk.Offset(-1, 0, 0)",
            "chunk.Offset(1, 0, 0)",
            "chunk.Offset(0, -1, 0)",
            "chunk.Offset(0, 1, 0)",
            "chunk.Offset(0, 0, -1)",
            "chunk.Offset(0, 0, 1)",
            "FloorDivide",
        ),
    ))
    errors.extend(reject(
        builder_path,
        builder,
        ("CurrentAuthoritativeDepth = 1",),
    ))

    depth_path, depth = read("DigTerrainRenderSnapshotBuilder.Depth.cs")
    errors.extend(require(
        depth_path,
        depth,
        (
            "depthVolume.SolidCells",
            "depthVolume.SolidMaterialId",
            "depthVolume.Hardness",
            "CalculateDepthChunkVersion",
            "ChunkForCell(key, chunkSize)",
        ),
    ))

    depth_adapter_path, depth_adapter = read("DigWorldRenderer.DepthTerrain.cs")
    errors.extend(require(
        depth_adapter_path,
        depth_adapter,
        (
            "TerrainDepthVolumePresenter",
            "SetTerrainDepthVolume",
            "_terrainDepthPresenter.Present(",
            "RefreshChunkedTerrain();",
        ),
    ))

    cell_path, cell = read("DigCellVisual.cs")
    errors.extend(require(
        cell_path,
        cell,
        (
            "DisableInteractionCollider",
            "collider.enabled = false;",
        ),
    ))

    adapter_path, adapter = read("DigWorldRenderer.VisualCatalog.cs")
    errors.extend(require(
        adapter_path,
        adapter,
        (
            "RefreshChunkedTerrain(WorldViewModel world)",
            "_terrainSnapshotBuilder.Build(",
            "_terrainDepthVolume",
            "EnsureTerrainChunkRenderer().Render(snapshot, terrainVisualCatalog)",
            "SetTunnelDigInteractionActive",
            "renderer.enabled = _tunnelDigInteractionActive",
            "collider.enabled = visual.gameObject.activeSelf",
            "_tunnelDigInteractionActive",
            "visual.Model.IsDesignated",
        ),
    ))
    errors.extend(reject(
        adapter_path,
        adapter,
        (
            "private void LateUpdate()",
            "EnsureTerrainChunkRenderer().Render(\n                _cells",
        ),
    ))

    world_path, world = read("DigWorldRenderer.cs")
    errors.extend(require(
        world_path,
        world,
        (
            "RefreshChunkedTerrain(world);",
            "RefreshChunkedTerrain();",
        ),
    ))

    rock_path = RUNTIME / "DigRockVolumeRenderer.cs"
    if rock_path.exists():
        errors.append(
            f"{rock_path.relative_to(ROOT)}: separate rock renderer must remain removed"
        )

    interaction_path, interaction = read("DigWorldInteraction.cs")
    errors.extend(reject(
        interaction_path,
        interaction,
        (
            "_renderer!.TryGetCell",
            "_renderer.TryGetCell",
            "ContextWorldTargetKind.Ground",
        ),
    ))

    movement_path, movement = read("DigWorldInteraction.TunnelMovement.cs")
    errors.extend(reject(
        movement_path,
        movement,
        ("_renderer.TryGetWalkSurface", "_renderer!.TryGetWalkSurface"),
    ))

    room_path, room = read("DigWorldInteraction.CaveRooms.cs")
    errors.extend(require(
        room_path,
        room,
        (
            "SetCaveRoomPlanningPreset",
            "SetTunnelDigInteractionActive(active: true)",
            "_renderer!.TryGetCell",
        ),
    ))

    drawing_path, drawing = read("DigWorldInteraction.Excavation.cs")
    errors.extend(require(
        drawing_path,
        drawing,
        (
            "SetTunnelDigInteractionActive(UsesTunnelCellInteraction(mode))",
            "UsesTunnelCellInteraction",
            "_renderer!.TryGetCell",
        ),
    ))

    if errors:
        print("Unity terrain chunk contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: Unity terrain chunk contracts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
