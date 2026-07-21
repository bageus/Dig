#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRESENTATION = ROOT / "src" / "Dig.Presentation.Abstractions" / "Rendering"
RUNTIME = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing rendering contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden rendering fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []
    enums_path = PRESENTATION / "RenderPipelineEnums.cs"
    effect_path = PRESENTATION / "EffectSpawnRequest.cs"
    light_path = PRESENTATION / "LightRequest.cs"
    budget_path = PRESENTATION / "RenderFrameBudget.cs"
    plan_path = PRESENTATION / "RenderBudgetPlan.cs"
    presentation = "\n".join(read(path) for path in sorted(PRESENTATION.glob("*.cs")))

    errors.extend(require(enums_path, read(enums_path), (
        "RenderSurfaceKind", "Lit", "Unlit", "Overlay",
        "RenderMaterialSemantic", "Creature", "Emissive", "Vfx",
        "VfxCategory", "Excavation", "Deposit", "Construction",
        "Production", "Status", "Combat", "Ambient",
        "VfxPriority", "RealtimeLightKind", "RealtimeLightPriority",
    )))
    errors.extend(require(effect_path, read(effect_path), (
        "EffectSpawnRequest", "RequestId", "EffectId", "ParticleBudget",
        "Scale", "DurationSeconds", "Version", "particleBudget < 1",
    )))
    errors.extend(require(light_path, read(light_path), (
        "LightRequest", "RealtimeLightKind", "RealtimeLightPriority",
        "Range", "Intensity", "CastsShadows", "ValidateColor", "Version",
    )))
    errors.extend(require(budget_path, read(budget_path), (
        "RenderFrameBudget", "MaximumEffects", "MaximumParticles",
        "MaximumRealtimeLights", "MaximumShadowedLights",
        "new RenderFrameBudget(64, 2048, 8, 2)",
    )))
    errors.extend(require(plan_path, read(plan_path), (
        "RenderBudgetPlan", "SelectedLight", "DroppedEffects", "DroppedLights",
        "right.Priority.CompareTo(left.Priority)", "DistanceSquared(",
        "StringComparison.Ordinal", "Duplicate effect request id",
        "Duplicate light request id", "MaximumShadowedLights",
    )))
    errors.extend(reject(PRESENTATION, presentation, (
        "UnityEngine", "GameObject", "MonoBehaviour", "Shader.Find",
        "new Material", "ICommand", "Handle(", "Animator",
    )))

    profile_path = RUNTIME / "DigRenderMaterialProfile.cs"
    catalog_path = RUNTIME / "DigRenderMaterialCatalog.cs"
    library_path = RUNTIME / "DigRenderMaterialLibrary.cs"
    errors.extend(require(profile_path, read(profile_path), (
        "RenderMaterialSemantic", "RenderSurfaceKind", "enableInstancing",
        "FallbackTint", "StableKey",
    )))
    errors.extend(require(catalog_path, read(catalog_path), (
        "DigRenderMaterialProfile[] profiles", "TryResolve(",
        "StringComparer.Ordinal", "Duplicate render material profile",
    )))
    library = read(library_path)
    errors.extend(require(library_path, library, (
        "GraphicsSettings.currentRenderPipeline", "VisualCatalogs/RenderMaterials",
        "Dictionary<string, Material>", "HashSet<Material>",
        "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit",
        "enableInstancing = true", "DigStylizedLightingRig",
        "DigPresentationEffectBridge",
    )))

    vfx_profile_path = RUNTIME / "DigVfxProfile.cs"
    vfx_catalog_path = RUNTIME / "DigVfxCatalog.cs"
    vfx_instance_path = RUNTIME / "DigPooledVfxInstance.cs"
    vfx_player_path = RUNTIME / "DigPooledVfxPlayer.cs"
    errors.extend(require(vfx_profile_path, read(vfx_profile_path), (
        "StableId", "VfxCategory", "MaximumInstances", "MaximumParticles",
        "DigVisualPrefabRoot", "Range(1, 32)", "Range(1, 512)",
    )))
    errors.extend(require(vfx_catalog_path, read(vfx_catalog_path), (
        "DigVfxProfile[] profiles", "TryResolve(", "StringComparer.Ordinal",
        "Duplicate VFX profile id",
    )))
    errors.extend(require(vfx_instance_path, read(vfx_instance_path), (
        "ParticleSystem", "maxParticles = particleBudget", "rateOverTime",
        "sharedMaterial = material", "IsExpired(", "StopAndHide(",
    )))
    player = read(vfx_player_path)
    errors.extend(require(vfx_player_path, player, (
        "MaximumPoolSize = 64", "Dictionary<string, DigPooledVfxInstance>",
        "Stack<DigPooledVfxInstance>", "existing.Version == request.Version",
        "MaximumEffects", "MaximumParticles", "MaximumInstances",
        "ActiveParticleCount", "LastDroppedEffectCount",
        "transform.InverseTransformPoint(camera.transform.position)",
        "Resources.Load<DigVfxCatalog>",
    )))

    light_pool_path = RUNTIME / "DigRealtimeLightPool.cs"
    lighting_path = RUNTIME / "DigStylizedLightingRig.cs"
    errors.extend(require(light_pool_path, read(light_pool_path), (
        "RenderBudgetPlan.Create(", "MaximumRealtimeLights", "SelectedLight",
        "LightShadows.Soft", "LightShadows.None", "ActiveCount", "PoolSize",
        "LastDroppedLightCount",
        "transform.InverseTransformPoint(camera.transform.position)",
    )))
    errors.extend(require(lighting_path, read(lighting_path), (
        "AmbientMode.Flat", "ambientLight", "Key Light", "Rim Light",
        "LightType.Directional", "LightShadows.None",
    )))

    creature_path = RUNTIME / "DigCreatureRenderer.Resources.cs"
    creature = read(creature_path)
    errors.extend(require(creature_path, creature, (
        "DigRenderMaterialLibrary", "RenderMaterialSemantic.Creature",
        "RenderSurfaceKind.Lit",
    )))
    errors.extend(reject(creature_path, creature, (
        "Shader.Find", "new Material", "Destroy(_material)",
    )))
    runtime_contract = library + player + read(light_pool_path) + read(lighting_path)
    errors.extend(reject(RUNTIME, runtime_contract, (
        "ICommand", "Handle(", "ApplyDamage", "CompleteAttack",
        "SpawnCreature", "Commit(", "Animator.Set",
    )))

    domain_projector = PRESENTATION / "PresentationDomainEffectProjector.cs"
    errors.extend(require(domain_projector, read(domain_projector), (
        "BuildingConstructionProgressed", "ProductionWorkApplied",
        "CombatAttackResolved", "CombatStatusTicked", "AgentExternalEffectApplied",
        "PresentationEffectKind.ConstructionProgress",
        "PresentationEffectKind.ProductionPulse",
        "PresentationEffectKind.CombatImpact",
        "PresentationEffectKind.StatusPulse",
    )))
    effect_runtime = RUNTIME / "DigPresentationEffectRuntime.cs"
    errors.extend(require(effect_runtime, read(effect_runtime), (
        "ReadNewEvents", "DroppedEventCount", "TrackProductionEmitters",
        "PresentationEffectKind.LavaGlow", "PresentationEffectKind.CrystalGlow",
        "PresentationEffectKind.CampfireGlow",
        "PresentationEffectKind.ProductionBuildingGlow",
        "PresentationEffectKind.AmbientDust", "_bridge!.Present(",
    )))
    bootstrap = RUNTIME / "DigUnityBootstrap.cs"
    errors.extend(require(bootstrap, read(bootstrap), (
        "DigPresentationEffectRuntime", "effectRuntime.Publish",
        "effectRuntime.Flush(agentSession.Tick)",
    )))
    spatial_effects = RUNTIME / "DigAgentSimulationDriverBase.PresentationEffects.cs"
    errors.extend(require(spatial_effects, read(spatial_effects), (
        "EffectRuntime.Publish", "PresentationEffectKind.ExcavationImpact",
    )))
    errors.extend(reject(spatial_effects, read(spatial_effects), (
        "DigPresentationEffectBridge", "bridge.Present(",
    )))

    if errors:
        print("Unity stylized rendering and VFX contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("PASS: shared materials, stylized lighting and bounded VFX/light pools")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
