# Режимы перемещения жителей

Статус: `APPROVED`.

Tracking issues: [#386](https://github.com/bageus/Dig/issues/386), [#137](https://github.com/bageus/Dig/issues/137).

Родительская спецификация: [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md).

Связанные системы: Navigation, Agents/Needs, Inventory, personal mobility, Jobs и Presentation.

## 1. Назначение

Система выбирает один типизированный режим для каждого authoritative cell transition resident. Один resolver применяется к automatic movement, manual tunnel order и spatial-work approach. Режим определяет fixed-tick cadence, длительность visual interpolation, action presentation и диагностическую причину, но не становится владельцем позиции или route.

## 2. Владение состоянием

- Agents владеет position, active intent и Alertness.
- Navigation владеет route и `TunnelTraversalKind`.
- Inventory владеет переносимыми предметами, BuildingBox category и cargo speed multiplier.
- Application `ResidentMovementModeResolver` объединяет эти snapshots в derived resolution.
- Presentation получает typed view model и не коммитит движение.

Movement mode, interpolation progress и last interruption view model не сохраняются как authoritative gameplay state. После load они вычисляются повторно.

## 3. Режимы

- `Normal` — обычный supported/depth transition без более сильной причины.
- `Tired` — Alertness находится на существующей critical границе `2000` или ниже.
- `ForcedFast` — игрок повторно назначил тот же destination уже активному manual route.
- `Fleeing` — authoritative active intent равен `Flee`.
- `Carrying` — resident переносит BuildingBox; fast/personal mobility запрещены.
- `Mobility` — выбран Reithamster или Hoverboard.
- `Climbing` — `VerticalClimb` или `ShaftGapTraverse`.

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

`ResidentMovementModeDefinition` хранит:

- `SpeedMultiplier` для fixed-tick cadence;
- `TransitionDurationMultiplier` для Presentation.

Inventory cargo multiplier умножается на mode speed multiplier. Authoritative movement ограничен одним cell transition за fixed tick; multiplier выше `1` не создаёт несколько commits в один tick. Все movement sources вызывают один cadence gate непосредственно перед transition.

Точные коэффициенты tired/fast/fleeing/mobility и legacy automatic-distance threshold относятся к Q-014 `BALANCE_TBD`. До утверждения production catalog использует нейтральные `1.0` definitions и не выдумывает legacy числа. Существующий authoritative Inventory cargo multiplier продолжает действовать.

## 7. Personal mobility boundary

Resolver поддерживает `Reithamster` и `Hoverboard`, forced-repeat и nullable automatic long-route policy. Production activation остаётся выключенной до появления stable item IDs/runtime definitions и утверждённых Q-014 values. Это validation boundary, а не fallback к строковым именам или скрытым magic coefficients.

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
- Hoverboard детерминированно сильнее Reithamster;
- repeat определяется только совпадением active destination;
- смена destination и repeat публикуют разные typed reasons;
- critical Alertness выбирает `Tired`;
- custom data definitions изменяют cadence и visual duration без изменения resolver code;
- neutral production definitions не изобретают Q-014 numbers;
- moving BuildingBox resident использует Carry presentation;
- source/unit tests покрывают priority, repeat, diagnostics и все movement sources;
- Unity Play Mode проверяет visual duration и Carry projection; фактический licensed run нужен для `VERIFIED`.

## 11. Открытые balance-параметры

Business workflow и priority утверждены. Открыты только Q-014 values:

- speed/duration multipliers для non-normal modes;
- automatic personal-mobility route threshold;
- stable Reithamster/Hoverboard runtime content definitions.
