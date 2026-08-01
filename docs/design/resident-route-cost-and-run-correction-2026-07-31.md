# Коррекция стоимости маршрута и режима бега residents

Статус: `APPROVED`.

Tracking issue: [#386](https://github.com/bageus/Dig/issues/386).

Расширяет authoritative specification [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md) и не создаёт второго Navigation owner.

## Подтверждённые правила

- Общий `NavigationPathfinder` сравнивает route лексикографически: количество `ShaftGapTraverse` → количество `VerticalClimb` → traversal cost/length → deterministic `CellId` tie-break.
- При одинаковом количестве shaft-gap плоский supported/depth-обход выбирается раньше более короткого маршрута с карабканьем.
- Существующее предпочтение route без shaft-gap перед прямым пересечением шахты сохраняется.
- `VerticalClimb` медленнее обычного supported/depth movement.
- Обычное ненагруженное движение по supported/depth route отображается бегом.
- Carrying, tired, climbing, shaft-gap и другие explicit modes сохраняют собственную скорость и animation.

## Acceptance

- unit regression: flat detour побеждает более короткий vertical-climb route;
- прежний depth-detour/shaft-gap regression остаётся зелёным;
- run выбирается только для ordinary unburdened supported/depth movement;
- Play Mode подтверждает реальную скорость, run/climb animations, interruption cleanup и opposite climbers.

## Владение и сохранение

Authoritative cell, route и traversal kind остаются в Agents/Navigation. Run/climb pose, interpolation и offsets — Presentation-only и не сериализуются.
