# Staged production package lifecycle — 2026-08-01

Статус: staged package implementation присутствует в текущем `main`; licensed Unity runtime evidence требуется до повышения system status до `VERIFIED`.

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

После merge с текущим `main` устаревшие регрессии синхронизированы с утверждённым lifecycle: active cancel сохраняет reservations до normal completion, split Unity partial-файлы входят в source-contract coverage, а staged package владеет output identity вместо legacy per-unit placement loop.

## Unity compile regression corrections — 2026-08-01

Локальный Unity compiler сначала обнаружил namespace drift после разнесения staged-package runtime по partial-файлам:

- `DigBuildingProductionZones.cs` использовал `ProductionPackageContent`, но не импортировал authoritative owner `Dig.Domain.Content`;
- `DigTerrainWorkSession.ProductionPackages.cs` вручную создавал excavation-specific `TerrainWorkRoutePlan` для direct package use.

Первое исправление добавило корректный content namespace. Повторная локальная компиляция показала, что второе место было не просто отсутствующим `using`, а неправильной архитектурной зависимостью: package use не является excavation route и не должен создавать `TerrainWorkRoutePlan`.

Окончательное исправление:

- удаляет прямую зависимость package-use partial от `TerrainWorkRoutePlan` и `Dig.Application.Navigation`;
- направляет движение к package через существующий `PlanBuildingProductionRoute` и `_buildingProductionRoutes`;
- очищает тот же production-route owner после terminal package-use commit;
- сохраняет ordinary Navigation travel и уже выбранную supported adjacent work position;
- добавляет source contract, запрещающий возврат `TerrainWorkRoutePlan` в package-use partial.

Observable package lifecycle, output placement, cancel/failure и save/load behavior не изменены.

## Internal-stock material transit regression correction — 2026-08-01

Runtime создавал unfinished package и направлял production worker к внутреннему складу, но не выполнял authoritative pickup зарезервированной единицы. `ApplyProductionWorkHandler` списывал reservation непосредственно из building inventory, поэтому world workflow останавливался у склада и не мог перейти к обработке материала.

Отдельно direct pickup проверял свободные resident slots по всему доступному количеству internal stack, хотя команда забирает только одну единицу. Stack из нескольких единиц поэтому ошибочно блокировался при наличии одного свободного слота.

Исправление:

- добавляет production-owned transfer ровно одной order-reserved единицы из `ItemLocation.InBuilding` в конкретный resident slot;
- сохраняет reservation на carried unit до committed material step;
- запрещает material-step work без физически перенесённой единицы у назначенного worker;
- после work consume-ит именно carried reserved unit, а не скрытое содержимое building inventory;
- использует один derived internal-stock work cell для automatic production и direct pickup;
- direct internal-stock pickup рассчитывает capacity для одной available/unreserved единицы, сохраняя запрет на pickup зарезервированного production материала.

Добавлены domain/application regressions на pickup → carry → consume и source contract на Unity routing.

## Evidence

Для correction branch обязательны:

- architecture/file-size/C# compatibility;
- Unity source contracts, включая route-owner и production material-transit regressions;
- Release build и полный .NET suite;
- headless smoke и оба deterministic soak;
- Stage 2 exports.

Фактический licensed Unity EditMode/PlayMode run остаётся обязательным для статуса `VERIFIED`.
