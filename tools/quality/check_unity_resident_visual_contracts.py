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
        "catch (Exception exception)",
        "CreatePrimitiveResidentAgent(model)",
        "GameObject.CreatePrimitive(PrimitiveType.Capsule)",
        "visual.transform.SetParent(root.transform, worldPositionStays: false)",
        "visual.transform.localPosition = new Vector3(0f, 0.60f, 0f)",
        "root.AddComponent<CapsuleCollider>()",
        "private const float ResidentWorldScale = 0.5f;",
        "root.transform.localScale = Vector3.one * ResidentWorldScale;",
        "root.AddComponent<DigAgentVisual>()",
        "InitializeSimple(model, _normalMaterial!, _selectedMaterial!)",
    )))
    errors.extend(reject(renderer_path, renderer, (
        "FindObjectOfType<", "FindObjectsOfType<", "AnimateRoute(",
    )))
    resident_scale_applications = renderer.count(
        "root.transform.localScale = Vector3.one * ResidentWorldScale;")
    if resident_scale_applications != 2:
        errors.append(
            f"{renderer_path.relative_to(ROOT)}: resident world scale must be "
            "applied to both authored and fallback resident roots")

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
        "Quaternion.LookRotation(Vector3.back, Vector3.up)",
        "ResolveSocket(DigResidentSocketKind kind)",
        "_rig.SetSelected(selected)", "PresentAction(",
        "internal void InitializeSimple(",
        "GetComponentsInChildren<Renderer>(includeInactive: true)",
        "transform.position = ToWorld(_currentVisualX, _currentY, _currentZ)",
        "SetFreeformDestination(CellId cell, float offsetX)",
    )))
    errors.extend(reject(visual_path, visual, (
        "Animator.Set", "ApplyRootMotion", "ICommand", "Handle(",
        "PlayRoute(", "UpdateRoute(", "_routeStepDuration", "_routeIndex",
    )))

    scale_test_path = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" \
        / "Tests" / "PlayMode" / "ResidentWorldScalePlayModeTests.cs"
    scale_test = read(scale_test_path)
    errors.extend(require(scale_test_path, scale_test, (
        "Rendered_resident_root_is_half_scale_without_moving_its_feet",
        "renderer.Render(new[] { Resident() }, movementDuration: 0.1f);",
        "resident.localScale, Is.EqualTo(Vector3.one * 0.5f)",
        "resident.GetComponent<CapsuleCollider>()",
    )))

    projection_path = RUNTIME / "DigTunnelProjection.cs"
    projection = read(projection_path)
    errors.extend(require(projection_path, projection, (
        "WalkSurfaceY(cellY) - ResidentFootSink",
        "Resident and creature roots are authored at their feet",
    )))
    errors.extend(reject(projection_path, projection, (
        "+ ResidentHalfHeight",
    )))

    movement_path = RUNTIME / "DigAgentVisual.Movement.cs"
    movement = read(movement_path)
    errors.extend(require(movement_path, movement, (
        "using Dig.Presentation.Agents;",
        "AgentInterpolatedSpatialPosition",
        "AgentSpatialPositionInterpolator.Interpolate(",
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

    print("PASS: composite resident rig uses only authoritative per-tick movement interpolation")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
