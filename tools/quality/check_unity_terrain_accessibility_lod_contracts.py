#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRESENTATION = ROOT / "src" / "Dig.Presentation.Abstractions" / "World"
RUNTIME = (
    ROOT
    / "unity"
    / "Dig.Unity"
    / "Assets"
    / "Dig.Unity"
    / "Runtime"
)


def read(path: Path) -> str:
    if not path.exists():
        return ""
    return path.read_text(encoding="utf-8-sig")


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing accessibility/LOD contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden accessibility/LOD fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []

    policy_path = PRESENTATION / "TerrainVisualDetailPolicy.cs"
    policy = read(policy_path)
    errors.extend(require(
        policy_path,
        policy,
        (
            "TerrainVisualDetailLevel",
            "Marker = 0",
            "Reduced = 1",
            "Full = 2",
            "DefaultReducedExitPixelsPerCell = 8f",
            "DefaultReducedEnterPixelsPerCell = 12f",
            "DefaultFullExitPixelsPerCell = 22f",
            "DefaultFullEnterPixelsPerCell = 28f",
            "Resolve(",
            "current",
            "pixelsPerCell >= _fullEnter",
            "pixelsPerCell >= _reducedEnter",
        ),
    ))
    errors.extend(reject(
        policy_path,
        policy,
        ("UnityEngine", "Camera", "GameObject", "Collider"),
    ))

    profile_path = RUNTIME / "DigTerrainDepositVisualProfile.cs"
    profile = read(profile_path)
    errors.extend(require(
        profile_path,
        profile,
        (
            "DigTerrainDepositShape",
            "Nodule",
            "Plate",
            "Crystal",
            "Seam",
            "Pebble",
            "DigTerrainDepositProfileKind.Iron",
            "DigTerrainDepositProfileKind.Gold",
            "DigTerrainDepositProfileKind.Crystal",
            "DigTerrainDepositProfileKind.Coal",
            "return DigTerrainDepositShape.Pebble",
        ),
    ))

    catalog_path = RUNTIME / "DigTerrainVisualCatalog.Deposits.cs"
    catalog = read(catalog_path)
    errors.extend(require(
        catalog_path,
        catalog,
        (
            "ResolveDepositShape",
            "profile?.Shape",
            "ResolveFallbackDepositShape",
            "hash % 5u",
        ),
    ))

    builder_path = RUNTIME / "DigTerrainChunkMeshBuilder.cs"
    builder = read(builder_path)
    errors.extend(require(
        builder_path,
        builder,
        (
            "DigTerrainVisualCatalog? catalog",
            "TerrainVisualDetailLevel detailLevel",
            "Enum.IsDefined(typeof(TerrainVisualDetailLevel)",
            "catalog,",
            "detailLevel);",
        ),
    ))

    topology_path = RUNTIME / "DigTerrainChunkMeshBuilder.DepositDecorations.cs"
    topology = read(topology_path)
    errors.extend(require(
        topology_path,
        topology,
        (
            "catalog?.ResolveDepositShape(",
            "ResolveFallbackDepositShape",
            "TerrainVisualDetailLevel.Full",
            "TerrainVisualDetailLevel.Reduced ? 1 : 0",
            "AddDepositCluster(",
            "AddDepositConnectors(",
        ),
    ))

    shapes_path = RUNTIME / "DigTerrainChunkMeshBuilder.DepositDecorationGeometry.cs"
    shapes = read(shapes_path)
    errors.extend(require(
        shapes_path,
        shapes,
        (
            "AddNodule",
            "AddPlate",
            "AddCrystal",
            "AddSeam",
            "AddPebbles",
            "damageHeight",
            "TerrainVisualDetailLevel.Marker",
            "TerrainVisualDetailLevel.Full ? 3 : 2",
        ),
    ))

    primitives_path = (
        RUNTIME / "DigTerrainChunkMeshBuilder.DepositDecorationPrimitives.cs"
    )
    primitives = read(primitives_path)
    errors.extend(require(
        primitives_path,
        primitives,
        ("AddDepositPyramid", "AddDepositFlatQuad", "AddDecorationTriangle"),
    ))

    connectors_path = (
        RUNTIME / "DigTerrainChunkMeshBuilder.DepositDecorationConnectors.cs"
    )
    connectors = read(connectors_path)
    errors.extend(require(
        connectors_path,
        connectors,
        (
            "AddDepositConnectors",
            "int maximumConnectors",
            "added < maximumConnectors",
            "AddDepositConnector",
        ),
    ))

    for path, text in (
        (topology_path, topology),
        (shapes_path, shapes),
        (primitives_path, primitives),
        (connectors_path, connectors),
    ):
        errors.extend(reject(
            path,
            text,
            ("GameObject", "Collider", "UnityEngine.Random", "Instantiate("),
        ))

    renderer_path = RUNTIME / "DigTerrainChunkRenderer.cs"
    renderer = read(renderer_path)
    errors.extend(require(
        renderer_path,
        renderer,
        (
            "DigTerrainRenderSnapshot? _lastSnapshot",
            "TerrainVisualDetailLevel _detailLevel",
            "SetDetailLevel",
            "Mix(ref hash, (ulong)detailLevel, prime)",
            "DigTerrainChunkMeshBuilder.Build(",
            "catalog,",
            "_detailLevel);",
        ),
    ))

    lod_path = RUNTIME / "DigWorldRenderer.VisualLod.cs"
    lod = read(lod_path)
    errors.extend(require(
        lod_path,
        lod,
        (
            "TerrainVisualDetailPolicy",
            "LateUpdate",
            "Camera.main",
            "WorldToScreenPoint",
            "TransformPoint(Vector3.right)",
            "TransformPoint(Vector3.forward)",
            "if (next == _terrainVisualDetailLevel)",
            "_terrainChunkRenderer?.SetDetailLevel(next)",
            "_caveTemplateTrimRenderer?.SetDetailLevel(next)",
        ),
    ))
    errors.extend(reject(
        lod_path,
        lod,
        ("_cells", "foreach", "GameObject", "Collider", "Physics.Raycast"),
    ))

    cave_builder_path = RUNTIME / "DigCaveTemplateTrimMeshBuilder.cs"
    cave_builder = read(cave_builder_path)
    errors.extend(require(
        cave_builder_path,
        cave_builder,
        (
            "TerrainVisualDetailLevel detailLevel",
            "detailLevel != TerrainVisualDetailLevel.Marker",
            "detailLevel == TerrainVisualDetailLevel.Reduced",
            "index % 2 != 0",
            "AddSideWalls",
            "AddBackWall",
        ),
    ))

    cave_renderer_path = RUNTIME / "DigCaveTemplateTrimRenderer.cs"
    cave_renderer = read(cave_renderer_path)
    errors.extend(require(
        cave_renderer_path,
        cave_renderer,
        (
            "CaveTemplateTrimVolumeViewModel? _lastVolume",
            "SetDetailLevel",
            "CalculateSignature(instance, _detailLevel)",
            "DigCaveTemplateTrimMeshBuilder.Build(instance, _detailLevel)",
            "Mix(ref hash, (ulong)detailLevel, prime)",
        ),
    ))
    errors.extend(reject(
        cave_renderer_path,
        cave_renderer,
        ("MeshCollider", "BoxCollider", "UnityEngine.Random"),
    ))

    if errors:
        print("Unity terrain accessibility/LOD contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: Unity terrain accessibility and LOD contracts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
