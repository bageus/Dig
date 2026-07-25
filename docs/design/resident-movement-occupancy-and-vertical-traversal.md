# Перемещение гномов, совместная клетка и вертикальное карабканье

Статус: `QUESTIONNAIRE`.

Tracking issue: [#386](https://github.com/bageus/Dig/issues/386).

Связанные документы:

- [`../implementation/navigation.md`](../implementation/navigation.md);
- [`../implementation/layered-tunnel-movement.md`](../implementation/layered-tunnel-movement.md);
- [`ladders-and-elevators.md`](ladders-and-elevators.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md).

## 1. Назначение

Система разрешает встречное движение в узких тоннелях без вечной блокировки, но запрещает физически невозможное прохождение residents друг сквозь друга.

## 2. Владение состоянием

- Agents владеет authoritative logical cell каждого resident.
- Navigation владеет route/traversal result и производными occupancy costs.
- Movement/Application валидирует cell transition и разрешает конфликты одного tick.
- Presentation владеет interpolation, lateral offset и climbing animation.

## 3. Подтверждённое поведение

- несколько residents могут временно находиться в одной logical cell, чтобы обойти друг друга;
- их visual positions разводятся внутри клетки;
- direct opposite swap `A->B cell` и `B->A cell` одним simulation step запрещён;
- visual bodies не должны проходить друг через друга;
- authoritative transition определяется logical cells, а не текущим interpolated transform;
- vertical tunnel transition отображается как карабканье по стене спиной к основной камере;
- руки и ноги получают climbing animation;
- visual offset не изменяет route, save или job reachability.

## 4. Conflict resolution

Решение переходов должно быть deterministic и учитывать полный набор planned movements одного tick. Partial per-agent commit не должен зависеть от случайного порядка Unity objects.

Подтверждены только запрет direct swap и возможность temporary shared cell. Maximum occupancy, уступание и deadlock recovery остаются открытыми.

## 5. Vertical traversal

Для перехода `Y` между связанными vertical tunnel cells:

- Domain/Navigation проверяет допустимый transition;
- resident начинает climbing visual state;
- корпус ориентируется спиной к основной камере;
- interpolation идёт между projected centers/surfaces;
- после authoritative arrival visual возвращается к normal locomotion;
- interruption/replan не оставляет resident в climbing pose.

## 6. Инварианты

- один resident имеет одну authoritative logical position;
- один tick не содержит взаимный direct swap;
- visual overlap не используется как collision authority;
- shared-cell policy не создаёт teleport или route skip;
- deadlock resolution не отменяет прямой приказ без reason;
- save/load восстанавливает cells и active action, но не lateral visual offsets.

## 7. Открытые вопросы

- **Q-MOVE-001:** maximum occupancy обычной клетки.
- **Q-MOVE-002:** разрешён ли многотиковый exchange через temporary shared cell.
- **Q-MOVE-003:** priority rule для встречных residents.
- **Q-MOVE-004:** retreat-to-bay, wait или replan при deadlock.
- **Q-MOVE-005:** применяется ли wall-climb visual к ladders/elevators до отдельных animations.
- **Q-MOVE-006:** могут ли предметы/строительные work positions уменьшать effective occupancy клетки.

## 8. Диагностика и acceptance

Показываются route, next cell, occupancy claims, rejected transitions, priority reason, wait duration, replan count и current visual locomotion mode.

Тесты:

- два residents навстречу в коридоре;
- три и более residents в узком участке;
- direct orders с разным приоритетом;
- no direct swap property;
- bounded recovery;
- vertical up/down transition;
- interruption и save/load mid-route;
- Play Mode проверка отсутствия visual pass-through.
