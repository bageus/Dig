# Runtime selection, excavation progress и item placement

Статус реализации: `IMPLEMENTED`, runtime Play Mode verification pending.

Authoritative design: [`../design/runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md).

Tracking: [#387](https://github.com/bageus/Dig/issues/387), [#388](https://github.com/bageus/Dig/issues/388), [#390](https://github.com/bageus/Dig/issues/390), [#398](https://github.com/bageus/Dig/issues/398).

## Исправленные первопричины

### World selection и input priority

Selected-resident movement перехватывал pointer до completed-building selection и части item interactions. Runtime priority resolver теперь обрабатывает completed buildings и world items до свободного movement target. Selection здания открывает функции, переключает Buildings roster и использует единый selected building id для world/HUD/management.

### BuildingBox selection и world/HUD sync

`ContextInputRouter` возвращал `SelectBuildingBox`, но Unity `ApplyEffects` не обрабатывал этот effect. Поэтому world click потреблялся без фактического runtime selection. Effect подключён; world click и Buildings roster используют один `StackId` и очищают несовместимые selections.

Первый physical highlight создавал отдельный полупрозрачный cube по размеру interaction collider. Он визуально захватывал пространство вокруг коробки и мог выглядеть как подсветка клетки/соседних объектов. Отдельная surface удалена. Selection теперь меняет tint только существующих renderer-ов visual instances выбранной коробки; pool reset снимает tint, а HUD row остаётся независимой проекцией того же `StackId`.

### BuildingBox unpack preview

Representative ghost resolver выбирал `BuildingVisualState.BuildingBox` для `Z0`, поэтому unpacking визуально оставался коробкой. Unpack preview всегда использует completed-building visual profile, а отдельный confirmation path не зависит от front collider под pointer. После успешного plan source box остаётся selected, если она ещё существует в authoritative world location.

### Item pickup, placement и collision

Generic item pickup больше не зависит от `Alt`; `Alt` остаётся обязательным для BuildingBox. World item collider переведён в trigger: он участвует в raycast, но не является физическим препятствием. Inventory single-click создаёт локальный transparent ghost, confirmation вызывает существующий authoritative drop handler, double-click сохраняет immediate drop в клетке resident. После drop используется существующий Inventory/world gravity resolver.

### Excavation quarter ownership и geometry

`ExcavationWorkCoordinator` существовал, но runtime jobs обходили его и переводили работу в `Finalize` напрямую. Теперь tunnel и spatial excavation выполняют один authoritative quarter swing на work cadence, остаются в `PerformWork` до 4/4, сохраняют completed mask при reassignment/retry и удаляют состояние только после terrain commit.

Старый quarter marker закрашивал completed part почти чёрным кубом. Solid cell переключается на четыре части породы при первом completed quarter; завершённая часть геометрии отключается, а remaining parts сохраняют material/tint породы. Designation overlay отдельно скрывает completed quarter и больше не создаёт чёрную пластину.

### Nearest automatic excavation и drag-stroke batching

PR #414 сравнивал Navigation route до work position, но ordinary tunnel tool по-прежнему вызывал `SynchronizeDesignations` после каждой нарисованной клетки. Первый painted/created job мог быть claimed до появления остальных клеток stroke, поэтому порядок рисования скрыто переопределял nearest rule. Особенно заметно это было на вертикальном front-slice tunnel: нижняя правая клетка могла назначаться раньше ближайшей верхней.

Tunnel drag теперь сначала stage-ит все World designations, а на LMB release один раз выполняет job reconciliation и assignment полного stroke batch. Planner среди reachable jobs сначала сравнивает 3D Manhattan distance от текущей клетки resident до самой target cell, затем Navigation route cost до work position, `CellId` и `JobId`. Одинаковая/shared work position больше не делает нижний target равным верхнему. Единый automatic pool применяет тот же порядок к ordinary и spatial excavation.

### Unity compiler guards

Quarter work явно переводит signed generation seed в его 32-bit unsigned representation перед расширением до `ulong`. Skill resolution использует единственный актуальный source, привязанный через `BindExcavationSkillSource`; ссылка на удалённый legacy `_manualExcavationMiningSkill` устранена. Inventory routing проверяет nullable selected resident id до `EntityId.Parse` и после guard явно передаёт non-null значение для Unity Roslyn. Это устраняет diagnostics `CS1503`, `CS0103` и `CS8604` без изменения authoritative gameplay state.

### Continuation и cave rooms

Manual connected-zone planning был ограничен radius 4 и XY adjacency. Frontier/cluster resolution учитывает соседние Z layers и весь connected target set. Если назначенный ordinary Dig job после refresh не имеет успешного route/work cell, assignment снимается, quarter state сохраняется, а job возвращается в общий pool вместо вечного `Claimed/InProgress` без движения.

Room commit раньше передавал `VolumeCells` в atomic `SetDigDesignations`, хотя volume включал уже открытую entrance cell; Domain корректно отклонял весь batch. План разделяет полный `VolumeCells`, открытый `BaseTunnelCells` и фактический `ExcavationCells`. Commit назначает только rock mask. Planner требует полный сквозной base tunnel, проверяет mineability каждой остальной 3D-клетки и возвращает per-cell diagnostics. Runtime разрешает pointer на породе над тоннелем и preview красит конкретные missing/unmineable/protected cells.

## Изменённые owners

- `InventoryState` остаётся единственным владельцем item location/quantity/reservations.
- `BuildingsState` и building commands остаются владельцами placement/assembly/packing commits.
- `JobSystem` остаётся владельцем excavation lifecycle/stage/worker.
- `ExcavationWorkCoordinator` владеет per-target completed-quarter mask и active quarter assignments.
- Unity Presentation владеет только selected ids, renderer tint, transparent ghosts, hover/cursor и partial-progress geometry.

## Regression coverage

- input router: BuildingBox selection versus pickup and generic item pickup;
- world/HUD BuildingBox selection effect и shared StackId;
- Play Mode: selection не создаёт дополнительную geometry и меняет tint только физической коробки;
- completed-building selection before movement;
- final-building unpack ghost on Z0;
- inventory item placement and trigger-collider source contracts;
- low-skill quarter assignment stability and 4/4 finalization gate;
- completed quarter removes rock geometry and does not use black fill;
- shared-work-cell spatial assignment выбирает ближайшую target cell, даже если дальний job имеет меньший id;
- source contract: tunnel drag stage-ит designations и reconciles jobs только после release;
- automatic ordinary/spatial selection использует target-distance → route-cost → CellId → JobId;
- unroutable assigned Dig job releases its worker reservation without losing quarter progress;
- room commit uses `ExcavationCells`, full base-tunnel validation and per-cell invalid preview diagnostics;
- Unity quarter seed type normalization, current excavation skill source и guarded non-null resident IDs;
- 12-cell horizontal/depth connected cluster;
- room outline and quarter marker synchronization.

## Проверка

Repository quality, C# compatibility, module-boundary, Unity source-contract и `.NET` build/tests выполняются в GitHub Actions. Добавлен Play Mode regression для box-only renderer tint. Полный интерактивный Unity Play Mode workflow вертикального tunnel stroke остаётся обязательным для перевода excavation runtime в `VERIFIED`.
