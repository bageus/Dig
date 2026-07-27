# Generic building production and internal supply implementation

Статус: implementation slice опубликован в draft PR #441; все доступные repository CI checks зелёные. Unity Play Mode fixture checked in, но Unity Test Runner фактически не запускался.

Authoritative design: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).
Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

## Реализованные владельцы

- Production владеет data-driven workstation recipes, очередями, active material step и progressive consumption.
- BuildingSupplyState владеет internal input capacities, delivery toggles, incoming reservations и protected-source policy.
- InventoryState владеет физическими item entities, mixed sequential pickup/deposit transactions и BuildingBox outputs.
- JobSystem владеет supply и production worker lifecycle, reservations, blocked/retry/cancel.
- Skills выдаёт exactly-once grants после завершения одного production order.
- Presentation строит product icons, ingredient tooltip, shortage tint, queue count и stock toggles из generic view model.
- Unity runtime исполняет supply/production jobs, посещает каждый reserved world source, размещает output перед зданием и отображает раздельные internal-stock piles.

## Campfire content

Unpacked campfire использует одну generic workstation definition с future `AnimationProfileId`, четырьмя input stock definitions и шестью recipes: Tent, Stone mason workshop, Wooden workshop, Campfire, Grilled mushroom и Grilled hamster.

Runtime не содержит отдельных production branches для этих recipe IDs. Добавление нового workstation или recipe выполняется через content definitions.

## Save/load

Save format v7 сохраняет queue, active order/material step, consumed inputs, delivery toggles, incoming supply batches и production/supply jobs. Loader проверяет building/assembly/production/supply job cross-references и не повторяет уже committed material steps или outputs.

## Regression fixes in PR #441

- Campfire production fixture использует полный `BuildingBoxAssemblyJob` lifecycle и зарегистрированный save codec.
- Unity package manifest/lock синхронизированы с текущим `main`: `com.unity.test-framework` `1.6.0`, source `builtin`.
- Первый sequential source pickup переводит claimed supply job в `InProgress/AcquireItem` до проверки стадии; последующие источники продолжают тот же job и сохраняют authoritative worker identity.
- Campfire placement source-contract проверяет generic `BuildingCatalog.FindByBoxItemId`, а не прямую runtime-ссылку на campfire ID.
- Ветка PR перебазирована поверх актуального `main` после mushroom/BuildingBox Play Mode fixes; новые runtime и presentation изменения `main` сохранены, transport files удалены.

## Test coverage

- content/catalog validation и точный campfire recipe matrix;
- queue without inputs, orange shortage и tooltip models;
- progressive per-material timing/consumption и exactly-once skill grants;
- mixed/partial protected supply и последовательный pickup каждого world source;
- deterministic front-cell output и BuildingBox identity;
- v6→v7 migration, active supply и mid-step round-trip;
- Unity source contracts и checked-in Play Mode fixture для production panel/internal stock piles.

## CI evidence

Rebased head `baf061d63cd8670820631debd0d587cbaecd724f`:

- Quality run 5635 (`30311704660`): architecture/file-size/C# compatibility, Unity module/source contracts, Release restore/build, full .NET test suite, headless smoke, standard deterministic soak и large-settlement soak — `success`.
- Export Stage 2 v2 run 424 (`30311704639`) — `success`.
- Export Stage 2 v3 run 429 (`30311704645`) — `success`.

Unity Test Runner фактически не запускался; систему нельзя считать `VERIFIED`, а PR остаётся draft до получения Play Mode evidence или явного принятия этого residual verification work.
