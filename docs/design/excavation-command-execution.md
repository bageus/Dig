# Выполнение прямой и автоматической многоклеточной копки

Статус: `QUESTIONNAIRE`.

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
3. Job matching назначает подходящих residents.
4. Resident идёт к рабочей позиции, выполняет work и commit клетки.
5. Remaining cells сохраняются и продолжают иметь jobs.
6. Свободные residents берут следующие доступные jobs.
7. Status tunnel/depth/room action — «Копает».

## 4. Прямой приказ выбранному resident

Прямой приказ:

- использует те же ordinary excavation jobs, reservations и commit;
- инициирует немедленную попытку назначить выбранному resident доступную работу в указанной связанной зоне;
- не закрепляет resident за зоной до полного завершения;
- не удаляет jobs этой зоны из общего списка;
- не запрещает другим свободным residents подключаться к той же зоне;
- не создаёт эксклюзивный player override на все remaining cells;
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

Zone membership пересчитывается из authoritative designations, а не хранится как список job ids.

## 6. Reconciliation и ошибки

- completed job удаляется из indexes;
- remaining cell получает или сохраняет ordinary job;
- failed/cancelled job не останавливает simulation driver;
- повторные ticks переоценивают pending cells и candidates;
- ошибка одного resident/job не блокирует других workers зоны;
- временно освобождённый job возвращается в общий matching pool.

## 7. Eraser

Eraser удаляет выбранные unfinished designations, active/nonterminal jobs и связанные reservations.

Уже committed empty terrain не восстанавливается. Eraser не удаляет unrelated jobs в той же клетке.

При split/merge зоны jobs пересобираются из оставшихся designations. Точная adjacency и priority policy остаются открытыми.

## 8. Инварианты

- одна клетка имеет не более одного active excavation commit path;
- один resident выполняет не более одного active excavation job;
- одна exclusive work position не принадлежит двум workers;
- direct order не удаляет zone jobs из общего списка;
- другие свободные residents могут подключаться к direct-started zone;
- remaining cells не теряются при job replacement;
- динамически добавленная связанная клетка не требует повторного direct click;
- work progress изменяется одним authoritative cadence;
- continuation не зависит от Unity frame rate;
- Presentation не владеет zone membership или progress.

## 9. Решённые вопросы

- **Q-DIG-001:** direct order относится к связанной excavation zone, но не закрепляет выбранного resident за ней до завершения.
- **Q-DIG-002:** другие automatic residents могут одновременно подключаться к той же zone через обычный job matching.
- **Q-DIG-008:** связанные tunnel/depth/room cells, добавленные во время работы, автоматически входят в active zone.

## 10. Открытые вопросы

- **Q-DIG-003:** порядок клеток: frontier, nearest reachable, drawing order или stable CellId?
- **Q-DIG-004:** что происходит при временно недостижимой следующей клетке?
- **Q-DIG-005:** новый direct order заменяет текущий active action выбранного resident, ставится в очередь или отклоняется?
- **Q-DIG-006:** требуется ли отдельная отмена direct priority без стирания designation?
- **Q-DIG-007:** как split/merge влияет на zone priority и порядок jobs?
- **Q-DIG-009:** какая adjacency определяет связанность в 3D?
- **Q-DIG-010:** существует ли числовой priority boost для direct-started zone и когда он заканчивается?

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
- route, tool, cadence и progress owner;
- last transition и failure reason.

## 13. Acceptance

Обязательные scenarios:

- 10+ последовательных tunnel cells;
- несколько residents одновременно копают одну zone;
- direct order выбранному resident не блокирует подключение других;
- добавление новых связанных tunnel cells во время копки;
- присоединение depth и room cells;
- depth excavation без circle marker dependency;
- room template до полного завершения;
- selected resident освобождает/завершает job, а zone продолжает выполняться;
- unreachable/retry;
- erase части плана и split zone;
- save/load mid-zone;
- failure одного job без остановки симуляции;
- Unity Play Mode проверяет cursor, status, job list и продолжение после первой клетки.