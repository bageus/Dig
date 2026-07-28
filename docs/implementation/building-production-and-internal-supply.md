# Generic building production and internal supply implementation

Статус: `IMPLEMENTED` в PR #465. Основной slice merged через PR #441; Unity compile regressions исправлены в PR #452 и PR #457. Unity Play Mode fixtures checked in, но Unity Test Runner фактически не запускался.

Authoritative design: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).
Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

## Реализованные владельцы

- Production владеет data-driven workstation recipes, очередями, active material step и progressive consumption.
- BuildingSupplyState владеет internal input capacities, delivery toggles, incoming reservations и protected-source policy.
- InventoryState владеет физическими item entities, mixed sequential pickup/deposit transactions и BuildingBox outputs.
- JobSystem владеет supply и production worker lifecycle, reservations, blocked/retry/cancel.
- Skills выдаёт exactly-once grants после завершения одного production order.
- Presentation строит product icons, ingredient tooltip, shortage tint, queue count, decrement control и stock toggles из generic view model.
- Unity runtime исполняет supply/production jobs, начинает supply route у workstation, посещает каждый reserved world source, размещает output перед зданием и отображает раздельные internal-stock piles.

## Campfire content

Unpacked campfire использует одну generic workstation definition с future `AnimationProfileId`, четырьмя input stock definitions и шестью recipes: Tent, Stone mason workshop, Wooden workshop, Campfire, Grilled mushroom и Grilled hamster.

Runtime не содержит отдельных production branches для этих recipe IDs. Добавление нового workstation или recipe выполняется через content definitions.

## Completion workflow in PR #465

- Supply batch атомарно планируется и резервируется command handler до движения worker. После assignment resident сначала приходит к workstation work position и подтверждает active reserved route, затем обходит world sources и возвращается к зданию для deposit.
- Internal-stock pile имеет trigger collider и generic `DigBuildingInternalStockVisual`. При одном selected resident обычный LMB создаёт quantity-one pickup только из `AvailableQuantity`; production reservation нельзя украсть. Если delivery toggle включён, следующий synchronization снова создаёт replacement demand.
- Product row показывает `−` при ненулевой очереди. Один click отменяет newest queued order данного recipe; active order выбирается только когда queued order больше нет. Неиспользованные reservations освобождаются через существующий cancel handler.
- World-item pickup contracts/save codec расширены source location, quantity и split destination stack ID с backward-compatible defaults для старого world pickup save.

## Save/load

Save format v7 сохраняет queue, active order/material step, consumed inputs, delivery toggles, incoming supply batches и production/supply jobs. Loader проверяет building/assembly/production/supply job cross-references и не повторяет уже committed material steps или outputs.

World-item pickup codec сохраняет optional source kind/owner и destination stack ID; старые payload без этих полей восстанавливаются как полный pickup из world cell.

## Regression fixes

- Campfire production fixture использует полный `BuildingBoxAssemblyJob` lifecycle и зарегистрированный save codec.
- Unity package manifest/lock синхронизированы с текущим `main`: `com.unity.test-framework` `1.6.0`, source `builtin`.
- Sequential source pickup сохраняет authoritative worker identity и посещает каждый reserved source.
- Campfire placement source-contract проверяет generic `BuildingCatalog.FindByBoxItemId`, а не прямую runtime-ссылку на campfire ID.
- Ветка PR #441 была перебазирована поверх mushroom/BuildingBox Play Mode fixes; transport files удалены.
- PR #452 импортировал `Dig.Application.Jobs` для assignment execution/synchronization partials.
- PR #457 fully qualified три `Dig.Application.Jobs.AdvanceJobCommand` transitions в runtime partial.
- PR #465 освобождает item reservation и resident slot claims вместе, если создание generalized pickup job завершается ошибкой.
- Release build на первом completion head выявил nullable-flow warning после typed source validation; final code сохраняет `ItemStackSnapshot source = stack!` и использует validated snapshot для quantity/item-capacity operations.
- Completed pickup освобождает перенесённую quantity reservation после successful job completion.
- Building supply допускает пустой transit-ID list, когда reserved material полностью объединяется с существующим resident stack; deposit IDs остаются обязательными.

## Test coverage

- content/catalog validation и точный campfire recipe matrix;
- queue without inputs, orange shortage, enqueue и one-order decrement;
- progressive per-material timing/consumption и exactly-once skill grants;
- mixed/partial protected supply, workstation-first route и последовательный pickup каждого world source;
- quantity-one direct internal-stock pickup, reserved-quantity protection и replacement demand;
- deterministic front-cell output и BuildingBox identity;
- v6→v7 migration, active supply, pickup codec compatibility и mid-step round-trip;
- Unity source contracts для HUD/input/runtime composition;
- checked-in Play Mode fixture для trigger piles, building/item identity и non-blocking stock visuals.

## CI evidence

Validated connector head `48f8f69a27ec5fa8ab052f29731a69e287c67c03`:

- Quality run 5927 (`30320277799`) — `success`: architecture/file-size/C# compatibility, Unity source contracts, Release restore/build, full `.NET` test suite, headless smoke, standard deterministic soak и large-settlement soak;
- Export Stage 2 v2 run 511 (`30320277778`) — `success`;
- Export Stage 2 v3 run 516 (`30320277775`) — `success`;
- checksum-verified implementation apply и repository-local quality/source-contract gates — `success`.

Final status/index/design evidence commit is applied after this validated code head. A final docs-only Quality run is required on the PR head before Ready for review.

Unity Test Runner фактически не запускался; систему нельзя считать `VERIFIED` до повторного Unity compile/Play Mode evidence.
