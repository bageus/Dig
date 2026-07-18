from __future__ import annotations

from pathlib import Path


def dump_runtime_references(
    root: Path,
    texts: dict[Path, str],
) -> None:
    needles = (
        "ClearSelection(",
        ".Select(null",
        "SetAgentSelection(null",
        "AgentRenderer.Render(",
        "private void Update()",
        "protected void Update()",
        "public void Update()",
    )
    print("UNITY RUNTIME REFERENCE DIAGNOSTICS")
    for path in sorted(texts):
        lines = texts[path].splitlines()
        matches: list[str] = []
        for line_number, line in enumerate(lines, start=1):
            if any(needle in line for needle in needles):
                matches.append(f"  {line_number}: {line.strip()}")
        if matches:
            print(path.relative_to(root))
            for match in matches:
                print(match)
