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


def require_fragments(
    path: Path,
    text: str,
    contract_name: str,
    fragments: tuple[str, ...],
) -> list[str]:
    errors: list[str] = []
    for fragment in fragments:
        if fragment not in text:
            errors.append(
                f"{path.relative_to(ROOT)}: missing {contract_name} contract "
                f"fragment {fragment!r}"
            )
    return errors


def reject_fragments(
    path: Path,
    text: str,
    contract_name: str,
    fragments: tuple[str, ...],
) -> list[str]:
    errors: list[str] = []
    for fragment in fragments:
        if fragment in text:
            errors.append(
                f"{path.relative_to(ROOT)}: forbidden {contract_name} fragment "
                f"{fragment!r}"
            )
    return errors


def check_side_view_contracts(texts: dict[Path, str]) -> list[str]:
    camera_path = RUNTIME_ROOT / "DigCameraController.cs"
    bootstrap_path = RUNTIME_ROOT / "DigUnityBootstrap.cs"
    camera = texts.get(camera_path, "")
    bootstrap = texts.get(bootstrap_path, "")
    errors = require_fragments(
        camera_path,
        camera,
        "side-view camera",
        (
            "_camera.orthographic = false;",
            "SideViewYaw + orbit.Yaw",
            "KeyCode.LeftControl",
            "KeyCode.RightControl",
            "SideViewCameraFraming.CalculateDistance",
        ),
    )
    if "_camera.orthographic = true;" in camera:
        errors.append(
            f"{camera_path.relative_to(ROOT)}: side-view camera must remain perspective"
        )

    errors.extend(require_fragments(
        bootstrap_path,
        bootstrap,
        "side-view world",
        (
            "ConfigureSideViewRoot();",
            "Quaternion.Euler(90f, 0f, 0f)",
            "DigCameraController",
        ),
    ))
    return errors


def check_tunnel_contracts(texts: dict[Path, str]) -> list[str]:
    agent_path = RUNTIME_ROOT / "DigAgentVisual.cs"
    bootstrap_path = RUNTIME_ROOT / "DigUnityBootstrap.cs"
    interaction_path = RUNTIME_ROOT / "DigWorldInteraction.cs"
    interaction_partial_path = RUNTIME_ROOT / "DigWorldInteraction.TunnelMovement.cs"
    projection_path = RUNTIME_ROOT / "DigTunnelProjection.cs"
    renderer_path = RUNTIME_ROOT / "DigTunnelDemoRenderer.cs"
    world_renderer_path = RUNTIME_ROOT / "DigWorldRenderer.cs"
    composition_path = RUNTIME_ROOT / "DigAgentSession.TunnelMovement.cs"
    session_path = RUNTIME_ROOT / "DigAgentSession.cs"
    errors: list[str] = []
    errors.extend(require_fragments(
        agent_path,
        texts.get(agent_path, ""),
        "spatial resident",
        (
            "model.CellZ",
            "transform.localPosition",
            "DigTunnelProjection.ResidentLocalPosition",
            "PlayRoute(",
        ),
    ))
    errors.extend(require_fragments(
        bootstrap_path,
        texts.get(bootstrap_path, ""),
        "platform cave bootstrap",
        (
            "DigTunnelDemoRenderer",
            "worldRenderer.SetTunnelCutaway(agentSession.TunnelVolume)",
            "interaction.SetTunnelMovement",
            "x4 tunnel volume",
        ),
    ))
    errors.extend(require_fragments(
        interaction_path,
        texts.get(interaction_path, ""),
        "tunnel input",
        (
            "TryClearResidentSelection(right)",
            "TryApplyTunnelMove(hit, left)",
        ),
    ))
    errors.extend(require_fragments(
        interaction_partial_path,
        texts.get(interaction_partial_path, ""),
        "resident selection",
        (
            "_agentRenderer.Select(null)",
            "_tunnelRenderer?.ShowRoute(null)",
            "Resident selection cleared.",
        ),
    ))
    errors.extend(require_fragments(
        projection_path,
        texts.get(projection_path, ""),
        "tunnel depth projection",
        (
            "DepthSpacing",
            "ResidentDepthOffset",
            "cell.Z * DepthSpacing",
        ),
    ))
    errors.extend(require_fragments(
        renderer_path,
        texts.get(renderer_path, ""),
        "platform cave rendering",
        (
            "Walkable plane",
            "Cave ceiling",
            "Cave back wall",
            "Mathf.Abs(DigTunnelProjection.DepthSpacing)",
        ),
    ))
    errors.extend(require_fragments(
        world_renderer_path,
        texts.get(world_renderer_path, ""),
        "terrain cutaway",
        (
            "SetTunnelCutaway",
            "CaveCeilingY",
            "ApplyTunnelCutaway();",
        ),
    ))
    errors.extend(require_fragments(
        composition_path,
        texts.get(composition_path, ""),
        "tunnel movement composition",
        (
            "TunnelNavigationVolume volume",
            "MoveAgentThroughTunnelCommandHandler",
            "_manualTunnelOrders",
        ),
    ))
    errors.extend(require_fragments(
        session_path,
        texts.get(session_path, ""),
        "platform resident composition",
        (
            "TunnelNavigationVolume.CreateDemo",
            "initialPosition: new SpatialCellId",
            "HasManualTunnelOrder(agent.Id)",
            "_tunnelVolume != null",
        ),
    ))
    return errors


def check_hud_contracts(texts: dict[Path, str]) -> list[str]:
    hud_path = RUNTIME_ROOT / "DigHudOverlay.cs"
    hud = texts.get(hud_path, "")
    errors = require_fragments(
        hud_path,
        hud,
        "compact HUD",
        (
            "HudWidth = 420f",
            "HudHeight = 280f",
            "GUILayout.BeginScrollView",
        ),
    )
    errors.extend(reject_fragments(
        hud_path,
        hud,
        "full-screen help overlay",
        (
            "Click inside Game view before using controls",
            "WASD / arrows pan",
            "Select dwarf, then LMB tunnel destination",
            "RMB terrain toggles digging",
        ),
    ))
    return errors


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

    errors: list[str] = check_side_view_contracts(texts)
    errors.extend(check_tunnel_contracts(texts))
    errors.extend(check_hud_contracts(texts))
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
