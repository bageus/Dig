#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRESENTATION = ROOT / "src" / "Dig.Presentation.Abstractions" / "Buildings"
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
        f"{path.relative_to(ROOT)}: missing building visual contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden building visual fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []

    state_path = PRESENTATION / "BuildingVisualState.cs"
    state = read(state_path)
    errors.extend(require(
        state_path,
        state,
        (
            "BuildingVisualState",
            "BuildingBox",
            "Assembly",
            "Completed",
            "Damaged",
            "Packing",
            "BuildingVisualStateResolver",
            "if (isPacking)",
        ),
    ))
    errors.extend(reject(
        state_path,
        state,
        ("UnityEngine", "GameObject", "ICommand", "Handle("),
    ))

    world_path = PRESENTATION / "BuildingWorldModels.cs"
    world = read(world_path)
    errors.extend(require(
        world_path,
        world,
        (
            "BuildingOrientation Orientation",
            "WorkPositionX",
            "WorkPositionY",
            "CompletedWork",
            "RequiredWork",
            "AssemblyProgress",
            "BuildingVisualState VisualState",
            "snapshot.Orientation",
            "snapshot.WorkPosition.X",
            "snapshot.CompletedWork",
            "snapshot.Definition.RequiredWork",
        ),
    ))
    errors.extend(reject(
        world_path,
        world,
        ("UnityEngine", "GameObject", "Transform", "Material"),
    ))

    anchor_path = RUNTIME / "DigBuildingAnchor.cs"
    anchor = read(anchor_path)
    errors.extend(require(
        anchor_path,
        anchor,
        (
            "DigBuildingAnchorKind",
            "Worker",
            "Visitor",
            "Input",
            "Output",
            "Storage",
            "Vfx",
            "DigBuildingAnchorMask",
            "string stableId",
        ),
    ))

    authoring_path = RUNTIME / "DigBuildingPrefabAuthoring.cs"
    authoring = read(authoring_path)
    errors.extend(require(
        authoring_path,
        authoring,
        (
            "Vector2Int footprintSize",
            "Vector2 pivotCell",
            "BuildingOrientation authoredOrientation",
            "DigBuildingAnchor[] anchors",
            "authoredOrientation != BuildingOrientation.North",
            "ResolveSelectionColliders()",
            "selection collider",
            "ValidateAnchors",
            "requiredAnchors & ~available",
            "outside ModelRoot",
        ),
    ))
    errors.extend(reject(
        authoring_path,
        authoring,
        ("ICommand", "BuildingSnapshot", "BuildingPlacementValidator"),
    ))

    profile_path = RUNTIME / "DigBuildingVisualProfile.cs"
    profile = read(profile_path)
    errors.extend(require(
        profile_path,
        profile,
        (
            "DigBuildingProfileKind",
            "Campfire",
            "Furnace",
            "Storage",
            "buildingBoxPrefab",
            "assemblyPrefab",
            "completedPrefab",
            "damagedPrefab",
            "packingPrefab",
            "DigBuildingPrefabAuthoring",
            "AppendValidation",
            "DigBuildingVisualResolution",
        ),
    ))
    errors.extend(reject(
        profile_path,
        profile,
        ("BuildingState", "BuildingsState", "ICommand", "Handle("),
    ))

    catalog_path = RUNTIME / "DigBuildingVisualCatalog.cs"
    catalog = read(catalog_path)
    errors.extend(require(
        catalog_path,
        catalog,
        (
            "DigBuildingVisualProfile[] profiles",
            "ResolveBuilding(",
            "BuildingVisualState state",
            "RequireKind(DigBuildingProfileKind.Campfire",
            "RequireKind(DigBuildingProfileKind.Furnace",
            "RequireKind(DigBuildingProfileKind.Storage",
            "Duplicate building profile id",
        ),
    ))

    visual_path = RUNTIME / "DigBuildingVisual.cs"
    visual = read(visual_path)
    errors.extend(require(
        visual_path,
        visual,
        (
            "DigVisualPrefabFactory.Create(",
            "PrimitiveType.Cube",
            "InvalidateAsset()",
            "Model.Orientation",
            "Model.AssemblyProgress",
            "Model.Functions.PackingProgress",
            "BuildingVisualState.Assembly",
            "BuildingVisualState.Packing",
            "ResolveLocalBounds",
            "SetSelected(bool selected)",
        ),
    ))
    errors.extend(reject(
        visual_path,
        visual,
        (
            "GameObject.CreatePrimitive",
            "for (int index = 0; index < Model.Footprint.Count; index++)\n            {\n                GameObject",
            "ICommand",
            "BuildingState",
        ),
    ))

    renderer_path = RUNTIME / "DigBuildingRenderer.cs"
    renderer = read(renderer_path)
    errors.extend(require(
        renderer_path,
        renderer,
        (
            "Resources.Load<DigBuildingVisualCatalog>",
            "visualCatalog.ResolveBuilding(",
            "model.DefinitionId",
            "model.VisualState",
            "visual.InvalidateAsset();",
            "SelectById",
            "InstanceCount",
        ),
    ))
    errors.extend(reject(
        renderer_path,
        renderer,
        (
            "GameObject.CreatePrimitive",
            "new Material(",
            "_normalMaterial",
            "_selectedMaterial",
        ),
    ))

    ghost_path = RUNTIME / "DigBuildingBoxGhostRenderer.cs"
    ghost = read(ghost_path)
    errors.extend(require(
        ghost_path,
        ghost,
        (
            "visualCatalog.ResolveBuilding(",
            "BuildingVisualState.BuildingBox",
            "DigVisualPrefabFactory.Create(",
            "preview.Origin.X",
            "preview.Orientation",
            "preview.Footprint",
            "preview.WorkPosition",
            "DisableColliders(_previewInstance)",
            "SetLayerRecursively(_previewInstance, layer: 2)",
        ),
    ))
    errors.extend(reject(
        ghost_path,
        ghost,
        (
            "Dictionary<CellId, GameObject>",
            "_cells",
            "BuildingPlacementValidator",
            "ICommand",
        ),
    ))

    factory_path = RUNTIME / "DigVisualPrefabFactory.cs"
    factory = read(factory_path)
    errors.extend(require(
        factory_path,
        factory,
        (
            "asset.Prefab == null",
            "Object.Instantiate(asset.Prefab)",
            "DigVisualPrefabRoot",
            "DigVisualTintTarget",
        ),
    ))

    for path, text in (
        (anchor_path, anchor),
        (authoring_path, authoring),
        (profile_path, profile),
        (catalog_path, catalog),
        (visual_path, visual),
        (renderer_path, renderer),
        (ghost_path, ghost),
    ):
        errors.extend(reject(
            path,
            text,
            ("UnityEngine.Random", "FindObjectOfType<", "FindObjectsOfType<"),
        ))

    if errors:
        print("Unity building visual contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: Unity building visual contracts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
