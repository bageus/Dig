#!/usr/bin/env python3
from __future__ import annotations

import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[2]
CONFIG_PATH = Path(__file__).with_name("quality_config.json")


def load_config() -> dict[str, object]:
    with CONFIG_PATH.open("r", encoding="utf-8") as stream:
        return json.load(stream)


def iter_checked_files(config: dict[str, object]) -> Iterable[Path]:
    extensions = set(config["extensions"])
    excluded = set(config["excluded_directories"])

    for relative_root in config["source_roots"]:
        source_root = ROOT / str(relative_root)
        if not source_root.exists():
            continue

        for path in source_root.rglob("*"):
            if not path.is_file() or path.suffix not in extensions:
                continue

            relative_parts = path.relative_to(ROOT).parts
            if any(part in excluded for part in relative_parts):
                continue

            yield path


def check_file_lengths(config: dict[str, object]) -> list[str]:
    max_lines = int(config["max_lines"])
    errors: list[str] = []

    for path in iter_checked_files(config):
        with path.open("r", encoding="utf-8") as stream:
            line_count = sum(1 for _ in stream)

        if line_count > max_lines:
            relative = path.relative_to(ROOT)
            errors.append(
                f"{relative}: {line_count} lines exceeds the {max_lines}-line limit"
            )

    return errors


def project_name(project_path: Path) -> str:
    tree = ET.parse(project_path)
    root = tree.getroot()

    assembly_name = root.find(".//AssemblyName")
    if assembly_name is not None and assembly_name.text:
        return assembly_name.text.strip()

    return project_path.stem


def project_references(project_path: Path) -> set[str]:
    tree = ET.parse(project_path)
    references: set[str] = set()

    for node in tree.getroot().findall(".//ProjectReference"):
        include = node.attrib.get("Include")
        if include:
            normalized = include.replace("\\", "/")
            references.add(Path(normalized).stem)

    return references


def check_project_dependencies(config: dict[str, object]) -> list[str]:
    allowed = {
        str(name): set(references)
        for name, references in config["allowed_project_references"].items()
    }
    discovered: dict[str, set[str]] = {}
    errors: list[str] = []

    for project_path in sorted((ROOT / "src").rglob("*.csproj")):
        name = project_name(project_path)
        discovered[name] = project_references(project_path)

    for name, allowed_references in allowed.items():
        if name not in discovered:
            errors.append(f"Missing configured source project: {name}")
            continue

        unexpected = discovered[name] - allowed_references
        if unexpected:
            errors.append(
                f"{name} has forbidden project references: {sorted(unexpected)}"
            )

    unconfigured = set(discovered) - set(allowed)
    for name in sorted(unconfigured):
        errors.append(
            f"Source project {name} is not present in allowed_project_references"
        )

    errors.extend(find_dependency_cycles(discovered))
    return errors


def find_dependency_cycles(graph: dict[str, set[str]]) -> list[str]:
    visited: set[str] = set()
    active: list[str] = []
    errors: list[str] = []

    def visit(node: str) -> None:
        if node in active:
            cycle_start = active.index(node)
            cycle = active[cycle_start:] + [node]
            errors.append("Project dependency cycle: " + " -> ".join(cycle))
            return

        if node in visited:
            return

        active.append(node)
        for dependency in graph.get(node, set()):
            if dependency in graph:
                visit(dependency)
        active.pop()
        visited.add(node)

    for project in graph:
        visit(project)

    return errors


def check_domain_boundaries(config: dict[str, object]) -> list[str]:
    domain_root = ROOT / "src" / "Dig.Domain"
    banned_tokens = [str(token) for token in config["domain_banned_tokens"]]
    errors: list[str] = []

    for path in sorted(domain_root.rglob("*.cs")):
        text = path.read_text(encoding="utf-8")
        for token in banned_tokens:
            if token in text:
                relative = path.relative_to(ROOT)
                errors.append(f"{relative}: forbidden Domain dependency token '{token}'")

    return errors


def main() -> int:
    config = load_config()
    checks = [
        ("file length", check_file_lengths),
        ("project dependencies", check_project_dependencies),
        ("domain boundaries", check_domain_boundaries),
    ]

    all_errors: list[str] = []
    for name, check in checks:
        errors = check(config)
        if errors:
            all_errors.extend(f"[{name}] {error}" for error in errors)
        else:
            print(f"PASS: {name}")

    if all_errors:
        print("\nQuality checks failed:", file=sys.stderr)
        for error in all_errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("All quality checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
