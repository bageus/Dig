#!/usr/bin/env python3
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RUNTIME = ROOT / "unity" / "Dig.Unity" / "Assets" / "Dig.Unity" / "Runtime"
BOOTSTRAP = RUNTIME / "DigUnityBootstrap.cs"
HUD = RUNTIME / "DigGameHudCanvas.cs"
MINIMAP = RUNTIME / "DigGameHudCanvas.Minimap.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""


def require(path: Path, text: str, fragments: tuple[str, ...]) -> list[str]:
    return [
        f"{path.relative_to(ROOT)}: missing early adaptive HUD contract {fragment!r}"
        for fragment in fragments
        if fragment not in text
    ]


def main() -> int:
    bootstrap = read(BOOTSTRAP)
    hud = read(HUD)
    minimap = read(MINIMAP)
    errors: list[str] = []
    errors.extend(require(BOOTSTRAP, bootstrap, (
        "DigGameHudCanvas gameHud = CreateStartupGameHud(hud);",
        "StartRuntime(hud, gameHud);",
        "gameHud.InitializeStartup(hud);",
        "hud.AttachGameHudCanvas(gameHud);",
        '_startupStage = "creating world";',
        '_startupStage = "binding uGUI game HUD";',
        "gameHud.Initialize(",
    )))
    create_index = bootstrap.find("CreateStartupGameHud(hud)")
    start_index = bootstrap.find("StartRuntime(hud, gameHud)")
    world_index = bootstrap.find('_startupStage = "creating world";')
    bind_index = bootstrap.find('_startupStage = "binding uGUI game HUD";')
    if min(create_index, start_index, world_index, bind_index) < 0:
        errors.append(f"{BOOTSTRAP.relative_to(ROOT)}: adaptive HUD startup order is incomplete")
    elif not create_index < start_index < world_index < bind_index:
        errors.append(
            f"{BOOTSTRAP.relative_to(ROOT)}: adaptive HUD must attach before runtime creation "
            "and bind after runtime initialization"
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
    if errors:
        print("Unity adaptive HUD startup contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("PASS: adaptive HUD attaches before startup and binds safely afterward")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
