# Staged production package lifecycle — 2026-08-01

Статус: implementation находится в draft PR #536; automated CI и licensed Unity runtime evidence должны быть записаны до повышения system status.

Authoritative design: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).
Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

## Реализованный scope

- Production создаёт unfinished package как настоящую quantity-one Inventory entity в первой доступной world cell справа от building footprint.
- Right-side resolver не имеет фиксированного шестиклеточного лимита: занятые клетки пропускаются до правой границы мира; side/left/rear fallback отсутствует.
- Unfinished package сохраняет stable stack identity, order owner, lifecycle/version и manifest metadata; Presentation не даёт ей pickup/use affordance.
- Explicit cancel active order не обрывает текущую единицу: job/package/progress продолжаются до normal close и output commit.
- Forced direct movement production worker удаляет unfinished package, оставляет уже consumed materials потраченными, освобождает unused reservations, reset-ит тот же order в `Queued` с нулевым progress и не уменьшает counter.
- Building output трансформирует ту же package identity в обычную BuildingBox и передаёт её существующему BuildingBox lifecycle.
- Non-building output закрывается как package kind/item `food`, `weapon` или `tool`, содержит сохранённый recipe output manifest и не подбирается ordinary pickup.
- Closed non-building package получает animated `Use` cursor; selected resident выполняет direct travel/work/finalize job, ломает package и exactly once materialize-ит весь manifest в прежней world cell.
- Save/load codec сохраняет unfinished/closed package entities и active package-use job.

## Основные owners

- Domain: `ProductionOutputPackageState`, `ProductionState`, `InventoryState.ReplaceProductionPackage`, `ProductionOrderState.ResetForRetry`.
- Application: create/interrupt/complete package handlers, package-use lifecycle handlers, save adapters/codecs.
- Unity runtime: building production package creation/finalization, forced-command interruption, package-use navigation/execution, cursor/input routing.
- Presentation: `WorldItemInteractionKind.Use`, package-specific non-pickup projection.

## Regression coverage

Добавлены проверки:

- active explicit cancel finishes current unit;
- forced move wastes consumed inputs, removes package and retains queued counter;
- closed food package opens exactly once;
- BuildingBox output preserves package stack identity;
- unfinished package round-trips as Inventory entity;
- output placement skips more than six occupied right cells;
- package-use job codec round-trip;
- unfinished/closed package interaction projection;
- Unity source wiring for package creation, Finalize identity, forced interruption and animated use cursor.

## Evidence

Локально перед публикацией прошли:

- `python3 tools/quality/check_quality.py`;
- `python3 tools/quality/check_unity_source_contracts.py`;
- `python3 tools/quality/check_unity_item_visual_contracts.py`;
- `git diff --check` для опубликованного patch.

GitHub Actions build/test/smoke/soak и Unity runtime status будут добавлены после завершения checks текущего PR head. До executed licensed EditMode/PlayMode evidence система не является `VERIFIED`.
