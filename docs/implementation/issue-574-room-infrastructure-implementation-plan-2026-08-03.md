# Issue 574 — план реализации назначения комнат и инфраструктуры тоннелей

Статус: `IN PROGRESS` в ветке `agent/issue-574-room-infrastructure-foundation`.

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
- `JobsState` остаётся владельцем lifecycle, worker/position claims и retry state.
- Presentation только проецирует authoritative snapshots и отправляет commands.

Новые Domain-типы не зависят от Unity, файловой системы или Infrastructure layer.

## 4. Последовательность реализации

### Slice 1 — deterministic tunnel anchor foundation

Статус: выполняется первым.

- добавить engine-independent модели horizontal segment, anchor kind и next target;
- origin anchor: room exit или vertical junction;
- completed wooden support и completed door становятся structural anchors;
- next target находится ровно через 10 ordered horizontal cells после последнего актуального anchor;
- support/door раньше pending target отменяет derived old target и пересчитывает новый;
- stone trim никогда не становится anchor;
- target за пределами segment отсутствует;
- stable ordering и duplicate/idempotency guards;
- unit tests для `origin -> 10`, `support 5 -> 15`, `door 5 -> 15`, repeated commits, split segments и segment end.

Планируемые файлы:

- `src/Dig.Domain/World/TunnelInfrastructureModels.cs`;
- `src/Dig.Domain/World/TunnelInfrastructureState.cs`;
- `tests/Dig.Tests/TunnelInfrastructureAnchorTests.cs`.

### Slice 2 — Application synchronization и automatic jobs

- commands/queries для регистрации segments, completion wooden support/door и чтения diagnostics;
- synchronization из completed excavation/template-room provenance;
- range filter: 20 cells 3D Manhattan до occupied cell completed building;
- low-priority automatic support и junction-trim jobs;
- no-source остаётся pending без phantom reservation;
- interruption сохраняет automatic target/job;
- player cancellation не реализуется до ответа `Q-TUNNEL-008`.

Планируемые области:

- `src/Dig.Application/Tunnels/`;
- `src/Dig.Domain/Jobs/`;
- существующие building/excavation synchronization boundaries;
- integration tests в `tests/Dig.Tests/`.

### Slice 3 — persistence и migration для tunnel infrastructure

- save ordered segments, anchor cells/kinds, next target identity, decorative targets и reservations;
- load пересчитывает только derived target от последнего completed anchor;
- obsolete target не восстанавливается;
- versioned migration не добавляет anchors в legacy saves без evidence;
- deterministic save round trip и idempotency tests.

Планируемые области:

- `src/Dig.Application/Saving/`;
- `src/Dig.Infrastructure/Saving/SaveGameCompositionRoot.cs`;
- serialization/migration tests.

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
- room marker/menu, count `0/1`, requested/active purpose, progress and typed reasons;
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

## 6. Acceptance для первого PR slice

Первый PR может быть принят отдельно, если:

- существует один authoritative `TunnelInfrastructureState` для rolling anchors;
- room-exit/vertical-junction origin создаёт next target через 10 ordered cells;
- manual/automatic wooden support на cell 5 пересчитывает next target на cell 15;
- completed door на cell 5 делает то же;
- obsolete derived target не остаётся дубликатом;
- stone trim не влияет на anchor chain;
- repeated commit idempotent либо возвращает typed conflict без mutation;
- segment split и save-ready snapshots детерминированы;
- unit tests проходят;
- открытые gameplay blockers остаются явно перечислены и не реализованы предположениями.

## 7. Статус и отчётность

- design и issue обновляются до каждого изменения подтверждённой истины;
- после каждого merged slice этот файл получает фактические changed files и validation evidence;
- `docs/systems/README.md` остаётся `QUESTIONNAIRE`, пока блокирующие observable decisions открыты;
- issue #574 остаётся открытым до выполнения полного acceptance и runtime evidence.
