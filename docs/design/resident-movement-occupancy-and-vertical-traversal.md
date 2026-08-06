# Перемещение гномов, совместная клетка и вертикальное карабканье

Статус: `APPROVED`.

Дополнение о непрерывной позиции: правила клеточного центра ниже сохраняются только
для грубой маршрутизации и legacy-команд. Авторитетная позиция внутри поверхности и
новая матрица способности карабкаться определены в
[`continuous-surface-movement.md`](continuous-surface-movement.md).

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
- Navigation владеет route и типизированными traversal edges: `SupportedWalk`, `VerticalClimb`, `ShaftGapTraverse`, `DepthTraverse`.
- Movement/Application валидирует cell transitions.
- Presentation проецирует authoritative traversal/work posture в interpolation, directional lane, permissive overlap fallback и climbing animation.

Visual lane, overlap offset и interpolation не являются отдельными навигационными клетками и не сохраняются.

## 3. Горизонтальные directional lanes

- гном, движущийся вправо, по возможности идёт немного правее центра тоннеля;
- гном, движущийся влево, по возможности идёт немного левее центра;
- directional offset является только presentation preference, а не collision/occupancy barrier;
- совпадение logical cell само по себе не блокирует проход;
- building footprints и work positions не сужают обычный тоннель и не создают однополосную геометрию;
- если preferred side занята, resident может пройти визуально сквозь другого actor либо Navigation выбирает любой доступный альтернативный route;
- local avoidance не меняет authoritative position и не создаёт permanent wait.

Обычный горизонтальный тоннель не использует правило «одна клетка — один гном».

## 4. Несколько гномов одного направления

Несколько residents одного направления используют одну directional preference, но обязательного chain spacing нет.

- фиксированный минимальный visual interval не задаётся;
- остановка переднего resident не блокирует задних навсегда;
- задний может временно пройти сквозь visual body переднего либо получить альтернативный route;
- отдельные parallel sub-lanes и one-way capacity не создаются;
- порядок authoritative transitions остаётся deterministic.

## 5. Встречное горизонтальное движение

- противоположные направления по возможности используют разные lateral offsets;
- direct opposite logical swap одним simulation step остаётся запрещённым;
- несколько residents могут временно находиться в одной logical cell;
- visual overlap разрешён как fallback, если preferred side занята;
- stationary resident не создаёт permanent wait: moving resident проходит сквозь него либо использует любой доступный обходной route;
- в текущей системе нет действительно однополосных transitions.

## 6. Vertical traversal

Для связанного vertical tunnel:

- Domain/Navigation проверяет допустимый transition;
- resident входит в climbing visual state;
- корпус ориентируется спиной к основной камере;
- руки и ноги получают climbing animation;
- interpolation идёт между vertical cells;
- resident во время перехода считается карабкающимся по валидному vertical link, а не стоящим на отдельной floor support;
- вход из поддерживаемой horizontal cell в первую открытую vertical-tunnel cell и выход из последней vertical cell в поддерживаемую horizontal cell являются частью того же валидного climbing route: vertical provenance достаточно у shaft endpoint перехода;
- две соседние по Y обычные open cells без vertical provenance не образуют climbing transition;
- обычный climbing workflow не генерирует `SupportLost` и не переводит actor в падение;
- падение возможно только после внешнего knockback/push/impact, описанного в [`entity-fall-knockback-and-vertical-shafts.md`](entity-fall-knockback-and-vertical-shafts.md);
- после arrival visual возвращается к normal locomotion;
- interruption/replan не оставляет resident в climbing pose;
- resident, выполняющий mining из vertical-tunnel cell без full actor support, остаётся в stationary climbing stance спиной к камере на всё время `PerformWork`; любой completed quarter supporting cell уже отменяет full actor support;
- terminal completion/release не возвращает unsupported resident в standing pose: climbing сохраняется до следующего traversal или supported landing;
- если новый job/direct order не назначен, movement planner выбирает ближайшую достижимую supported walk cell и спускает/выводит resident туда обычными typed traversal edges;
- горизонтальный переход через клетку, где vertical shaft уходит вниз и нет full floor support, является `ShaftGapTraverse`, а не обычной ходьбой; visual использует climbing stance даже при неизменном Y.
- path selection сначала минимизирует количество `ShaftGapTraverse`, затем длину маршрута и deterministic tie-break. Поэтому доступный depth-обход выбирается раньше прямого перехода через шахту, даже если он длиннее.

После полного excavation commit новая открытая horizontal или vertical cell обязана войти в authoritative movement/topology projection до следующей route/movement попытки. Resident не может видеть выкопанную клетку, но получать stale `closed/not traversable` из отдельного tunnel volume или movement surface.

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
- horizontal lane preference выбирается по направлению, а не по случайному object order;
- обязательный chain spacing отсутствует;
- stationary actor не блокирует широкий тоннель: разрешены overlap fallback или alternative route;
- building footprints/work positions не превращают тоннель в one-lane transition;
- vertical opposite climbers не блокируют друг друга;
- visual overlap разрешён в horizontal fallback и при vertical crossing;
- valid climbing transition и stationary shaft work не создают unsupported actor state;
- horizontal-to-shaft entry и shaft-to-horizontal exit не требуют ошибочно помечать horizontal floor cell как vertical tunnel;
- actor не падает из vertical tunnel без подтверждённого external impact result;
- shared-cell policy не создаёт teleport или route skip;
- горизонтальный shaft gap не маскируется `SupportedWalk`;
- route без shaft-gap имеет приоритет над более коротким route через gap;
- full excavation commit синхронизирует World, Navigation map, resident tunnel volume и movement surfaces до следующего authoritative transition;
- derived refresh failure не оставляет визуально открытую клетку логически закрытой;
- save/load не сохраняет lateral offsets, overlap offsets или interpolation.

## 8. Решённые вопросы

- **Q-MOVE-001:** горизонтальный проход не использует жёсткий occupancy limit по logical cell.
- **Q-MOVE-002:** встречные horizontal residents по возможности используют разные lanes; direct logical swap одним tick запрещён.
- **Q-MOVE-003:** directional offsets являются presentation preference, а не hard collision policy.
- **Q-MOVE-004:** stationary resident не блокирует проход; разрешены overlap fallback или alternative route.
- **Q-MOVE-005:** ladders и elevators используют собственные visuals, wall-climb animation к ним не применяется.
- **Q-MOVE-006:** building footprints/work positions не сужают обычный тоннель.
- **Q-MOVE-007:** обязательной цепочки и фиксированного spacing нет; residents могут overlap или reroute.
- **Q-MOVE-008:** при занятой preferred side допускается проход сквозь actor либо любой доступный обходной route.
- **Q-MOVE-009:** opposite climbers в vertical tunnel проходят друг сквозь друга без ожидания.
- **Q-MOVE-010:** в текущей системе нет действительно однополосных transitions.
- **Q-MOVE-011:** минимальный visual interval не задаётся; остановка переднего resident не создаёт permanent wait.
- **Q-MOVE-012:** vertical climbing использует валидный traversal link; сценарий самопроизвольной потери опоры во время обычного перехода отсутствует.
- **Q-MOVE-013:** entry/exit между поддерживаемой horizontal cell и shaft cell является vertical transition, если shaft endpoint имеет vertical provenance; обе клетки не обязаны ошибочно классифицироваться как vertical.
- **Q-MOVE-014:** mining из shaft cell без пола использует stationary climbing stance спиной к камере; authoritative resident cell остаётся shaft work cell.
- **Q-MOVE-015:** horizontal crossing над открытым vertical shaft является `ShaftGapTraverse` и использует climbing visual.
- **Q-MOVE-016:** route planner предпочитает любой достижимый route без `ShaftGapTraverse`, включая depth-обход, и только затем сравнивает длину.
- **Q-MOVE-017:** partial excavation supporting cell отменяет full actor support после первого completed quarter.
- **Q-MOVE-018:** unsupported posture переживает terminal work state; новый job имеет приоритет, иначе deterministic recovery route ведёт к ближайшей supported walk cell.

## 9. Открытые вопросы

Нет открытых business rules для текущего scope. Новые ограничения collision/one-lane geometry требуют отдельного изменения authoritative specification.

## 10. Диагностика

Показываются:

- route и next cell;
- logical transition;
- preferred directional lane;
- overlap fallback или alternative-route reason;
- rejected horizontal swap;
- vertical crossing state;
- active vertical link;
- external impact fall trigger, если он был;
- wait/replan reason;
- current locomotion mode.

## 11. Acceptance

- два horizontal residents навстречу по возможности используют разные lane offsets;
- occupied preferred side разрешает visual overlap fallback или alternative route;
- stationary center resident не создаёт permanent wait;
- несколько residents одного направления не требуют fixed chain spacing;
- остановка переднего resident не блокирует задних навсегда;
- no horizontal direct swap property;
- два opposite climbers проходят друг сквозь друга без блокировки;
- normal vertical climbing не запускает fall без external impact;
- после horizontal excavation resident входит в новую cell и продолжает frontier job без redraw;
- после vertical/depth excavation новая cell сразу доступна valid climbing transition, включая entry из horizontal floor cell в первую shaft cell;
- resident, mining sideways/depth from unsupported или partially cut support, остаётся спиной к камере в climbing pose во время работы;
- completion/interrupt без нового job не включает standing на unsupported cell; resident остаётся climbing и достигает nearest supported walk cell;
- новый reachable job после completion имеет приоритет над idle support recovery;
- horizontal crossing через shaft gap использует climbing pose, даже если logical Y не меняется;
- при наличии depth-обхода route не использует shaft-gap crossing;
- knockback/push в open shaft передаёт управление fall system;
- interruption и save/load mid-route сохраняют authoritative cell/action, но не presentation offsets;
- Play Mode подтверждает directional offsets, разрешённый horizontal overlap fallback и vertical overlap.

## Supported stationary actions

После traversal resident может начать work/eat только в клетке с полной ровной actor support surface. `SupportedWalk` и поддерживаемый `DepthTraverse` допустимы для подхода; `VerticalClimb`, `ShaftGapTraverse`, воздух и partial support не являются action position. Target-adjacent selectors рассматривают same-height `X/Z` neighbours, поэтому depth-позиция за объектом имеет такой же статус, как позиция слева/справа.
