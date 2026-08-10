#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PLAYMODE = (
    ROOT
    / "Assets"
    / "Dig.Unity"
    / "Tests"
    / "PlayMode"
)
RUNTIME = (
    ROOT
    / "Assets"
    / "Dig.Unity"
    / "Runtime"
)


def require(path: Path, fragments: tuple[str, ...]) -> list[str]:
    if not path.exists():
        return [f"{path.relative_to(ROOT)} is missing"]
    text = path.read_text(encoding="utf-8-sig")
    return [
        f"{path.relative_to(ROOT)}: missing excavation Play Mode contract {item!r}"
        for item in fragments
        if item not in text
    ]


def reject(path: Path, fragments: tuple[str, ...]) -> list[str]:
    if not path.exists():
        return []
    text = path.read_text(encoding="utf-8-sig")
    return [
        f"{path.relative_to(ROOT)}: obsolete excavation contract remains {item!r}"
        for item in fragments
        if item in text
    ]


def main() -> int:
    errors: list[str] = []
    world_tests = PLAYMODE / "WorldOwnedExcavationPlayModeTests.cs"
    continuation_tests = PLAYMODE / "ExcavationContinuationPlayModeTests.cs"
    direct_tests = PLAYMODE / "DirectExcavationOrderPlayModeTests.cs"
    combat = RUNTIME / "DigTerrainWorkCombatInterruption.cs"
    workflow = ROOT / ".github" / "workflows" / "unity-playmode.yml"

    errors.extend(require(world_tests, (
        "Partial_quarters_and_open_cell_use_the_same_world_snapshot",
        "Shaft_gap_uses_climbing_visual_and_interrupt_cleans_the_pose",
        "Depth_detour_wins_and_opposite_climbers_remain_active",
        "ExcavationQuarter.All",
        "TunnelTraversalKind.ShaftGapTraverse",
        "TunnelTraversalKind.DepthTraverse",
        "TraversalKinds.Contains(TunnelTraversalKind.DepthTraverse)",
        "TraversalKinds.Contains(TunnelTraversalKind.ShaftGapTraverse)",
        "_climbingWorkPose",
    )))
    errors.extend(reject(world_tests, (
        "Does.Contain(TunnelTraversalKind",
        "Does.Not.Contain(TunnelTraversalKind",
    )))
    errors.extend(require(continuation_tests, (
        "Twelve_opened_cells_keep_geometry_cursor_and_route_in_sync",
        "const int cellCount = 12",
        "SetTunnelDigInteractionActive",
        "visual.Model.IsExcavationOpen",
        "collider.enabled",
        "volume.FindPath",
    )))
    errors.extend(require(direct_tests, (
        "Manual_cluster_preserves_automatic_job_owner",
        "Combat_interrupt_releases_active_excavation_without_losing_the_job",
        "InterruptForCombat",
        "LoadManualQuarterAssignment",
        "Is.Null",
    )))
    errors.extend(reject(direct_tests, (
        'Invoke(terrain, "LoadManualQuarterAssignment", manualWorker),\n            Is.Not.Null',
    )))
    errors.extend(require(combat, (
        "InterruptForCombat",
        "ReleaseAssignmentsForAgents",
        "_excavationQuarterWork.Cancel",
    )))
    errors.extend(require(workflow, (
        "Resolve Unity activation",
        "id: activation",
        "configured=false",
        "Unity runtime tests blocked",
        "steps.activation.outputs.configured == 'true'",
        "Run Unity EditMode and PlayMode tests",
        "Validate executed Unity runtime evidence",
        "Record blocked runtime evidence",
        "game-ci/unity-test-runner@v4",
        "unityVersion: 6000.0.71f1",
        "testMode: All",
        "artifactsPath: artifacts/unity-tests",
        "unity-editmode-playmode-results",
        "unity-runtime-evidence",
        "validate_unity_runtime_evidence.py",
    )))
    errors.extend(reject(workflow, (
        "Validate Unity activation",
        "::error title=Unity activation missing",
    )))

    if errors:
        print("Unity excavation Play Mode contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: excavation Play Mode scenarios and CI gate are present")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
