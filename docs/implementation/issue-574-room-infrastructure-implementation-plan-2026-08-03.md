# Issue 574 — план реализации назначения комнат и инфраструктуры тоннелей

Статус: `IN PROGRESS`.

Authoritative specification: [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).  
Tracking issue: [#574](https://github.com/bageus/Dig/issues/574).

## 1. Цель

Реализовать подтверждённую часть системы без создания новых игровых правил:

- persistent room-upgrade lifecycle;
- room purpose state и подтверждённые bonuses;
- automatic/manual tunnel infrastructure;
- rolling structural-anchor chain, где completed wooden support и completed door становятся новой точкой отсчёта;
- decorative stone trim без structural protection;
- deterministic delayed collapse, buried-item recovery и save/load;
- Unity UI/input/read models и end-to-end regression coverage.

Статус specification остаётся `QUESTIONNAIRE`, пока не закрыты перечисленные ниже observable decisions. Реализация спорных ветвей до ответа запрещена.

## 2. Открытые блокеры

1. `Q-ROOM-003`: room membership для building bonuses и момент проверки Sleep/Eat position.
2. `Q-ROOM-007`: Tall Bedroom и первый полный каталог Farm/Workshop layouts для Small/Large/Tall.
3. `Q-TUNNEL-006A`: deterministic retry interval для полностью actor-blocked collapse.
4. `Q-TUNNEL-008`: player cancellation policy для pending automatic support/trim jobs.

До закрытия вопросов разрешены только решения, не зависящие от них. В коде не будет временных default-правил, которые могут стать вторым источником истины.

## 3. Владение состоянием и границы

- `WorldState` продолжает владеть terrain cells, excavation mutation, room/template provenance и buried-item attachment.
- Новый `RoomInfrastructureState` владеет upgrade order, material ledger, cancellation lock, requested/active purpose и profile references.
- Новый `TunnelInfrastructureState` владеет ordered horizontal segments, structural anchors, next automatic target, decorative targets и collapse schedule.
- `BuildingsState` остаётся владельцем completed building/door lifecycle; tunnel state получает только immutable completion facts через Application synchronization.
- `InventoryState` остаётся владельцем stack identity, quantities, locations и reservations.
- `JobSystem` остаётся владельцем lifecycle, worker/position claims и retry state.
- Presentation только проецирует authoritative snapshots и отправляет commands.

Новые Domain-типы не зависят от Unity, файловой системы или Infrastructure layer.

## 4. Последовательность реализации

### Slice 1 — deterministic tunnel anchor foundation

Статус: `MERGED` в PR #578.

Реализовано:

- engine-independent horizontal segments и structural anchors;
- room-exit/vertical-junction origins;
- completed wooden support и completed door как rolling anchors;
- next target ровно через 10 ordered horizontal cells;
- `cell 5 -> target 15`;
- obsolete derived target не хранится вторым источником истины;
- stable split chains, snapshots, typed events и regression tests.

Validation PR #578:

- quality/build passed;
- `1390/1390` .NET tests passed;
- headless smoke, standard soak и large soak passed with deterministic replay;
- Unity Test Runner был blocked отсутствием activation, поэтому runtime verification не заявлена.

### Slice 2A — Application contracts и automatic support job synchronization

Статус: `MERGED` в PR #579.

Реализовано:

- CQRS repository/commands/query для регистрации segment и completed support/door anchors;
- automatic support range `20` по XYZ Manhattan до completed-building occupied cell;
- минимальный допустимый ordinary-work priority `0`;
- source selection только из revealed, reachable, unreserved world stacks;
- stable source order: distance, cell, stack id;
- no-source создаёт один job в `Created` без Inventory/Job reservations;
- появление mushroom leg разрешает тот же definition и переводит job в `Available`;
- новый rolling anchor system-cancels obsolete target, освобождает source reservation и создаёт replacement;
- ordinary interruption возвращает job в `Available`, source reservation остаётся за job, другой worker может продолжить;
- unresolved и source-resolved automatic-job definitions сохраняются через `job.tunnel_automatic_work.v1`;
- production save registry проверяет coverage нового concrete `JobDefinition`;
- player-cancel command/API намеренно отсутствует до ответа `Q-TUNNEL-008`.

Validation PR #579:

- architecture, file-size, C# 9 compatibility, dependency и Domain-boundary checks passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1401/1401`;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Stage 2 v2/v3 exports passed;
- actual Unity EditMode/PlayMode execution was skipped because activation was unavailable.

### Slice 2B — topology synchronization, execution и junction trim

#### Slice 2B-1 — authoritative junction target and automatic trim job

Статус: `MERGED` в PR #582. Подробный implementation note: [`issue-574-tunnel-junction-trim-lifecycle-2026-08-03.md`](issue-574-tunnel-junction-trim-lifecycle-2026-08-03.md).

Реализовано:

- unique pending/completed decorative stone-trim target на vertical-junction cell;
- left/right chains не создают duplicate targets;
- stable segment owner и deterministic rebind при удалении направления;
- segment removal system-cancels automatic jobs и освобождает Inventory reservations;
- low-priority `JunctionStoneTrim` использует range `20` и deterministic source selection;
- no-source остаётся `Created` без phantom reservations;
- появление stone разрешает тот же job;
- completion удаляет authoritative target, stale job отменяется;
- player cancellation не реализована до ответа `Q-TUNNEL-008`.

Validation PR #582:

- Release build: `0` warnings, `0` errors;
- full .NET suite: `1412/1412`;
- headless smoke и оба deterministic soak passed;
- actual Unity EditMode/PlayMode execution was skipped because activation was unavailable.

#### Slice 2B-2a — automatic work final commit

Статус: код был review/merged в nested PR #584 и восстановлен для merge в `main` через PR #590. Подробный implementation note: [`issue-574-tunnel-automatic-work-execution-2026-08-03.md`](issue-574-tunnel-automatic-work-execution-2026-08-03.md).

Реализовано:

- final commit принимает только `TunnelAutomaticWorkJobDefinition` в `InProgress/Finalize` с authoritative worker;
- exact source stack/item/world-cell/reservation preflight;
- current support/trim target revalidation перед mutation;
- wooden support расходует ровно один `material.mushroom_leg`, становится structural anchor и переносит rolling target;
- junction trim расходует ровно один `material.stone`, остаётся decorative;
- final stage завершает job и освобождает JobSystem claims;
- worker получает ровно `70` fixed-point units (`+0.7`) Woodworking или Stonework;
- skill idempotency использует stable automatic job identity;
- stale target/source/reservation отклоняются до mutation;
- terminal replay не расходует материал и не начисляет skill повторно.

Validation исходного slice:

- Release build: `0` warnings, `0` errors;
- full .NET suite: `1416/1416`;
- automatic-work execution regressions, smoke и оба deterministic soak passed;
- actual Unity EditMode/PlayMode execution was skipped because activation was unavailable.

#### Slice 2B-2b1 — completed provenance topology reconciliation

Статус: код был review/merged в nested PR #587 и восстановлен для merge в `main` через PR #590. Подробный implementation note: [`issue-574-tunnel-topology-provenance-reconciliation-2026-08-03.md`](issue-574-tunnel-topology-provenance-reconciliation-2026-08-03.md).

Реализовано:

- completed provenance supplies stable segment id, origin kind/cell, direction и exact ordered cells;
- identity = `origin kind + origin cell + direction`;
- repeated provenance idempotent;
- missing directions registered;
- removed directions cancel automatic work and release reservations;
- extension/shortening preserves anchors whose cells remain;
- completed junction trim survives geometry extension;
- obsolete support jobs cancel only when derived target changes;
- stable-id drift/reuse rejects before authoritative mutation;
- complete desired topology is preflight-validated before cross-owner mutation.

Validation исходного slice:

- Release build: `0` warnings, `0` errors;
- full .NET suite: `1422/1422`;
- six topology reconciliation regressions, smoke и оба deterministic soak passed;
- actual Unity EditMode/PlayMode execution was skipped because activation was unavailable.

#### Slice 2B-2b2a — Unity/runtime provenance composition

Статус: `READY FOR REVIEW` в PR #590. Подробный implementation note: [`issue-574-tunnel-runtime-provenance-composition-2026-08-03.md`](issue-574-tunnel-runtime-provenance-composition-2026-08-03.md).

Реализовано:

- `TunnelRuntimeTopologyProjector` читает только completed World cells, completed `CaveRoomPlan`, `PlannedTunnelCells` и `PlannedVerticalTunnelCells`;
- arbitrary open terrain не становится infrastructure provenance;
- room exits проецируются только наружу от completed room;
- completed horizontal/vertical intersections становятся deterministic junction origins;
- reset origins partition corridors без reverse duplicates;
- stable segment id зависит от immutable topology key, а не порядка input/длины geometry;
- topology reconciliation и support/trim synchronization выполняются до ordinary assignment;
- completed building footprints, revealed cells и current tunnel-navigation cells переиспользуют существующий planner;
- automatic jobs участвуют в ordinary candidate/assignment pass;
- worker движется к exact reserved source, затем к target;
- route failure освобождает worker assignment, сохраняя job-owned material reservation;
- stage execution использует существующий JobSystem;
- Finalize вызывает `CompleteTunnelAutomaticWorkHandler`;
- post-excavation topology reconciliation выполняется до world-item settlement;
- existing Job overlay показывает exact support/trim XYZ target.

Validation PR #590 на code head `d430b065baf6ff5ba4fc86958f62cb4faf47bbae`:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1435/1435`;
- topology projector, reconciliation, execution и Unity-composition regressions passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Unity workflow recorded blocked evidence; actual EditMode/PlayMode execution and executed-runtime-evidence validation were skipped.

#### Slice 2B-2b2b — completed infrastructure world visual projection

Статус: `READY FOR REVIEW` в stacked PR #591. Подробный implementation note: [`issue-574-tunnel-infrastructure-visual-projection-2026-08-03.md`](issue-574-tunnel-infrastructure-visual-projection-2026-08-03.md).

Реализовано:

- `TunnelInfrastructureVisualPresenter` читает только `TunnelInfrastructureSnapshot`;
- completed `WoodenSupport` anchors и completed junction stone-trim cells создают stable visual instances;
- Origin и Door anchors не создают duplicate support visuals;
- duplicate completed cells объединяются;
- stable instance id зависит только от visual kind и exact XYZ cell;
- `DigTunnelInfrastructureRenderer` создаёт collider-free wooden beam и low stone floor frame;
- Unity projection использует exact X/Y/Z через `DigTunnelProjection`;
- runtime публикует projection после topology synchronization и сразу после successful automatic-work Finalize;
- удаление authoritative completion fact удаляет rebuildable visual;
- Presentation не владеет gameplay state, reservations, navigation или input.

Validation PR #591 на code head `9fcb643a3d9cd2d3ea5b428f88d941305772d66e`:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks passed;
- Unity source contracts passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1430/1430`;
- presenter и Unity runtime composition regressions passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- checked-in Play Mode scenario покрывает creation, XYZ placement, collider absence и removal;
- Unity workflow recorded blocked evidence; actual EditMode/PlayMode execution и executed-runtime-evidence validation были skipped.

Осталось в Slice 2B:

- actual licensed Play Mode end-to-end evidence;
- player cancellation не реализуется до ответа `Q-TUNNEL-008`.

### Slice 3 — persistence и migration для tunnel infrastructure

Статус: `READY FOR REVIEW` в stacked PR #592. Подробный implementation note: [`issue-574-tunnel-infrastructure-persistence-2026-08-03.md`](issue-574-tunnel-infrastructure-persistence-2026-08-03.md).

Реализовано:

- save format повышен с `14` до `15`;
- сохраняются ordered segments, origin kind/cell, exact horizontal cells, structural anchor kind/cell/distance и aggregate/segment versions;
- current automatic-support target и pending junction-trim targets сохраняются как integrity evidence, но при load пересчитываются Domain owner;
- completed junction stone-trim cells сохраняются отдельно от structural anchors;
- runtime automatic-job sequence сохраняется и валидируется против уже сохранённых `TunnelAutomaticWorkJobDefinition`;
- obsolete derived support/trim target отклоняется при load и не восстанавливается;
- migration `save.v14_to_v15.tunnel_infrastructure` создаёт пустой infrastructure section без phantom anchors;
- legacy migration поднимает sequence выше существующих parseable automatic-work job ids;
- autosave переносит тот же tunnel runtime snapshot;
- Unity restore пересоздаёт repository/application handlers вокруг восстановленного authoritative aggregate и обновляет visual projection.

Validation PR #592 на code head `2ff8fef77749c983e5ae56595ccdc823af3dc4db`:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks passed;
- Unity source contracts passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1436/1436`;
- tunnel save round-trip, stale-target, sequence-collision, migration и runtime-restore regressions passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Unity workflow recorded blocked evidence; actual EditMode/PlayMode execution and executed-runtime-evidence validation were skipped.

### Slice 4 — room-upgrade core

#### Slice 4A — authoritative room aggregate, provenance и stock-cell planning

Статус: `READY FOR REVIEW` в stacked PR #593. Подробный implementation note: [`issue-574-room-upgrade-foundation-2026-08-03.md`](issue-574-room-upgrade-foundation-2026-08-03.md).

Реализовано без выбора default для `Q-ROOM-003` и `Q-ROOM-007`:

- stable infrastructure identity выводится из immutable completed template-instance id;
- регистрируются только completed `Small`, `Medium`, `Large` и `Tall` template instances;
- `UpgradeOrderCount` принимает только `0|1`, повторный order отклоняется;
- costs соответствуют confirmed material sets всех четырёх templates;
- aggregate хранит requested/active purpose, required/delivered/consumed/released ledgers, temporary-stock cell, completed material-unit ids и active job ids;
- temporary-stock planner рассматривает только exact room cells, open/reachable/unoccupied eligibility, Manhattan distance до geometric center и stable `CellId` tie-break;
- отсутствие подходящей клетки возвращает typed blocked result без mutation;
- cancel разрешён только до первого actual work start, возвращает attached jobs и released delivered quantities;
- work start необратимо блокирует cancellation;
- `RoomMaterialUnitId(item, ordinal)` коммитится exactly once, replay не меняет version;
- work job остаётся attached между последовательными material stages;
- partial progress, exact per-material committed-unit counts и lifecycle валидируются при restore;
- completion удаляет temporary stock/job claims и активирует последний requested purpose;
- post-completion purpose state может измениться без добавления bonus/layout/packing behavior;
- CQRS handlers, in-memory repository и immutable typed diagnostics добавлены.

Validation PR #593 на code head `b3dc06857d1adb2efc77e5a47477f8e9067c698e`:

- Quality run `30811581217`: success;
- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks passed;
- Unity source contracts passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1448/1448`;
- room cost/order/cancel/work-lock/idempotency/restore/provenance/stock-planner/diagnostics regressions passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Unity workflow `30811581174` recorded blocked evidence; actual EditMode/PlayMode execution and executed-runtime-evidence validation were skipped.

#### Slice 4B-1 — physical stock, hauling, staged work и grants

Статус: `READY FOR REVIEW` в stacked PR #594. Подробный implementation note: [`issue-574-room-upgrade-execution-2026-08-03.md`](issue-574-room-upgrade-execution-2026-08-03.md).

Реализовано без выбора default для `Q-ROOM-003` и `Q-ROOM-007`:

- один persistent `RoomUpgradeWorkJobDefinition` прикрепляется к active room upgrade;
- ordinary `HaulJobDefinition` резервирует revealed, reachable и unreserved world sources;
- source selection детерминирован по Manhattan distance до stock cell, затем `CellId` и `StackId`;
- delivered stacks физически перемещаются в exact temporary-stock world cell и резервируются room work job;
- work job остаётся `Created`, пока не доставлен полный material set, затем становится `Available`;
- repeated synchronization не создаёт duplicate jobs или reservations;
- material units коммитятся в порядке `RoomUpgradeCostCatalog`;
- первый actual `PerformWork` interval блокирует cancellation;
- каждый exact `RoomMaterialUnitId(item, ordinal)` расходует одну reserved stock unit и выдаёт `50` fixed-point skill units exactly once;
- stone/leg/iron/crystal отображаются в Stonework/Woodworking/Metallurgy/Alchemy;
- `ReleaseAssignment` сохраняет room ledger и stock reservations, позволяя другому worker продолжить тот же job;
- final material unit переводит job в `Finalize`, завершает upgrade и активирует последний requested purpose;
- pre-work cancel отменяет все attached jobs, освобождает source/stock reservations и оставляет delivered stacks ordinarily usable в комнате;
- `job.room_upgrade_work.v1` сохраняет room identity и exact work XYZ, production registry coverage обновлён.

Validation PR #594 на code head `f5e89ab370861cb1b05a326e502ce5f3e6a4f8bd`:

- Quality run `30815720973`: success;
- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks passed;
- Unity source contracts passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1453/1453`;
- full delivery/work/replay/cancel-lock/interruption/second-worker-resume workflow passed;
- pre-work partial-delivery cancel, catalog-order, synchronization-idempotency и work-job codec regressions passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Unity workflow `30815720965` recorded blocked evidence; actual EditMode/PlayMode execution and executed-runtime-evidence validation were skipped.

#### Slice 4B-2a — room persistence и Unity runtime composition

Статус: `READY FOR REVIEW` в stacked PR #595. Подробный implementation note: [`issue-574-room-runtime-persistence-composition-2026-08-03.md`](issue-574-room-runtime-persistence-composition-2026-08-03.md).

Реализовано без выбора default для `Q-ROOM-003` и `Q-ROOM-007`:

- save format повышен с `15` до `16`;
- сохраняются aggregate/per-room versions, stable room/template identities, lifecycle, purpose, exact stock XYZ, material ledgers, completed units и active job ids;
- completed-room provenance сохраняется с exact ordered room cells;
- deterministic room job/transit-stack sequence сохраняется и валидируется против persisted ids;
- load сначала восстанавливает Domain aggregate, затем проверяет provenance/world bounds/overlap, active jobs, Inventory reservations и JobSystem ownership;
- malformed provenance, orphan runtime job или stale sequence отклоняют load до publication;
- migration `save.v15_to_v16.room_infrastructure` создаёт пустой room section без phantom identities и поднимает sequence выше существующих parseable room ids;
- manual save и autosave переносят один authoritative room runtime snapshot;
- Unity проецирует только completed template instances и отклоняет provenance drift;
- room/stock/job synchronization выполняется до ordinary assignment;
- existing Inventory/JobSystem/Haul/candidate/skill owners переиспользуются;
- hauling движется source → exact temporary-stock cell, work — к exact work cell;
- route failure освобождает worker/position/resident-slot claims, сохраняя job-owned material reservation;
- stage execution вызывает existing Application delivery/work/finalize handlers;
- post-terrain commit выполняет повторную room synchronization;
- capture/restore восстанавливает aggregate, provenance и sequence и пересоздаёт handlers.

Validation PR #595 на code head `9d55c5778c0d7667b21ed39507174b8efbb3f2c5`:

- Quality run `30821792583`: success;
- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks passed;
- Unity source contracts passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1460/1460`;
- active save round-trip, migration, stale provenance/sequence, deterministic serialization и Unity-composition contracts passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Unity workflow `30821792579` recorded blocked evidence; actual EditMode/PlayMode execution and executed-runtime-evidence validation were skipped.

#### Slice 4B-2b — room marker, menu, read model и progress visuals

Статус: `READY FOR REVIEW` в PR #599. Подробный implementation note: [`issue-574-room-presentation-2026-08-03.md`](issue-574-room-presentation-2026-08-03.md).

Реализовано без выбора default для `Q-ROOM-003` и `Q-ROOM-007`:

- `RoomInfrastructurePresenter` проецирует authoritative aggregate, completed provenance и typed diagnostics без второго gameplay owner;
- world marker создаётся только для completed template-room identity и удаляется при исчезновении authoritative projection;
- marker click обрабатывается раньше resident movement/excavation, consumes click и очищает competing selections;
- existing central blocking HUD показывает template, lifecycle, count `0|1`, requested/active purpose, material delivery/work progress и blocker;
- `Improve` доступен только для `Unimproved + count 0`, повторный order не создаётся;
- pre-work cancel доступен только по authoritative cancellation diagnostics;
- pre-order purpose остаётся transient UI intent до successful order, после чего source truth только `RequestedPurpose` Domain;
- purpose choices: `None`, `Bedroom`, `KitchenDining`, `Workshop`, `Farm`;
- каждый completed material unit создаёт stable collider-free rebuildable piece;
- stone tiles, mushroom-leg posts, iron braces и crystal accents размещаются детерминированно по room bounds, ordinal и required count;
- presentation driver читает authoritative session projection и обновляет marker/HUD visuals после order, delivery, work, cancel, completion и load;
- resident, building, job, BuildingBox, marquee, Vuker и terrain-cell selection очищают selected room;
- checked-in Play Mode scenario покрывает clickable marker, единственный enabled collider, selection retention и rebuildable progress removal.

Validation PR #599 на code head `88e89a6d2d1545a80aae248f068d130f6c71694a`:

- Quality run `30829868966`: success;
- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks passed;
- Unity source contracts and native-field initialization checks passed;
- Release build: `0` warnings, `0` errors;
- full .NET suite: `1463/1463`;
- room presenter, command-wiring, input-ordering и partial-visual regressions passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Unity workflow `30829869713` recorded blocked evidence; actual EditMode/PlayMode execution and executed-runtime-evidence validation were skipped.

Не входят до ответа на blockers:

- точный bonus membership/check cadence;
- неполные Tall/Farm/Workshop content profiles.

### Slice 5 — confirmed purpose bonuses и compact profiles

После закрытия `Q-ROOM-003` и `Q-ROOM-007`:

- Bedroom Alertness `1.20`;
- Kitchen cooking/Nutrition `1.15`;
- Workshop production `1.15` и `+1` effective internal-stock capacity;
- Farm production `1.15`;
- explicit `BuildingDefinition × RoomTemplate × Purpose` profiles;
- wall-attached visual variants и mirrored rack/output anchors;
- purpose-switch packing lifecycle.

### Slice 6 — manual `U` placement и Unity presentation

- exact resident stack reservation;
- owner-locked manual job;
- interruption removes ghost/job and leaves material with owner resident;
- wooden support commit updates rolling anchor chain;
- stone floor/junction trim remains decorative;
- tunnel support/trim marker, progress и typed reasons;
- input shielding before movement/excavation;
- Unity source-contract and Play Mode tests.

### Slice 7 — collapse

Общая часть до ответа `Q-TUNNEL-006A`:

- deterministic delay `1..3` game days;
- eligibility excludes room, vertical, junction, wooden-supported и door-protected cells;
- deterministic candidate selection and actor substitution;
- collapse to `terrain.sand` without deposit/output;
- buried item identities/quantities recover exactly once after re-excavation;
- repeat scheduling for re-excavated unreinforced segment;
- save random sequence and schedule state.

Полностью actor-blocked retry schedule остаётся незавершённым до подтверждённого interval.

## 5. Проверка полного workflow

Для каждого slice обязательны:

- Domain tests критических инвариантов;
- Application integration tests commands/jobs/reservations;
- deterministic ordering/replay tests;
- save/load and migration tests;
- source-contract tests для Unity wiring;
- Play Mode/end-to-end scenario для runtime interaction;
- повторный следующий шаг, cancel/failure/retry и presentation refresh;
- `python tools/quality/check_quality.py`;
- Release restore/build/full .NET suite;
- headless deterministic smoke/soak, когда slice входит в simulation loop.

Нельзя повышать статус до `IMPLEMENTED` только по source-contract или компиляции. `VERIFIED` требует фактического Unity Play Mode/equivalent runtime evidence.

## 6. Статус и отчётность

- design и issue обновляются до каждого изменения подтверждённой истины;
- после каждого merged slice этот файл получает фактические changed files и validation evidence;
- `docs/systems/README.md` остаётся `QUESTIONNAIRE`, пока блокирующие observable decisions открыты;
- issue #574 остаётся открытым до выполнения полного acceptance и runtime evidence.
