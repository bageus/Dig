from pathlib import Path
from typing import Callable


def check_management_menu_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: Callable[[Path, str, str, tuple[str, ...]], list[str]],
) -> list[str]:
    menu_path = runtime_root / "DigGameHudCanvas.ManagementMenu.cs"
    table_path = runtime_root / "DigGameHudCanvas.ManagementTable.cs"
    dwarfs_path = runtime_root / "DigGameHudCanvas.ManagementDwarfs.cs"
    items_path = runtime_root / "DigGameHudCanvas.ManagementItems.cs"
    buildings_path = runtime_root / "DigGameHudCanvas.ManagementBuildings.cs"
    localization_path = runtime_root / "DigManagementLocalization.cs"
    terrain_path = runtime_root / "DigTerrainWorkSession.Management.cs"
    society_path = runtime_root / "DigAgentSession.Society.cs"
    errors = require_fragments(
        menu_path,
        texts.get(menu_path, ""),
        "management menu toggle and overlay",
        (
            "ToggleManagementDropdown",
            "ManagementOverlayKind.Dwarfs",
            "ManagementOverlayKind.Items",
            "ManagementOverlayKind.Buildings",
            "CloseManagementOverlay",
            "PointInManagementUi",
        ),
    )
    errors.extend(require_fragments(
        table_path,
        texts.get(table_path, ""),
        "top-right management overlay close button",
        ("Anchor(closeRect, 1f, 1f, 1f, 1f, -52f, -52f, -10f, -10f);",),
    ))
    errors.extend(require_fragments(
        dwarfs_path,
        texts.get(dwarfs_path, ""),
        "five resident management tabs",
        (
            "DwarfManagementTab.Standard",
            "DwarfManagementTab.Production",
            "DwarfManagementTab.Fight",
            "DwarfManagementTab.Family",
            "DwarfManagementTab.Inventory",
            "SettlementResidentManagementPresenter",
            "DigManagementLocalization.Resolve",
        ),
    ))
    errors.extend(require_fragments(
        items_path,
        texts.get(items_path, ""),
        "five item management tabs",
        (
            '"Materials"',
            '"Weapons"',
            '"Food"',
            '"Potions"',
            '"Tools"',
            "LoadItemSummary",
            "DigManagementLocalization.Resolve",
        ),
    ))
    errors.extend(require_fragments(
        buildings_path,
        texts.get(buildings_path, ""),
        "building management table",
        (
            "LoadBuildings",
            "BuildBuildingManagementTable",
            "DigManagementLocalization.Resolve",
        ),
    ))
    errors.extend(require_fragments(
        localization_path,
        texts.get(localization_path, ""),
        "English-only management table localization",
        (
            '["management.name"] = "Name"',
            '["resident.need.alertness.vigor"] = "Vigor"',
            '["resident.skill.stonework"] = "Stonework"',
            '["resident.skill.unarmed_combat"] = "Unarmed combat"',
            '["management.schedule.work"] = "Work time"',
            '["management.building.status.completed"] = "Completed"',
        ),
    ))
    localization = texts.get(localization_path, "")
    if any("\u0400" <= character <= "\u04ff" for character in localization):
        errors.append(
            f"{localization_path}: management table localization must not contain "
            "Cyrillic labels")
    context = "\n".join(
        text for path, text in texts.items()
        if path.name.startswith("DigGameHudCanvas.Context"))
    for fragment in ("All 12 skills", "BuildSkillInspectorShortcut"):
        if fragment in context:
            errors.append(
                f"{runtime_root}: obsolete resident All skills shortcut remains: "
                f"{fragment!r}")
    errors.extend(require_fragments(
        terrain_path,
        texts.get(terrain_path, ""),
        "authoritative inventory management projection",
        ("inventory.CreateSnapshot()", "inventory.Catalog", "LoadBuildings()"),
    ))
    errors.extend(require_fragments(
        society_path,
        texts.get(society_path, ""),
        "society-backed age and family projection",
        ("SocietyState", "LoadSocietySnapshot", "SocietyTick"),
    ))
    return errors
