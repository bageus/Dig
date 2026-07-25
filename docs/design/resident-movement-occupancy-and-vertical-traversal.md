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

Система обеспечивает непрерывное движение residents в горизонтальных и вертикальных тоннелях без блокировки из-за совпадения logical cell.

## 2. Владение состоянием

- Agents владеет authoritative logical cell resident.
- Navigation владеет route/traversal result.
- Movement/Application валидирует cell transitions.
- Presentation владеет interpolation, directional lane, spacing, local avoidance и climbing animation.

Visual lane и spacing не являются отдельными навигационными клетками и не сохраняются.

## 3. Горизонтальные directional lanes

- гном, движущийся вправо, идёт немного правее центра тоннеля;
- гном, движущийся влево, идёт немного левее центра;
- встречные гномы проходят рядом по разным lanes;
- stationary resident в центре обходится по свободной стороне;
- совпадение logical cell само по себе не блокирует проход;
- local avoidance не меняет route, job reachability или authoritative position.

Обычный горизонтальный тоннель не использует правило «одна клетка — один гном».

## 4. Несколько гномов одного направления

Гномы, движущиеся в одном направлении, идут цепочкой по одной directional lane.

Правила:

- порядок цепочки определяется текущим направлением и положением вдоль маршрута;
- идущий сзади сохраняет визуальный интервал;
- обгон внутри обычной клетки не требуется;
- chain spacing не блокирует logical route навсегда;
- временное сближение не должно превращаться в проход тел друг сквозь друга;
- при остановке переднего resident следующие замедляются или ждут, сохраняя bounded replan policy.

Количество параллельных sub-lanes для одного направления не используется.

## 5. Встречное горизонтальное движение

- противоположные направления используют разные lateral lanes;
- direct opposite logical swap одним simulation step запрещён;
- несколько residents могут временно находиться в одной logical cell;
- visual bodies не должны проходить друг сквозь друга;
- stationary center resident не создаёт permanent wait при наличии свободной стороны.

Hard deadlock policy требуется только для действительно однополосной геометрии.

## 6. Vertical traversal

Для связанного vertical tunnel:

- Domain/Navigation проверяет допустимый transition;
- resident входит в climbing visual state;
- корпус ориентируется спиной к основной камере;
- руки и ноги получают climbing animation;
- interpolation идёт между vertical cells;
- resident во время перехода считается карабкающимся по валидному vertical link, а не стоящим на отдельной floor support;
- обычный climbing workflow не генерирует `SupportLost` и не переводит actor в падение;
- падение возможно только после внешнего knockback/push/impact, описанного в [`entity-fall-knockback-and-vertical-shafts.md`](entity-fall-knockback-and-vertical-shafts.md);
- после arrival visual возвращается к normal locomotion;
- interruption/replan не оставляет resident в climbing pose.

В текущей модели vertical tunnel не разрушается под уже выполняющим переход actor. Если позже появятся обрушения, разрушаемые платформы или удаление активного traversal link, это потребует отдельной спецификации и не выводится из текущих правил движения.

### Встреча двух climbers

Два гнома, движущиеся навстречу в одном vertical tunnel, не блокируют друг друга и проходят друг сквозь друга.

Это явное исключение из горизонтального правила визуального непересечения:

- ожидание у входа не требуется;
- priority/retreat не требуется;
- vertical traversal обоих residents продолжает выполняться;
- Presentation допускает кратковременное visual overlap;
- authoritative positions и routes остаются валидными и не создают duplicate actor.

Точная техническая реализация same-tick crossing должна быть deterministic, но observable behavior зафиксировано: два climbers продолжают движение без блокировки.

## 7. Инварианты

- один resident имеет одну authoritative logical position;
- horizontal direct swap одним tick запрещён;
- horizontal lane выбирается по направлению, а не по случайному object order;
- residents одного направления движутся цепочкой;
- stationary actor не блокирует широкий тоннель при доступном обходе;
- vertical opposite climbers не блокируют друг друга;
- vertical visual overlap разрешён только как утверждённое traversal-исключение;
- valid climbing transition не создаёт unsupported actor state;
- actor не падает из vertical tunnel без подтверждённого external impact result;
- shared-cell policy не создаёт teleport или route skip;
- save/load не сохраняет lateral offsets, chain spacing или interpolation.

## 8. Решённые вопросы

- **Q-MOVE-001:** горизонтальный проход не использует жёсткий occupancy limit по logical cell.
- **Q-MOVE-002:** встречные horizontal residents проходят по разным lanes; direct logical swap одним tick запрещён.
- **Q-MOVE-003:** directional lanes устраняют необходимость приоритета в широком тоннеле.
- **Q-MOVE-004:** stationary resident обходится.
- **Q-MOVE-007:** residents одного направления идут цепочкой по одной lane.
- **Q-MOVE-009:** opposite climbers в vertical tunnel проходят друг сквозь друга без ожидания.
- **Q-MOVE-012:** vertical climbing использует валидный traversal link; сценарий самопроизвольной потери опоры во время обычного перехода отсутствует.

## 9. Открытые вопросы

- **Q-MOVE-005:** применяется ли wall-climb visual к ladders/elevators до отдельных animations?
- **Q-MOVE-006:** могут ли building footprints/work positions сужать тоннель и отключать обычный обход?
- **Q-MOVE-008:** как выбирается сторона обхода, если stationary actor смещён или обе стороны заняты?
- **Q-MOVE-010:** какие geometry links, кроме vertical tunnel, считаются реально однополосными?
- **Q-MOVE-011:** точный минимальный visual interval цепочки и реакция на резкую остановку переднего resident.

## 10. Диагностика

Показываются:

- route и next cell;
- logical transition;
- directional lane;
- chain predecessor и spacing target;
- avoidance target;
- rejected horizontal swap;
- vertical crossing state;
- active vertical link;
- external impact fall trigger, если он был;
- wait/replan reason;
- current locomotion mode.

## 11. Acceptance

- два horizontal residents навстречу используют разные lanes;
- moving resident обходит stationary center resident;
- несколько residents одного направления идут цепочкой;
- остановка переднего resident не создаёт permanent overlap;
- no horizontal direct swap property;
- два opposite climbers проходят друг сквозь друга без блокировки;
- normal vertical climbing не запускает fall без external impact;
- knockback/push в open shaft передаёт управление fall system;
- interruption и save/load mid-route сохраняют authoritative cell/action, но не presentation offsets;
- Play Mode подтверждает horizontal no-pass-through и разрешённый vertical overlap.
