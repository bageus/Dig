#!/usr/bin/env python3
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MANIFEST_PATH = ROOT / "unity" / "Dig.Unity" / "Packages" / "manifest.json"
LOCK_PATH = ROOT / "unity" / "Dig.Unity" / "Packages" / "packages-lock.json"
ASMDEF_PATH = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime" / "Dig.Unity.asmdef"

REQUIRED_MODULES = {
    "com.unity.modules.audio": "UnityEngine.AudioModule",
    "com.unity.modules.imgui": "UnityEngine.IMGUIModule",
    "com.unity.modules.inputlegacy": "UnityEngine.InputLegacyModule",
    "com.unity.modules.physics": "UnityEngine.PhysicsModule",
}


def load_json(path: Path) -> dict[str, object]:
    if not path.exists():
        raise FileNotFoundError(path.relative_to(ROOT))
    return json.loads(path.read_text(encoding="utf-8-sig"))


def main() -> int:
    errors: list[str] = []
    try:
        manifest = load_json(MANIFEST_PATH)
        package_lock = load_json(LOCK_PATH)
        assembly = load_json(ASMDEF_PATH)
    except (FileNotFoundError, json.JSONDecodeError) as error:
        print(f"Unity module configuration is invalid: {error}", file=sys.stderr)
        return 1

    manifest_dependencies = manifest.get("dependencies", {})
    lock_dependencies = package_lock.get("dependencies", {})
    assembly_references = set(assembly.get("references", []))

    for package_name, assembly_name in REQUIRED_MODULES.items():
        if manifest_dependencies.get(package_name) != "1.0.0":
            errors.append(f"manifest must include {package_name} 1.0.0")

        lock_entry = lock_dependencies.get(package_name)
        if not isinstance(lock_entry, dict):
            errors.append(f"packages-lock must include {package_name}")
        else:
            if lock_entry.get("version") != "1.0.0":
                errors.append(f"packages-lock must pin {package_name} to 1.0.0")
            if lock_entry.get("source") != "builtin":
                errors.append(f"packages-lock must mark {package_name} as builtin")

        if assembly_name not in assembly_references:
            errors.append(f"Dig.Unity.asmdef must reference {assembly_name}")

    if errors:
        print("Unity module checks failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: Unity built-in modules")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
