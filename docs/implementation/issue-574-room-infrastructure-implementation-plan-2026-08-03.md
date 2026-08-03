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
- 1390/1390 .NET tests passed;
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

Фактические области:

- `src/Dig.Domain/Jobs/TunnelAutomaticWorkJobDefinition.cs`;
- `src/Dig.Domain/World/TunnelAutomaticWorkPlanner.cs`;
- `src/Dig.Application/Tunnels/`;
- `src/Dig.Application/Saving/TunnelAutomaticWorkJobSaveCodec.cs`;
- `src/Dig.Infrastructure/InMemory/InMemoryTunnelInfrastructureRepository.cs`;
- `src/Dig.Infrastructure/Saving/SaveGameCompositionRoot.cs`;
- `tests/Dig.Tests/TunnelInfrastructureApplicationTests.cs`;
- `tests/Dig.Tests/TunnelAutomaticWorkPlannerTests.cs`;
- `tests/Dig.Tests/TunnelAutomaticWorkJobSaveCodecTests.cs`.

Validation PR #579:

- architecture, file-size, C# 9 compatibility, dependency and Domain-boundary checks passed;
- Release build passed with `0` warnings and `0` errors;
- full .NET suite passed: `1401/1401`;
- new range, source, no-phantom-reservation, obsolete-target, interruption/reassignment and save-codec regressions passed;
- headless smoke passed at tick `20`;
- standard deterministic soak passed with replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents passed with replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Stage 2 v2/v3 exports passed;
- Unity workflow recorded blocked runtime evidence: actual EditMode/PlayMode execution was skipped because activation was unavailable, therefore runtime verification is not claimed.

### Slice 2B — topology synchronization, execution и junction trim

#### Slice 2B-1 — authoritative junction target and automatic trim job

Статус: `READY FOR REVIEW` в PR #582. Подробный implementation note: [`issue-574-tunnel-junction-trim-lifecycle-2026-08-03.md`](issue-574-tunnel-junction-trim-lifecycle-2026-08-03.md).

Реализовано:

- `TunnelInfrastructureState` владеет одним pending/completed decorative stone-trim target на vertical-junction cell;
- left/right chains с одним junction origin не создают duplicate targets;
- stable owner выбирается по segment id и deterministic rebind выполняется при удалении одного направления;
- удаление segment system-cancels его automatic jobs и освобождает Inventory reservations;
- удаление последнего junction direction удаляет pending/completed trim provenance;
- low-priority `JunctionStoneTrim` job переиспользует range `20` и deterministic source selection из Slice 2A;
- no-source остаётся `Created` без phantom reservations;
- появление stone разрешает тот же job и переводит его в `Available`;
- completion убирает authoritative target, а synchronization отменяет stale job;
- save codec regression покрывает оба automatic work kinds;
- player cancellation не реализована до ответа `Q-TUNNEL-008`.

Validation PR #582:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks passed;
- Release build passed: `0` warnings, `0` errors;
- full .NET suite passed: `1412/1412`;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Stage 2 v2/v3 exports passed;
- Unity workflow recorded blocked runtime evidence; actual EditMode/PlayMode execution was skipped because activation was unavailable.

#### Slice 2B-2a — automatic work final commit

Статус: `READY FOR REVIEW` в stacked PR #584. Подробный implementation note: [`issue-574-tunnel-automatic-work-execution-2026-08-03.md`](issue-574-tunnel-automatic-work-execution-2026-08-03.md).

Реализовано:

- final commit принимает только `TunnelAutomaticWorkJobDefinition` в `InProgress/Finalize` с authoritative worker;
- source повторно проверяется по exact stack identity, item id, исходной world cell и reservation владельца-job;
- current support/trim target повторно сверяется с `TunnelInfrastructureState` непосредственно перед mutation;
- wooden support расходует ровно один `material.mushroom_leg`, становится structural anchor и переносит rolling target;
- junction trim расходует ровно один `material.stone`, остаётся decorative и не даёт structural protection;
- final stage завершает job и освобождает JobSystem claims;
- worker получает ровно `70` fixed-point units (`+0.7`) Woodworking или Stonework;
- skill idempotency использует stable automatic job identity;
- stale target, missing reservation и changed source отклоняются до material, infrastructure и skill mutation;
- terminal replay не может повторно расходовать материал или начислить skill.

Validation PR #584:

- architecture, file-size, C# 9 compatibility, compiler baseline, dependency и Domain-boundary checks passed;
- Release build passed: `0` warnings, `0` errors;
- full .NET suite passed: `1416/1416`;
- четыре новых execution regression tests passed;
- headless smoke passed at tick `20`;
- standard deterministic soak replay hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak with 64 residents replay hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Unity workflow recorded blocked runtime evidence; actual EditMode/PlayMode execution was skipped because activation was unavailable.

#### Slice 2B-2b — excavation/template provenance topology reconciliation

Осталось:

- synchronization из completed excavation/template-room provenance;
- deterministic creation/removal horizontal segments at room exits and vertical junctions;
- runtime stage/movement composition and Unity projection;
- interruption policies reuse Slice 2A;
- player cancellation не реализуется до ответа `Q-TUNNEL-008`.

### Slice 3 — persistence и migration для tunnel infrastructure

- save ordered segments, anchor cells/kinds, next target identity, decorative targets и reservations;
- load пересчитывает только derived target от последнего completed anchor;
- obsolete target не восстанавливается;
- versioned migration не добавляет anchors в legacy saves без evidence;
- deterministic save round trip и idempotency tests.

### Slice 4 — room-upgrade core

Можно реализовать до закрытия layout/membership вопросов:

- stable room infrastructure identity для completed template rooms;
- `UpgradeOrderCount` только `0|1`;
- nearest reachable free temporary-stock cell с stable tie-break;
- delivery/material ledger;
- cancel только до первого actual work interval;
- delivered items остаются в комнате и освобождаются для ordinary logistics;
- после work start операция обязана завершиться;
- per-unit commit, partial progress и material-specific `+0.5` skill exactly once;
- requested purpose может меняться до completion без reset;
- save/load и diagnostics.

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
- room marker/menu, count `0|1`, requested/active purpose, progress and typed reasons;
- input shielding before movement/excavation;
- Unity source-contract and Play Mode tests.

### Slice 7 — collapse

Общая часть до ответа `Q-TUNNEL-006A`:

- deterministic delay `1..3` game days;
- eligibility excludes room, vertical, junction, wooden-support and door-protected cells;
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
