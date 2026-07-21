#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRESENTATION = ROOT / "src" / "Dig.Presentation.Abstractions" / "Rendering"
RUNTIME = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime"
TESTS = ROOT / "tests" / "Dig.Tests"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing effect projection contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden effect projection fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []
    models_path = PRESENTATION / "PresentationEffectModels.cs"
    presenter_path = PRESENTATION / "PresentationEffectPresenter.cs"
    tests_path = TESTS / "PresentationEffectPresenterTests.cs"
    bridge_path = RUNTIME / "DigPresentationEffectBridge.cs"
    materials_path = RUNTIME / "DigRenderMaterialLibrary.cs"

    models = read(models_path)
    presenter = read(presenter_path)
    bridge = read(bridge_path)
    materials = read(materials_path)
    tests = read(tests_path)

    errors.extend(require(models_path, models, (
        "PresentationEffectKind", "ExcavationImpact", "DepositReveal",
        "ConstructionProgress", "ProductionPulse", "StatusPulse",
        "CombatImpact", "AmbientDust", "LavaGlow", "CrystalGlow",
        "CampfireGlow", "ProductionBuildingGlow",
        "PresentationEffectFact", "Magnitude", "Version",
        "PresentationEffectFrame.Empty", "IReadOnlyList<EffectSpawnRequest>",
        "IReadOnlyList<LightRequest>", "ReadOnlyCollection",
    )))
    errors.extend(require(presenter_path, presenter, (
        "PresentationEffectPresenter", "ordered.Sort(CompareFacts)",
        "StringComparer.Ordinal.Compare", "HashSet<string>",
        "Duplicate presentation effect id", "vfx.excavation.impact",
        "vfx.deposit.reveal", "vfx.construction.progress",
        "vfx.production.pulse", "vfx.status.pulse", "vfx.combat.impact",
        "vfx.ambient.dust", "light.lava", "light.crystal",
        "light.campfire", "light.production-building",
        "VfxPriority.Critical", "RealtimeLightPriority.Focused",
    )))
    errors.extend(reject(PRESENTATION, models + presenter, (
        "UnityEngine", "GameObject", "MonoBehaviour", "ParticleSystem",
        "LightType", "ICommand", "Handle(", "Complete", "Commit",
        "DateTime", "Guid.NewGuid", "Random",
    )))

    errors.extend(require(bridge_path, bridge, (
        "PresentationEffectPresenter", "PresentationEffectFrame.Empty",
        "DigPooledVfxPlayer", "DigRealtimeLightPool", "SetBudget(",
        "_vfxPlayer!.Play(frame.Effects", "_lightPool!.Render(frame.Lights",
        "ActiveEffectCount", "ActiveParticleCount", "ActiveLightCount",
    )))
    errors.extend(reject(bridge_path, bridge, (
        "Dig.Domain", "Dig.Application", "ICommand", "Handle(",
        "Complete", "Commit", "ApplyDamage", "FinishAttack",
    )))
    errors.extend(require(materials_path, materials, (
        "GetComponent<DigPresentationEffectBridge>()",
        "gameObject.AddComponent<DigPresentationEffectBridge>()",
    )))
    errors.extend(require(tests_path, tests, (
        "Empty_input_returns_canonical_empty_frame",
        "Input_order_does_not_change_projected_order",
        "Discrete_kinds_map_to_expected_categories",
        "Emissive_kinds_create_effect_and_light",
        "Magnitude_changes_visual_budget_without_changing_identity",
        "Duplicate_stable_event_ids_are_rejected",
    )))

    if errors:
        print("Unity presentation effect projection contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: immutable effect facts project deterministically into bounded VFX and lights")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
