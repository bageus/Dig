# Issue 574 — physical room stock и staged upgrade execution

Статус: `IMPLEMENTED IN BRANCH`.

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).  
Domain foundation dependency: PR [#593](https://github.com/bageus/Dig/pull/593).  
Implementation PR: [#594](https://github.com/bageus/Dig/pull/594).

## 1. Цель slice

Реализовать Slice `4B-1`: связать подтверждённый room-upgrade aggregate с физическими world stacks, обычными hauling jobs, staged work, interruption/resume и material-specific skill grants, не создавая вторых владельцев Inventory, Job или Agent progression state.

Slice не выбирает ответы на `Q-ROOM-003` и `Q-ROOM-007`, не добавляет room bonuses, compact layouts или purpose-switch packing.

## 2. Владение состоянием

- `RoomInfrastructureState` владеет order, temporary-stock cell, material ledgers, cancellation lock, completed material-unit ids и active job ids;
- `InventoryState` продолжает владеть stack identity, world location, quantities и reservations;
- `JobSystem` продолжает владеть delivery/work lifecycle, assignment и position claims;
- `IAgentSkillGrantService` остаётся единственным application boundary для progression grants;
- Application handlers только согласуют эти owners после полного preflight.

## 3. Delivery synchronization

`SynchronizeRoomUpgradeJobsHandler`:

- создаёт один persistent `RoomUpgradeWorkJobDefinition` для active operation;
- сохраняет work job в `Created`, пока полный required material set не доставлен;
- использует существующий `HaulJobDefinition` для ordinary delivery;
- рассматривает только revealed, reachable и currently unreserved world stacks;
- сортирует sources по Manhattan distance до temporary-stock cell, затем `CellId`, затем `StackId`;
- учитывает уже delivered и incoming quantities;
- ограничивает число новых delivery jobs переданным deterministic budget;
- повторный вызов не создаёт duplicate work jobs, hauling jobs или reservations.

## 4. Physical temporary stock

После завершения hauling:

1. exact quantity перемещается в `ItemLocation.InWorld(TemporaryStockCell)`;
2. destination stack резервируется за persistent room work job;
3. room material ledger увеличивает `Delivered` exactly once;
4. hauling job завершается и освобождает свои claims;
5. когда все материалы доставлены, room становится `ReadyForWork`, а work job — `Available`.

`InventoryState` получил только общие deterministic helpers для reservation по `ItemLocation + ItemId`; отдельный room inventory owner не создавался.

## 5. Staged work и progression

Material units выполняются строго в порядке `RoomUpgradeCostCatalog`.

Первый committed `PerformWork` interval:

- переводит room в `Improving`;
- необратимо блокирует cancellation;
- расходует одну unit из work-job reservation;
- коммитит exact `RoomMaterialUnitId(item, ordinal)`;
- выдаёт `50` fixed-point units (`+0.5`) exactly once.

Skill mapping:

- `material.stone` → `skill.stonework`;
- `material.mushroom_leg` → `skill.woodworking`;
- `material.iron` → `skill.metallurgy`;
- `material.crystal` → `skill.alchemy`.

Idempotency source включает room identity и material-unit identity. Replay уже committed unit не расходует Inventory и не начисляет skill повторно.

Последняя unit:

- завершает room improvement;
- удаляет temporary-stock reference и active job ids из room aggregate;
- активирует последний `RequestedPurpose`;
- переводит work job в `Finalize`, после чего отдельный finalization command завершает job lifecycle.

## 6. Interruption, resume и cancellation

Direct interruption использует существующий `JobSystem.ReleaseAssignment`:

- worker/position claims освобождаются;
- room material ledger и completed-unit ids сохраняются;
- Inventory reservations остаются за тем же work job;
- другой worker может claim/start тот же job и продолжить с первой незавершённой unit.

Pre-work cancellation:

- допустима только в `AwaitingMaterials` или `ReadyForWork`;
- system-cancels все attached delivery/work jobs;
- освобождает source, stock и resident-slot reservations;
- delivered stacks остаются в exact room cell;
- эти stacks снова доступны ordinary logistics;
- room возвращается в `Unimproved`, order count становится `0`;
- consumed ledger и upgrade skill grants отсутствуют.

## 7. Save boundary

Добавлены:

- `RoomUpgradeWorkJobDefinition`;
- codec `job.room_upgrade_work.v1`;
- production registry coverage нового concrete `JobDefinition`.

Codec сохраняет stable job id, room infrastructure id, exact work XYZ, priority, created tick, retry policy и dependencies.

Room aggregate save-document section, migration и Unity runtime restore входят в следующий Slice `4B-2`.

## 8. Regression coverage

Проверены:

- complete Small-room delivery → ReadyForWork → first worker commit/replay → cancellation lock → interruption → second-worker resume → completion;
- exactly-once skill grant первой material unit;
- final purpose activation и empty consumed stock;
- partial-delivery pre-work cancel с ordinary usable delivered stack;
- release всех source/stock reservations;
- catalog-order rejection без Inventory mutation;
- repeated synchronization без duplicate jobs/reservations;
- work-job codec round trip и production registry coverage.

## 9. Фактическая проверка

Code head `f5e89ab370861cb1b05a326e502ce5f3e6a4f8bd` прошёл Quality run `30815720973`:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks — passed;
- Unity source contracts — passed;
- Release build — `0` warnings, `0` errors;
- full .NET suite — `1453/1453`;
- headless smoke — passed at tick `20`;
- standard deterministic soak replay hash — `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large 64-resident deterministic soak replay hash — `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`.

Unity workflow `30815720965` completed only through blocked-evidence path:

- activation resolution — passed;
- actual EditMode/PlayMode execution — skipped;
- executed runtime evidence validation — skipped;
- blocked runtime evidence — recorded.

Поэтому runtime status не повышается до `VERIFIED`.

## 10. Следующий этап

Slice `4B-2`:

- room infrastructure save-document section и versioned migration;
- restore aggregate, active jobs, reservations и deterministic room job-id sequence;
- Unity completed-room synchronization, ordinary assignment, movement и stage execution;
- room marker/menu/read model, progress visuals и actual Play Mode scenario.
