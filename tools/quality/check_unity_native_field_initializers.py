#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RUNTIME = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime"

FIELD_INITIALIZER = re.compile(
    r"^[ \t]*(?:(?:public|private|protected|internal|static|readonly|volatile)\s+)*"
    r"MaterialPropertyBlock\??\s+[A-Za-z_][A-Za-z0-9_]*\s*=\s*"
    r"new\s+MaterialPropertyBlock\s*\(",
    re.MULTILINE,
)


def main() -> int:
    errors: list[str] = []
    for path in sorted(RUNTIME.rglob("*.cs")):
        text = path.read_text(encoding="utf-8-sig")
        if ": MonoBehaviour" not in text and ": ScriptableObject" not in text:
            continue
        for match in FIELD_INITIALIZER.finditer(text):
            line = text.count("\n", 0, match.start()) + 1
            errors.append(
                f"{path.relative_to(ROOT)}:{line}: MaterialPropertyBlock must be "
                "created lazily or in Awake/OnEnable, not in a Unity component field initializer"
            )

    if errors:
        print("Unity property-block field initializer checks failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: Unity property blocks are not created from component field initializers")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
