from pathlib import Path
from typing import Callable


def check_management_menu_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: Callable[[Path, str, str, tuple[str, ...]], list[str]],
) -> list[str]:
    menu_path = runtime_root / "DigGameHudCanvas.ManagementMenu.cs"
    dwarfs_path = runtime_root / "DigGameHudCanvas.ManagementDwarfs.cs"
    items_path = runtime_root / "DigGameHudCanvas.ManagementItems.cs"
    buildings_path = runtime_root / "DigGameHudCanvas.ManagementBuildings.cs"
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
        ),
    ))
    errors.extend(require_fragments(
        buildings_path,
        texts.get(buildings_path, ""),
        "building management table",
        ("LoadBuildings", "BuildBuildingManagementTable"),
    ))
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
