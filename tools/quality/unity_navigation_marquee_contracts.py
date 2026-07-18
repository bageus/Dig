from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_navigation_and_marquee_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
) -> list[str]:
    interaction = runtime_root / "DigWorldInteraction.cs"
    marquee = runtime_root / "DigWorldInteraction.MarqueeSelection.cs"
    marquee_renderer = runtime_root / "DigSelectionMarqueeRenderer.cs"
    excavation = runtime_root / "DigWorldInteraction.Excavation.cs"
    world_session = runtime_root / "DigWorldSession.TunnelNavigation.cs"
    agent_session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    navigation_sync = runtime_root / "DigAgentSimulationDriverBase.NavigationSync.cs"
    movement = runtime_root / "DigAgentSimulationDriverBase.TunnelMovement.cs"
    errors: list[str] = []
    errors.extend(require_fragments(
        interaction,
        texts.get(interaction, ""),
        "marquee input wiring",
        (
            "InitializeResidentMarquee();",
            "TryHandleResidentMarqueeSelection()",
            "CancelResidentMarquee();",
        ),
    ))
    errors.extend(require_fragments(
        marquee,
        texts.get(marquee, ""),
        "resident marquee selection",
        (
            "MarqueeThresholdPixels",
            "GetComponentsInChildren<DigAgentVisual>",
            "WorldToScreenPoint",
            "CreateScreenRect",
            "IsMarqueeBlockingTarget",
            "SelectResidentsInsideMarquee",
            "SelectCell(cell)",
        ),
    ))
    errors.extend(require_fragments(
        marquee_renderer,
        texts.get(marquee_renderer, ""),
        "marquee rectangle rendering",
        (
            "private void OnGUI()",
            "GUI.DrawTexture",
            "Texture2D.whiteTexture",
            "Screen.height",
        ),
    ))
    errors.extend(require_fragments(
        excavation,
        texts.get(excavation, ""),
        "vertical tunnel planning",
        (
            "ExcavationStrokeAxis.Vertical",
            "SetVerticalTunnelPlan",
            "_excavationAnchor.Value",
        ),
    ))
    errors.extend(require_fragments(
        world_session,
        texts.get(world_session, ""),
        "vertical tunnel plan ownership",
        (
            "_plannedVerticalTunnelCells",
            "PlannedVerticalTunnelCells",
            "SetVerticalTunnelPlan",
        ),
    ))
    errors.extend(require_fragments(
        agent_session,
        texts.get(agent_session, ""),
        "front navigation synchronization",
        (
            "SynchronizeFrontNavigation",
            "WithSynchronizedFrontLayer",
            "CreateTunnelMovementHandlers();",
        ),
    ))
    errors.extend(require_fragments(
        navigation_sync,
        texts.get(navigation_sync, ""),
        "authoritative world navigation refresh",
        (
            "SynchronizeExcavatedTunnelNavigation",
            "WorldSession.LoadSnapshot()",
            "WorldSession.PlannedVerticalTunnelCells",
        ),
    ))
    errors.extend(require_fragments(
        movement,
        texts.get(movement, ""),
        "fresh single and group navigation",
        (
            "SynchronizeExcavatedTunnelNavigation();",
            "MoveResidentThroughTunnel",
            "MoveResidentsThroughTunnel",
            "residentIds.Count == 1",
        ),
    ))
    return errors
