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
    context_path = runtime_root / "DigGameHudCanvas.Context.cs"
    interaction_path = runtime_root / "DigWorldInteraction.CanvasHud.cs"
    agent_session_path = runtime_root / "DigAgentSession.cs"
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
        "upper-right icon roster tabs",
        (
            "Jobs = 2",
            '"●"',
            '"■"',
            '"▲"',
            "_jobTabButton",
            "-356f, -596f, -20f, -20f",
        ),
    )
    errors.extend(require_fragments(
        factory_path,
        texts.get(factory_path, ""),
        "immediate tab content replacement",
        (
            "child.SetActive(false);",
            "Destroy(child);",
        ),
    ))
    errors.extend(require_fragments(
        roster_path,
        texts.get(roster_path, ""),
        "selectable resident building and job roster",
        (
            "SelectRightPanelTab",
            "_lastRosterSignature = string.Empty;",
            "RefreshRoster();",
            "LoadJobs()",
            "SelectResidentFromHud(id)",
            "SelectBuildingFromHud(id)",
            "SelectJobFromHud(id)",
            '"No dwarfs"',
            '"No completed buildings"',
            '"No jobs"',
        ),
    ))
    errors.extend(require_fragments(
        interaction_path,
        texts.get(interaction_path, ""),
        "runtime job selection",
        (
            "SelectJobFromHud",
            "_jobRenderer!.SelectById(jobId)",
            "_hud!.SetJobSelection",
        ),
    ))
    errors.extend(require_fragments(
        context_path,
        texts.get(context_path, ""),
        "authoritative selected job context",
        (
            "SelectedJobId",
            "_terrainSession!.LoadJobs().FirstOrDefault",
            'CreateButton("Small Room", rooms, "□"',
            'CreateButton("Tall Room", rooms, "▯"',
        ),
    ))
    errors.extend(reject_fragments(
        context_path,
        texts.get(context_path, ""),
        "unsupported job renderer model lookup",
        ("_jobRenderer!.SelectedModel", "КОПКА"),
    ))
    for path, text in texts.items():
        if not path.name.startswith("DigGameHudCanvas"):
            continue
        if CYRILLIC.search(text):
            errors.append(f"{path.relative_to(root)}: game HUD text must be English-only")

    errors.extend(require_fragments(
        agent_session_path,
        texts.get(agent_session_path, ""),
        "society-backed unique grounded demo residents",
        (
            "ResidentNameCatalog",
            "Dig.Domain.Society.ResidentIdentityGenerator",
            "generator.CreateBirthPlan(",
            "usedNames.Add(identity.Name)",
            "identity.Position.Y,",
            "0));",
        ),
    ))

    loop_text = texts.get(loop_path, "")
    errors.extend(require_fragments(
        loop_path,
        loop_text,
        "worker release and automatic excavation reassignment",
        (
            "EnforceDirectMovementOwnership(nextTick)",
            "SynchronizeDesignations(nextTick, before)",
        ),
    ))
    release_index = loop_text.find("EnforceDirectMovementOwnership(nextTick)")
    assignment_index = loop_text.find("SynchronizeDesignations(nextTick, before)")
    if release_index < 0 or assignment_index < 0 or release_index >= assignment_index:
        errors.append(
            f"{loop_path}: direct movement ownership must be released before assignment"
        )
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
