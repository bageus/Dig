#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RUNTIME = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime"
BOOTSTRAP = RUNTIME / "DigUnityBootstrap.cs"
HUD = RUNTIME / "DigGameHudCanvas.cs"
MINIMAP = RUNTIME / "DigGameHudCanvas.Minimap.cs"
RESIDENT_RENDERER = RUNTIME / "DigAgentRenderer.ResidentRig.cs"
RESIDENT_VISUAL = RUNTIME / "DigAgentVisual.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing runtime recovery contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def main() -> int:
    bootstrap = read(BOOTSTRAP)
    hud = read(HUD)
    minimap = read(MINIMAP)
    resident_renderer = read(RESIDENT_RENDERER)
    resident_visual = read(RESIDENT_VISUAL)
    errors: list[str] = []
    errors.extend(require(BOOTSTRAP, bootstrap, (
        "DigGameHudCanvas gameHud = CreateStartupGameHud(hud);",
        "StartRuntime(hud, gameHud);",
        "gameHud.InitializeStartup(hud);",
        "hud.AttachGameHudCanvas(gameHud);",
        '_startupStage = "creating world";',
        '_startupStage = "initializing interaction and simulation";',
        '_startupStage = "binding uGUI game HUD";',
        "gameHud.Initialize(",
        "RunPresentationStage(",
        '"rendering residents"',
        '"rendering buildings"',
        '"rendering navigation routes"',
        "Runtime started with {visualWarnings.Count} visual warning(s)",
    )))
    create_index = bootstrap.find("CreateStartupGameHud(hud)")
    start_index = bootstrap.find("StartRuntime(hud, gameHud)")
    world_index = bootstrap.find('_startupStage = "creating world";')
    bind_index = bootstrap.find('_startupStage = "binding uGUI game HUD";')
    render_index = bootstrap.find('"rendering world terrain"')
    if min(create_index, start_index, world_index, bind_index, render_index) < 0:
        errors.append(f"{BOOTSTRAP.relative_to(ROOT)}: adaptive HUD recovery order is incomplete")
    elif not create_index < start_index < world_index < bind_index < render_index:
        errors.append(
            f"{BOOTSTRAP.relative_to(ROOT)}: adaptive HUD must attach before runtime creation, "
            "bind before optional visual rendering, and keep rendering isolated"
        )
    errors.extend(require(HUD, hud, (
        "internal void InitializeStartup(DigHudOverlay legacyHud)",
        "CreateCanvasShell();",
        "if (_canvas != null)",
        "InitializeMinimapCamera();",
        "_initialized = true;",
    )))
    errors.extend(require(MINIMAP, minimap, (
        "if (_minimapCamera != null",
        "|| _mainCamera == null",
        "|| _world == null",
    )))
    errors.extend(require(RESIDENT_RENDERER, resident_renderer, (
        "catch (Exception exception)",
        "CreatePrimitiveResidentAgent(model);",
        "PrimitiveType.Capsule",
        "InitializeSimple(model, _normalMaterial!, _selectedMaterial!)",
        "AttachEquipmentSafely",
    )))
    errors.extend(require(RESIDENT_VISUAL, resident_visual, (
        "internal void InitializeSimple(",
        "_rig = null;",
        "GetComponentsInChildren<Renderer>(includeInactive: true)",
    )))
    if errors:
        print("Unity adaptive HUD and runtime recovery contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("PASS: adaptive HUD binds before isolated rendering and residents have a fallback")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
