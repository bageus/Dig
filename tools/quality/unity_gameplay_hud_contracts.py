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
    factory_path = runtime_root / "DigGameHudCanvas.Factory.cs"
    roster_path = runtime_root / "DigGameHudCanvas.Roster.cs"
    inventory_path = runtime_root / "DigGameHudCanvas.Inventory.cs"
    context_path = runtime_root / "DigGameHudCanvas.Context.cs"
    interaction_path = runtime_root / "DigWorldInteraction.CanvasHud.cs"
    agent_session_path = runtime_root / "DigAgentSession.cs"
    driver_path = runtime_root / "DigAgentSimulationDriverBase.cs"
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
        "upper-right icon roster tabs, top notifications and crisp canvas",
        (
            "Jobs = 2",
            '"●"',
            '"■"',
            '"▲"',
            "_jobTabButton",
            "-356f, -596f, -20f, -20f",
            "_canvas.pixelPerfect = true;",
            "new Vector2(1280f, 720f)",
            "scaler.matchWidthOrHeight = 0f;",
            "-620f, 14f, 620f, 112f",
            "Anchor(statusPanel, 0f, 1f, 1f, 1f, 20f, -44f, -376f, -8f);",
            "statusPanel.GetComponent<Image>().raycastTarget = false;",
        ),
    )
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
        inventory_path,
        texts.get(inventory_path, ""),
        "compact active-only two-row resident inventory",
        (
            "InventoryColumns = 3",
            "InventoryCellWidth = 62f",
            "InventoryCellHeight = 52f",
            "InventoryCellSpacing = 4f",
            'MainCompartmentTitle = ""',
            "BeginBottomLayout(hasExpansion ? 184f : 150f)",
            "ConfigureInventoryRootLayout",
            "layout.childAlignment = TextAnchor.MiddleLeft",
            "sectionElement.flexibleWidth = 0f",
            "GridLayoutGroup",
            "FixedColumnCount",
            "constraintCount = InventoryColumns",
            "BuildCompartmentIfActive",
            "inventory.GetCompartment(compartment).Count > 0",
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
        "one-line gender-colored selectable roster",
        (
            "SelectRightPanelTab",
            "LoadJobs()",
            "ResolveResidentSex(id)",
            "ResidentSex.Female",
            '"#FF83B9"',
            '"#70B7FF"',
            "ConfigureSingleLineRosterRow",
            "HorizontalWrapMode.Overflow",
            "resizeTextForBestFit = true",
            "SelectResidentFromHud(id)",
            "SelectBuildingFromHud(id)",
            "SelectJobFromHud(id)",
        ),
    ))
    errors.extend(reject_fragments(
        roster_path,
        texts.get(roster_path, ""),
        "multi-line roster entries",
        ('+ $"\\nHealth', '+ $"\\nCell', '+ $"\\n{job.Status}'),
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
        "single-row authoritative excavation and job context",
        (
            "SelectedJobId",
            "_terrainSession!.LoadJobs().FirstOrDefault",
            'CreateHorizontalRow("Excavation Tools", section, 56f)',
            "CreateRoomIconButton(",
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
