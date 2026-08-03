# Issue 574 — foundation улучшения комнат

Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).  
Persistence dependency: PR [#592](https://github.com/bageus/Dig/pull/592).  
Implementation PR: [#593](https://github.com/bageus/Dig/pull/593).

## 1. Цель slice

Реализовать первую независимую часть Slice 4: authoritative room-upgrade aggregate, completed-room provenance, deterministic temporary-stock cell planning, CQRS contracts и diagnostics. Slice не выбирает значения для `Q-ROOM-003` или `Q-ROOM-007` и не добавляет bonus/layout/packing behavior.

## 2. Authoritative identity и eligibility

`RoomInfrastructureProvenanceProjector` принимает только `ExcavationTemplateInstance` со статусом `Completed`.

- поддерживаются только template kinds `Small`, `Medium`, `Large`, `Tall`;
- stable infrastructure id выводится из immutable template-instance id;
- arbitrary excavated area не получает room infrastructure identity;
- repeated exact provenance idempotent;
- stable-id drift и повторное использование id другим template instance отклоняются до mutation.

`RoomInfrastructureState` становится единственным владельцем upgrade order, requested/active purpose, material ledger, temporary-stock identity, committed unit ids и associated job ids. World остаётся владельцем room/template geometry, Inventory — physical stacks/reservations, JobSystem — job lifecycle.

## 3. Upgrade lifecycle

Для каждой completed room регистрируется `RoomInfrastructureProjectState`.

- `UpgradeOrderCount` принимает только `0` или `1`;
- после первого order повторный order отклоняется;
- confirmed material costs задаёт `RoomUpgradeCostCatalog`;
- `RequestedPurpose` можно менять в delivery/work lifecycle без reset;
- до completion `ActivePurpose = None`;
- после completion активируется последний `RequestedPurpose`;
- post-completion purpose state может измениться, но packing/layout effects намеренно не реализованы в этом slice.

Material ledger хранит required, delivered, consumed и released-on-cancel quantities. Exact progress identity — `RoomMaterialUnitId(ItemId, Ordinal)`.

- replay уже committed unit не меняет state/version;
- committed unit count сверяется отдельно для каждого material при restore;
- impossible lifecycle, cost set, unit ordinal, released/consumed combination или duplicate identity отклоняются;
- work job остаётся attached между последовательными material stages;
- completion очищает temporary stock и active job ids.

## 4. Cancellation и interruption boundary

До первого actual work start cancel:

- разрешён в `AwaitingMaterials` и `ReadyForWork`;
- возвращает active job ids для system cancellation;
- возвращает delivered quantities как released ledger;
- сбрасывает order count в `0` и room обратно в `Unimproved`;
- не создаёт consumed units или skill grants.

`StartImprovementWork` переводит room в `Improving` и необратимо устанавливает cancellation lock. После этого cancel отклоняется, а partial progress сохраняется для другого worker. Реальное освобождение worker claims и повторное назначение остаются задачей runtime job integration Slice 4B.

## 5. Temporary stock planner

`RoomTemporaryStockCellPlanner` использует только:

- exact cells completed room provenance;
- authoritative World cell openness;
- переданный reachable-cell set;
- объединённый occupied-cell set buildings/items/residents;
- Manhattan distance до geometric center room bounds;
- stable `CellId` tie-break.

Результат typed:

- `Assigned`;
- `Retained` для уже назначенной всё ещё legal клетки;
- `BlockedNoFreeReachableCell` без authoritative mutation.

Planner не создаёт physical stock, stack или reservation. Эти owners подключаются в Slice 4B.

## 6. Application и diagnostics

Добавлены:

- `IRoomInfrastructureRepository`;
- synchronization/order/purpose/job/delivery/work/commit/cancel commands and handlers;
- `GetRoomInfrastructureQuery`;
- `InMemoryRoomInfrastructureRepository`;
- immutable diagnostics с typed blockers:
  - `TemporaryStockCellUnavailable`;
  - `MaterialsIncomplete`;
  - `WaitingForWorker`.

Diagnostics не владеет state и не добавляет gameplay defaults.

## 7. Regression coverage

Проверяются:

- exact costs всех четырёх templates;
- order count `0|1`;
- pre-work cancellation и released material ledger;
- work-start cancellation lock;
- latest requested purpose activation;
- exact material-unit replay idempotency;
- partial snapshot round trip;
- malformed lifecycle и cross-material committed-unit mismatch rejection;
- completed-only provenance и stable identity;
- idempotent synchronization и identity drift rejection;
- center-distance/stable-cell planning;
- fully blocked room и typed diagnostics.

## 8. Фактическая проверка

Code head `b3dc06857d1adb2efc77e5a47477f8e9067c698e` прошёл Quality run `30811581217`:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks — passed;
- Unity source contracts — passed;
- Release build — `0` warnings, `0` errors;
- full .NET suite — `1448/1448`;
- headless smoke — passed at tick `20`;
- standard deterministic soak replay hash — `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large 64-resident deterministic soak replay hash — `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`.

Unity workflow `30811581174` completed only through blocked-evidence path:

- activation resolution — passed;
- actual EditMode/PlayMode execution — skipped;
- executed runtime evidence validation — skipped;
- blocked runtime evidence — recorded.

Поэтому runtime status не повышается до `VERIFIED`.

## 9. Следующий slice

Slice 4B должен подключить существующие owners без создания второго источника истины:

- physical InventoryState room stock;
- ordinary hauling jobs/reservations;
- pre-work system cancellation и release;
- exact material consumption;
- material-specific `+0.5` skill grants exactly once;
- worker interruption/resume;
- save/migration/runtime composition;
- marker/menu/read model и Play Mode scenario.

`Q-ROOM-003` и `Q-ROOM-007` остаются открытыми и не блокируют перечисленную core lifecycle работу.
