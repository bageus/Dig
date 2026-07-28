# Generic building production and internal supply implementation

Статус: revised production-icon input is `IMPLEMENTED` in PR #501. Основной slice merged через PR #441; supply completion through PR #465. Actual licensed Unity Play Mode evidence remains pending.

Authoritative design: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).
Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

## Реализованные владельцы

- Production владеет data-driven workstation recipes, очередями, active material step и progressive consumption.
- BuildingSupplyState владеет internal input capacities, delivery toggles, incoming reservations и protected-source policy.
- InventoryState владеет физическими item entities, mixed sequential pickup/deposit transactions и BuildingBox outputs.
- JobSystem владеет supply и production worker lifecycle, reservations, blocked/retry/cancel.
- Skills выдаёт exactly-once grants после завершения одного production order.
- Presentation строит product icons, ingredient tooltip, shortage tint, queue count и stock toggles из generic view model.
- Unity runtime исполняет supply/production jobs, начинает supply route у workstation, посещает каждый reserved world source, размещает output перед зданием и отображает раздельные internal-stock piles.

## Production icon input correction — 2026-07-29

Предыдущая Unity-проекция создавала рядом с каждым product icon отдельную кнопку `−`. Это расходилось с последним подтверждённым правилом управления.

Исправленный observable contract:

- LMB на product icon вызывает один `EnqueueBuildingProduction`;
- RMB на том же icon вызывает один `CancelOneBuildingProduction`, только пока projected `QueuedCount > 0`;
- перед cancel Unity повторно читает authoritative production view; при нулевом non-terminal count команда не отправляется;
- отдельная minus/decrement button больше не создаётся;
- `CancelOneBuildingProduction` сохраняет authoritative policy: newest queued order first, active order only when queued orders отсутствуют;
- tooltip, orange shortage state и counter остаются на одном icon.

`DigProductionIconPointer` владеет только pointer presentation events (`hover` и RMB callback), но не меняет Production state напрямую. Command commit остаётся в `DigTerrainWorkSession`/Application handlers.

## Campfire content

Unpacked campfire использует одну generic workstation definition с future `AnimationProfileId`, четырьмя input stock definitions и шестью recipes: Tent, Stone mason workshop, Wooden workshop, Campfire, Grilled mushroom и Grilled hamster.

Runtime не содержит отдельных production branches для этих recipe IDs. Добавление нового workstation или recipe выполняется через content definitions.

## Supply workflow

- Supply batch атомарно планируется и резервируется command handler до движения worker.
- После assignment resident сначала приходит к workstation work position и подтверждает active reserved route, затем обходит world sources и возвращается к зданию для deposit.
- Internal-stock pile имеет trigger collider и generic `DigBuildingInternalStockVisual`.
- При одном selected resident обычный LMB создаёт quantity-one pickup только из `AvailableQuantity`; production reservation нельзя украсть.
- Если delivery toggle включён, следующий synchronization снова создаёт replacement demand.
- Automatic supply planner читает только revealed/reachable/unreserved world stacks. Уже находящийся в произвольном resident inventory material не является автоматическим source; resident inventory используется как зарезервированный transit cargo конкретного supply job.

## Save/load

Save format v7 сохраняет queue, active order/material step, consumed inputs, delivery toggles, incoming supply batches и production/supply jobs. Loader проверяет building/assembly/production/supply job cross-references и не повторяет уже committed material steps или outputs.

World-item pickup codec сохраняет optional source kind/owner и destination stack ID; старые payload без этих полей восстанавливаются как полный pickup из world cell. PR #500 registers the complete job codec set through `SaveGameCompositionRoot`.

## Regression fixes

- Campfire production fixture использует полный `BuildingBoxAssemblyJob` lifecycle и зарегистрированный save codec.
- Unity package manifest/lock синхронизированы с текущим `main`: `com.unity.test-framework` `1.6.0`, source `builtin`.
- Sequential source pickup сохраняет authoritative worker identity и посещает каждый reserved source.
- Campfire placement source-contract проверяет generic `BuildingCatalog.FindByBoxItemId`, а не прямую runtime-ссылку на campfire ID.
- PR #452 импортировал `Dig.Application.Jobs` для assignment execution/synchronization partials.
- PR #457 fully qualified три `Dig.Application.Jobs.AdvanceJobCommand` transitions в runtime partial.
- PR #465 освобождает item reservation и resident slot claims вместе, если создание generalized pickup job завершается ошибкой.
- Release build на первом completion head выявил nullable-flow warning после typed source validation; final code сохраняет validated snapshot для quantity/item-capacity operations.
- Completed pickup освобождает перенесённую quantity reservation после successful job completion.
- Building supply допускает пустой transit-ID list, когда reserved material полностью объединяется с существующим resident stack; deposit IDs остаются обязательными.
- PR #501 removes the separate minus button and binds decrement to RMB on the same product icon with a zero-count guard.

## Test coverage

- content/catalog validation и точный campfire recipe matrix;
- queue without inputs, orange shortage, enqueue и one-order decrement;
- product icon LMB/RMB source contract, absence of a separate minus icon and zero-count guard;
- executable Unity Play Mode pointer test verifies that left click does not invoke decrement, one RMB invokes exactly once, and unbound RMB is a no-op;
- progressive per-material timing/consumption и exactly-once skill grants;
- mixed/partial protected supply, workstation-first route и последовательный pickup каждого world source;
- quantity-one direct internal-stock pickup, reserved-quantity protection и replacement demand;
- deterministic front-cell output и BuildingBox identity;
- save composition, migration, active supply, pickup codec compatibility и mid-step round-trip;
- Unity source contracts для HUD/input/runtime composition;
- checked-in Play Mode fixture для trigger piles, building/item identity и non-blocking stock visuals.

## CI evidence

PR #501 merge-ref validation:

- Quality run `30406329012`: architecture/file-size/C# compatibility, Unity source/presentation contracts, Release build, 1101 .NET tests, headless smoke, standard deterministic soak and large-settlement soak — success;
- Export Stage 2 v2 run `30406329013` — success;
- Export Stage 2 v3 run `30406329052` — success;
- Unity workflow `30406329010` — workflow success, but `Run Play Mode tests` skipped by activation gate and no runtime result artifact was produced.

The system is `IMPLEMENTED`, not `VERIFIED`, until a licensed Unity Test Runner executes the checked-in production pointer scenario.