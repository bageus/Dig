#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRESENTATION = ROOT / "src" / "Dig.Presentation.Abstractions" / "Inventory"
RUNTIME = (
    ROOT
    / "unity"
    / "Dig.Unity"
    / "Assets"
    / "Dig.Unity"
    / "Runtime"
)


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing item visual contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden item visual fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def check_layout_contracts() -> list[str]:
    errors: list[str] = []
    models_path = PRESENTATION / "ItemStackVisualLayoutModels.cs"
    models = read(models_path)
    errors.extend(require(models_path, models, (
        "ItemStackQuantityBand",
        "Single",
        "Small",
        "Medium",
        "Large",
        "ItemReservationVisualState",
        "VisibleInstanceCount",
        "QuantityBadge",
        "LayoutVersion",
        "instances.Count > 4",
    )))
    errors.extend(reject(models_path, models, (
        "UnityEngine",
        "GameObject",
        "Material",
        "ICommand",
    )))

    presenter_path = PRESENTATION / "ItemStackVisualLayoutPresenter.cs"
    presenter = read(presenter_path)
    errors.extend(require(presenter_path, presenter, (
        "quantity == 1",
        "quantity <= 4",
        "quantity <= 9",
        "ItemStackQuantityBand.Large",
        "CreateInstances",
        "new[] { (-18, 0), (18, 0) }",
        "Visible",
        "value & (ulong)long.MaxValue",
    )))
    errors.extend(reject(presenter_path, presenter, (
        "UnityEngine",
        "Random",
        "Guid.NewGuid",
        "DateTime",
        "ICommand",
    )))
    return errors


def check_profile_contracts() -> list[str]:
    errors: list[str] = []
    profile_path = RUNTIME / "DigItemVisualProfile.cs"
    profile = read(profile_path)
    errors.extend(require(profile_path, profile, (
        "DigItemProfileKind",
        "Material",
        "Ore",
        "BuildingBox",
        "Food",
        "Alcohol",
        "Equipment",
        "DigItemCarrySocketPolicy",
        "DigItemRotationPolicy",
        "DigItemColliderPolicy",
        "Sprite? icon",
        "Vector3 worldScale",
        "Vector3 carryScale",
        "maxVisibleInstances",
        "maxVisibleInstances > 4",
        "DigVisualPrefabRoot",
    )))
    errors.extend(reject(profile_path, profile, (
        "BuildingState",
        "InventoryState",
        "ICommand",
        "Handle(",
    )))

    catalog_path = RUNTIME / "DigItemVisualCatalog.cs"
    catalog = read(catalog_path)
    errors.extend(require(catalog_path, catalog, (
        "DigItemVisualProfile[] profiles",
        "ResolveItem(string stableId)",
        "StringComparer.Ordinal",
        "Duplicate item profile id",
        "maxVisibleInstances: 4",
    )))
    return errors


def check_world_renderer_contracts() -> list[str]:
    errors: list[str] = []
    renderer_path = RUNTIME / "DigWorldItemRenderer.cs"
    renderer = read(renderer_path)
    errors.extend(require(renderer_path, renderer, (
        "Resources.Load<DigItemVisualCatalog>",
        "ItemStackVisualLayoutPresenter",
        "visualCatalog.ResolveItem(itemId)",
        "Stack<DigWorldItemVisual>",
        "MaximumPooledRoots",
        "PrepareForPool()",
        "Resolve(item.ItemId)",
    )))
    errors.extend(reject(renderer_path, renderer, (
        "item.IsBuildingBox",
        "PrimitiveType.Sphere",
        "GameObject.CreatePrimitive",
        "new Material(",
        "_resourceMaterial",
        "_boxMaterial",
    )))

    visual_path = RUNTIME / "DigWorldItemVisual.cs"
    visual = read(visual_path)
    errors.extend(require(visual_path, visual, (
        "RequireComponent(typeof(BoxCollider))",
        "Mathf.Clamp(resolution.MaxVisibleInstances, 1, 4)",
        "DigVisualPrefabFactory.Create(",
        "PrimitiveType.Cube",
        "VisibleInstanceCount",
        "QuantityBadge",
        "ItemReservationVisualState.Partial",
        "ItemReservationVisualState.Full",
        "gameObject.layer = interactive ? 0 : 2",
        "_interactionCollider!.enabled = interactive",
        "DisableColliders(instance)",
    )))
    errors.extend(reject(visual_path, visual, (
        "Model.IsBuildingBox",
        "GameObject.CreatePrimitive",
        "PrimitiveType.Sphere",
        "new Material(",
        "InventoryState",
        "ICommand",
    )))
    return errors


def check_carry_contracts() -> list[str]:
    errors: list[str] = []
    resolver_path = RUNTIME / "DigAgentRenderer.ItemVisualCatalog.cs"
    resolver = read(resolver_path)
    errors.extend(require(resolver_path, resolver, (
        "Resources.Load<DigItemVisualCatalog>",
        "itemVisualCatalog.ResolveItem(itemId)",
        "SetItemVisualCatalog",
        "visual.InvalidateAsset()",
    )))

    integration_path = RUNTIME / "DigAgentRenderer.InventoryAttachments.cs"
    integration = read(integration_path)
    errors.extend(require(integration_path, integration, (
        "visual.Configure(model, ResolveItemVisual(model.ItemId))",
    )))

    attachment_path = RUNTIME / "DigResidentInventoryAttachmentVisual.cs"
    attachment = read(attachment_path)
    errors.extend(require(attachment_path, attachment, (
        "DigItemVisualResolution resolution",
        "DigVisualPrefabFactory.Create(",
        "resolution.CarryScale",
        "ResolveSocket(model.Group, resolution.CarrySocket)",
        "DisableColliders(_instance)",
        "SetLayerRecursively(_instance, layer: 2)",
    )))
    errors.extend(reject(attachment_path, attachment, (
        "GameObject.CreatePrimitive",
        "new Material(",
        "PrimitiveType.Sphere",
        "InventoryState",
        "ICommand",
    )))
    return errors


def main() -> int:
    errors = check_layout_contracts()
    errors.extend(check_profile_contracts())
    errors.extend(check_world_renderer_contracts())
    errors.extend(check_carry_contracts())
    if errors:
        print("Unity item visual contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("PASS: item profiles, bounded stacks, pooling and carry reuse")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
