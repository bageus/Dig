#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

from unity_contract_helpers import read_texts, require_fragments


def check_runtime_regression_contracts(runtime_root: Path) -> list[str]:
    errors: list[str] = []
    movement_input = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    pointer_hits = runtime_root / "DigWorldInteraction.PointerHits.cs"
    targets = runtime_root / "DigWorldInteraction.SelectedResidentTargets.cs"
    marquee = runtime_root / "DigWorldInteraction.MarqueeSelection.cs"
    navigation_sync = runtime_root / "DigAgentSimulationDriverBase.NavigationSync.cs"
    tunnel_renderer = runtime_root / "DigTunnelDemoRenderer.cs"
    world_visual_catalog = runtime_root / "DigWorldRenderer.VisualCatalog.cs"
    job_visual = runtime_root / "DigJobVisual.cs"
    multi_worker = runtime_root / "DigTerrainWorkManualExcavation.MultiWorker.cs"
    quarter_work = runtime_root / "DigTerrainWorkExcavationQuarters.cs"

    files = (
        movement_input,
        pointer_hits,
        targets,
        marquee,
        navigation_sync,
        tunnel_renderer,
        world_visual_catalog,
        job_visual,
        multi_worker,
        quarter_work,
    )
    texts = read_texts(files, errors)

    errors.extend(require_fragments(
        movement_input,
        texts.get(movement_input, ""),
        "explicit LMB tunnel destinations and active job markers",
        (
            "TryApplyTunnelMove(RaycastHit[] hits",
            "TryAssignExplicitExcavation(hits, residentIds)",
            "CellId? surfaceTarget = ResolveExcavationTarget(hit)",
            "surfaceTarget.HasValue",
            "AssignSurfaceExcavation(",
            "IsTerminalJobStatus(job.Model.Status)",
            "TryResolveTunnelDestination(hit, out _, out _)",
            "ResolveSelectedResidentTarget(hits)",
            "_tunnelRenderer.TryGetCell",
            "_caveRoomFloorRenderer.TryGetCell",
            "MoveResidentsThroughTunnel",
        ),
    ))
    errors.extend(require_fragments(
        world_visual_catalog,
        texts.get(world_visual_catalog, ""),
        "designated direct-work hit proxies",
        (
            "collider.enabled = visual.gameObject.activeSelf",
            "_tunnelDigInteractionActive",
            "visual.Model.IsDesignated",
        ),
    ))
    errors.extend(require_fragments(
        multi_worker,
        texts.get(multi_worker, ""),
        "forced excavation independent from job ownership",
        (
            "AssignExcavationClusterToResidents",
            "AssignManualQuarterExcavation(",
            "Existing automatic jobs keep their owner",
            "BindManualExcavationResidentState",
            "ResolveManualMiningSkill",
        ),
    ))
    errors.extend(require_fragments(
        quarter_work,
        texts.get(quarter_work, ""),
        "manual quarter excavation adapter",
        (
            "ExcavationWorkCoordinator",
            "AssignManualQuarterExcavation",
            "LoadManualQuarterAssignment",
            "AdvanceManualQuarterExcavation",
            "LoadExcavationQuarterState",
        ),
    ))

    movement_input_text = texts.get(movement_input, "")
    explicit_method_index = movement_input_text.find(
        "private bool TryAssignExplicitExcavation("
    )
    surface_index = movement_input_text.find(
        "CellId? surfaceTarget = ResolveExcavationTarget(hit)",
        explicit_method_index,
    )
    movement_fallthrough_index = movement_input_text.find(
        "ResolveSelectedResidentTarget(hits)",
        explicit_method_index,
    )
    if (
        explicit_method_index < 0
        or surface_index < 0
        or movement_fallthrough_index < 0
        or surface_index >= movement_fallthrough_index
    ):
        errors.append(
            f"{movement_input}: explicit excavation must precede movement fallthrough"
        )

    return errors


def main() -> int:
    root = Path(__file__).resolve().parents[2]
    runtime_root = (
        root / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime"
    )
    errors = check_runtime_regression_contracts(runtime_root)
    if errors:
        print("Unity runtime regression contract checks failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: Unity runtime regression contracts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
