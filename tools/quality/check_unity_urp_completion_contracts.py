#!/usr/bin/env python3
from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
UNITY = ROOT / "unity" / "Dig.Unity"
RUNTIME = UNITY / "Assets" / "Dig.Unity" / "Runtime"
EDITOR = UNITY / "Assets" / "Dig.Unity" / "Editor"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""


def require(path: Path, fragments: tuple[str, ...]) -> list[str]:
    text = read(path)
    return [
        f"{path.relative_to(ROOT)}: missing URP completion contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, fragments: tuple[str, ...]) -> list[str]:
    text = read(path)
    return [
        f"{path.relative_to(ROOT)}: forbidden production rendering fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []
    manifest_path = UNITY / "Packages" / "manifest.json"
    lock_path = UNITY / "Packages" / "packages-lock.json"
    try:
        manifest = json.loads(read(manifest_path))
        package_lock = json.loads(read(lock_path))
    except json.JSONDecodeError as error:
        print(f"Unity URP package configuration is invalid: {error}", file=sys.stderr)
        return 1

    version = "17.0.4"
    if manifest.get("dependencies", {}).get(
        "com.unity.render-pipelines.universal") != version:
        errors.append(f"{manifest_path.relative_to(ROOT)}: URP must be pinned to {version}")
    lock = package_lock.get("dependencies", {}).get(
        "com.unity.render-pipelines.universal", {})
    if lock.get("version") != version or lock.get("source") != "registry":
        errors.append(f"{lock_path.relative_to(ROOT)}: URP lock entry must pin registry {version}")

    configurator = EDITOR / "DigUrpProjectConfigurator.cs"
    errors.extend(require(configurator, (
        "UniversalRendererData", "UniversalRenderPipelineAsset.Create(",
        "GraphicsSettings.defaultRenderPipeline", "QualitySettings.renderPipeline",
        "DigUniversalRenderer.asset", "DigUniversalRenderPipeline.asset",
        "RenderMaterials.asset", "Vfx.asset", "enableInstancing = true",
        "vfx.excavation.impact", "vfx.deposit.reveal",
        "vfx.construction.progress", "vfx.production.pulse",
        "vfx.status.pulse", "vfx.combat.impact", "vfx.ambient.dust",
    )))

    lit_shader = UNITY / "Assets" / "Dig.Unity" / "Shaders" / "DigStylizedLit.shader"
    unlit_shader = UNITY / "Assets" / "Dig.Unity" / "Shaders" / "DigStylizedUnlit.shader"
    errors.extend(require(lit_shader, (
        'Shader "Dig/Stylized Lit"', '"RenderPipeline"="UniversalPipeline"',
        "input.color", "GetMainLight", "_EmissionColor",
    )))
    errors.extend(require(unlit_shader, (
        'Shader "Dig/Stylized Unlit"', '"Queue"="Transparent"',
        "Blend SrcAlpha OneMinusSrcAlpha", "input.color",
    )))

    mesh_data = RUNTIME / "DigTerrainChunkMeshData.cs"
    mesh_builder = RUNTIME / "DigTerrainChunkMeshBuilder.cs"
    mesh_visual = RUNTIME / "DigTerrainChunkVisual.cs"
    vertex_color = RUNTIME / "DigTerrainVertexColor.cs"
    errors.extend(require(mesh_data, ("Color[] colors", "Colors = colors",)))
    errors.extend(require(mesh_builder, ("DigTerrainVertexColor.Build(",)))
    errors.extend(require(mesh_visual, ("_mesh.colors = data.Colors",)))
    errors.extend(require(vertex_color, (
        "ApplyAmbientOcclusion", "StableNoise", "ResolveBaseColor",
        "HasVisibleDeposit", "DigTerrainSurfaceRole.FreshCut",
    )))

    terrain_renderer = RUNTIME / "DigTerrainChunkRenderer.cs"
    errors.extend(require(terrain_renderer, (
        "DigRenderMaterialLibrary", "RenderMaterialSemantic.Terrain",
        "RenderSurfaceKind.Lit",
    )))
    errors.extend(reject(terrain_renderer, ("Shader.Find(", "new Material(")))
    effects = RUNTIME / "DigTerrainWorkSession.PresentationEffects.cs"
    errors.extend(require(effects, (
        "BindPresentationEffectSink", "ExcavationImpact", "DepositReveal",
    )))
    errors.extend(require(RUNTIME / "DigPooledVfxInstance.cs", ("localPosition",)))
    errors.extend(require(RUNTIME / "DigRealtimeLightPool.cs", ("localPosition",)))

    if errors:
        print("Unity URP completion contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("PASS: pinned URP, authored shared assets, terrain AO and live pooled effects")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
