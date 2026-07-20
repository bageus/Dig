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
        f"{path.relative_to(ROOT)}: missing cave template trim contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden cave template trim fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []

    view_path = PRESENTATION / "CaveTemplateTrimViewModels.cs"
    view = read(view_path)
    errors.extend(require(
        view_path,
        view,
        (
            "CaveTemplateTrimRole",
            "Entrance",
            "Arch",
            "SideWall",
            "BackWall",
            "CaveTemplateTrimRowViewModel",
            "CaveTemplateTrimInstanceViewModel",
            "CaveTemplateTrimVolumeViewModel",
            "TemplateId",
            "InstanceId",
            "ArchDepths",
            "HasBackWall",
            "Maximum",
        ),
    ))
    errors.extend(reject(
        view_path,
        view,
        ("UnityEngine", "GameObject", "Collider", "_tunnelCutaway"),
    ))

    presenter_path = PRESENTATION / "CaveTemplateTrimPresenter.cs"
    presenter = read(presenter_path)
    errors.extend(require(
        presenter_path,
        presenter,
        (
            "IReadOnlyCollection<CaveRoomPlan> completedPlans",
            "CaveTemplateTrimVolumeViewModel.Empty()",
            '"cave.template.small"',
            '"cave.template.medium"',
            '"cave.template.large"',
            '"cave.template.tall"',
            "CaveRoomPlanner.InterpolateWidth",
            "preset.Depth - 1",
            "ResolveVariant",
            "CalculateVersion",
        ),
    ))
    errors.extend(reject(
        presenter_path,
        presenter,
        (
            "UnityEngine",
            "UnityEngine.Random",
            "TunnelNavigationVolume",
            "TerrainDepthVolumeViewModel",
            "cutaway",
        ),
    ))

    profile_path = RUNTIME / "DigCaveTemplateVisualProfile.cs"
    profile = read(profile_path)
    errors.extend(require(
        profile_path,
        profile,
        (
            "DigCaveTemplateProfileKind",
            "Small",
            "Medium",
            "Large",
            "Tall",
            "Material? entranceMaterial",
            "Material? archMaterial",
            "Material? sideWallMaterial",
            "Material? backWallMaterial",
            "Resolve(CaveTemplateTrimRole role)",
        ),
    ))
    errors.extend(reject(
        profile_path,
        profile,
        ("GameObject", "Collider", "UnityEngine.Random"),
    ))

    catalog_path = RUNTIME / "DigTerrainVisualCatalog.CaveTemplates.cs"
    catalog = read(catalog_path)
    errors.extend(require(
        catalog_path,
        catalog,
        (
            "DigCaveTemplateVisualProfile[] caveTemplateProfiles",
            "ResolveCaveTemplate(",
            "AppendCaveTemplateValidation",
            "RequireCaveTemplateKind(DigCaveTemplateProfileKind.Small",
            "RequireCaveTemplateKind(DigCaveTemplateProfileKind.Medium",
            "RequireCaveTemplateKind(DigCaveTemplateProfileKind.Large",
            "RequireCaveTemplateKind(DigCaveTemplateProfileKind.Tall",
        ),
    ))

    main_catalog_path = RUNTIME / "DigTerrainVisualCatalog.cs"
    main_catalog = read(main_catalog_path)
    errors.extend(require(
        main_catalog_path,
        main_catalog,
        (
            "AppendCaveTemplateValidation(errors);",
            "ResetCaveTemplateProfileLookup();",
        ),
    ))

    builder_path = RUNTIME / "DigCaveTemplateTrimMeshBuilder.cs"
    builder = read(builder_path)
    errors.extend(require(
        builder_path,
        builder,
        (
            "DigCaveTemplateTrimMeshData Build(",
            "CaveTemplateTrimRole.Entrance",
            "CaveTemplateTrimRole.Arch",
            "CaveTemplateTrimRole.SideWall",
            "CaveTemplateTrimRole.BackWall",
            "instance.ArchDepths",
            "AddSideWalls",
            "AddBackWall",
            "instance.Variant",
        ),
    ))
    errors.extend(reject(
        builder_path,
        builder,
        (
            "GameObject",
            "Collider",
            "MeshCollider",
            "BoxCollider",
            "UnityEngine.Random",
            "_tunnelCutaway",
        ),
    ))

    geometry_path = RUNTIME / "DigCaveTemplateTrimMeshBuilder.Geometry.cs"
    geometry = read(geometry_path)
    errors.extend(require(
        geometry_path,
        geometry,
        ("AddPlaneRibbon", "AddDoubleSidedQuad", "AddQuad"),
    ))
    errors.extend(reject(
        geometry_path,
        geometry,
        ("GameObject", "Collider", "UnityEngine.Random"),
    ))

    renderer_path = RUNTIME / "DigCaveTemplateTrimRenderer.cs"
    renderer = read(renderer_path)
    errors.extend(require(
        renderer_path,
        renderer,
        (
            "Dictionary<string, DigCaveTemplateTrimVisual>",
            "CaveTemplateTrimVolumeViewModel volume",
            "DigCaveTemplateTrimMeshBuilder.Build(instance)",
            "catalog?.ResolveCaveTemplate(templateId, role)",
            "CalculateSignature(instance)",
            "Cave Template Trim Visuals",
            "InstanceCount",
            "VertexCount",
            "TriangleCount",
        ),
    ))
    errors.extend(reject(
        renderer_path,
        renderer,
        (
            "GameObject.CreatePrimitive",
            "AddComponent<Collider",
            "MeshCollider",
            "BoxCollider",
            "DigTunnelCellVisual",
            "UnityEngine.Random",
        ),
    ))

    adapter_path = RUNTIME / "DigWorldRenderer.CaveTemplateTrims.cs"
    adapter = read(adapter_path)
    errors.extend(require(
        adapter_path,
        adapter,
        (
            "CaveTemplateTrimVolumeViewModel.Empty()",
            "SetCaveTemplateTrims",
            "RefreshCaveTemplateTrims",
            "EnsureCaveTemplateTrimRenderer().Render(",
            "terrainVisualCatalog",
        ),
    ))

    runtime_path = RUNTIME / "DigAgentSimulationDriverBase.CaveRooms.cs"
    runtime = read(runtime_path)
    errors.extend(require(
        runtime_path,
        runtime,
        (
            "CaveTemplateTrimPresenter",
            "_caveTemplateTrimPresenter.Present(completedPlans)",
            "WorldRenderer.SetCaveTemplateTrims(",
            "IReadOnlyList<CaveRoomPlan> completedPlans",
        ),
    ))
    errors.extend(reject(
        runtime_path,
        runtime,
        ("_tunnelCutaway", "CreateCaveShell", "CreateShellPart"),
    ))

    if errors:
        print("Unity cave template trim contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: Unity cave template trim contracts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
