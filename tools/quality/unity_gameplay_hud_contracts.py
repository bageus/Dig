from __future__ import annotations

import re
from pathlib import Path
from typing import Callable

RequireFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]
RejectFragments = Callable[[Path, str, str, tuple[str, ...]], list[str]]
CYRILLIC = re.compile(r"[А-Яа-яЁё]")


def check_gameplay_hud_and_work_contracts(
    root: Path,
    runtime_root: Path,
    texts: dict[Path, str],
    require_fragments: RequireFragments,
    reject_fragments: RejectFragments,
) -> list[str]:
    hud_path = runtime_root / "DigGameHudCanvas.cs"
    layout_path = runtime_root / "DigGameHudCanvas.Layout.cs"
    factory_path = runtime_root / "DigGameHudCanvas.Factory.cs"
    roster_path = runtime_root / "DigGameHudCanvas.Roster.cs"
    resident_rows_path = runtime_root / "DigGameHudCanvas.ResidentRows.cs"
    inventory_path = runtime_root / "DigGameHudCanvas.Inventory.cs"
    context_path = runtime_root / "DigGameHudCanvas.Context.cs"
    minimap_path = runtime_root / "DigGameHudCanvas.Minimap.cs"
    minimap_pointer_path = runtime_root / "DigMinimapPointer.cs"
    clock_path = runtime_root / "DigGameHudCanvas.Clock.cs"
    camera_path = runtime_root / "DigCameraController.cs"
    bootstrap_path = runtime_root / "DigUnityBootstrap.cs"
    interaction_path = runtime_root / "DigWorldInteraction.CanvasHud.cs"
    agent_session_path = runtime_root / "DigAgentSession.cs"
    agent_hud_path = runtime_root / "DigAgentSession.Hud.cs"
    driver_path = runtime_root / "DigAgentSimulationDriverBase.cs"
    driver_hud_path = runtime_root / "DigAgentSimulationDriverBase.Hud.cs"
    room_session_path = runtime_root / "DigWorldSession.CaveRooms.cs"
    loop_path = runtime_root / "DigAgentSimulationDriverBase.Loop.cs"
    router_path = (
        root
        / "src"
        / "Dig.Presentation.Abstractions"
        / "Input"
        / "ContextInputRouter.World.cs"
    )
    router_text = router_path.read_text(encoding="utf-8-sig") if router_path.exists() else ""

    errors = require_fragments(
        hud_path,
        texts.get(hud_path, ""),
        "adaptive HUD shell and input shielding",
        (
            "Jobs = 2",
            '"●"',
            '"■"',
            '"▲"',
            "_canvas.pixelPerfect = true;",
            "scaler.matchWidthOrHeight = 0.5f;",
            "CreateMinimapShell();",
            "CreateClockShell();",
            "ApplyResponsiveLayout();",
            "Contains(_minimapPanel, screenPoint)",
            "Contains(_clockPanel, screenPoint)",
            '"Notification Ticker"',
        ),
    )
    errors.extend(require_fragments(
        layout_path,
        texts.get(layout_path, ""),
        "non-overlapping canvas-space responsive HUD anchors",
        (
            "MinimumSidePanelWidth",
            "MaximumRosterWidth",
            "RectTransform canvasRect = (RectTransform)transform",
            "canvasRect.rect.width",
            "canvasRect.rect.height",
            "_statusPanel!",
            "_minimapPanel!",
            "_clockPanel!",
            "_rightPanel!",
            "_bottomPanel!",
            "SetBottomPanelHeight",
        ),
    ))
    errors.extend(require_fragments(
        factory_path,
        texts.get(factory_path, ""),
        "sharp button and generated room icon rendering",
        (
            "child.SetActive(false);",
            "Destroy(child);",
            "FontStyle.Bold",
            "Outline outline",
            "CreateRoomIconButton(",
            "CreateIconBar(",
            "raycastTarget = false;",
        ),
    ))
    errors.extend(require_fragments(
        minimap_path,
        texts.get(minimap_path, ""),
        "zoomable bottom-left minimap",
        (
            "RenderTexture",
            "orthographic = true",
            "DigSideViewProjection.WorldCenter",
            "ZoomMinimap",
            '"Minimap Zoom Out"',
            '"Minimap Zoom In"',
            "MinimumMinimapZoom",
            "MaximumMinimapZoom",
            "DisposeMinimap",
        ),
    ))
    errors.extend(require_fragments(
        minimap_pointer_path,
        texts.get(minimap_pointer_path, ""),
        "minimap mouse wheel input",
        ("IScrollHandler", "eventData.scrollDelta.y", "Scrolled?.Invoke"),
    ))
    errors.extend(require_fragments(
        camera_path,
        texts.get(camera_path, ""),
        "world-camera scroll isolation over HUD",
        (
            "using UnityEngine.EventSystems;",
            "EventSystem.current.IsPointerOverGameObject()",
            "Input.mouseScrollDelta.y",
        ),
    ))
    errors.extend(require_fragments(
        clock_path,
        texts.get(clock_path, ""),
        "game clock and selected-resident work schedule",
        (
            '"DAY 1 · 00:00"',
            '"WORK TIME"',
            '"REST TIME"',
            "TryGetResidentWorkWindow",
            "SetResidentWorkWindow",
            "all other time is rest",
            "CreateClockTicks",
            "Mathf.Sin(angle) * radius",
        ),
    ))
    errors.extend(require_fragments(
        inventory_path,
        texts.get(inventory_path, ""),
        "compact active-only two-row resident inventory",
        (
            "InventoryColumns = 3",
            "InventoryCellWidth = 62f",
            "InventoryCellHeight = 52f",
            'MainCompartmentTitle = ""',
            "BeginBottomLayout(hasExpansion ? 184f : 150f)",
            "layout.childAlignment = TextAnchor.MiddleLeft",
            "GridLayoutGroup",
            "constraintCount = InventoryColumns",
            "BuildCompartmentIfActive",
        ),
    ))
    errors.extend(reject_fragments(
        inventory_path,
        texts.get(inventory_path, ""),
        "inactive placeholders, Main heading and oversized inventory",
        (
            '"MAIN · 6"',
            '"CARGO · NO EXPANSION"',
            'CreateHorizontalRow("Slots"',
            '"Unavailable"',
            "InventoryCellWidth = 82f",
            "InventoryCellHeight = 76f",
        ),
    ))
    errors.extend(require_fragments(
        roster_path,
        texts.get(roster_path, ""),
        "authoritative roster selection and tab switching",
        (
            "SelectRightPanelTab",
            "LoadResidentRoster",
            "ResidentRosterViewModel",
            "BuildResidentRows(residents)",
            "SelectBuildingFromHud(id)",
            "SelectJobFromHud(id)",
        ),
    ))
    errors.extend(require_fragments(
        resident_rows_path,
        texts.get(resident_rows_path, ""),
        "compact health schedule mood and expanded resident details",
        (
            "CreateCompactHealthBar",
            "CreateScheduleIndicator",
            '"W!"',
            "CreateMoodIndicator",
            "BuildResidentDetails",
            '"Nutrition"',
            '"Alertness"',
            "resident.Skills.TopFive",
            "NeedColor",
            "SelectResidentFromHud(resident.Id)",
        ),
    ))
    errors.extend(require_fragments(
        agent_hud_path,
        texts.get(agent_hud_path, ""),
        "authoritative resident HUD and schedule bridge",
        (
            "ResidentRosterPresenter",
            "LoadResidentRoster",
            "CreateSnapshot(_tick)",
            "TryGetWorkWindow",
            "SetAgentWorkRestWindowCommand",
        ),
    ))
    errors.extend(require_fragments(
        driver_hud_path,
        texts.get(driver_hud_path, ""),
        "simulation HUD adapter",
        (
            "LoadResidentRoster",
            "TryGetResidentWorkWindow",
            "SetResidentWorkWindow",
        ),
    ))
    errors.extend(require_fragments(
        bootstrap_path,
        texts.get(bootstrap_path, ""),
        "HUD world camera and schedule composition",
        (
            "agentSession.InitializeHudSchedule(worldSession.Journal);",
            "targetCamera,",
            "world);",
        ),
    ))
    errors.extend(require_fragments(
        interaction_path,
        texts.get(interaction_path, ""),
        "runtime job selection",
        ("SelectJobFromHud", "_jobRenderer!.SelectById(jobId)", "_hud!.SetJobSelection"),
    ))
    errors.extend(require_fragments(
        context_path,
        texts.get(context_path, ""),
        "single-row context using responsive height",
        (
            "SelectedJobId",
            "_terrainSession!.LoadJobs().FirstOrDefault",
            'CreateHorizontalRow("Excavation Tools", section, 56f)',
            "CreateRoomIconButton(",
            "SetBottomPanelHeight(height)",
        ),
    ))
    for path, text in texts.items():
        if path.name.startswith("DigGameHudCanvas") and CYRILLIC.search(text):
            errors.append(f"{path.relative_to(root)}: game HUD text must be English-only")

    errors.extend(require_fragments(
        agent_session_path,
        texts.get(agent_session_path, ""),
        "society-backed unique resident identity and sex projection",
        (
            "ResidentNameCatalog",
            "ResidentIdentityGenerator",
            "generator.CreateBirthPlan(",
            "usedNames.Add(identity.Name)",
            "Dictionary<EntityId, ResidentSex>",
            "residentSexes.Add(agent.Id, identity.Sex)",
            "ResolveResidentSex",
        ),
    ))
    errors.extend(require_fragments(
        driver_path,
        texts.get(driver_path, ""),
        "resident sex HUD adapter",
        ("ResidentSex ResolveResidentSex", "AgentSession?.ResolveResidentSex"),
    ))
    errors.extend(reject_fragments(
        room_session_path,
        texts.get(room_session_path, ""),
        "application-internal cave room result factory from Unity",
        ("CaveRoomPlanResult.Failure(",),
    ))

    loop_text = texts.get(loop_path, "")
    errors.extend(require_fragments(
        loop_path,
        loop_text,
        "worker release and automatic excavation reassignment",
        ("EnforceDirectMovementOwnership(nextTick)", "SynchronizeDesignations(nextTick, before)"),
    ))
    release_index = loop_text.find("EnforceDirectMovementOwnership(nextTick)")
    assignment_index = loop_text.find("SynchronizeDesignations(nextTick, before)")
    if release_index < 0 or assignment_index < 0 or release_index >= assignment_index:
        errors.append(f"{loop_path}: direct movement ownership must be released before assignment")
    errors.extend(reject_fragments(
        loop_path,
        loop_text,
        "automatic excavation rock hauling",
        ("SynchronizeHauling(nextTick, before)",),
    ))
    errors.extend(reject_fragments(
        router_path,
        router_text,
        "ordinary ground cell selection",
        ("PresentationInputEffect.SelectGround",),
    ))
    return errors
