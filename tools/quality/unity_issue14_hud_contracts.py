from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]
RejectFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_issue14_hud_contracts(
    root: Path,
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
    reject_fragments: RejectFragments,
) -> list[str]:
    inventory_path = runtime_root / "DigGameHudCanvas.Inventory.cs"
    roster_path = runtime_root / "DigGameHudCanvas.Roster.cs"
    resident_rows_path = runtime_root / "DigGameHudCanvas.ResidentRows.cs"
    virtualization_path = runtime_root / "DigGameHudCanvas.RosterVirtualization.cs"
    notifications_path = runtime_root / "DigGameHudCanvas.Notifications.cs"
    navigation_path = runtime_root / "DigWorldInteraction.Notifications.cs"
    technology_path = runtime_root / "DigGameHudCanvas.Context.Technology.cs"
    localization_path = runtime_root / "DigHudLocalization.cs"
    decisions_path = runtime_root / "DigWorldInteraction.Decisions.cs"
    combat_path = runtime_root / "DigWorldInteraction.Combat.cs"
    excavation_path = runtime_root / "DigWorldInteraction.Excavation.cs"
    overlay_path = runtime_root / "DigWorldOverlayRenderer.Render.cs"
    inventory_text = texts.get(inventory_path, "")
    errors: list[str] = []

    inventory_start = inventory_text.find("private void BuildInventoryContext(")
    inventory_end = inventory_text.find(
        "private void ConfigureInventoryRootLayout()",
        inventory_start,
    )
    weapon_index = inventory_text.find(
        "ResidentInventoryCompartment.Weapon",
        inventory_start,
        inventory_end,
    )
    main_index = inventory_text.find(
        "ResidentInventoryCompartment.Main",
        inventory_start,
        inventory_end,
    )
    cargo_index = inventory_text.find(
        "ResidentInventoryCompartment.Cargo",
        inventory_start,
        inventory_end,
    )
    if not (inventory_start >= 0 and weapon_index < main_index < cargo_index):
        errors.append(
            f"{inventory_path.relative_to(root)}: inventory sections must render "
            "in Weapon -> Main -> Cargo order"
        )

    errors.extend(require_fragments(
        roster_path,
        texts.get(roster_path, ""),
        "resident roster virtualization entry point",
        ("RefreshResidentRows(residentRoster);", "ResetResidentRowPool();"),
    ))
    errors.extend(reject_fragments(
        resident_rows_path,
        texts.get(resident_rows_path, ""),
        "full skill inspector embedded in virtualized roster row",
        ("BuildSkillInspector(parent, resident.Skills);",),
    ))
    errors.extend(require_fragments(
        localization_path,
        texts.get(localization_path, ""),
        "typed Russian HUD localization",
        (
            '"resident.need.alertness.vigor"] = "Бодрость"',
            "Resolve(activity.LocalizationKey)",
            "Resolve(notification.LocalizationKey)",
        ),
    ))
    errors.extend(require_fragments(
        decisions_path,
        texts.get(decisions_path, ""),
        "Unity attack application adapter",
        (
            "case ApplicationInputCommandKind.AttackTarget:",
            "ApplyAttack(decision);",
        ),
    ))
    errors.extend(require_fragments(
        combat_path,
        texts.get(combat_path, ""),
        "hostile target routing to combat intent",
        (
            "IssuePlayerAttackOrder(",
            "ContextWorldTargetKind.HostileResident",
            "CreatureDisposition.Hostile",
        ),
    ))
    errors.extend(require_fragments(
        excavation_path,
        texts.get(excavation_path, ""),
        "release-to-commit atomic eraser batch",
        (
            "_excavationEraseBatch.Add(target);",
            "ApplyExcavationEraseBatch();",
            "Release LMB to apply",
        ),
    ))
    errors.extend(require_fragments(
        overlay_path,
        texts.get(overlay_path, ""),
        "pooled world overlay integrations",
        (
            "OverlaySemanticKind.Designation",
            "OverlaySemanticKind.BuildingFootprint",
            "OverlaySemanticKind.StorageDemand",
            "OverlaySemanticKind.Deposit",
            "OverlaySemanticKind.Fog",
            "OverlaySemanticKind.DirtyChunk",
            "OverlaySemanticKind.NavigationDiagnostic",
            "HideRemainder(_selection, count);",
        ),
    ))
    errors.extend(require_fragments(
        resident_rows_path,
        texts.get(resident_rows_path, ""),
        "typed resident sex read model",
        ("ResidentSexIndicator sex = resident.Sex;",),
    ))
    errors.extend(require_fragments(
        virtualization_path,
        texts.get(virtualization_path, ""),
        "bounded resident roster virtualization and incremental row reuse",
        (
            "ResidentRowPoolCapacity = 16",
            "roster.GetWindow(",
            "EnsureResidentRowPool();",
            "BindResidentRow(slot, resident);",
            "slot.Button.onClick.RemoveAllListeners();",
            "ClearChildren(slot.Root);",
            "BuildResidentRowSignature",
            "OnRightPanelScrolled",
        ),
    ))
    errors.extend(reject_fragments(
        resident_rows_path,
        texts.get(resident_rows_path, ""),
        "unbounded resident row creation",
        ("foreach (ResidentRosterRowViewModel resident",),
    ))
    errors.extend(require_fragments(
        notifications_path,
        texts.get(notifications_path, ""),
        "event-driven notification ticker lifecycle",
        (
            "ReadEventsAfter(",
            "_notificationTicker.Ingest(events, residentIds);",
            "HandleNotificationClick",
            "PointerEventData.InputButton.Left",
            "_notificationTicker.DismissCurrent();",
            "NavigateToNotification(notification.NavigationTarget)",
        ),
    ))
    errors.extend(require_fragments(
        navigation_path,
        texts.get(navigation_path, ""),
        "notification source navigation",
        (
            "NavigateToNotification",
            "NavigateToResidentNotification",
            "NavigateToJobNotification",
            "ShowTechnologyDescription",
            "_cameraController!.Focus",
        ),
    ))
    errors.extend(require_fragments(
        technology_path,
        texts.get(technology_path, ""),
        "technology notification description panel",
        (
            "ShowTechnologyDescription",
            "ShowTechnologyDescriptionPanel",
            '"TECHNOLOGY DISCOVERED"',
            "DismissTechnologyDescription",
        ),
    ))
    return errors
