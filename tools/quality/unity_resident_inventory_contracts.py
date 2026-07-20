from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_resident_inventory_runtime_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
) -> list[str]:
    visual_path = runtime_root / "DigAgentVisual.cs"
    buildings_path = runtime_root / "DigTerrainWorkSession.Buildings.cs"
    session_path = runtime_root / "DigTerrainWorkSession.cs"
    equipment_path = runtime_root / "DigResidentEquipment.cs"
    feedback_path = runtime_root / "DigResidentInventory.Feedback.cs"
    renderer_path = runtime_root / "DigAgentRenderer.InventoryAttachments.cs"
    attachment_path = runtime_root / "DigResidentInventoryAttachmentVisual.cs"
    hud_feedback_path = runtime_root / "DigGameHudCanvas.InventoryFeedback.cs"
    agent_session_path = runtime_root / "DigAgentSession.cs"
    movement_filter_path = runtime_root / "DigAgentSession.MovementFilter.cs"
    driver_path = runtime_root / "DigAgentSimulationDriverBase.cs"
    cadence_path = runtime_root / "DigTerrainWorkSession.MovementCadence.cs"
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
        "shared resident inventory and generated resource catalog creation",
        (
            "CreateDemoResidentInventory(",
            "worldSession.TerrainDepositDefinitions",
            "InMemoryInventoryRepository inventoryRepository",
        ),
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
            "visual.Configure(model, ResolveItemVisual(model.ItemId))",
        ),
    ))
    errors.extend(require_fragments(
        attachment_path,
        texts.get(attachment_path, ""),
        "resident attachment item profile geometry",
        (
            "DigItemVisualResolution resolution",
            "ResolveSocket(model.Group, resolution.CarrySocket)",
            "resolution.CarryScale",
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
