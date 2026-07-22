from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]
RejectFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_automatic_planning_contracts(
    root: Path,
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
    reject_fragments: RejectFragments,
) -> list[str]:
    del root
    ui_path = runtime_root / "DigGameHudCanvas.AutomaticPlanning.cs"
    agent_hud_path = runtime_root / "DigAgentSession.Hud.cs"
    driver_hud_path = runtime_root / "DigAgentSimulationDriverBase.Hud.cs"
    candidate_paths = (
        runtime_root / "DigTerrainWorkSession.cs",
        runtime_root / "DigTerrainSpatialExcavation.cs",
        runtime_root / "DigTerrainHauling.cs",
        runtime_root / "DigBuildingBoxAssemblyExecution.cs",
        runtime_root / "DigBuildingPackingExecution.cs",
        runtime_root / "DigJobSession.cs",
    )
    dynamic_path = runtime_root / "DigTerrainWorkDesignations.cs"
    movement_path = runtime_root / "DigTerrainWorkDirectMovement.cs"

    errors = require_fragments(
        ui_path,
        texts.get(ui_path, ""),
        "selected-resident automatic planning toggle",
        (
            '"Automatic Planning Toggle"',
            "TryGetResidentAutomaticPlanning",
            "SetResidentAutomaticPlanning",
            "manual orders remain available",
            '"AUTO\\nON"',
            '"AUTO\\nOFF"',
        ),
    )
    errors.extend(require_fragments(
        agent_hud_path,
        texts.get(agent_hud_path, ""),
        "authoritative automatic planning command bridge",
        ("SetAgentAutomaticPlanningCommand", "AutomaticPlanningEnabled"),
    ))
    errors.extend(require_fragments(
        driver_hud_path,
        texts.get(driver_hud_path, ""),
        "automatic planning simulation HUD adapter",
        ("TryGetResidentAutomaticPlanning", "SetResidentAutomaticPlanning"),
    ))
    for candidate_path in candidate_paths:
        errors.extend(require_fragments(
            candidate_path,
            texts.get(candidate_path, ""),
            "automatic assignment opt-out eligibility",
            ("IsAvailableForAutomaticPlanning",),
        ))
        errors.extend(reject_fragments(
            candidate_path,
            texts.get(candidate_path, ""),
            "alive-only automatic assignment eligibility",
            ("isAvailable: agent.IsAlive",),
        ))
    errors.extend(require_fragments(
        dynamic_path,
        texts.get(dynamic_path, ""),
        "dynamic excavation automatic assignment policy",
        ("IsAvailableForAutomaticWork(agent)",),
    ))
    errors.extend(require_fragments(
        movement_path,
        texts.get(movement_path, ""),
        "automatic assignment opt-out and manual movement policy",
        (
            "agent.IsAvailableForAutomaticPlanning",
            "_isManualMovementActive?.Invoke(agent.Id)",
        ),
    ))
    return errors
