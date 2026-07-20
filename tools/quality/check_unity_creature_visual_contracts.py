#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRESENTATION = ROOT / "src" / "Dig.Presentation.Abstractions" / "Creatures"
RUNTIME = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""


def combined(prefix: str) -> str:
    return "\n".join(read(path) for path in sorted(RUNTIME.glob(prefix + "*.cs")))


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing creature visual contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden creature visual fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []
    enums_path = PRESENTATION / "CreatureVisualEnums.cs"
    snapshot_path = PRESENTATION / "CreatureVisualSnapshot.cs"
    models_path = PRESENTATION / "CreatureVisualViewModels.cs"
    presenter_path = PRESENTATION / "CreatureVisualPresenter.cs"
    plan_path = PRESENTATION / "CreatureRenderReconciliationPlan.cs"
    presentation = "\n".join(read(path) for path in sorted(PRESENTATION.glob("*.cs")))
    errors.extend(require(enums_path, read(enums_path), (
        "CreatureVisualFamily", "Plant", "Vuker", "Arachnid", "Biped",
        "LargeDemon", "SmallCreature", "CreatureLifecycleVisualStage",
        "Seed", "Egg", "Larva", "Child", "Adult",
        "CreatureActionVisualState", "Attack", "Hit", "Death", "Growth", "Special",
        "CreatureMarkerShape", "Ring", "Shield", "Spikes",
        "CreatureLodTier", "CreatureAnimationUpdatePolicy",
    )))
    errors.extend(require(snapshot_path, read(snapshot_path), (
        "CreatureVisualSnapshot", "Stable", "SpeciesId", "Disposition",
        "IsMoving", "IsAttacking", "ShowImpact", "IsGrowing",
        "IsSpecialAction", "ActionProgress", "Version",
    )))
    errors.extend(require(models_path, read(models_path), (
        "CreatureAppearanceViewModel", "RigId", "VariantId", "IsFallback",
        "CreatureActionVisualViewModel", "CreatureLodViewModel",
        "UpdateIntervalFrames", "RenderBody",
    )))
    presenter = read(presenter_path)
    errors.extend(require(presenter_path, presenter, (
        "enemy.plant.poison", "enemy.plant.fire", "enemy.vuker",
        "enemy.vuker.sulfur", "enemy.spider", "creature.spider.egg",
        "enemy.demon.swallower", "enemy.demon.lava", "enemy.troll",
        "enemy.goblin", "creature.hamster", "creature.larva",
        "creature.rig.plant", "creature.rig.vuker", "creature.rig.arachnid",
        "creature.rig.biped", "creature.rig.demon", "creature.rig.small",
        "creature.rig.fallback", "CreatureMarkerShape.Shield",
        "CreatureMarkerShape.Spikes", "PresentAction(", "PresentLod(",
        "CreatureActionVisualState.Death", "CreatureActionVisualState.Hit",
        "CreatureAnimationUpdatePolicy.Frozen", "value & (ulong)long.MaxValue",
    )))
    errors.extend(require(plan_path, read(plan_path), (
        "CreatureRenderReconciliationPlan", "populationCap",
        "Creature visual population cap was exceeded", "Duplicate creature visual id",
        "CreateIds", "UpdateIds", "RemoveIds",
    )))
    errors.extend(reject(PRESENTATION, presentation, (
        "UnityEngine", "GameObject", "MonoBehaviour", "Animator",
        "ICommand", "Handle(", "DateTime", "Guid.NewGuid", "Random",
    )))

    profile_path = RUNTIME / "DigCreatureVisualProfile.cs"
    catalog_path = RUNTIME / "DigCreatureVisualCatalog.cs"
    errors.extend(require(profile_path, read(profile_path), (
        "StableSpeciesId", "RigStableId", "CreatureVisualFamily",
        "Range(3, 32)", "DigVisualPrefabRoot", "DigCreatureVisualResolution",
    )))
    errors.extend(require(catalog_path, read(catalog_path), (
        "DigCreatureVisualProfile[] profiles", "ResolveCreature(",
        "StringComparer.Ordinal", "Duplicate creature profile id",
    )))

    rig_path = RUNTIME / "DigCreatureRig.cs"
    rig = read(rig_path)
    errors.extend(require(rig_path, rig, (
        "MaterialPropertyBlock", "DigCreatureAnchorKind", "Equipment", "Drop",
        "InsideCreature", "Vfx", "ApplyAppearance(", "ApplyAction(",
        "ApplyLod(", "CreatureMarkerShape", "CreatureDisposition.Tamed",
        "CreatureDisposition.Hostile", "CreatureLodTier.Far",
    )))

    factory_path = RUNTIME / "DigCreatureRigFactory.cs"
    factory = combined("DigCreatureRigFactory")
    errors.extend(require(factory_path, factory, (
        "BuildRepresentative(", "CreatureVisualFamily.Plant",
        "CreatureVisualFamily.Vuker", "CreatureVisualFamily.Arachnid",
        "CreatureVisualFamily.Biped", "CreatureVisualFamily.LargeDemon",
        "BuildSmallCreature", "Marker Ring", "Marker Shield", "Marker Spikes",
        "Anchor Equipment", "Anchor Drop", "Anchor Inside Creature", "Anchor VFX",
        "DisableChildColliders(", "maximumRenderers",
    )))

    visual_path = RUNTIME / "DigCreatureVisual.cs"
    visual = read(visual_path)
    errors.extend(require(visual_path, visual, (
        "CreatureVisualSnapshot Model", "RequiresRigRebuild(", "ReplaceRig(",
        "ApplySnapshot(", "ResolveAnchor(", "Vector3.Lerp(",
        "DigTunnelProjection.ResidentWorldPosition",
    )))
    errors.extend(reject(visual_path, visual, (
        "Animator.Set", "ApplyRootMotion", "ICommand", "Handle(",
    )))

    renderer_path = RUNTIME / "DigCreatureRenderer.cs"
    renderer = combined("DigCreatureRenderer")
    errors.extend(require(renderer_path, renderer, (
        "CreatureRenderReconciliationPlan.Create(", "MaximumPoolSize = 64",
        "Stack<DigCreatureVisual>", "AcquireRoot(", "RemoveCreature(",
        "TryGetCreature(", "SelectById(", "TryResolveAnchor(",
        "Resources.Load<DigCreatureVisualCatalog>", "enableInstancing = true",
        "CreatureAnimationUpdatePolicy", "WorldToViewportPoint",
    )))
    errors.extend(reject(renderer_path, renderer, (
        "GameObject.CreatePrimitive(PrimitiveType.Capsule)",
        "FindObjectOfType<", "FindObjectsOfType<",
    )))

    bootstrap_path = RUNTIME / "DigUnityBootstrap.cs"
    errors.extend(require(bootstrap_path, read(bootstrap_path), (
        "using Dig.Presentation.Creatures;", "DigCreatureRenderer creatureRenderer",
        "Array.Empty<CreatureVisualSnapshot>()", "creatureRenderer.Render(",
    )))

    if errors:
        print("Unity creature visual contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: creature families, lifecycle variants, markers, pooling and LOD")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
