#!/usr/bin/env python3
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ROOT_MANIFEST_PATH = ROOT / "Packages" / "manifest.json"
ROOT_LOCK_PATH = ROOT / "Packages" / "packages-lock.json"
MANIFEST_PATH = ROOT / "Packages" / "manifest.json"
LOCK_PATH = ROOT / "Packages" / "packages-lock.json"
ASMDEF_PATH = ROOT / "Assets" / "Dig.Unity" / "Runtime" / "Dig.Unity.asmdef"

REQUIRED_PACKAGES = {
    "com.unity.cloud.gltfast": ("6.19.0", "registry"),
    "com.unity.render-pipelines.universal": ("17.0.4", "builtin"),
    "com.unity.modules.animation": ("1.0.0", "builtin"),
    "com.unity.modules.audio": ("1.0.0", "builtin"),
    "com.unity.modules.imgui": ("1.0.0", "builtin"),
    "com.unity.modules.particlesystem": ("1.0.0", "builtin"),
    "com.unity.modules.physics": ("1.0.0", "builtin"),
    "com.unity.test-framework": ("1.6.0", "builtin"),
    "com.unity.ugui": ("2.0.0", "builtin"),
}

REQUIRED_ASSEMBLIES = {
    "UnityEngine.AnimationModule",
    "UnityEngine.AudioModule",
    "UnityEngine.IMGUIModule",
    "UnityEngine.InputLegacyModule",
    "UnityEngine.ParticleSystemModule",
    "UnityEngine.PhysicsModule",
    "UnityEngine.UI",
}

FORBIDDEN_PACKAGES = {
    "com.unity.modules.inputlegacy": (
        "legacy input is exposed by UnityEngine.InputLegacyModule and is not "
        "a resolvable Unity 6 package"
    ),
    "org.khronos.unitygltf": (
        "Dig.Unity uses the pinned com.unity.cloud.gltfast importer; the legacy "
        "UnityGLTF git package must not be restored"
    ),
}

CONFLICT_MARKERS = ("<<<<<<<", "=======", ">>>>>>>")


def load_json(path: Path) -> dict[str, object]:
    if not path.exists():
        raise FileNotFoundError(path.relative_to(ROOT))

    text = path.read_text(encoding="utf-8-sig")
    for marker in CONFLICT_MARKERS:
        if marker in text:
            raise ValueError(
                f"{path.relative_to(ROOT)} contains unresolved conflict marker {marker}"
            )

    return json.loads(text)


def validate_package(
    errors: list[str],
    manifest_dependencies: dict[str, object],
    lock_dependencies: dict[str, object],
    package_name: str,
    version: str,
    source: str,
    owner: str,
) -> None:
    if manifest_dependencies.get(package_name) != version:
        errors.append(f"{owner} manifest must include {package_name} {version}")

    lock_entry = lock_dependencies.get(package_name)
    if not isinstance(lock_entry, dict):
        errors.append(f"{owner} packages-lock must include {package_name}")
        return

    if lock_entry.get("version") != version:
        errors.append(f"{owner} packages-lock must pin {package_name} to {version}")
    if lock_entry.get("source") != source:
        errors.append(f"{owner} packages-lock must mark {package_name} as {source}")


def main() -> int:
    errors: list[str] = []
    try:
        root_manifest = load_json(ROOT_MANIFEST_PATH)
        root_lock = load_json(ROOT_LOCK_PATH)
        manifest = load_json(MANIFEST_PATH)
        package_lock = load_json(LOCK_PATH)
        assembly = load_json(ASMDEF_PATH)
    except (FileNotFoundError, ValueError, json.JSONDecodeError) as error:
        print(f"Unity module configuration is invalid: {error}", file=sys.stderr)
        return 1

    root_manifest_dependencies = root_manifest.get("dependencies", {})
    root_lock_dependencies = root_lock.get("dependencies", {})
    manifest_dependencies = manifest.get("dependencies", {})
    lock_dependencies = package_lock.get("dependencies", {})
    assembly_references = set(assembly.get("references", []))

    validate_package(
        errors,
        root_manifest_dependencies,
        root_lock_dependencies,
        "com.unity.modules.animation",
        "1.0.0",
        "builtin",
        "root Unity host",
    )

    for package_name, (version, source) in sorted(REQUIRED_PACKAGES.items()):
        validate_package(
            errors,
            manifest_dependencies,
            lock_dependencies,
            package_name,
            version,
            source,
            "Dig.Unity",
        )

    for package_name, reason in FORBIDDEN_PACKAGES.items():
        if package_name in manifest_dependencies:
            errors.append(f"manifest must not include {package_name}: {reason}")
        if package_name in lock_dependencies:
            errors.append(f"packages-lock must not include {package_name}: {reason}")

    for assembly_name in sorted(REQUIRED_ASSEMBLIES):
        if assembly_name not in assembly_references:
            errors.append(f"Dig.Unity.asmdef must reference {assembly_name}")

    if errors:
        print("Unity module checks failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print("PASS: root and Dig.Unity packages and assembly references")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
