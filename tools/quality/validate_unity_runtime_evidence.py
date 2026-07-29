#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import sys
import tempfile
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class RequiredTest:
    mode: str
    full_name: str


class EvidenceError(RuntimeError):
    pass


def parse_required_test(value: str) -> RequiredTest:
    mode, separator, full_name = value.partition("=")
    if not separator or not mode.strip() or not full_name.strip():
        raise argparse.ArgumentTypeError(
            "required tests must use MODE=FULLY_QUALIFIED_TEST_NAME"
        )
    return RequiredTest(mode.strip(), full_name.strip())


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def discover_test_results(artifacts_dir: Path) -> tuple[list[Path], dict[str, str]]:
    xml_files = sorted(artifacts_dir.rglob("*.xml"))
    if not xml_files:
        raise EvidenceError("no Unity test result XML files were found")

    test_cases: dict[str, str] = {}
    parsed_files: list[Path] = []
    for path in xml_files:
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError:
            continue

        cases_in_file = 0
        for element in root.iter():
            if local_name(element.tag) != "test-case":
                continue
            full_name = (
                element.attrib.get("fullname")
                or element.attrib.get("full-name")
                or element.attrib.get("name")
                or ""
            ).strip()
            result = element.attrib.get("result", "").strip()
            if not full_name or not result:
                continue
            test_cases[full_name] = result
            cases_in_file += 1

        if cases_in_file > 0:
            parsed_files.append(path)

    if not test_cases:
        raise EvidenceError("Unity XML files contained no test-case results")
    return parsed_files, test_cases


def find_test_result(test_cases: dict[str, str], required_name: str) -> str | None:
    exact = test_cases.get(required_name)
    if exact is not None:
        return exact
    matches = [
        result
        for name, result in test_cases.items()
        if name.endswith(required_name)
    ]
    return matches[0] if len(matches) == 1 else None


def validate_runtime_log(artifacts_dir: Path, relative_path: str) -> Path:
    path = artifacts_dir / relative_path
    if not path.is_file():
        raise EvidenceError(f"required runtime log is missing: {relative_path}")
    content = path.read_text(encoding="utf-8-sig")
    required_fragments = (
        "status=passed",
        "scene=Assets/Scenes/Main.unity",
        "consoleErrors=0",
    )
    missing = [fragment for fragment in required_fragments if fragment not in content]
    if missing:
        raise EvidenceError(
            f"runtime log {relative_path} is incomplete: missing {', '.join(missing)}"
        )
    return path


def verified_manifest(
    artifacts_dir: Path,
    required_tests: list[RequiredTest],
    required_logs: list[str],
) -> dict[str, object]:
    xml_files, test_cases = discover_test_results(artifacts_dir)
    non_passed = sorted(
        (name, result)
        for name, result in test_cases.items()
        if result.casefold() != "passed"
    )
    if non_passed:
        preview = ", ".join(f"{name}={result}" for name, result in non_passed[:5])
        raise EvidenceError(f"Unity suite contains non-passing tests: {preview}")

    modes: list[dict[str, str]] = []
    for required in required_tests:
        result = find_test_result(test_cases, required.full_name)
        if result is None:
            raise EvidenceError(
                f"required {required.mode} test did not execute: {required.full_name}"
            )
        if result.casefold() != "passed":
            raise EvidenceError(
                f"required {required.mode} test did not pass: "
                f"{required.full_name}={result}"
            )
        modes.append(
            {
                "mode": required.mode,
                "requiredTest": required.full_name,
                "result": result,
            }
        )

    log_files = [validate_runtime_log(artifacts_dir, value) for value in required_logs]
    return {
        "schemaVersion": 1,
        "status": "verified",
        "reason": None,
        "unityVersion": os.environ.get("UNITY_VERSION", "6000.0.71f1"),
        "commitSha": os.environ.get("GITHUB_SHA", "local"),
        "runId": os.environ.get("GITHUB_RUN_ID", "local"),
        "testCount": len(test_cases),
        "passedCount": len(test_cases),
        "failedCount": 0,
        "requiredModes": modes,
        "resultFiles": [str(path.relative_to(artifacts_dir)) for path in xml_files],
        "runtimeLogs": [str(path.relative_to(artifacts_dir)) for path in log_files],
    }


def blocked_manifest(reason: str, required_tests: list[RequiredTest]) -> dict[str, object]:
    return {
        "schemaVersion": 1,
        "status": "blocked",
        "reason": reason,
        "unityVersion": os.environ.get("UNITY_VERSION", "6000.0.71f1"),
        "commitSha": os.environ.get("GITHUB_SHA", "local"),
        "runId": os.environ.get("GITHUB_RUN_ID", "local"),
        "testCount": 0,
        "passedCount": 0,
        "failedCount": 0,
        "requiredModes": [
            {
                "mode": required.mode,
                "requiredTest": required.full_name,
                "result": "not-executed",
            }
            for required in required_tests
        ],
        "resultFiles": [],
        "runtimeLogs": [],
    }


def write_manifest(path: Path, manifest: dict[str, object]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(manifest, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )


def write_fixture_xml(path: Path, cases: list[tuple[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    root = ET.Element("test-run")
    suite = ET.SubElement(root, "test-suite", {"name": "fixture"})
    for name, result in cases:
        ET.SubElement(
            suite,
            "test-case",
            {"name": name.rsplit(".", 1)[-1], "fullname": name, "result": result},
        )
    ET.ElementTree(root).write(path, encoding="utf-8", xml_declaration=True)


def run_self_test() -> int:
    edit = RequiredTest("EditMode", "Fixture.EditMode.Required")
    play = RequiredTest("PlayMode", "Fixture.PlayMode.Required")
    with tempfile.TemporaryDirectory() as temporary:
        root = Path(temporary)
        write_fixture_xml(
            root / "results.xml",
            [(edit.full_name, "Passed"), (play.full_name, "Passed")],
        )
        runtime = root / "runtime" / "representative-scene.log"
        runtime.parent.mkdir(parents=True, exist_ok=True)
        runtime.write_text(
            "status=passed\nscene=Assets/Scenes/Main.unity\nconsoleErrors=0\n",
            encoding="utf-8",
        )
        manifest = verified_manifest(
            root,
            [edit, play],
            ["runtime/representative-scene.log"],
        )
        if manifest["status"] != "verified" or manifest["testCount"] != 2:
            raise EvidenceError("verified self-test fixture was not accepted")

        write_fixture_xml(
            root / "results.xml",
            [(edit.full_name, "Passed"), (play.full_name, "Failed")],
        )
        try:
            verified_manifest(root, [edit, play], ["runtime/representative-scene.log"])
        except EvidenceError:
            pass
        else:
            raise EvidenceError("failed Unity test fixture was incorrectly accepted")

        blocked = blocked_manifest("activation unavailable", [edit, play])
        if blocked["status"] != "blocked" or blocked["testCount"] != 0:
            raise EvidenceError("blocked evidence manifest is invalid")

    print("PASS: Unity runtime evidence validator self-test")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Validate executed Unity EditMode/PlayMode evidence."
    )
    parser.add_argument("--artifacts-dir", type=Path)
    parser.add_argument("--manifest", type=Path)
    parser.add_argument(
        "--required-test",
        action="append",
        default=[],
        type=parse_required_test,
    )
    parser.add_argument("--required-log", action="append", default=[])
    parser.add_argument("--blocked-reason")
    parser.add_argument("--self-test", action="store_true")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    if args.self_test:
        return run_self_test()
    if args.artifacts_dir is None or args.manifest is None:
        print("--artifacts-dir and --manifest are required", file=sys.stderr)
        return 2

    if args.blocked_reason:
        manifest = blocked_manifest(args.blocked_reason, args.required_test)
        write_manifest(args.manifest, manifest)
        print(f"BLOCKED: {args.blocked_reason}")
        return 0

    try:
        manifest = verified_manifest(
            args.artifacts_dir,
            args.required_test,
            args.required_log,
        )
    except EvidenceError as error:
        manifest = {
            "schemaVersion": 1,
            "status": "failed",
            "reason": str(error),
            "unityVersion": os.environ.get("UNITY_VERSION", "6000.0.71f1"),
            "commitSha": os.environ.get("GITHUB_SHA", "local"),
            "runId": os.environ.get("GITHUB_RUN_ID", "local"),
        }
        write_manifest(args.manifest, manifest)
        print(f"Unity runtime evidence validation failed: {error}", file=sys.stderr)
        return 1

    write_manifest(args.manifest, manifest)
    print(
        "PASS: executed Unity EditMode/PlayMode evidence is verified "
        f"({manifest['testCount']} tests)"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
