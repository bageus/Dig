# World-owned excavation authority

Статус: архитектурная реализация слита в PR #463. Draft PR #472 добавляет фактические Unity Play Mode regression scenarios и отдельный CI workflow; статус `VERIFIED` допускается только после успешного Unity Test Runner run.

Authoritative specifications:

- [`../design/excavation-command-execution.md`](../design/excavation-command-execution.md);
- [`../design/runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md);
- [`../design/resident-movement-occupancy-and-vertical-traversal.md`](../design/resident-movement-occupancy-and-vertical-traversal.md).

Tracking: [#388](https://github.com/bageus/Dig/issues/388), [#386](https://github.com/bageus/Dig/issues/386), [#87](https://github.com/bageus/Dig/issues/87), [#15](https://github.com/bageus/Dig/issues/15).

## Исправленная первопричина

Предыдущая реализация разделяла excavation progress между `ExcavationWorkCoordinator`, World, terrain renderer, cursor и navigation projections. Завершённые quarters могли исчезнуть визуально, пока `CellState` оставался полностью solid и designated. Из этого состояния следовали повторная копка визуально пустой клетки, невозможность войти в неё, ложная опора над шахтой и stale item gravity.

Теперь World является единственным владельцем:

- `CompletedExcavationQuarters`;
- target-owned `ExcavationCutPattern`;
- исходного material provenance для mining output;
- атомарного перехода `4/4 -> empty + designation cleared`.

Каждый completed quarter коммитится в World немедленно. Renderer, cursor, Jobs reconciliation, support, Navigation и save/load читают один World snapshot. Cleanup job/output после открытия клетки остаётся idempotent и не может вернуть terrain в solid state.

## Геометрия и рабочая позиция

`ExcavationCutPattern` определяется планом target, а не текущей боковой позицией worker:

- vertical front-slice tunnel использует `HorizontalRows` и удаляет ближнюю верхнюю строку перед нижней;
- horizontal tunnel использует `VerticalColumns`;
- depth excavation использует отдельный `DepthFace`.

После каждого quarter runtime повторно оценивает actor support и work position. При наличии supported side cell используется standing stance, затем рассматривается достижимая adjacent-depth position, иначе shaft cell остаётся authoritative work cell с stationary climbing stance спиной к камере.

## Navigation

Transitions имеют явный тип:

- `SupportedWalk`;
- `VerticalClimb`;
- `ShaftGapTraverse`;
- `DepthTraverse`.

Path search минимизирует число `ShaftGapTraverse`, затем обычную стоимость маршрута и deterministic tie-break. Поэтому горизонтальный проход через открытую шахту использует climbing presentation только при отсутствии доступного depth detour; при наличии обхода resident предпочитает depth route.

## Save/load

Save format v8 сохраняет completed-quarter mask, cut pattern и source material. Миграция v7 создаёт пустое excavation-progress состояние без изменения существующих cells/jobs. Round-trip обязан восстанавливать 1/4–3/4 geometry, cursor eligibility, support и continuation без повторного начала работы.

## Regression coverage

Domain/Application tests покрывают atomic fourth-quarter commit, idempotent retry, source material provenance, vertical horizontal-row ordering, support-loss posture, adjacent-depth work position, typed shaft-gap transition, depth detour и save v8 migration/round-trip.

Unity Play Mode coverage в PR #472 включает:

- реальную 1/4–3/4 quarter geometry и удаление quarter colliders;
- `4/4` World projection без designation и без Dig interaction collider;
- двенадцать последовательных открытий с синхронными geometry, interaction proxy и route;
- `ShaftGapTraverse` climbing presentation при неизменном Y и cleanup после завершения/interrupt;
- stationary climbing work pose и cleanup;
- предпочтение depth detour;
- двух встречных vertical climbers без блокировки;
- direct excavation без второго manual owner;
- combat-priority interruption, освобождающий ordinary excavation job без cancellation/completion.

`.github/workflows/unity-playmode.yml` запускает `game-ci/unity-test-runner@v4` на Unity `6000.0.71f1`, `testMode: PlayMode` и сохраняет test artifacts. Activation resolver различает два состояния:

- при настроенных Unity secrets Play Mode запускается и его test failure блокирует PR;
- без Unity secrets licensed step пропускается с warning и Job Summary, поэтому отсутствие внешних credentials не маскируется как падение тестов.

Для Personal требуются `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`; для Pro — `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`.

`tools/quality/check_unity_excavation_playmode_contracts.py` является отдельным blocking source-contract gate, но не заменяет успешный Unity run.

## Фактическое evidence PR #472

Quality, Release build, .NET tests, headless smoke, standard soak, large soak и оба export workflow проходят на ветке PR. Первые Unity workflow runs завершались failure до запуска Editor, потому что activation preflight ошибочно трактовал отсутствие repository secrets как test failure. PR #472 исправляет это: отсутствие credentials теперь даёт explicit skipped-evidence warning, а не красный тестовый check.

Код и исполняемые Play Mode scenarios подготовлены, но система остаётся не `VERIFIED` до настройки license secrets и успешного повторного run с XML/log artifacts.

Item support-loss trigger остаётся общим workflow системы #387. Excavation гарантирует, что полный World commit и refresh precede новые pickup/hauling reservations; выбор atomic или multi-tick item falling state не расширяется здесь, пока Q-ITEM-006 остаётся открытым.

## Unity enum-assertion compile regression (2026-07-28)

Unity Safe Mode reported `CS1503` in `WorldOwnedExcavationPlayModeTests.cs`: the bundled NUnit API resolved `Does.Contain(...)` through its string-oriented overload, so `TunnelTraversalKind.DepthTraverse` could not be converted to `string`. The following `Does.Not.Contain(...)` assertion had the same API-drift risk.

The fixture now evaluates the typed `IReadOnlyList<TunnelTraversalKind>` through LINQ `Contains` and asserts the resulting boolean with `Is.True` / `Is.False`. This keeps the navigation behavior unchanged while avoiding NUnit overload inference differences between the repository test runner and Unity's embedded test framework.

`check_unity_excavation_playmode_contracts.py` requires both typed `TraversalKinds.Contains(...)` checks and rejects the obsolete enum-valued `Does.Contain` / `Does.Not.Contain` forms.

## Spatial designation lifecycle regression (2026-07-28)

Unity runtime exception `world.excavation.quarter_requires_designation` exposed a depth-excavation creation gap: `DesignateSpatialExcavation` published `SpatialDigJobDefinition` and Presentation tint without first committing `CellDesignation.Dig` to authoritative World state. Quarter cadence therefore reached the strict World commit with a solid but undesignated target.

The runtime now commits the exact target designation before job publication, journals that World mutation and marks derived projections dirty. `SyncDigDesignationJobsHandler` treats a nonterminal `SpatialDigJobDefinition` as the job owner for its designated target, suppressing and cleaning legacy duplicate ordinary `DigJobDefinition` instances. A final pre-swing guard removes stale coordinator cadence without generating hidden progress if any job reaches a solid undesignated target.

Regression coverage includes Application tests for spatial designation ownership/duplicate cleanup, a Unity-facing source contract, and `SpatialExcavationDesignationPlayModeTests`, which creates a real demo depth plan, verifies the World designation, verifies a single target job, and advances the same quarter commit path that previously threw.
