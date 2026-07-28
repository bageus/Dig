# Excavation quarter, climbing stance and cave-room regressions — 2026-07-28

Статус: `IMPLEMENTED`; фактический Unity Play Mode запуск обязателен для `VERIFIED`.

Authoritative specifications and tracking:

- [`../design/excavation-command-execution.md`](../design/excavation-command-execution.md), issue [#388](https://github.com/bageus/Dig/issues/388);
- [`../design/resident-movement-occupancy-and-vertical-traversal.md`](../design/resident-movement-occupancy-and-vertical-traversal.md), issue [#386](https://github.com/bageus/Dig/issues/386);
- [`../design/excavation-room-templates-and-deposits.md`](../design/excavation-room-templates-and-deposits.md), issue [#87](https://github.com/bageus/Dig/issues/87).

## Повторно подтверждённые runtime symptoms

После первого исправления #481 оставались observable regressions:

- work-facing вычислял unsupported pose только из active `PerformWork`; после terminal completion target очищался, и resident снова отображался стоящим на частично/полностью удалённой опоре;
- integer-only `ResolveRowMinX` мог только сдвинуть чётную строку Small целой клеткой, поэтому геометрически центрированный профиль `5 -> 4 -> 3` был невозможен;
- каталог Small всё ещё содержал depth `2`;
- completed trim создавал mesh в world coordinates, но parenting под уже повёрнутый bootstrap root применял side-view rotation второй раз и давал отдельную проекцию выше основной сцены;
- после последнего room job не существовало обычного движения к опоре, поэтому unsupported resident мог остаться логически в shaft cell без нового action;
- Medium имел только preview fixture, но не regression на фактический completed-room renderer.

## Исправление ownership и lifecycle

- `CaveRoomRowProfile` хранит exact doubled boundaries, physical cell range и required `ExcavationQuarter` mask для каждой boundary cell;
- Small имеет depth `3`; его middle row использует две half-cell границы: справа от левой клетки и слева от правой;
- `CaveRoomPlan.ExcavationTargets` является единственным источником required mask для room child jobs;
- coordinator помечает quarters вне required mask недоступными, поэтому half-cell child job заканчивается на `2/4`;
- `CompletePartialTerrainWorkCommandHandler` снимает designation, завершает job/reservations и skill grant, но не вызывает `World.Excavate`, не создаёт output и не открывает Navigation;
- room completion проверяет full targets как air, а partial targets как solid shell с выполненным required mask и снятой designation;
- shell protection использует тот же row profile и защищает обе оставшиеся внешние половины;
- preview, trim, floor cells и runtime activation используют общий профиль;
- completed trim root сохраняет world-space identity под rotated bootstrap;
- work-facing использует World support и open tunnel cell независимо от наличия active mining target;
- idle/terminal unsupported resident сохраняет climbing stance;
- `UnsupportedResidentRecoveryPlanner` выбирает reachable full-support destination с минимальным shaft-gap count, route cost и deterministic `CellId`; active assigned job имеет приоритет.

## Regression coverage

.NET tests проверяют:

- Small depth `3`, exact volume/target counts и centered half-cell masks;
- Medium `8 -> 7 -> 6` profile и completed trim provenance;
- partial `2/4` completion без air/output/navigation mutation и rejection незавершённого mask;
- обе защищённые half-cell shell boundaries;
- nearest supported recovery, отсутствие recovery на full support и job-before-recovery routing contract;
- world-space trim root source contract.

Unity Play Mode fixtures проверяют:

- idle resident остаётся в climbing pose после partial support loss без active Job;
- Small boundary visuals удаляют внутренние половины с обеих сторон;
- Medium completed trim создаёт mesh и остаётся на высоте комнаты под реальным `90°` rotated root;
- существующие quarter-axis и Medium preview scenarios.

Save/load существующего World quarter mask остаётся unchanged: partial boundary progress хранится в тех же `CompletedExcavationQuarters`. Полный runtime save of session-local cave plans остаётся частью открытого parent feature #87 и не объявляется `VERIFIED` этим bugfix.
