# World-owned excavation authority

Статус: архитектурная реализация слита в PR #463. Draft PR #472 добавляет фактические Unity Play Mode regression scenarios и отдельный blocking CI workflow; статус `VERIFIED` допускается только после успешного Unity Test Runner run.

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

`.github/workflows/unity-playmode.yml` запускает `game-ci/unity-test-runner@v4` на Unity `6000.0.71f1`, `testMode: PlayMode` и сохраняет test artifacts. Перед запуском workflow валидирует activation secrets и выдаёт конкретную ошибку вместо неразличимого runner failure. Для Personal требуются `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`; для Pro — `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`.

`tools/quality/check_unity_excavation_playmode_contracts.py` является отдельным source-contract gate, но не заменяет успешный Unity run.

## Фактическое evidence PR #472

Head `6a6ccac6446e5ead119cef3039077434bb1718eb`:

- Quality run #6052 (`30347650859`) — success: architecture/source contracts, excavation Play Mode source gate, Release build, .NET tests, headless smoke и оба deterministic soak profiles;
- Export Stage 2 v2 #524 (`30347650856`) — success;
- Export Stage 2 v3 #529 (`30347650830`) — success;
- Unity Play Mode #3 (`30347650866`) остановлен на `Validate Unity activation`: repository Actions secrets для Unity не настроены; Unity Editor и тесты не запускались.

Поэтому код и исполняемые Play Mode scenarios подготовлены, но система остаётся не `VERIFIED` до настройки license secrets и успешного повторного run с XML/log artifacts.

Item support-loss trigger остаётся общим workflow системы #387. Excavation гарантирует, что полный World commit и refresh precede новые pickup/hauling reservations; выбор atomic или multi-tick item falling state не расширяется здесь, пока Q-ITEM-006 остаётся открытым.
