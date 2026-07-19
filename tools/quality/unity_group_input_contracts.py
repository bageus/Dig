from __future__ import annotations

from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]
RejectFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]


def check_tunnel_and_group_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
    reject_fragments: RejectFragments,
) -> list[str]:
    agent_visual_path = runtime_root / "DigAgentVisual.cs"
    agent_renderer_path = runtime_root / "DigAgentRenderer.cs"
    bootstrap_path = runtime_root / "DigUnityBootstrap.cs"
    interaction_path = runtime_root / "DigWorldInteraction.cs"
    movement_path = runtime_root / "DigWorldInteraction.TunnelMovement.cs"
    selection_path = runtime_root / "DigWorldInteraction.Selection.cs"
    projection_path = runtime_root / "DigTunnelProjection.cs"
    tunnel_renderer_path = runtime_root / "DigTunnelDemoRenderer.cs"
    world_renderer_path = runtime_root / "DigWorldRenderer.cs"
    agent_session_path = runtime_root / "DigAgentSession.TunnelMovement.cs"
    driver_path = runtime_root / "DigAgentSimulationDriverBase.TunnelMovement.cs"
    session_path = runtime_root / "DigAgentSession.cs"
    errors: list[str] = []

    agent_visual = texts.get(agent_visual_path, "")
    errors.extend(require_fragments(
        agent_visual_path,
        agent_visual,
        "world-axis resident movement",
        (
            "model.CellZ",
            "transform.position",
            "DigTunnelProjection.ResidentWorldPosition",
            "Quaternion.LookRotation(Vector3.back, Vector3.up)",
            "PlayRoute(",
        ),
    ))
    errors.extend(reject_fragments(
        agent_visual_path,
        agent_visual,
        "rotated-root resident movement",
        ("transform.localPosition", "ResidentLocalPosition"),
    ))
    errors.extend(require_fragments(
        agent_renderer_path,
        texts.get(agent_renderer_path, ""),
        "multi-resident selection ownership",
        (
            "_selectedIds",
            "_selectionOrder",
            "SelectedAgentIds",
            "SelectedCount",
            "ToggleSelection",
            "ClearSelection",
            "PublishSelectionSnapshot",
        ),
    ))
    errors.extend(require_fragments(
        interaction_path,
        texts.get(interaction_path, ""),
        "global cancel and additive selection routing",
        (
            "Input.GetMouseButtonDown(1)",
            "CancelCurrentInteraction();",
            "IsAdditiveResidentSelectionPressed()",
            "ToggleResidentSelection(agent)",
            "TryApplyTunnelMove(hit, left)",
        ),
    ))
    errors.extend(require_fragments(
        selection_path,
        texts.get(selection_path, ""),
        "global interaction cancellation",
        (
            "KeyCode.LeftShift",
            "KeyCode.RightShift",
            "DisableExcavationDrawing();",
            "DisableCaveRoomPlanning();",
            "CancelBuildingPlacement();",
            "_agentRenderer!.ClearSelection();",
            "_jobRenderer!.Select(null);",
            "_buildingRenderer!.Select(null);",
            "_tunnelRenderer?.ShowRoute(null);",
        ),
    ))
    errors.extend(require_fragments(
        movement_path,
        texts.get(movement_path, ""),
        "group destination movement",
        (
            "SelectedAgentIds",
            "MoveResidentsThroughTunnel",
            "residentIds.Count",
            "SpatialCellId destination",
        ),
    ))
    errors.extend(require_fragments(
        agent_session_path,
        texts.get(agent_session_path, ""),
        "group movement composition",
        (
            "MoveAgentsThroughTunnelCommandHandler",
            "MoveResidentsThroughTunnel",
            "_groupTunnelMovement",
            "_manualTunnelOrders.Add",
        ),
    ))
    errors.extend(require_fragments(
        driver_path,
        texts.get(driver_path, ""),
        "group route animation",
        (
            "MoveAgentsThroughTunnelReport",
            "report.Entries",
            "AgentRenderer.AnimateRoute",
            "AgentRenderer.SelectedCount",
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
            "x4 rock volume",
        ),
    ))
    errors.extend(require_fragments(
        projection_path,
        texts.get(projection_path, ""),
        "explicit XYZ world projection",
        (
            "CellWorldPosition",
            "ResidentWorldPosition",
            "-cell.Y",
            "cell.Z * DepthSpacing",
        ),
    ))
    tunnel_renderer = texts.get(tunnel_renderer_path, "")
    errors.extend(require_fragments(
        tunnel_renderer_path,
        tunnel_renderer,
        "XZ depth platforms with logical XY shafts",
        (
            "Walkable plane",
            "DigTunnelProjection.FloorWorldPosition",
            "if (vertical || cell.Z == 0",
            "Layered Tunnel Floors",
            "SetPositionAndRotation",
            "_route.useWorldSpace = true",
        ),
    ))
    errors.extend(reject_fragments(
        tunnel_renderer_path,
        tunnel_renderer,
        "synthetic shaft and cave frame geometry",
        (
            '"Shaft {cell}"',
            "Cave ceiling",
            "Cave back wall",
            "CreateCaveShell",
            "_verticalMaterial",
        ),
    ))
    errors.extend(require_fragments(
        world_renderer_path,
        texts.get(world_renderer_path, ""),
        "terrain cutaway",
        ("SetTunnelCutaway", "CaveCeilingY", "ApplyTunnelCutaway();"),
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


def check_group_hud_contracts(
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
    reject_fragments: RejectFragments,
) -> list[str]:
    hud_path = runtime_root / "DigHudOverlay.cs"
    hud = texts.get(hud_path, "")
    errors = require_fragments(
        hud_path,
        hud,
        "collapsible multi-selection HUD",
        (
            "HudWidth = 420f",
            "CollapsedHudHeight = 34f",
            "CurrentHudHeight",
            '_isCollapsed ? "+" : "-"',
            "GUILayout.BeginScrollView",
            "_selectedAgentCount",
            "SELECTED RESIDENTS:",
            "SetAgentSelection(AgentViewModel? selected, int selectedCount)",
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
