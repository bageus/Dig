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

Система обеспечивает непрерывное выполнение тоннеля, глубины или комнаты после первой клетки и объединяет прямые приказы с общим Jobs lifecycle.

## 2. Владение состоянием

- World владеет designations и фактом excavation commit клетки.
- Jobs владеет job identity, stage, assignment, progress и terminal state.
- Reservations владеет worker/tool/position claims.
- Agents владеет active action/player override.
- Presentation показывает designation, cursor и typed status, но не хранит progress.

Отдельный presentation/manual timer не может быть вторым источником work progress.

## 3. Подтверждённый workflow designation

1. Player рисует tunnel/depth/room cells.
2. Application валидирует target mask и создаёт/reconciles ordinary excavation jobs.
3. Job matching назначает подходящих residents.
4. Resident идёт к рабочей позиции, выполняет work и commit клетки.
5. Remaining designated cells сохраняются и получают/сохраняют jobs.
6. Resident или planner продолжает следующую доступную клетку.
7. Status для tunnel/depth/room action — «Копает».

## 4. Подтверждённый direct order contract

- прямой приказ выбранному resident использует те же jobs, reservations и commit;
- direct order не создаёт параллельный progress timer;
- replacement/reconciliation job повторно связывается с remaining target group;
- completed job id удаляется из indexes, но target group живёт, пока есть designated cells;
- ошибка одного job не отключает общий simulation driver;
- повторные ticks переоценивают pending groups и доступные jobs.

## 5. Eraser

Eraser удаляет selected unfinished designations, active/nonterminal jobs и связанные reservations. Уже committed empty terrain не восстанавливается. Eraser не должен удалять unrelated jobs в той же клетке.

## 6. Invariants

- одна клетка имеет не более одного активного excavation commit path;
- direct и automatic workers не получают одну exclusive work position одновременно;
- completed/failed/cancelled job не остаётся в manual group indexes;
- remaining target cells не теряются при job replacement;
- work progress изменяется одним authoritative cadence;
- next-cell continuation не зависит от Unity frame rate.

## 7. Открытые вопросы

- **Q-DIG-001:** direct order закрепляет resident за всей group или только повышает приоритет следующей клетки?
- **Q-DIG-002:** могут ли другие residents одновременно подключаться к direct-ordered group?
- **Q-DIG-003:** порядок клеток: frontier, nearest reachable, drawing order или stable CellId?
- **Q-DIG-004:** поведение при временно недостижимой следующей клетке.
- **Q-DIG-005:** новый direct order заменяет текущую group или добавляется в очередь?
- **Q-DIG-006:** отдельная отмена player override без стирания designation.

## 8. Save/Load

Сохраняются designations, jobs, assignments, stages, progress, reservations и player override/group identity, если оно влияет на выбор. Presentation cursor и hover не сохраняются. После загрузки indexes полностью rebuildable из authoritative snapshots.

## 9. Диагностика и acceptance

Диагностика показывает target group, remaining cells, active/replacement job ids, assigned resident, candidate rejection, route, tool, cadence, progress owner и last transition.

Обязательные scenarios:

- 10+ последовательных tunnel cells;
- depth excavation без circle marker dependency;
- room template до полного завершения;
- direct order после первой клетки;
- automatic workers после direct order;
- unreachable/retry;
- erase части плана;
- save/load mid-group;
- failure одного job без остановки симуляции.
