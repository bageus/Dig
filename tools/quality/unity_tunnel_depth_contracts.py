from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_tunnel_depth_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
) -> list[str]:
    interaction = runtime_root / "DigWorldInteraction.cs"
    drawing = runtime_root / "DigWorldInteraction.Excavation.cs"
    depth_input = runtime_root / "DigWorldInteraction.TunnelDepthExcavation.cs"
    session = runtime_root / "DigAgentSession.TunnelMovement.cs"
    runtime = runtime_root / "DigAgentSimulationDriverBase.CaveRooms.cs"
    hud = runtime_root / "DigHudOverlay.Excavation.cs"
    errors: list[str] = []
    errors.extend(require_fragments(
        interaction,
        texts.get(interaction, ""),
        "tunnel depth input priority",
        (
            "TryHandleCaveRoomPlacement()",
            "TryHandleTunnelDepthExcavation()",
            "TryHandleExcavationStroke()",
        ),
    ))
    errors.extend(require_fragments(
        drawing,
        texts.get(drawing, ""),
        "single-click depth mode",
        (
            "Depth = 3",
            "DigExcavationDrawingMode.Depth",
            "excavate one deeper layer",
            "if (_excavationMode == DigExcavationDrawingMode.Depth)",
        ),
    ))
    errors.extend(require_fragments(
        depth_input,
        texts.get(depth_input, ""),
        "one-layer tunnel depth interaction",
        (
            "Input.GetMouseButtonDown(0)",
            "ResolveTunnelDepthSource",
            "TryGetWalkSurface",
            "TryGetCell",
            "ExcavateTunnelDepth",
            "RefreshCompletedCaveRooms(force: true)",
            "Select the new cell to continue one layer deeper",
        ),
    ))
    errors.extend(require_fragments(
        session,
        texts.get(session, ""),
        "authoritative tunnel depth ownership",
        (
            "TunnelDepthExcavationPolicy",
            "_tunnelDepthExcavations",
            "TunnelDepthExcavations",
            "ExcavateTunnelDepth",
            "ExpandTunnelVolume(new[] { target })",
        ),
    ))
    errors.extend(require_fragments(
        runtime,
        texts.get(runtime, ""),
        "tunnel depth runtime refresh",
        (
            "AgentSession.ExcavateTunnelDepth(source)",
            "tunnelRenderer.Initialize(AgentSession.TunnelVolume)",
            "AgentSession.TunnelDepthExcavations",
        ),
    ))
    errors.extend(require_fragments(
        hud,
        texts.get(hud, ""),
        "tunnel depth controls",
        (
            'GUILayout.Button("Depth"',
            "DigExcavationDrawingMode.Depth",
            "each click opens only Z+1, up to Z=3",
        ),
    ))
    return errors
