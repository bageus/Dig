#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

from unity_cave_room_contracts import check_cave_room_runtime_contracts
from unity_excavation_contracts import check_excavation_contracts
from unity_group_input_contracts import (
    check_group_hud_contracts,
    check_tunnel_and_group_contracts,
)
from unity_navigation_marquee_contracts import check_navigation_and_marquee_contracts
from unity_tunnel_depth_contracts import check_tunnel_depth_contracts

ROOT = Path(__file__).resolve().parents[2]
RUNTIME_ROOT = (
    ROOT
    / "unity"
    / "Dig.Unity"
    / "Assets"
    / "Dig.Unity"
    / "Runtime"
)

INTERNAL_TYPE_DECLARATION = re.compile(
    r"^[ \t]*internal\s+"
    r"(?:(?:abstract|sealed|static|partial|readonly|ref)\s+)*"
    r"(?:class|struct|record)\s+(?P<name>[A-Za-z_][A-Za-z0-9_]*)",
    re.MULTILINE,
)
PARTIAL_CLASS_DECLARATION = re.compile(
    r"\bpartial\s+class\s+(?P<name>[A-Za-z_][A-Za-z0-9_]*)"
)
UNITY_MESSAGE_METHOD = re.compile(
    r"^[ \t]*(?:(?:private|protected|public|internal)\s+)?"
    r"(?:(?:static|virtual|override|sealed|new)\s+)*"
    r"void\s+"
    r"(?P<name>Awake|Start|Update|LateUpdate|FixedUpdate|OnGUI|OnEnable|"
    r"OnDisable|OnDestroy|OnValidate)\s*\(\s*\)",
    re.MULTILINE,
)
PROTECTED_MEMBER = re.compile(
    r"^[ \t]*protected\s+(?!internal\b)"
    r"(?:(?:static|readonly|const|volatile|virtual|abstract|override|sealed|"
    r"new|unsafe|async)\s+)*"
    r"(?P<type>[A-Za-z_][A-Za-z0-9_.]*)\??"
    r"(?:\s*<[^>\n]+>)?\s+[A-Za-z_][A-Za-z0-9_]*"
)
KNOWN_SYMBOL_IMPORTS = {
    "InventoryErrors": "Dig.Domain.Inventory",
    "JobErrors": "Dig.Domain.Jobs",
}
OBSOLETE_OBJECT_LOOKUPS = (
    "FindObjectOfType<",
    "FindObjectsOfType<",
)


def runtime_files() -> list[Path]:
    if not RUNTIME_ROOT.exists():
        return []
    return sorted(RUNTIME_ROOT.rglob("*.cs"))


def require_fragments(
    path: Path,
    text: str,
    contract_name: str,
    fragments: tuple[str, ...],
) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing {contract_name} contract fragment {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def reject_fragments(
    path: Path,
    text: str,
    contract_name: str,
    fragments: tuple[str, ...],
) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: forbidden {contract_name} fragment {fragment!r}"
        for fragment in fragments
        if fragment in text
    ]


def check_side_view_contracts(texts: dict[Path, str]) -> list[str]:
    camera_path = RUNTIME_ROOT / "DigCameraController.cs"
    bootstrap_path = RUNTIME_ROOT / "DigUnityBootstrap.cs"
    camera = texts.get(camera_path, "")
    errors = require_fragments(
        camera_path,
        camera,
        "side-view camera",
        (
            "_camera.orthographic = false;",
            "SideViewYaw + orbit.Yaw",
            "KeyCode.LeftControl",
            "KeyCode.RightControl",
            "SideViewCameraFraming.CalculateDistance",
        ),
    )
    if "_camera.orthographic = true;" in camera:
        errors.append(
            f"{camera_path.relative_to(ROOT)}: side-view camera must remain perspective"
        )
    errors.extend(require_fragments(
        bootstrap_path,
        texts.get(bootstrap_path, ""),
        "side-view world",
        (
            "ConfigureSideViewRoot();",
            "Quaternion.Euler(90f, 0f, 0f)",
            "DigCameraController",
        ),
    ))
    return errors


def check_resident_inventory_runtime_contracts(
    texts: dict[Path, str],
) -> list[str]:
    visual_path = RUNTIME_ROOT / "DigAgentVisual.cs"
    buildings_path = RUNTIME_ROOT / "DigTerrainWorkSession.Buildings.cs"
    session_path = RUNTIME_ROOT / "DigTerrainWorkSession.cs"
    equipment_path = RUNTIME_ROOT / "DigResidentEquipment.cs"
    feedback_path = RUNTIME_ROOT / "DigResidentInventory.Feedback.cs"
    renderer_path = RUNTIME_ROOT / "DigAgentRenderer.InventoryAttachments.cs"
    attachment_path = RUNTIME_ROOT / "DigResidentInventoryAttachmentVisual.cs"
    hud_feedback_path = RUNTIME_ROOT / "DigGameHudCanvas.InventoryFeedback.cs"
    agent_session_path = RUNTIME_ROOT / "DigAgentSession.cs"
    movement_filter_path = RUNTIME_ROOT / "DigAgentSession.MovementFilter.cs"
    driver_path = RUNTIME_ROOT / "DigAgentSimulationDriverBase.cs"
    cadence_path = RUNTIME_ROOT / "DigTerrainWorkSession.MovementCadence.cs"
    errors = require_fragments(
        visual_path,
        texts.get(visual_path, ""),
        "resident equipment visual",
        (
            "_equipmentVisual.Configure(",
            "equipment.ItemId,",
            "EquipmentAppearanceKind.Generic,",
            "equipmentMaterial);",
        ),
    )
    errors.extend(require_fragments(
        buildings_path,
        texts.get(buildings_path, ""),
        "shared resident inventory",
        (
            "_buildingInventoryRepository = _inventoryRepository;",
            "ResidentInventoryExpansionContent.BasketItemId",
            "ResidentInventoryExpansionContent.WeaponHarnessItemId",
        ),
    ))
    errors.extend(require_fragments(
        session_path,
        texts.get(session_path, ""),
        "shared resident inventory creation",
        ("CreateDemoResidentInventory(outputItemId);",),
    ))
    errors.extend(require_fragments(
        equipment_path,
        texts.get(equipment_path, ""),
        "single authoritative equipment projection",
        (
            "ReferenceEquals(_buildingInventoryRepository, _inventoryRepository)",
            "_inventoryRepository.Get().CreateSnapshot()",
        ),
    ))
    errors.extend(require_fragments(
        feedback_path,
        texts.get(feedback_path, ""),
        "resident expansion feedback",
        (
            "LoadResidentExpansionFeedback",
            "LoadResidentInventoryAttachments",
            "_residentInventoryAttachmentPresenter.Present(",
        ),
    ))
    errors.extend(require_fragments(
        renderer_path,
        texts.get(renderer_path, ""),
        "resident inventory attachments",
        (
            "RenderInventoryAttachments(",
            "InventoryExpansionGroup.Cargo",
            "InventoryExpansionGroup.Weapon",
            "DigResidentInventoryAttachmentVisual",
        ),
    ))
    errors.extend(require_fragments(
        attachment_path,
        texts.get(attachment_path, ""),
        "resident attachment geometry",
        (
            "BuildCargo(model.Tier, material);",
            "BuildWeapon(model.Tier, material);",
            "model.VisualAttachmentId",
        ),
    ))
    errors.extend(require_fragments(
        hud_feedback_path,
        texts.get(hud_feedback_path, ""),
        "inventory tooltip and spill confirmation",
        (
            "SpillConfirmationSeconds",
            "ShowInventorySlotFeedback(",
            "ConfirmExpansionSpill(",
            "RequiresSpillConfirmation",
        ),
    ))
    errors.extend(require_fragments(
        movement_filter_path,
        texts.get(movement_filter_path, ""),
        "resident movement target filtering",
        (
            "SetMovementTargetFilter(",
            "_movementTargetFilter",
            "ApplyMovementTargetFilter(",
        ),
    ))
    errors.extend(require_fragments(
        agent_session_path,
        texts.get(agent_session_path, ""),
        "session-level cargo cadence",
        ("movementTargets = ApplyMovementTargetFilter(movementTargets, _tick);",),
    ))
    errors.extend(require_fragments(
        driver_path,
        texts.get(driver_path, ""),
        "cargo cadence composition",
        (
            "AgentSession.SetMovementTargetFilter(",
            "TerrainSession.ApplyResidentMovementCadence",
        ),
    ))
    errors.extend(require_fragments(
        cadence_path,
        texts.get(cadence_path, ""),
        "authoritative cargo cadence policy",
        (
            "ApplyResidentMovementCadence(",
            "IsResidentMovementDue(residentId, tick)",
        ),
    ))
    return errors


def check_generic_source_contracts(
    texts: dict[Path, str],
    internal_types: set[str],
) -> list[str]:
    errors: list[str] = []
    messages: dict[tuple[str, str], list[Path]] = {}
    for path, text in texts.items():
        relative = path.relative_to(ROOT)
        partial = PARTIAL_CLASS_DECLARATION.search(text)
        if partial is not None:
            class_name = partial.group("name")
            for match in UNITY_MESSAGE_METHOD.finditer(text):
                messages.setdefault((class_name, match.group("name")), []).append(relative)

        for symbol, namespace in KNOWN_SYMBOL_IMPORTS.items():
            if f"{symbol}." not in text:
                continue
            if f"using {namespace};" in text or f"{namespace}.{symbol}." in text:
                continue
            errors.append(
                f"{relative}: {symbol} requires 'using {namespace};' "
                "or a fully qualified reference"
            )

        for obsolete_lookup in OBSOLETE_OBJECT_LOOKUPS:
            if obsolete_lookup in text:
                errors.append(
                    f"{relative}: Unity 6 runtime must not use obsolete "
                    f"{obsolete_lookup[:-1]}"
                )

        for line_number, line in enumerate(text.splitlines(), start=1):
            member = PROTECTED_MEMBER.match(line)
            if member is None:
                continue
            type_name = member.group("type").rsplit(".", maxsplit=1)[-1]
            if type_name in internal_types:
                errors.append(
                    f"{relative}:{line_number}: protected member exposes internal type "
                    f"{type_name}; use private protected/internal or widen the type"
                )
    for (class_name, method_name), paths in sorted(messages.items()):
        if len(paths) <= 1:
            continue
        locations = ", ".join(str(path) for path in paths)
        errors.append(
            f"partial class {class_name} declares Unity message {method_name}() "
            f"more than once: {locations}"
        )
    return errors


def main() -> int:
    files = runtime_files()
    if not files:
        print("Unity source contract checks failed: runtime sources are missing.", file=sys.stderr)
        return 1

    texts = {path: path.read_text(encoding="utf-8-sig") for path in files}
    internal_types = {
        match.group("name")
        for text in texts.values()
        for match in INTERNAL_TYPE_DECLARATION.finditer(text)
    }
    errors: list[str] = check_side_view_contracts(texts)
    errors.extend(check_resident_inventory_runtime_contracts(texts))
    errors.extend(check_tunnel_and_group_contracts(
        RUNTIME_ROOT,
        texts,
        require_fragments,
        reject_fragments,
    ))
    errors.extend(check_excavation_contracts(
        ROOT,
        RUNTIME_ROOT,
        texts,
        require_fragments,
        reject_fragments,
    ))
    errors.extend(check_cave_room_runtime_contracts(
        RUNTIME_ROOT,
        texts,
        require_fragments,
    ))
    errors.extend(check_tunnel_depth_contracts(
        RUNTIME_ROOT,
        texts,
        require_fragments,
    ))
    errors.extend(check_navigation_and_marquee_contracts(
        RUNTIME_ROOT,
        texts,
        require_fragments,
    ))
    errors.extend(check_group_hud_contracts(
        RUNTIME_ROOT,
        texts,
        require_fragments,
        reject_fragments,
    ))
    errors.extend(check_generic_source_contracts(texts, internal_types))

    if errors:
        print("Unity source contract checks failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: Unity source contracts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
