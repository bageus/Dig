# Production material workbench/package lifecycle — 2026-08-02

Статус: `IMPLEMENTED` в ветке `bugfix/production-material-package-step-lifecycle`; tracking issues [#433](https://github.com/bageus/Dig/issues/433) и [#459](https://github.com/bageus/Dig/issues/459). Licensed Unity Play Mode evidence остаётся обязательным для `VERIFIED`.

Authoritative specifications:

- [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md);
- [`../design/campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md).

## Наблюдаемое поведение

Production worker корректно забирал одну reserved mushroom cap из внутреннего склада, но raw unit оставался видимым в resident inventory на всём протяжении processing timer. При обычной campfire duration `900` ticks и normal tick interval `0.8s` это составляло примерно двенадцать реальных минут. После завершения таймера material сразу считался consumed, а worker шёл к package только после всех material steps.

## Первопричина

- `ApplyProductionWorkHandler` одновременно владел processing и физическим consumption carried raw unit;
- `ProductionOrderState.AddMaterialWork` по завершению timer сразу увеличивал `_currentStepIndex` и мог переводить order в `ReadyToComplete`;
- runtime различал только «raw carried / not carried» и не имел authoritative workbench или processed-awaiting-package phase;
- package создавался после подхода к workstation, а не после отдельного первого подхода к finished-output zone;
- multi-material recipe не требовал отдельного package deposit после каждого material;
- save data сохранял только `CompletedTicks` и `IsConsumed`, поэтому не мог восстановить точку между processing и package deposit;
- Play Mode regression использовал duration `1`, поэтому shortcut завершался в одном цикле и не показывал длительное carried-состояние.

## Исправление

- `ProductionState` владеет material phases `AwaitingMaterial`, `StagedOnWorkbench`, `Processing`, `ProcessedAwaitingPackage`, `Deposited`;
- `CarriedRaw` остаётся физическим состоянием `InventoryState` и определяется exact order reservation в resident slot;
- отдельный stage command consumes carried raw unit на workbench exactly once до первого processing tick;
- `ApplyProductionWorkHandler` больше не расходует Inventory и не завершает material step;
- processing timer переводит step только в `ProcessedAwaitingPackage`;
- отдельный deposit command валидирует order-owned unfinished package и только затем заполняет material segment;
- следующий material нельзя получить, пока предыдущий processed step не deposited;
- последний deposit переводит order в `ReadyToComplete` и job в `Finalize`; close выполняется у той же package position;
- claimed worker сначала идёт к right output cell и создаёт unfinished package, затем выполняет `stock -> workbench -> package` для каждого material;
- save data сохраняет explicit phase; legacy partial progress consumes прежний reserved raw unit в migration и восстанавливает processing без duplication;
- package creation, raw staging, processing, deposit и close остаются generic и не содержат campfire-specific runtime branch.

## Regression coverage

- application test проверяет carried raw, workbench removal до processing, partial processing, processed-awaiting-package и final deposit;
- progressive multi-material tests используют отдельное staging/deposit для каждого unit;
- save/load test восстанавливает mid-processing phase и продолжает обязательные последующие deposits;
- source-contract test запрещает Inventory consumption в `ApplyProductionWorkHandler` и требует stage/deposit handlers;
- checked-in Play Mode scenario использует multi-tick duration и наблюдает package-before-acquire, carried raw, staged/processing без raw icon, processed-awaiting-package, deposit и closed output.

## Evidence boundary

Repository quality, Release build, .NET tests, headless smoke and deterministic soak устанавливают `IMPLEMENTED`. Реальное перемещение resident, визуальное исчезновение raw icon, material progress segment, package deposit/close и повторный пользовательский цикл считаются `VERIFIED` только после licensed Unity EditMode/PlayMode execution.
