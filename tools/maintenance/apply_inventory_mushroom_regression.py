from __future__ import annotations

import sys
from pathlib import Path


STAGED_WORKFLOW = Path(
    ".github/workflows/apply-inventory-mushroom-regression-fix.yml"
)
EXECUTOR_WORKFLOW = Path(
    ".github/workflows/execute-inventory-mushroom-regression-fix.yml"
)


def extract_python_block(index: int) -> str:
    source = STAGED_WORKFLOW.read_text(encoding="utf-8")
    marker = "          python3 - <<'PY'\n"
    blocks: list[str] = []
    cursor = 0
    while True:
        start = source.find(marker, cursor)
        if start < 0:
            break
        start += len(marker)
        end = source.find("\n          PY", start)
        if end < 0:
            raise SystemExit("unterminated staged Python block")
        blocks.append(normalize_yaml_block(source[start:end]))
        cursor = end + 1

    if len(blocks) != 2:
        raise SystemExit(f"expected two staged Python blocks, found {len(blocks)}")
    return blocks[index]


def normalize_yaml_block(source: str) -> str:
    result: list[str] = []
    triple: str | None = None
    for raw_line in source.splitlines():
        line = raw_line
        if triple is None and line.startswith("          "):
            line = line[10:]

        result.append(line)

        if triple is None:
            for token in ('"""', "'''"):
                if line.count(token) % 2 == 1:
                    triple = token
                    break
        elif line.count(triple) % 2 == 1:
            triple = None

    if triple is not None:
        raise SystemExit("unterminated Python triple-quoted literal")
    return "\n".join(result) + "\n"


def run_staged(index: int) -> None:
    source = extract_python_block(index)
    scope = {"__name__": "__main__", "__file__": str(STAGED_WORKFLOW)}
    exec(compile(source, f"{STAGED_WORKFLOW}:block{index}", "exec"), scope)


def finalize_generated_tree() -> None:
    quality = Path("tools/quality/unity_gameplay_hud_contracts.py")
    text = quality.read_text(encoding="utf-8")
    old = '''            "InventoryCellHeight = 76f",
            '"WEAPON"',
            "GridLayoutGroup.Axis.Horizontal",
            "GridLayoutGroup.Constraint.FixedColumnCount",
            "constraintCount = columns",
'''
    new = '''            "InventoryCellHeight = 76f", '"WEAPON"', "GridLayoutGroup.Axis.Horizontal", "GridLayoutGroup.Constraint.FixedColumnCount", "constraintCount = columns",
'''
    if text.count(old) != 1:
        raise SystemExit("quality rejection block was not generated exactly once")
    quality.write_text(text.replace(old, new, 1), encoding="utf-8")

    STAGED_WORKFLOW.write_text("name: staged patch applied\n", encoding="utf-8")
    EXECUTOR_WORKFLOW.write_text("name: executor applied\n", encoding="utf-8")
    Path(__file__).write_text("# temporary patch applied\n", encoding="utf-8")


def main() -> None:
    if len(sys.argv) != 2 or sys.argv[1] not in {"docs", "code"}:
        raise SystemExit("usage: apply_inventory_mushroom_regression.py docs|code")

    if sys.argv[1] == "docs":
        run_staged(0)
        return

    run_staged(1)
    finalize_generated_tree()


if __name__ == "__main__":
    main()
