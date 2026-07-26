#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / ".github" / "workflows" / "quality.yml"

REQUIRED_FRAGMENTS = (
    "python tools/quality/check_quality_workflow_contracts.py",
    "Run headless smoke test",
    "dotnet run --project src/Dig.Headless/Dig.Headless.csproj",
    "--soak --profile standard",
    "--report soak-report-standard.json",
    "--soak --profile large",
    "--report soak-report-large.json",
    "name: headless-smoke-log",
    "name: soak-report-standard",
    "name: soak-report-large",
    "retention-days: 14",
)

ORDERED_FRAGMENTS = (
    "dotnet test Dig.sln",
    "Run headless smoke test",
    "Run standard deterministic soak",
    "Run large settlement deterministic soak",
)

FORBIDDEN_FRAGMENTS = (
    "continue-on-error: true",
)


def main() -> int:
    if not WORKFLOW.exists():
        print("Quality workflow checks failed:", file=sys.stderr)
        print("- .github/workflows/quality.yml is missing", file=sys.stderr)
        return 1

    workflow = WORKFLOW.read_text(encoding="utf-8")
    errors: list[str] = []
    for fragment in REQUIRED_FRAGMENTS:
        if fragment not in workflow:
            errors.append(f"quality workflow must include {fragment!r}")

    previous = -1
    for fragment in ORDERED_FRAGMENTS:
        index = workflow.find(fragment)
        if index < 0:
            continue
        if index <= previous:
            errors.append(
                "quality workflow must run build/tests before smoke, standard soak, "
                "and large soak"
            )
            break
        previous = index

    for fragment in FORBIDDEN_FRAGMENTS:
        if fragment in workflow:
            errors.append(
                f"blocking quality gates must not include {fragment!r}"
            )

    if errors:
        print("Quality workflow checks failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: blocking headless smoke and deterministic soak quality gates")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
