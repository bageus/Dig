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
2. Application валидирует target mask и создаёт/reconciles ordinary excavation jobs.
3. Для каждого свободного подходящего resident job matching рассматривает все доступные excavation jobs и выбирает ближайшую достижимую target cell по фактической длине Navigation route.
4. При одинаковой длине маршрута применяется deterministic tie-break по `CellId`, затем по `JobId`.
5. Более дальняя достижимая excavation cell не назначается resident, пока существует более близкая допустимая cell того же доступного excavation pool.
6. Resident идёт к рабочей позиции, выполняет work и commit клетки.
7. Remaining cells сохраняются и продолжают иметь jobs.
8. Свободные residents независимо выбирают свои ближайшие доступные jobs; один job/work position не назначается двум residents.
9. Status tunnel/depth/room action — «Копает».

Правило nearest-reachable применяется одинаково к horizontal tunnel, vertical/depth excavation и child cells комнаты. Оно относится и к automatic planner, и к direct command; direct command отличается приоритетным запуском выбранного resident, а не способом выбора target.

## 4. Прямой приказ выбранному resident

Прямой приказ:

- использует те же ordinary excavation jobs, reservations и commit;
- инициирует немедленную попытку назначить выбранному resident доступную работу в указанной связанной зоне;
- не закрепляет resident за зоной до полного завершения;
- не удаляет jobs этой зоны из общего списка;
- не запрещает другим свободным residents подключаться к той же зоне;
- не создаёт эксклюзивный player override на все remaining cells;
- новый direct order заменяет текущее небоевое action выбранного resident и немедленно пытается назначить ближайшую доступную excavation cell;
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

Zone membership пересчитывается из authoritative designations, а не хранится как список job ids. Для любой попытки назначения конкретному resident берётся ближайшая достижимая cell по фактической длине маршрута; равные варианты используют deterministic tie-break. Это правило не ограничено direct order.

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
- continuation не зависит от Unity frame rate;
- при наличии нескольких допустимых excavation jobs resident получает ближайшую reachable cell независимо от horizontal/vertical/room plan kind;
- Presentation не владеет zone membership или progress.

## 9. Решённые вопросы

- **Q-DIG-001:** direct order относится к связанной excavation zone, но не закрепляет выбранного resident за ней до завершения.
- **Q-DIG-002:** другие automatic residents могут одновременно подключаться к той же zone через обычный job matching.
- **Q-DIG-003:** выбранному resident назначается ближайшая достижимая excavation cell.
- **Q-DIG-004:** временно недостижимая cell остаётся обычным job; planner периодически повторяет попытку без forced movement в тупик.
- **Q-DIG-005:** direct order заменяет текущее небоевое action resident; self-defense/combat выше по приоритету.
- **Q-DIG-006:** отдельной отмены direct priority нет.
- **Q-DIG-007:** persistent zone priority отсутствует, поэтому split/merge только пересобирает ordinary jobs из оставшихся designations.
- **Q-DIG-008:** связанные tunnel/depth/room cells, добавленные во время работы, автоматически входят в active zone.
- **Q-DIG-009:** обычная зона использует X/Y adjacency внутри слоя; room instance дополнительно объединяет свои child cells на следующих Z-слоях. Произвольная Z adjacency не объединяет разные plans.
- **Q-DIG-010:** числового priority boost нет; ordinary jobs равны, direct player command выше jobs, combat defense выше direct jobs.
- **Q-DIG-011:** nearest-reachable selection является общим правилом для automatic и direct excavation, включая horizontal, vertical/depth и room cells.

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
- route cost и nearest-target tie-break reason;
- tool, cadence и progress owner;
- last transition и failure reason.

## 13. Acceptance

Обязательные scenarios:

- 10+ последовательных tunnel cells;
- несколько residents одновременно копают одну zone;
- direct order заменяет текущее небоевое action выбранного resident, выбирает ближайшую reachable cell и не блокирует подключение других;
- automatic resident при нескольких available jobs выбирает ближайшую reachable horizontal cell;
- automatic resident при vertical/depth plan сначала выбирает ближайшую по route cell, а не дальний child job по порядку id/creation;
- room child jobs также выбираются по ближайшему фактическому route;
- равные route costs дают deterministic CellId/JobId tie-break;
- добавление новых связанных tunnel cells во время копки;
- X/Y continuation для tunnel/depth без случайного объединения по Z;
- room instance продолжает child cells на следующих Z-слоях;
- depth excavation без circle marker dependency;
- room template до полного завершения;
- selected resident освобождает/завершает job, а zone продолжает выполняться;
- unreachable cell остаётся в job list и переоценивается без принудительного движения resident в тупик;
- interruption или erase после 1/4, 2/4 и 3/4 оставляет реально удалённые quarters; повторное designation продолжает с сохранённого mask;
- erase части плана и split zone;
- save/load mid-zone;
- failure одного job без остановки симуляции;
- Unity Play Mode проверяет cursor, status, job list, nearest target и продолжение после первой клетки.
