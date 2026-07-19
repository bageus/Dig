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
        f"{path.relative_to(ROOT)}: missing terrain profile contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden terrain profile fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []

    roles_path, roles = read("DigTerrainSurfaceRole.cs")
    errors.extend(require(
        roles_path,
        roles,
        (
            "DigTerrainProfileKind",
            "Sand",
            "Stone",
            "MetalBearing",
            "Crystalline",
            "Lava",
            "Unmineable",
            "DigTerrainSurfaceRole",
            "Floor",
            "Wall",
            "Ceiling",
            "FreshCut",
        ),
    ))

    profile_path, profile = read("DigTerrainVisualProfile.cs")
    errors.extend(require(
        profile_path,
        profile,
        (
            "string stableId",
            "DigTerrainProfileKind kind",
            "Material? floorMaterial",
            "Material? wallMaterial",
            "Material? ceilingMaterial",
            "Material? freshCutMaterial",
            "Resolve(DigTerrainSurfaceRole role)",
            "AppendValidation",
        ),
    ))
    errors.extend(reject(
        profile_path,
        profile,
        ("GameObject", "Collider", "UnityEngine.Random"),
    ))

    catalog_path, catalog = read("DigTerrainVisualCatalog.cs")
    errors.extend(require(
        catalog_path,
        catalog,
        (
            "DigTerrainVisualProfile[] profiles",
            "Dictionary<string, DigTerrainVisualProfile>",
            "ResolveSurface(",
            "profile.Resolve(role)",
            "override IReadOnlyList<string> ValidateCatalog()",
            "RequireKind(DigTerrainProfileKind.Sand",
            "RequireKind(DigTerrainProfileKind.Stone",
            "RequireKind(DigTerrainProfileKind.MetalBearing",
            "RequireKind(DigTerrainProfileKind.Crystalline",
            "RequireKind(DigTerrainProfileKind.Lava",
            "RequireKind(DigTerrainProfileKind.Unmineable",
        ),
    ))
    errors.extend(reject(
        catalog_path,
        catalog,
        ("switch (stableId", "GameObject.CreatePrimitive", "AddComponent<Collider"),
    ))

    base_path, base_catalog = read("DigVisualCatalog.cs")
    errors.extend(require(
        base_path,
        base_catalog,
        (
            "public virtual IReadOnlyList<string> ValidateCatalog()",
            "protected virtual void OnEnable()",
            "protected virtual void OnValidate()",
        ),
    ))

    key_path, key = read("DigTerrainMaterialKey.cs")
    errors.extend(require(
        key_path,
        key,
        (
            "DigTerrainSurfaceRole role",
            "Role = role;",
            "internal DigTerrainSurfaceRole Role",
            "Role == other.Role",
        ),
    ))

    mesh_path, mesh = read("DigTerrainChunkMeshBuilder.cs")
    errors.extend(require(
        mesh_path,
        mesh,
        (
            "GetFaceSubmesh",
            "snapshot.IsCutaway(neighbour)",
            "DigTerrainSurfaceRole.FreshCut",
            "DigTerrainSurfaceRole.Floor",
            "DigTerrainSurfaceRole.Wall",
            "DigTerrainSurfaceRole.Ceiling",
        ),
    ))

    renderer_path, renderer = read("DigTerrainChunkRenderer.cs")
    errors.extend(require(
        renderer_path,
        renderer,
        (
            "catalog.ResolveSurface(key.MaterialId, key.Role)",
            "ResolveSolidFallbackColor",
            "switch (key.Role)",
        ),
    ))

    if errors:
        print("Unity terrain visual profile contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: Unity terrain visual profile contracts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
