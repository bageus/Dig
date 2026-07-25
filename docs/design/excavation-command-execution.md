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

- World владеет designations, их connectivity и фактом excavation commit клетки.
- Jobs владеет job identity, stage, assignment, progress и terminal state.
- Reservations владеет worker/tool/position claims.
- Agents владеет active action и player override, связывающий resident с excavation zone.
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
- direct order закрепляет resident за **всей связанной выделенной excavation zone**, а не только за следующей клеткой;
- resident продолжает работу, пока в этой связанной зоне остаются доступные незавершённые клетки;
- если во время выполнения к зоне добавляются связанные клетки тоннеля, глубины или комнаты, они входят в тот же direct order и resident продолжает копать их;
- расширение зоны не создаёт второй progress timer и не сбрасывает уже выполненную работу;
- replacement/reconciliation job повторно связывается с remaining target zone;
- completed job id удаляется из indexes, но direct group живёт, пока есть связанные designated cells;
- ошибка одного job не отключает общий simulation driver;
- повторные ticks переоценивают pending zone и доступные jobs.

Direct order заканчивается, когда связанная зона полностью завершена, явно отменена или перестала существовать. Точные правила split/merge и отмены остаются открытыми.

## 5. Dynamic zone membership

Zone membership должна пересчитываться из authoritative designations, а не храниться только как список job ids.

Подтверждено:

- новая связанная клетка присоединяется к выполняемой зоне;
- тип designation может быть tunnel, depth или room;
- завершённая клетка удаляется из remaining set, но не разрывает player override автоматически;
- повторная отрисовка поверх уже входящей клетки не дублирует job;
- связь resident с зоной переживает job reconciliation.

Не утверждены adjacency rule, поведение при слиянии двух direct zones и выбор компоненты после разделения eraser-ом.

## 6. Eraser

Eraser удаляет selected unfinished designations, active/nonterminal jobs и связанные reservations. Уже committed empty terrain не восстанавливается. Eraser не должен удалять unrelated jobs в той же клетке.

Если eraser разделяет direct zone на несколько компонент, дальнейшая привязка resident определяется после ответа на Q-DIG-007.

## 7. Invariants

- одна клетка имеет не более одного активного excavation commit path;
- direct и automatic workers не получают одну exclusive work position одновременно;
- completed/failed/cancelled job не остаётся в direct group indexes;
- remaining target cells не теряются при job replacement;
- динамически добавленная связанная клетка не требует повторного direct click;
- work progress изменяется одним authoritative cadence;
- next-cell continuation не зависит от Unity frame rate;
- direct override ссылается на authoritative zone identity/connectivity, а не на presentation stroke.

## 8. Решённые вопросы

- **Q-DIG-001:** direct order закрепляет выбранного resident за всей связанной excavation zone до её завершения или явной отмены.
- **Q-DIG-008:** связанные клетки, добавленные во время работы, автоматически входят в active direct zone, включая tunnel, depth и room designations.

## 9. Открытые вопросы

- **Q-DIG-002:** могут ли другие automatic residents одновременно подключаться к зоне, за которой закреплён direct resident?
- **Q-DIG-003:** порядок клеток: frontier, nearest reachable, drawing order или stable CellId?
- **Q-DIG-004:** поведение при временно недостижимой следующей клетке.
- **Q-DIG-005:** новый direct order заменяет текущую zone, ставится в очередь или разрешён только после отмены старой?
- **Q-DIG-006:** отдельная отмена player override без стирания designation.
- **Q-DIG-007:** что происходит при split/merge: resident выбирает компоненту с текущей рабочей клеткой, всю объединённую зону или получает новый выбор?
- **Q-DIG-009:** какая adjacency определяет «связанную» зону в 3D и могут ли разные Z-слои соединяться только через designated depth cells?

## 10. Save/Load

Сохраняются designations, jobs, assignments, stages, progress, reservations и player override/zone identity, если оно влияет на выбор. После загрузки connectivity и indexes полностью rebuildable из authoritative snapshots. Presentation cursor и hover не сохраняются.

## 11. Диагностика и acceptance

Диагностика показывает zone id/connectivity version, remaining cells, dynamically joined cells, active/replacement job ids, assigned resident, candidate rejection, route, tool, cadence, progress owner и last transition.

Обязательные scenarios:

- 10+ последовательных tunnel cells;
- добавление новых связанных tunnel cells во время копки;
- присоединение depth и room cells к active zone;
- depth excavation без circle marker dependency;
- room template до полного завершения;
- direct order после первой клетки;
- automatic workers после direct order согласно будущей Q-DIG-002 policy;
- unreachable/retry;
- erase части плана и split zone;
- save/load mid-zone;
- failure одного job без остановки симуляции.
