# Режимы перемещения жителей

Статус: `APPROVED`.

Tracking issues: [#386](https://github.com/bageus/Dig/issues/386), [#137](https://github.com/bageus/Dig/issues/137), [#601](https://github.com/bageus/Dig/issues/601).

Родительские спецификации:

- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md);
- [`unified-game-time-and-action-cadence.md`](unified-game-time-and-action-cadence.md).

Связанные системы: Navigation, Agents/Needs, Inventory, personal mobility, Jobs и Presentation.

## 1. Назначение

Система выбирает один типизированный режим для каждого authoritative cell transition resident. Один resolver применяется к automatic movement, manual tunnel order и spatial-work approach. Режим определяет fixed-tick cadence, длительность visual interpolation, action presentation и диагностическую причину, но не становится владельцем позиции или route.

## 2. Владение состоянием

- Agents владеет position, active intent и Alertness.
- Navigation владеет route и `TunnelTraversalKind`.
- Inventory владеет переносимыми предметами, BuildingBox category и cargo speed multiplier.
- Application `ResidentMovementModeResolver` объединяет snapshots в derived resolution.
- Domain `ResidentInventoryMovementCadence` переводит fixed-point speed в число due cell transitions текущего tick.
- Presentation получает typed view model и не коммитит движение.

Movement mode, interpolation progress и fractional movement budget не сохраняются как authoritative gameplay state. После load они вычисляются повторно.

## 3. Режимы

- `Normal` — обычный supported/depth transition без более сильной причины; базовая скорость `1.25 cells/tick`.
- `Tired` — Alertness находится на границе `2000` или ниже; базовая скорость `1 cell/tick`.
- `ForcedFast` — игрок повторно назначил тот же destination уже активному manual route; базовая скорость `1.25 cells/tick`.
- `Fleeing` — authoritative active intent равен `Flee`; базовая скорость `1.25 cells/tick`.
- `Carrying` — resident переносит BuildingBox; базовая скорость `1 cell/tick` до Inventory cargo multiplier.
- `Mobility` — выбран Reithamster или Hoverboard; текущая fallback cadence `1.25 cells/tick`, точный personal profile остаётся content boundary.
- `Climbing` — `VerticalClimb` или `ShaftGapTraverse`; базовая скорость `0.5 cells/tick`.

## 4. Deterministic priority

Resolver применяет первый подходящий пункт:

1. `Climbing` для vertical/shaft-gap traversal;
2. `Carrying` для BuildingBox;
3. `Mobility`, когда personal mobility разрешена policy;
4. `Fleeing`;
5. `ForcedFast`;
6. `Tired`;
7. `Normal`.

Hoverboard имеет приоритет над Reithamster. BuildingBox блокирует оба personal mobility variants и `ForcedFast`. Climbing posture не заменяется carry/fast animation.

## 5. Repeat policy

Repeat не использует wall clock, frame time или приблизительное расстояние. Команда является повторной, когда новый manual route заменяет уже активный manual route того же resident и имеет тот же конечный `CellId`.

- другая цель публикует `ReplacedByCommand`;
- та же цель публикует `RepeatedCommand` и включает forced-repeat policy;
- replan сохраняет repeat flag;
- завершённый route больше не является repeat source.

## 6. Cadence и transition duration

Одна соседняя cell transition стоит `1000` fixed-point movement units.

- run добавляет `1250 units/tick`;
- walk добавляет `1000 units/tick`;
- climb добавляет `500 units/tick`.

На прямом supported route run создаёт `1, 1, 1, 2` due transitions за четыре последовательных ticks. Walk создаёт `1, 1, 1, 1`; climb — `0, 1, 0, 1`.

Tick с двумя run transitions не является teleport:

- после первого commit route пересчитывается из новой authoritative cell;
- второй переход использует тот же movement command source;
- текущий traversal edge повторно ограничивает budget, поэтому run не переносит второй substep в `VerticalClimb` или `ShaftGapTraverse`;
- каждый промежуточный `CellId`, traffic restriction, route freshness и interruption проверяются отдельно;
- needs, work, combat и production не выполняют второй simulation advance.

Inventory cargo multiplier умножается на mode speed multiplier. Derived remainder не создаёт вторую authoritative position и не сохраняется.

## 7. Personal mobility boundary

Resolver поддерживает `Reithamster` и `Hoverboard`, forced-repeat и nullable automatic long-route policy. Точные personal mobility speeds и automatic route threshold остаются Q-014/content decisions. Они не могут обходить shared cell-transition validation или создавать Unity-only movement owner.

## 8. Typed interruption reasons

Movement diagnostics используют:

- `Completed`;
- `ReplacedByCommand`;
- `RepeatedCommand`;
- `HigherPriorityAction`;
- `AgentDead`;
- `RouteUnavailable`;
- `TraversalRejected`;
- `MovementRejected`.

Failure одного resident не прекращает simulation loop. Последняя причина доступна read model/diagnostics и не изменяет World/Navigation authority.

## 9. Presentation

Presentation:

- умножает базовую transition duration на resolved visual multiplier;
- сохраняет `Carry` action во время движения BuildingBox;
- использует существующий climbing visual для climbing mode;
- не выводит authoritative mode только из Transform delta;
- не сохраняет interpolation или mode при save/load.

## 10. Acceptance

- automatic, manual и spatial-work movement используют один resolver и cadence gate;
- traversal mode имеет приоритет над carry/fast/flee visuals;
- BuildingBox блокирует forced fast и personal mobility;
- repeat определяется только совпадением active destination;
- critical Alertness выбирает `Tired`;
- Normal run выполняет ровно пять validated transitions за четыре ticks на прямом supported route;
- Tired/Carrying walk выполняет четыре transitions за четыре ticks до cargo penalty;
- Climbing выполняет две transitions за четыре ticks;
- второй run substep не меняет command source и не перескакивает intermediate cell;
- переход в climbing edge повторно ограничивает текущий tick budget;
- moving BuildingBox resident использует Carry presentation;
- Domain/Application/source tests покрывают cadence, priority, repeat и все movement sources;
- Unity Play Mode проверяет фактические cells, visual duration, Carry projection, interruption и следующий повторный маршрут; licensed run нужен для `VERIFIED`.

## 11. Открытые balance-параметры

Утверждены базовые run/walk/climb значения. Открыты только personal mobility параметры:

- отдельные Reithamster/Hoverboard speed profiles;
- automatic personal-mobility route threshold;
- stable Reithamster/Hoverboard runtime content definitions.
