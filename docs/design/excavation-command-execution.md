# Выполнение прямой и автоматической многоклеточной копки

Статус: `APPROVED`.

Tracking issue: [#388](https://github.com/bageus/Dig/issues/388).

Parent feature: [#87](https://github.com/bageus/Dig/issues/87).

Связанные документы:

- [`excavation-room-templates-and-deposits.md`](excavation-room-templates-and-deposits.md);
- [`../implementation/z0-excavation-planning.md`](../implementation/z0-excavation-planning.md);
- [`../implementation/unity-terrain-work-vertical-slice.md`](../implementation/unity-terrain-work-vertical-slice.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md).

## 1. Назначение

Система обеспечивает непрерывное выполнение тоннеля, глубины или комнаты после первой клетки и объединяет прямые приказы с обычным Jobs lifecycle.

## 2. Владение состоянием

- World владеет designations, connectivity и excavation commit клетки.
- Jobs владеет job identity, priority, assignment, progress и terminal state.
- Reservations владеет worker/tool/position claims.
- Agents владеет только текущим active action конкретного resident.
- Presentation показывает designation, cursor и typed status, но не хранит progress.

Прямой приказ не создаёт второго timer, отдельного manual job owner или постоянного эксклюзивного закрепления resident за зоной.

## 3. Обычный workflow designation

1. Player рисует tunnel/depth/room cells.
2. Все клетки одного завершённого drag stroke или template command сначала вносятся в World как единый logical designation batch; job reconciliation и automatic assignment запускаются только после завершения batch, а не после первой нарисованной клетки.
3. Application валидирует target mask и создаёт/reconciles ordinary excavation jobs для полного доступного batch.
4. Для каждого свободного подходящего resident job matching рассматривает все доступные excavation jobs и отбрасывает targets без достижимой work position.
5. Среди достижимых targets выбирается клетка с минимальной 3D grid-distance от текущей клетки resident до самой excavation target cell: `|dx| + |dy| + |dz|`.
6. Navigation route cost до work position используется после target distance: сначала как обязательная проверка reachability, затем как tie-break между одинаково близкими target cells.
7. Если target distance и route cost равны, применяется deterministic tie-break по `CellId`, затем по `JobId`.
8. Более дальняя excavation target cell не назначается resident, пока существует более близкая достижимая target cell того же доступного excavation pool.
9. Resident идёт к рабочей позиции, выполняет work и commit клетки.
10. После полного `4/4` commit authoritative World, Navigation map, resident tunnel topology и movement surfaces обязаны увидеть открытую клетку в том же simulation tick до следующей попытки маршрута, назначения или движения. Ошибка derived refresh не может оставить визуально открытую клетку логически/физически закрытой.
11. Любая полностью открытая cell, принадлежащая authoritative tunnel plan, немедленно остаётся допустимой work/movement cell для следующей excavation target независимо от наличия floor support. `plannedVertical` является только provenance вертикального перехода и не может быть единственным способом сохранить обычную planned tunnel cell в topology. При vertical stroke переход из поддерживаемой horizontal entry cell в первую vertical cell и обратно является валидным climbing transition; соседние обычные open cells без vertical provenance не получают vertical traversal автоматически.
12. При копке сверху вниз resident полностью удаляет горизонтальную верхнюю пару quarters нижней target cell до перехода к нижней паре; высокий skill не превращает partial progress в вертикальную колонку.
13. Remaining cells сохраняются и продолжают иметь jobs.
14. Свободные residents независимо выбирают свои ближайшие доступные jobs; один job/work position не назначается двум residents.
15. Если work cell находится в vertical tunnel без floor support, resident выполняет mining в stationary climbing stance спиной к камере, а не стоит в воздухе.
16. Для depth target из vertical tunnel work position выбирается в таком порядке: достижимая открытая horizontal cell слева/справа на source depth; затем достижимая открытая depth cell слева/справа от target; затем сама vertical source cell с climbing-work stance. Один выбранный work cell остаётся authoritative position reservation job.
17. Status tunnel/depth/room action — «Копает».

Правило nearest-target применяется одинаково к horizontal tunnel, вертикальному тоннелю на фронтальном срезе, vertical/depth excavation и child cells комнаты. Оно относится и к automatic planner, и к direct command; direct command отличается приоритетным запуском выбранного resident, а не способом выбора target.

Несколько spatial jobs могут иметь одну и ту же или одинаково удалённую work position. Это не делает более глубокую/нижнюю target cell равной верхней: первичным остаётся расстояние от resident до самой target cell. Поэтому при копке сверху вниз верхняя правая/левая клетка выбирается раньше нижней клетки, если она ближе к resident и достижима.

## 4. Прямой приказ выбранному resident

Прямой приказ:

- использует те же ordinary excavation jobs, reservations и commit;
- инициирует немедленную попытку назначить выбранному resident доступную работу в указанной связанной зоне;
- не закрепляет resident за зоной до полного завершения;
- не удаляет jobs этой зоны из общего списка;
- не запрещает другим свободным residents подключаться к той же зоне;
- не создаёт эксклюзивный player override на все remaining cells;
- новый direct order заменяет текущее небоевое action resident и немедленно пытается назначить ближайшую достижимую excavation target cell по тому же target-distance → route-cost → CellId → JobId правилу;
- combat/self-defense interruption имеет приоритет выше direct order и любых jobs;
- после completion/release/blocked выбранный resident подчиняется обычному planner/job matching.

Таким образом, прямой приказ является приоритетным пользовательским запуском работы, а не персональной долгосрочной собственностью зоны.

## 5. Связанная excavation zone

Direct target определяется связанной группой незавершённых tunnel/depth/room designations.

Подтверждено:

- новые связанные клетки, добавленные во время копки, входят в ту же zone;
- jobs для новых клеток появляются через обычный reconciliation;
- любой свободный подходящий resident может взять новый job;
- несколько residents могут одновременно работать в разных допустимых позициях одной zone;
- одна exclusive work position не может быть занята двумя residents;
- повторная отрисовка существующей клетки не дублирует job;
- completion одной клетки не уничтожает remaining zone.

Zone membership пересчитывается из authoritative designations, а не хранится как список job ids. Для любой попытки назначения конкретному resident берётся ближайшая достижимая target cell по 3D grid-distance; фактический Navigation route до work position проверяет reachability и разрешает равенство. Это правило не ограничено direct order.

## 6. Reconciliation и ошибки

- completed job удаляется из indexes;
- remaining cell получает или сохраняет ordinary job;
- failed/cancelled job не останавливает simulation driver;
- повторные ticks переоценивают pending cells и candidates;
- ошибка одного resident/job не блокирует других workers зоны;
- временно освобождённый job возвращается в общий matching pool;
- если уже назначенный excavation job после navigation refresh больше не имеет достижимой work position/path, runtime снимает assignment, отменяет только worker reservation, сохраняет authoritative quarter progress и возвращает job в `Available` для повторного matching;
- если у designated cell пока нет достижимой work position, она остаётся в job list и периодически переоценивается обычным planner без forced movement в тупик;
- на текущем тестовом этапе все ordinary jobs имеют одинаковый числовой priority; direct order является наивысшим player-command override, но не создаёт сохраняемый numeric zone boost.

## 7. Eraser

Eraser удаляет выбранные unfinished designations, active/nonterminal jobs и связанные reservations.

Уже committed empty terrain и завершённые quarters не восстанавливаются. Eraser снимает designation/job только с оставшейся части клетки; повторное назначение продолжает работу с сохранённого quarter mask. Eraser не удаляет unrelated jobs в той же клетке.

При split/merge зоны jobs пересобираются из оставшихся designations. Persistent zone priority отсутствует.

Связанность определяется так:

- обычные tunnel/depth designations объединяются только через face-neighbor X/Y в одном Z-слое;
- room instance является единой зоной по своему authoritative template/instance id и включает связанные child cells следующих Z-слоёв;
- произвольные Z-соседи разных room/tunnel plans не объединяются только из-за совпадения X/Y.

## 8. Инварианты

- одна клетка имеет не более одного active excavation commit path;
- один resident выполняет не более одного active excavation job;
- одна exclusive work position не принадлежит двум workers;
- direct order не удаляет zone jobs из общего списка;
- другие свободные residents могут подключаться к direct-started zone;
- remaining cells не теряются при job replacement;
- динамически добавленная связанная клетка не требует повторного direct click;
- work progress изменяется одним authoritative cadence;
- каждое завершение quarter немедленно удаляет соответствующую геометрию породы; отмена/release/reassignment не возвращает её;
- полный `4/4` commit атомарно делает клетку открытой для World, route planning, authoritative resident movement и tunnel interaction surfaces;
- любая открытая authoritative planned tunnel cell доступна как следующий work/movement step без повторной разметки, даже если floor support отсутствует;
- первый/последний переход между horizontal entry cell и vertical shaft разрешён, если vertical provenance имеет хотя бы shaft endpoint; произвольные stacked open cells без vertical provenance не становятся climbing route;
- при target ниже resident сначала полностью завершается верхняя горизонтальная пара quarters target; дальняя пара не выбирается тем же swing, пока near pair unfinished;
- визуально открытая клетка не может оставаться закрытой в Navigation, resident tunnel topology или collider/movement projection;
- support-loss предметов, вызванный excavation commit, проверяется до новых pickup/hauling reservations;
- continuation не зависит от Unity frame rate;
- drag-stroke creation order не может определить первый assigned job: assignment начинается после reconciliation полного stroke batch;
- при наличии нескольких допустимых excavation jobs resident получает ближайшую reachable target cell независимо от horizontal/vertical/room plan kind;
- vertical tunnel work без пола использует climbing presentation, но authoritative work cell/job остаются в Jobs/Navigation;
- depth excavation допускает side-horizontal и adjacent-depth work positions с одной authoritative reservation;
- Presentation не владеет zone membership или progress.

## 9. Решённые вопросы

- **Q-DIG-001:** direct order относится к связанной excavation zone, но не закрепляет выбранного resident за ней до завершения.
- **Q-DIG-002:** другие automatic residents могут одновременно подключаться к той же zone через обычный job matching.
- **Q-DIG-003:** выбранному resident назначается ближайшая достижимая excavation target cell.
- **Q-DIG-004:** временно недостижимая cell остаётся обычным job; planner периодически повторяет попытку без forced movement в тупик.
- **Q-DIG-005:** direct order заменяет текущее небоевое action resident; self-defense/combat выше по приоритету.
- **Q-DIG-006:** отдельной отмены direct priority нет.
- **Q-DIG-007:** persistent zone priority отсутствует, поэтому split/merge только пересобирает ordinary jobs из оставшихся designations.
- **Q-DIG-008:** связанные tunnel/depth/room cells, добавленные во время работы, автоматически входят в active zone.
- **Q-DIG-009:** обычная зона использует X/Y adjacency внутри слоя; room instance дополнительно объединяет свои child cells на следующих Z-слоях. Произвольная Z adjacency не объединяет разные plans.
- **Q-DIG-010:** числового priority boost нет; ordinary jobs равны, direct player command выше jobs, combat defense выше direct jobs.
- **Q-DIG-011:** nearest-target selection является общим правилом для automatic и direct excavation, включая horizontal, vertical/depth и room cells.
- **Q-DIG-012:** первичный proximity key — 3D Manhattan distance до самой target cell; route до work position является reachability gate и вторичным tie-break.
- **Q-DIG-013:** ordinary drag stroke reconciles/assigns jobs один раз после завершения stroke, поэтому первый painted/created job не получает скрытый приоритет.

## 10. Открытые вопросы

Открытых observable business-вопросов для текущего direct/automatic excavation workflow нет. Новые нестандартные 3D shapes требуют отдельного решения, а не расширения adjacency по предположению.

## 11. Save/Load

Сохраняются designations, jobs, assignments, stages, progress и reservations.

Если direct order создаёт сохраняемый priority marker, он должен иметь stable zone identity и rebuildable connectivity. Если direct order только немедленно назначает job, отдельное состояние direct zone не сохраняется.

Presentation cursor и hover не сохраняются.

## 12. Диагностика

Диагностика показывает:

- zone id/connectivity version;
- remaining и dynamically joined cells;
- active/replacement job ids;
- workers, work positions и reservations;
- direct command source;
- priority reason;
- candidate rejection;
- target distance, route cost и nearest-target tie-break reason;
- tool, cadence и progress owner;
- last transition и failure reason.

## 13. Acceptance

Обязательные scenarios:

- 10+ последовательных tunnel cells;
- несколько residents одновременно копают одну zone;
- direct order заменяет текущее небоевое action выбранного resident, выбирает ближайшую reachable target cell и не блокирует подключение других;
- automatic resident при нескольких available jobs выбирает ближайшую reachable horizontal cell;
- vertical front-slice tunnel, нарисованный сверху вниз или снизу вверх, после release назначает resident ближайшую верхнюю правую/левую клетку, а не первую созданную нижнюю клетку;
- automatic resident при vertical/depth plan сначала выбирает ближайнюю target cell, даже если несколько jobs имеют одинаковую work position/route;
- room child jobs также выбираются по расстоянию до target cell;
- равные target distances используют route cost, затем deterministic CellId/JobId tie-break;
- добавление новых связанных tunnel cells во время копки;
- X/Y continuation для tunnel/depth без случайного объединения по Z;
- room instance продолжает child cells на следующих Z-слоях;
- depth excavation без circle marker dependency;
- room template до полного завершения;
- selected resident освобождает/завершает job, а zone продолжает выполняться;
- unreachable cell остаётся в job list и переоценивается без принудительного движения resident в тупик;
- interruption или erase после 1/4, 2/4 и 3/4 оставляет реально удалённые quarters; повторное designation продолжает с сохранённого mask;
- после первой и каждой последующей полностью выкопанной planned tunnel cell resident может войти в неё и продолжить следующую клетку без erase/redraw;
- после полного vertical/depth commit новая клетка входит в climbing/topology projection, а unsupported world item запускает существующий gravity workflow до новых reservations;
- downward partial excavation при 2/4 удаляет верхнюю горизонтальную половину клетки;
- mining из unsupported shaft cell показывает stationary climbing stance спиной к камере;
- depth target выбирает side horizontal work cell, затем adjacent open depth work cell, затем shaft fallback;
- recoverable failure после World mutation повторно синхронизирует derived topology и не пытается повторно выкопать уже открытую клетку;
- erase части плана и split zone;
- save/load mid-zone;
- failure одного job без остановки симуляции;
- Unity Play Mode проверяет cursor, status, полный stroke batch, выбранный nearest target и продолжение после первой клетки.
