#!/usr/bin/env python3
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "unity" / "Dig.Unity" / "Packages" / "manifest.json"


def main() -> int:
    if not MANIFEST.exists():
        print("Unity package manifest is missing.", file=sys.stderr)
        return 1
    dependencies = json.loads(MANIFEST.read_text(encoding="utf-8-sig")).get("dependencies", {})
    actual = dependencies.get("com.unity.modules.physics")
    if actual != "1.0.0":
        print(
            "com.unity.modules.physics must be pinned to '1.0.0'; "
            f"found {actual!r}",
            file=sys.stderr,
        )
        return 1
    print("PASS: Unity package manifest")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
