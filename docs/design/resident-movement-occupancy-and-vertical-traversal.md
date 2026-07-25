# Перемещение гномов, совместная клетка и вертикальное карабканье

Статус: `QUESTIONNAIRE`.

Tracking issue: [#386](https://github.com/bageus/Dig/issues/386).

Связанные документы:

- [`../implementation/navigation.md`](../implementation/navigation.md);
- [`../implementation/layered-tunnel-movement.md`](../implementation/layered-tunnel-movement.md);
- [`ladders-and-elevators.md`](ladders-and-elevators.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`entity-fall-knockback-and-vertical-shafts.md`](entity-fall-knockback-and-vertical-shafts.md).

## 1. Назначение

Система разрешает естественное встречное движение в достаточно широком тоннеле. Гномы обходят друг друга внутри визуальной ширины прохода, не блокируют маршрут из-за совпадения logical cell и не проходят телами друг сквозь друга.

## 2. Владение состоянием

- Agents владеет authoritative logical cell каждого resident.
- Navigation владеет route/traversal result.
- Movement/Application валидирует cell transition и конфликтующие переходы одного tick.
- Presentation владеет interpolation, directional lateral lane, local avoidance и climbing animation.

Visual lane не является отдельной навигационной клеткой и не сохраняется.

## 3. Подтверждённое горизонтальное движение

- обычный тоннель визуально достаточно широк для обхода гномов;
- гном, движущийся вправо, идёт немного правее центральной линии тоннеля;
- гном, движущийся влево, идёт немного левее центральной линии тоннеля;
- встречные гномы проходят рядом по разным lateral lanes, а не сквозь друг друга;
- если гном стоит по центру тоннеля, движущиеся гномы обходят его по свободной стороне;
- несколько residents могут иметь одну logical cell, поскольку logical cell не моделирует точную ширину тела;
- authoritative route остаётся клеточным, а lateral displacement является rebuildable Presentation state;
- direct opposite swap `A -> B cell` и `B -> A cell` одним simulation step остаётся запрещённым как логический transition;
- local avoidance не должен телепортировать, пропускать route cell или менять job reachability.

Таким образом, обычное горизонтальное движение не должно использовать «одна клетка — один гном» как блокирующую occupancy policy.

## 4. Directional lane и local avoidance

Базовые visual targets:

- движение вправо — правая lateral lane;
- движение влево — левая lateral lane;
- stationary/working resident — центральная или рабочая позиция;
- обход stationary actor — временный offset в свободную сторону с возвратом на directional lane.

Lane resolver должен быть deterministic для одинакового snapshot и не зависеть от порядка Unity objects. Visual bodies не должны пересекаться на экране, но physics collider не становится вторым владельцем logical movement.

Количество гномов одного направления, выбор стороны при нескольких obstacles и поведение в сужениях остаются открытыми.

## 5. Conflict resolution

Решение logical transitions учитывает полный набор planned movements одного tick. Partial per-agent commit не должен зависеть от случайного порядка объектов.

Подтверждено:

- совпадение destination logical cell само по себе не блокирует нормальный горизонтальный проход;
- direct swap одним tick запрещён;
- visual lane/local avoidance решает обычный обход в широком тоннеле;
- permanent wait из-за stationary resident в центре запрещён, если существует визуально свободная сторона.

Hard deadlock policy требуется только для геометрии, где обход действительно невозможен: vertical passage, doorway, ladder/elevator link или специально узкий footprint.

## 6. Vertical traversal

Для перехода между связанными vertical tunnel cells:

- Domain/Navigation проверяет допустимый transition;
- resident начинает climbing visual state;
- корпус ориентируется спиной к основной камере;
- руки и ноги получают climbing animation;
- interpolation идёт между projected centers/surfaces;
- после authoritative arrival visual возвращается к normal locomotion;
- interruption/replan не оставляет resident в climbing pose.

Правила встречного движения двух climbers в одной вертикальной колонне остаются открытыми.

## 7. Инварианты

- один resident имеет одну authoritative logical position;
- один tick не содержит взаимный direct swap;
- visual overlap не используется как collision authority;
- shared-cell policy не создаёт teleport или route skip;
- right/left lane выбирается из направления движения, а не из случайного object order;
- stationary center actor не блокирует широкий тоннель, если доступна обходная visual lane;
- save/load восстанавливает cells и active action, но не lateral offsets;
- падение в vertical shaft не является обычным climbing transition и описывается в #396.

## 8. Решённые вопросы

- **Q-MOVE-001 (обычный горизонтальный тоннель):** жёсткий лимит occupancy по logical cell не используется для блокировки прохода; точная body capacity решается visual lanes/local avoidance.
- **Q-MOVE-002:** встречные гномы могут пройти через одну logical cell за несколько ticks по разным visual lanes, но direct logical swap одним tick запрещён.
- **Q-MOVE-003 (обычный тоннель):** directional lanes устраняют необходимость приоритета между встречными residents при наличии места для обхода.
- **Q-MOVE-004 (обычный тоннель):** stationary resident обходится, а не заставляет другого retreat/wait.

## 9. Открытые вопросы

- **Q-MOVE-005:** применяется ли wall-climb visual к ladders/elevators до отдельных animations?
- **Q-MOVE-006:** могут ли building footprints/work positions физически сужать тоннель и отключать обычный обход?
- **Q-MOVE-007:** сколько visual sub-lanes используется для нескольких гномов, движущихся в одном направлении, и должны ли они выстраиваться друг за другом?
- **Q-MOVE-008:** как выбирается сторона обхода, если stationary actor не по центру или обе стороны частично заняты?
- **Q-MOVE-009:** что происходит при встрече двух climbers в vertical tunnel: ожидание у входа, priority или возможность разойтись?
- **Q-MOVE-010:** какие geometry links считаются реально однополосными и требуют deadlock recovery?

## 10. Диагностика и acceptance

Показываются route, next cell, logical transitions, current directional lane, avoidance target, rejected direct swap, obstacle actor, wait duration и visual locomotion mode.

Тесты:

- два residents навстречу используют разные lanes;
- движущийся resident обходит stationary center resident;
- три и более residents в одном участке;
- несколько residents одного направления;
- no direct swap property;
- doorway/vertical single-lane recovery после утверждения policy;
- vertical up/down transition;
- interruption и save/load mid-route;
- Play Mode проверка отсутствия visual pass-through.
