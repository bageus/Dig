#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRESENTATION = ROOT / "src" / "Dig.Presentation.Abstractions" / "Agents"
RUNTIME = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""


def combined(prefix: str) -> str:
    return "\n".join(read(path) for path in sorted(RUNTIME.glob(prefix + "*.cs")))


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing resident visual contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden resident visual fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def main() -> int:
    errors: list[str] = []
    models_path = PRESENTATION / "ResidentVisualModels.cs"
    presenter_path = PRESENTATION / "ResidentVisualPresenter.cs"
    models = read(models_path)
    presenter = read(presenter_path)
    errors.extend(require(models_path, models, (
        "ResidentBodyVariant", "ResidentAgeVisualBand",
        "ResidentHairVisualVariant", "ResidentHeadwearRole",
        "ResidentActionVisualState", "Idle", "Walk", "Dig", "Carry",
        "Build", "Pickup", "Drop", "Hit", "Death",
        "ResidentAppearanceViewModel", "ResidentActionVisualViewModel",
    )))
    errors.extend(require(presenter_path, presenter, (
        "PresentAppearance(", "PresentAction(", "model.ActionProgress",
        "!model.IsAlive", "isMoving", "isCarrying", "showImpact",
        "OffsetBasis", "Prime", "value & (ulong)long.MaxValue",
    )))
    errors.extend(reject(PRESENTATION, models + presenter, (
        "UnityEngine", "GameObject", "MonoBehaviour", "Animator",
        "ICommand", "Handle(", "DateTime", "Guid.NewGuid", "Random",
    )))

    profile_path = RUNTIME / "DigResidentVisualProfile.cs"
    catalog_path = RUNTIME / "DigResidentVisualCatalog.cs"
    profile = read(profile_path)
    catalog = read(catalog_path)
    errors.extend(require(profile_path, profile, (
        "ResidentBodyVariant", "maximumRenderers", "Range(10, 24)",
        "DigVisualPrefabRoot", "DigResidentVisualResolution",
    )))
    errors.extend(require(catalog_path, catalog, (
        "DigResidentVisualProfile[] profiles", "ResolveResident(",
        "StringComparer.Ordinal", "Duplicate resident profile id",
    )))

    renderer_path = RUNTIME / "DigAgentRenderer.cs"
    renderer = combined("DigAgentRenderer")
    errors.extend(require(renderer_path, renderer, (
        "CreateResidentAgent(model)", "new GameObject($\"Resident",
        "CapsuleCollider", "ResolveResidentVisual()",
        "DigResidentRigFactory.Create(",
        "agentVisual.SetSelected(_selectedIds.Contains(model.Id))",
        "Resources.Load<DigResidentVisualCatalog>",
        "enableInstancing = true",
    )))
    errors.extend(reject(renderer_path, renderer, (
        "GameObject.CreatePrimitive(PrimitiveType.Capsule)",
        "FindObjectOfType<", "FindObjectsOfType<",
    )))

    rig_path = RUNTIME / "DigResidentRig.cs"
    factory_path = RUNTIME / "DigResidentRigFactory.cs"
    rig = read(rig_path)
    factory = read(factory_path)
    errors.extend(require(rig_path, rig, (
        "MaterialPropertyBlock", "GetPropertyBlock", "SetPropertyBlock",
        "DigResidentSocketKind", "Head", "LeftHand", "RightHand",
        "Back", "Cargo", "Vfx", "ApplyAppearance(", "ApplyAction(",
        "ResidentActionVisualState.Death",
    )))
    errors.extend(require(factory_path, factory, (
        "Low Poly Resident Rig", "BuildRepresentative(",
        "maximumRenderers", "GetComponentsInChildren<Renderer>",
        "Socket Head", "Socket Left Hand", "Socket Right Hand",
        "Socket Back", "Socket Cargo", "Socket VFX",
        "DisableChildColliders(",
    )))

    visual_path = RUNTIME / "DigAgentVisual.cs"
    visual = combined("DigAgentVisual")
    errors.extend(require(visual_path, visual, (
        "AgentSpatialPositionInterpolator.Interpolate(",
        "DigTunnelProjection.ResidentWorldPosition",
        "PlayRoute(", "Quaternion.LookRotation(Vector3.back, Vector3.up)",
        "ResolveSocket(DigResidentSocketKind kind)",
        "_rig?.SetSelected(selected)", "PresentAction(",
    )))
    errors.extend(reject(visual_path, visual, (
        "Animator.Set", "ApplyRootMotion", "ICommand", "Handle(",
    )))

    attachments_path = RUNTIME / "DigAgentRenderer.InventoryAttachments.cs"
    attachments = read(attachments_path)
    errors.extend(require(attachments_path, attachments, (
        "DigResidentSocketKind.Cargo", "DigResidentSocketKind.Back",
        "agent.ResolveSocket(socket)",
    )))

    if errors:
        print("Unity resident visual contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: resident profiles, composite rig, sockets and action projection")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
