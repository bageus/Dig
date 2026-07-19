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
            "DigTerrainChunkMeshBuilder.Build(",
            "CalculateSignature(",
            "RebuildCount",
            "VertexCount",
            "TriangleCount",
            "MeshFilter",
        ),
    ))
    errors.extend(reject(
        chunk_path,
        chunk,
        ("MeshCollider", "BoxCollider", "AddComponent<Collider"),
    ))

    mesh_path, mesh = read("DigTerrainChunkMeshBuilder.cs")
    errors.extend(require(
        mesh_path,
        mesh,
        (
            "IsRenderedSolid",
            "ResolveOffset",
            "solidCells",
            "cutawayCells",
            "protectedCells",
        ),
    ))
    errors.extend(reject(
        mesh_path,
        mesh,
        ("UnityEngine.Random", "MeshCollider", "BoxCollider"),
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
            "SetTunnelDigInteractionActive",
            "renderer.enabled = _tunnelDigInteractionActive",
            "collider.enabled = _tunnelDigInteractionActive",
            "EnsureTerrainChunkRenderer().Render(",
        ),
    ))

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

    drawing_path, drawing = read("DigWorldInteraction.Excavation.cs")
    errors.extend(require(
        drawing_path,
        drawing,
        (
            "_renderer!.SetTunnelDigInteractionActive(",
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
