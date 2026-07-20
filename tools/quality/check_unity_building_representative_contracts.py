#!/usr/bin/env python3
from __future__ import annotations

import json
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
PACK = (
    ROOT
    / "unity"
    / "Dig.Unity"
    / "Assets"
    / "Dig.Unity"
    / "Resources"
    / "Dig"
    / "VisualCatalogs"
    / "RepresentativeBuildings.json"
)

TRIANGLES = {
    "Box": 12,
    "Pyramid": 6,
    "Octahedron": 8,
    "Wedge": 8,
}
REQUIRED_IDS = {
    "Campfire": {"kitchen.campfire", "building.campfire"},
    "Furnace": {"building.furnace", "building.forge", "demo.workshop.box"},
    "Storage": {"building.arsenal", "building.storage"},
}
REQUIRED_ANCHORS = {
    "Campfire": {"Worker", "Input", "Output", "Vfx"},
    "Furnace": {"Worker", "Input", "Output", "Vfx"},
    "Storage": {"Worker", "Visitor", "Input", "Output", "Storage"},
}


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing representative contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden representative fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def validate_pack() -> list[str]:
    if not PACK.exists():
        return [f"{PACK.relative_to(ROOT)}: representative building pack is missing"]

    try:
        document = json.loads(PACK.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        return [f"{PACK.relative_to(ROOT)}: invalid JSON: {error}"]

    errors: list[str] = []
    profiles = document.get("profiles")
    if not isinstance(profiles, list) or not profiles:
        return [f"{PACK.relative_to(ROOT)}: profiles must be a non-empty list"]

    seen_ids: set[str] = set()
    seen_kinds: set[str] = set()
    for index, profile in enumerate(profiles):
        if not isinstance(profile, dict):
            errors.append(f"profile {index}: expected an object")
            continue
        kind = profile.get("kind")
        ids = profile.get("stableIds")
        parts = profile.get("parts")
        anchors = profile.get("anchors")
        if kind not in REQUIRED_IDS:
            errors.append(f"profile {index}: invalid kind {kind!r}")
            continue
        seen_kinds.add(kind)
        id_set = set(ids) if isinstance(ids, list) else set()
        missing_ids = REQUIRED_IDS[kind] - id_set
        if missing_ids:
            errors.append(f"profile {index}: missing aliases {sorted(missing_ids)}")
        for stable_id in id_set:
            if not isinstance(stable_id, str) or not stable_id.strip():
                errors.append(f"profile {index}: empty stable id")
            elif stable_id in seen_ids:
                errors.append(f"profile {index}: duplicate stable id {stable_id!r}")
            else:
                seen_ids.add(stable_id)

        renderer_budget = profile.get("maxRenderers")
        triangle_budget = profile.get("maxTriangles")
        if not isinstance(renderer_budget, int) or not 0 < renderer_budget <= 16:
            errors.append(f"profile {index}: maxRenderers must be 1..16")
        if not isinstance(triangle_budget, int) or not 0 < triangle_budget <= 512:
            errors.append(f"profile {index}: maxTriangles must be 1..512")

        if not isinstance(parts, list) or not parts:
            errors.append(f"profile {index}: parts must be non-empty")
        else:
            marker_count = sum(part.get("detail") == "Marker" for part in parts)
            triangle_count = sum(TRIANGLES.get(part.get("shape"), 10000) for part in parts)
            if marker_count == 0:
                errors.append(f"profile {index}: no Marker silhouette")
            if isinstance(renderer_budget, int) and len(parts) > renderer_budget:
                errors.append(f"profile {index}: renderer budget exceeded")
            if isinstance(triangle_budget, int) and triangle_count > triangle_budget:
                errors.append(f"profile {index}: triangle budget exceeded")

        if not isinstance(anchors, list) or not anchors:
            errors.append(f"profile {index}: anchors must be non-empty")
        else:
            anchor_kinds = {anchor.get("kind") for anchor in anchors}
            missing_anchors = REQUIRED_ANCHORS[kind] - anchor_kinds
            if missing_anchors:
                errors.append(
                    f"profile {index}: missing anchor kinds {sorted(missing_anchors)}"
                )
            anchor_ids = [anchor.get("stableId") for anchor in anchors]
            if any(not isinstance(value, str) or not value.strip() for value in anchor_ids):
                errors.append(f"profile {index}: anchor stable ids are required")
            if len(anchor_ids) != len(set(anchor_ids)):
                errors.append(f"profile {index}: anchor stable ids must be unique")

    missing_kinds = set(REQUIRED_IDS) - seen_kinds
    if missing_kinds:
        errors.append(f"missing representative kinds {sorted(missing_kinds)}")
    return [f"{PACK.relative_to(ROOT)}: {error}" for error in errors]


def validate_runtime() -> list[str]:
    errors: list[str] = []
    data_path = RUNTIME / "DigRepresentativeBuildingData.cs"
    data = read(data_path)
    errors.extend(require(data_path, data, (
        "HardRendererLimit = 16",
        "HardTriangleLimit = 512",
        "markerCount == 0",
        "ResolveAnchorMask",
    )))

    library_path = RUNTIME / "DigRepresentativeBuildingPrefabLibrary.cs"
    library = read(library_path)
    errors.extend(require(library_path, library, (
        "Resources.Load<TextAsset>",
        "JsonUtility.FromJson",
        "enableInstancing = true",
        "Dictionary<string, GameObject>",
        "TryResolve(",
    )))
    errors.extend(reject(library_path, library, (
        "GameObject.CreatePrimitive",
        "UnityEngine.Random",
        "StaticBatchingUtility",
    )))

    templates_path = RUNTIME / "DigRepresentativeBuildingPrefabLibrary.Templates.cs"
    templates = read(templates_path)
    errors.extend(require(templates_path, templates, (
        "MeshFilter",
        "MeshRenderer",
        "DigBuildingPrefabAuthoring",
        "ConfigureRuntime(",
        "DigBuildingAnchor",
        "BuildingVisualState.BuildingBox",
        "BuildingVisualState.Packing",
        "BuildingVisualState.Damaged",
    )))
    errors.extend(reject(templates_path, templates, (
        "GameObject.CreatePrimitive",
        "ICommand",
        "BuildingState",
        "StaticBatchingUtility",
    )))

    lod_path = RUNTIME / "DigBuildingRenderer.Lod.cs"
    lod = read(lod_path)
    errors.extend(require(lod_path, lod, (
        "TerrainVisualDetailPolicy",
        "BuildingPixelsPerCell",
        "BuildingVisibleRendererCount",
        "BuildingVisibleTriangleCount",
        "BuildingRebuildCount",
        "visual.SetDetailLevel(next)",
    )))

    visual_path = RUNTIME / "DigBuildingVisual.cs"
    visual = read(visual_path)
    errors.extend(require(visual_path, visual, (
        "DigBuildingDetailGroup[]",
        "GetIndexCount(0)",
        "SetDetailLevel(",
        "ApplyDetailLevel()",
    )))

    ghost_path = RUNTIME / "DigBuildingBoxGhostRenderer.Representatives.cs"
    ghost = read(ghost_path)
    errors.extend(require(ghost_path, ghost, (
        "DigRepresentativeBuildingPrefabLibrary.Acquire()",
        "_representatives.TryResolve(",
        "BuildingVisualState.BuildingBox",
    )))
    return errors


def main() -> int:
    errors = validate_pack()
    errors.extend(validate_runtime())
    if errors:
        print("Unity representative building contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("PASS: representative building content, LOD and budgets")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
