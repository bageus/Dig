from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]
RejectFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_excavation_contracts(
    root: Path,
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
    reject_fragments: RejectFragments,
) -> list[str]:
    del root
    rock_path = runtime_root / "DigRockVolumeRenderer.cs"
    interaction_path = runtime_root / "DigWorldInteraction.cs"
    drawing_path = runtime_root / "DigWorldInteraction.Excavation.cs"
    designations_path = runtime_root / "DigTerrainWorkDesignations.cs"
    manual_path = runtime_root / "DigTerrainWorkManualExcavation.cs"
    driver_path = runtime_root / "DigAgentSimulationDriverBase.Excavation.cs"
    hud_path = runtime_root / "DigHudOverlay.Excavation.cs"
    bootstrap_path = runtime_root / "DigUnityBootstrap.cs"
    errors: list[str] = []
    errors.extend(require_fragments(
        rock_path,
        texts.get(rock_path, ""),
        "solid rock volume",
        (
            "for (int z = 1; z < volume.Depth; z++)",
            "IsSolidRock",
            "volume.IsOpen(cell)",
            "MeshFilter",
            "IndexFormat.UInt32",
        ),
    ))
    errors.extend(require_fragments(
        drawing_path,
        texts.get(drawing_path, ""),
        "explicit Z0 excavation drawing",
        (
            "DigExcavationDrawingMode.Horizontal",
            "DigExcavationDrawingMode.Vertical",
            "CanActivateExcavationDrawing",
            "active: leftButton",
            "AssignExcavationCluster",
            "new SpatialCellId(selected.CellX, selected.CellY, 0)",
        ),
    ))
    interaction = texts.get(interaction_path, "")
    errors.extend(require_fragments(
        interaction_path,
        interaction,
        "explicit excavation routing",
        (
            "TryHandleExcavationInput(hit, left, right)",
            "excavationTool: ExcavationToolKind.None",
            "hud.SetExcavationControls(this)",
        ),
    ))
    errors.extend(reject_fragments(
        interaction_path,
        interaction,
        "implicit RMB excavation",
        ("ExcavationToolKind.Tunnel",),
    ))
    errors.extend(require_fragments(
        designations_path,
        texts.get(designations_path, ""),
        "frontier-only automatic excavation",
        (
            "IsExcavationFrontier",
            "NoCandidates",
            "agent.CellZ == 0",
            "IsManualExcavationJob(job.Id)",
        ),
    ))
    errors.extend(require_fragments(
        manual_path,
        texts.get(manual_path, ""),
        "manual excavation cluster",
        (
            "radius: 4",
            "AssignSpecificJobCommand",
            "ContinueManualExcavation",
            "AssignNextManualExcavation",
        ),
    ))
    errors.extend(require_fragments(
        driver_path,
        texts.get(driver_path, ""),
        "excavation application adapter",
        (
            "ApplyExcavationDesignation",
            "SynchronizeDesignations",
            "RefreshExcavationPresentation",
        ),
    ))
    errors.extend(require_fragments(
        hud_path,
        texts.get(hud_path, ""),
        "excavation HUD controls",
        ("Horizontal", "Vertical", "P-", "P+"),
    ))
    errors.extend(require_fragments(
        bootstrap_path,
        texts.get(bootstrap_path, ""),
        "rock volume composition",
        ("DigRockVolumeRenderer", "rockRenderer.Initialize"),
    ))
    return errors
