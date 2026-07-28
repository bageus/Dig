# World-owned excavation authority

Статус: реализация опубликована в draft PR #463; Unity Editor / Play Mode verification остаётся обязательной до статуса `VERIFIED`.

Authoritative specifications:

- [`../design/excavation-command-execution.md`](../design/excavation-command-execution.md);
- [`../design/runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md);
- [`../design/resident-movement-occupancy-and-vertical-traversal.md`](../design/resident-movement-occupancy-and-vertical-traversal.md).

Tracking: [#388](https://github.com/bageus/Dig/issues/388), [#386](https://github.com/bageus/Dig/issues/386), [#87](https://github.com/bageus/Dig/issues/87).

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

Добавлены Domain/Application tests для atomic fourth-quarter commit, idempotent retry, source material provenance, vertical horizontal-row ordering, support-loss posture и adjacent-depth work position. Navigation tests покрывают typed shaft-gap transition и предпочтение depth detour как в tunnel volume, так и в общем pathfinder. Save tests переведены на v8 и проверяют migration/round-trip. Unity source contracts требуют World-derived cursor/terrain progress и typed climbing movement.

Repository CI не заменяет Unity Test Runner. Финальная runtime проверка должна пройти длинную горизонтальную и вертикальную копку, interruption/retry, вход в каждую открытую cell, cursor removal, item fall, support-loss climbing stance и depth detour в одном Play Mode workflow.
