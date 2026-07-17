#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RUNTIME_ROOT = (
    ROOT
    / "unity"
    / "Dig.Unity"
    / "Assets"
    / "Dig.Unity"
    / "Runtime"
)

INTERNAL_TYPE_DECLARATION = re.compile(
    r"^[ \t]*internal\s+"
    r"(?:(?:abstract|sealed|static|partial|readonly|ref)\s+)*"
    r"(?:class|struct|record)\s+(?P<name>[A-Za-z_][A-Za-z0-9_]*)",
    re.MULTILINE,
)
PARTIAL_CLASS_DECLARATION = re.compile(
    r"\bpartial\s+class\s+(?P<name>[A-Za-z_][A-Za-z0-9_]*)"
)
UNITY_MESSAGE_METHOD = re.compile(
    r"^[ \t]*(?:(?:private|protected|public|internal)\s+)?"
    r"(?:(?:static|virtual|override|sealed|new)\s+)*"
    r"void\s+"
    r"(?P<name>Awake|Start|Update|LateUpdate|FixedUpdate|OnGUI|OnEnable|"
    r"OnDisable|OnDestroy|OnValidate)\s*\(\s*\)",
    re.MULTILINE,
)
PROTECTED_MEMBER = re.compile(
    r"^[ \t]*protected\s+(?!internal\b)"
    r"(?:(?:static|readonly|const|volatile|virtual|abstract|override|sealed|"
    r"new|unsafe|async)\s+)*"
    r"(?P<type>[A-Za-z_][A-Za-z0-9_.]*)\??"
    r"(?:\s*<[^>\n]+>)?\s+[A-Za-z_][A-Za-z0-9_]*"
)
KNOWN_SYMBOL_IMPORTS = {
    "InventoryErrors": "Dig.Domain.Inventory",
    "JobErrors": "Dig.Domain.Jobs",
}
OBSOLETE_OBJECT_LOOKUPS = (
    "FindObjectOfType<",
    "FindObjectsOfType<",
)


def runtime_files() -> list[Path]:
    if not RUNTIME_ROOT.exists():
        return []
    return sorted(RUNTIME_ROOT.rglob("*.cs"))


def main() -> int:
    files = runtime_files()
    if not files:
        print("Unity source contract checks failed: runtime sources are missing.", file=sys.stderr)
        return 1

    texts = {
        path: path.read_text(encoding="utf-8-sig")
        for path in files
    }
    internal_types = {
        match.group("name")
        for text in texts.values()
        for match in INTERNAL_TYPE_DECLARATION.finditer(text)
    }

    errors: list[str] = []
    messages: dict[tuple[str, str], list[Path]] = {}
    for path, text in texts.items():
        relative = path.relative_to(ROOT)
        partial = PARTIAL_CLASS_DECLARATION.search(text)
        if partial is not None:
            class_name = partial.group("name")
            for match in UNITY_MESSAGE_METHOD.finditer(text):
                key = (class_name, match.group("name"))
                messages.setdefault(key, []).append(relative)

        for symbol, namespace in KNOWN_SYMBOL_IMPORTS.items():
            if f"{symbol}." not in text:
                continue
            if f"using {namespace};" in text or f"{namespace}.{symbol}." in text:
                continue
            errors.append(
                f"{relative}: {symbol} requires 'using {namespace};' "
                "or a fully qualified reference"
            )

        for obsolete_lookup in OBSOLETE_OBJECT_LOOKUPS:
            if obsolete_lookup in text:
                errors.append(
                    f"{relative}: Unity 6 runtime must not use obsolete "
                    f"{obsolete_lookup[:-1]}"
                )

        for line_number, line in enumerate(text.splitlines(), start=1):
            member = PROTECTED_MEMBER.match(line)
            if member is None:
                continue
            type_name = member.group("type").rsplit(".", maxsplit=1)[-1]
            if type_name in internal_types:
                errors.append(
                    f"{relative}:{line_number}: protected member exposes internal type "
                    f"{type_name}; use private protected/internal or widen the type"
                )

    for (class_name, method_name), paths in sorted(messages.items()):
        if len(paths) <= 1:
            continue
        locations = ", ".join(str(path) for path in paths)
        errors.append(
            f"partial class {class_name} declares Unity message {method_name}() "
            f"more than once: {locations}"
        )

    if errors:
        print("Unity source contract checks failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: Unity source contracts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
