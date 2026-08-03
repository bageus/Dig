# Синхронизация реального и игрового времени через единый коэффициент

Статус: `APPROVED`.

Decision date: 2026-08-03.

Tracking issue: [#601](https://github.com/bageus/Dig/issues/601).

Эта correction является authoritative для разделов времени и needs в [`unified-game-time-and-action-cadence.md`](unified-game-time-and-action-cadence.md) и [`runtime-needs-supply-sleep-food-recovery.md`](runtime-needs-supply-sleep-food-recovery.md).

## 1. Причина correction

После merge PR #603 игровая шкала оставалась распределена между несколькими владельцами:

- simulation driver использовал длительность fixed tick;
- аналоговые часы при отсутствии выбранного resident использовали legacy fallback `24 ticks/day`;
- passive Nutrition/Alertness/Health использовали `DailySchedule.TicksPerDay` конкретного resident.

Из-за этого UI clock, расписание и потребности могли идти с разными скоростями. Персональное расписание не должно быть владельцем длины игровых суток.

## 2. Единый коэффициент

`GameTimeCadence` является единственным владельцем преобразования real time в game time.

На normal speed (`x1`):

- `1 simulation tick = 1 real second`;
- `1 simulation tick = 24 game seconds`;
- authoritative коэффициент равен `24 game seconds / real second`;
- `150 real seconds = 150 ticks = 1 game hour`;
- `3 600 real seconds = 3 600 ticks = 1 game day`.

`x2` и `x4` умножают тот же коэффициент до `48` и `96 game seconds / real second`. Pause даёт коэффициент `0`; single-step коммитит ровно один tick и 24 game seconds.

Коэффициент задаётся в одном Domain contract. UI, needs, schedules, save/load diagnostics и tests не могут хранить отдельные копии `24`, `150` или `3 600` как независимую истину.

## 3. Global game clock

- Clock projection всегда строится из `SimulationClock.TickIndex` через `GameTimeCadence`.
- Выбор, hover или отсутствие resident не меняют скорость и фазу часов.
- Clock hands показывают global hour/minute/second projection.
- Resident schedule overlay может меняться по выбранному resident, но не управляет текущим игровым временем.
- Legacy fallback `24 ticks/day` запрещён.

## 4. Needs

- Passive Nutrition, Alertness и starvation Health используют global `GameTimeCadence.TicksPerDay`.
- `DailySchedule.TicksPerDay` определяет только разбиение расписания Work/Rest/Sleep и должен совпадать с global calendar в live composition.
- Изменение или legacy-восстановление персонального schedule resolution не может ускорить или замедлить hunger/alertness/Health.
- Full Nutrition расходуется за два global game days (`7 200 ticks`), Alertness — за три (`10 800 ticks`), starvation Health — за половину global day (`1 800 ticks`).
- Один simulation tick применяет не более одной passive delta.

## 5. Save/load и migration

- Existing simulation tick не масштабируется.
- Calendar projection после load вычисляется из сохранённого tick и текущего global coefficient.
- Legacy resident schedule с другой resolution не становится владельцем needs cadence; schedule должен быть нормализован/проецирован отдельно без replay passive deltas.

## 6. Diagnostics

Runtime diagnostics должны показывать:

- real seconds per normal tick;
- game seconds per simulation tick;
- effective game-seconds-per-real-second с учётом playback speed;
- global day/hour/minute/second;
- global ticks per hour/day;
- needs depletion periods в global ticks.

## 7. Acceptance

- на `x1` за 150 real seconds clock проходит ровно один game hour;
- на `x1` за 3 600 real seconds clock проходит ровно один game day;
- без selected/hovered resident clock использует тот же global projection;
- выбор resident меняет только schedule overlay, но не положение стрелок;
- resident с legacy/custom `DailySchedule.TicksPerDay = 24` не теряет полную Nutrition за 48 ticks;
- full Nutrition достигает нуля только на 7 200-м global tick без recovery;
- x2/x4 изменяют clock и needs одинаковым множителем, потому что оба зависят от количества committed ticks;
- pause не изменяет clock или needs, single-step изменяет их ровно на один tick;
- Domain, Application, Unity source-contract и Play Mode regressions покрывают clock без selection, needs с mismatched schedule resolution и playback multipliers.

## 8. Verification boundary

Build/source-contract tests недостаточно для `VERIFIED`. Нужен licensed Unity Play Mode scenario с секундомером: normal x1, отсутствие selection, затем выбор resident, pause/single-step/x2/x4 и наблюдение Nutrition на тех же committed ticks.
