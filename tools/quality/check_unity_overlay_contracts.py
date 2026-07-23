#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRESENTATION = ROOT / "src" / "Dig.Presentation.Abstractions" / "Overlays"
RUNTIME = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""


def combined(prefix: str) -> str:
    return "\n".join(read(path) for path in sorted(RUNTIME.glob(prefix + "*.cs")))


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing overlay contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden overlay fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []

    enums_path = PRESENTATION / "OverlayEnums.cs"
    enums = read(enums_path)
    errors.extend(require(enums_path, enums, (
        "OverlayLayerKind", "Selection", "Preview", "Jobs", "Routes",
        "OverlaySemanticKind", "PreviewValid", "PreviewInvalid",
        "JobBlocked", "JobAttention", "OverlayShapeKind", "Cross",
        "OverlayPatternKind", "CrossHatch", "OverlayVisibilityProfile",
        "Release", "Debug", "All",
    )))

    definitions = "\n".join(
        read(path) for path in sorted(PRESENTATION.glob("*.cs"))
    )
    errors.extend(require(PRESENTATION, definitions, (
        "new OverlayLayerDefinition(OverlayLayerKind.Selection, 900",
        "new OverlayLayerDefinition(OverlayLayerKind.Preview, 800",
        "new OverlayLayerDefinition(OverlayLayerKind.Jobs, 500",
        "new OverlayLayerDefinition(OverlayLayerKind.Routes, 300, true",
        "OverlayShapeKind.Diamond, OverlayPatternKind.Double",
        "OverlayShapeKind.Cross, OverlayPatternKind.CrossHatch",
        "profile == OverlayVisibilityProfile.Release && definition.DebugOnly",
        "IReadOnlyDictionary<OverlayLayerKind, bool>? visibilityOverrides",
        "OverlayVisibilitySnapshot",
    )))
    errors.extend(reject(PRESENTATION, definitions, (
        "UnityEngine", "GameObject", "Material", "MonoBehaviour",
        "ICommand", "Handle(", "DateTime", "Guid.NewGuid",
    )))

    manager_path = RUNTIME / "DigOverlayManager.cs"
    manager = combined("DigOverlayManager")
    errors.extend(require(manager_path, manager, (
        "Dictionary<OverlayLayerKind, List<Transform>>",
        "RegisterLayer(OverlayLayerKind layer, Transform root)",
        "ResolveSortingOrder(OverlayLayerKind layer)",
        "SetVisibilityProfile(OverlayVisibilityProfile profile)",
        "ToggleLayer(OverlayLayerKind layer)",
        "Input.GetKeyDown(KeyCode.F2)",
        "Input.GetKeyDown(KeyCode.F3)",
        "Input.GetKeyDown(KeyCode.F4)",
        "DigRenderMaterialLibrary",
        "RenderMaterialSemantic.Overlay",
        "RenderSurfaceKind.Overlay",
        "MaterialPropertyBlock",
        "_colorProperties.SetColor(\"_BaseColor\"",
        "ConfigureLineRenderer(",
        "DigOverlayMetadata",
    )))
    errors.extend(reject(manager_path, manager, (
        "ICommand", "InventoryState", "JobSystem", "BuildingState",
        "FindObjectOfType<", "FindObjectsOfType<",
        "Shader.Find(", "new Material(",
    )))

    job_path = RUNTIME / "DigJobRenderer.cs"
    job = read(job_path) + read(RUNTIME / "DigJobVisual.cs")
    errors.extend(require(job_path, job, (
        "RegisterLayer(OverlayLayerKind.Jobs",
        "ConfigureRenderer(",
        "OverlaySemanticKind.Selection",
        "OverlaySemanticKind.JobBlocked",
        "OverlaySemanticKind.JobAttention",
        "appearance.Shape == OverlayShapeKind.Cross",
    )))
    errors.extend(reject(job_path, job, (
        "new Material(", "_statusMaterials", "KeyCode.F3",
        "Job Diagnostic Overlay [F3]",
    )))

    route_path = RUNTIME / "DigNavigationRouteRenderer.cs"
    route = read(route_path)
    errors.extend(require(route_path, route, (
        "RegisterLayer(OverlayLayerKind.Routes",
        "ConfigureLineRenderer(",
        "OverlaySemanticKind.Route",
        "UnregisterLayer(OverlayLayerKind.Routes",
    )))
    errors.extend(reject(route_path, route, (
        "new Material(", "KeyCode.F4", "Navigation Routes [F4]",
    )))

    cave_path = RUNTIME / "DigCaveRoomPreviewRenderer.cs"
    cave = combined("DigCaveRoomPreviewRenderer")
    errors.extend(require(cave_path, cave, (
        "EdgeCount = 12",
        "OverlaySemanticKind.PreviewValid",
        "RegisterLayer(OverlayLayerKind.Preview",
        "while (_edges.Count < EdgeCount)",
        "RoomPreviewColor",
        "UpdateFill(corners)",
    )))
    errors.extend(reject(cave_path, cave, (
        "new Material(", "_validMaterial", "_invalidMaterial",
        "OverlaySemanticKind.PreviewInvalid",
        "invalid cross",
    )))

    metadata_path = RUNTIME / "DigOverlayMetadata.cs"
    metadata = read(metadata_path)
    errors.extend(require(metadata_path, metadata, (
        "OverlayLayerKind", "OverlaySemanticKind", "OverlayShapeKind",
        "OverlayPatternKind", "Configure(",
    )))

    if errors:
        print("Unity overlay contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: unified overlay priorities, styles, visibility and renderer reuse")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
