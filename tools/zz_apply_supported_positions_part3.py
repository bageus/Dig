from pathlib import Path


def replace_required(path: str, old: str, new: str) -> None:
    p = Path(path)
    text = p.read_text()
    if old not in text:
        raise SystemExit(f"Required text not found in {path}: {old[:120]!r}")
    p.write_text(text.replace(old, new, 1))


def append_once(path: str, marker: str, content: str) -> None:
    p = Path(path)
    text = p.read_text()
    if marker not in text:
        p.write_text(text.rstrip() + "\n\n" + content.strip() + "\n")

mushdoc = "docs/design/mushroom-growth-and-chopping.md"
replace_required(
    mushdoc,
    "5. Resident освобождается от несовместимого небоевого direct action, получает route к допустимой work position и идёт к грибу.\n",
    """5. Resident освобождается от несовместимого небоевого direct action, получает route к допустимой work position и идёт к грибу. Work position обязана находиться на той же высоте `Y`, быть соседней по `X` или depth `Z` и иметь полную ровную actor support surface. Вертикальные `Y±1`, shaft-gap и partial-support клетки запрещены. Если боковые `X±1` клетки являются пропастью, resolver обязан рассмотреть поддерживаемые `Z±1` позиции за/перед грибом до blocked result.
""")
replace_required(
    mushdoc,
    "- unreachable work position возвращает typed reason и не создаёт успешную axe feedback;\n",
    """- unreachable или unsupported work position возвращает typed reason и не создаёт успешную axe feedback;
- потеря полной опоры после создания job отменяет текущую chop attempt до следующего swing; retry заново разрешает same-height `X/Z` work position;
""")
replace_required(
    mushdoc,
    "- Presentation не хранит authoritative growth/chop progress.\n",
    """- Presentation не хранит authoritative growth/chop progress.
- resident никогда не выполняет mushroom swing в воздухе, на вертикальной соседней клетке или над частично выкопанной опорой.
""")
append_once(
    mushdoc,
    "## 13. Decision log",
    """## 13. Decision log

| Date | Decision | Source |
|---|---|---|
| 2026-07-30 | Любая mushroom work position находится на той же высоте, имеет полную опору и выбирается по X/Z; supported depth position используется, когда боковые клетки являются пропастью. | user, #423 |""")

fooddoc = "docs/design/campfire-cooking-and-food-use.md"
replace_required(
    fooddoc,
    """Direct and autonomous food use share one Agent meal owner.

A grilled mushroom portion provides""",
    """Direct and autonomous food use share one Agent meal owner.

`Eat` является stationary action: meal может начаться и продолжаться только когда resident стоит в клетке с полной ровной actor support surface. Shaft gap, воздух и partial-support клетка запрещены. Guard выполняется до reservation/consume, поэтому direct inventory use на неподдерживаемой позиции возвращает `resident.food_meal.unsupported_standing_position`, сохраняет порцию и не создаёт active meal. После world pickup тот же guard либо начинает meal на поддерживаемой source cell, либо оставляет порцию в inventory для безопасного retry. Если опора исчезает во время meal, action прерывается до следующего bite по обычному interruption contract.

A grilled mushroom portion provides""")
replace_required(
    fooddoc,
    "Domain/application tests must cover recipe quantity, protected source filtering, one dependency chop, deterministic output-ring placement, duration at Cooking 0/25/100, exactly-once completion, direct pickup/eat reservation, three bites, interruption and retry.\n",
    "Domain/application tests must cover recipe quantity, protected source filtering, one dependency chop, deterministic output-ring placement, duration at Cooking 0/25/100, exactly-once completion, direct pickup/eat reservation, supported-standing guard before consume, three bites, support-loss interruption and retry.\n")

append_once(
    "docs/design/needs-continuous-actions.md",
    "## Supported stationary action position",
    """## Supported stationary action position

`Eat`, `Sleep`, `Leisure` и любая stationary work phase не могут выполнять progress без полной actor support surface под resident. Для текущего food slice `Eat` проверяет это условие до consume и перед каждым следующим simulation advance; потеря опоры прерывает действие typed reason без replay уже применённых bite effects. Конкретные target-based jobs используют same-height action positions по горизонтальным осям `X/Z`, а не вертикальный `Y` offset.""")
append_once(
    "docs/design/resident-movement-occupancy-and-vertical-traversal.md",
    "## Supported stationary actions",
    """## Supported stationary actions

После traversal resident может начать work/eat только в клетке с полной ровной actor support surface. `SupportedWalk` и поддерживаемый `DepthTraverse` допустимы для подхода; `VerticalClimb`, `ShaftGapTraverse`, воздух и partial support не являются action position. Target-adjacent selectors рассматривают same-height `X/Z` neighbours, поэтому depth-позиция за объектом имеет такой же статус, как позиция слева/справа.""")
append_once(
    "docs/systems/README.md",
    "## Supported stationary action correction — 2026-07-30",
    """## Supported stationary action correction — 2026-07-30

- Mushroom work positions and food meals: `IMPLEMENTED` pending licensed Unity runtime verification.
- Authoritative rules: [`mushroom-growth-and-chopping.md`](../design/mushroom-growth-and-chopping.md), [`campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md), [`needs-continuous-actions.md`](../design/needs-continuous-actions.md), [`resident-movement-occupancy-and-vertical-traversal.md`](../design/resident-movement-occupancy-and-vertical-traversal.md).
- Tracking: #423, #459, PR #521.
- Evidence: [`supported-stationary-action-positions-2026-07-30.md`](../implementation/supported-stationary-action-positions-2026-07-30.md).""")

Path("docs/implementation/supported-stationary-action-positions-2026-07-30.md").write_text(r"""# Supported stationary action positions — 2026-07-30

Status: `IMPLEMENTED` pending licensed Unity Play Mode verification.

Tracking: #423, #459, PR #521.

## Root cause

Mushroom work-position resolution treated vertical `Y±1` cells as neighbours and did not require full actor support. In Dig coordinates `Y` is vertical and `Z` is depth, so side voids could make a resident select an airborne or vertically displaced work cell while a valid supported cell existed behind the mushroom.

Food meal start consumed the carried portion before any world-support policy was consulted. Active meals were advanced by Agent autonomy even when the resident cell no longer had full support.

## Correction

The shared Unity stationary-action policy now:
- generates same-height neighbours on `X±1` and bounded depth `Z±1`;
- requires `HasFullActorSupport` below every stationary action cell;
- permits only supported walk/depth transitions for mushroom approach;
- revalidates support before mushroom swings;
- guards meal start before reservation/consume and interrupts an active meal before another bite when support is lost.

The mushroom resolver therefore selects a supported depth cell when left/right cells are void instead of allowing airborne work.

## Evidence

Fast .NET regressions cover the selector source contract and quantity-safe meal rejection. A checked-in Unity Play Mode scenario boots the real demo world and calls the actual mushroom resolver for a side-void/depth-supported case. Hosted Unity execution may remain blocked when activation is unavailable; do not promote the systems to `VERIFIED` without licensed runtime results.
""")
