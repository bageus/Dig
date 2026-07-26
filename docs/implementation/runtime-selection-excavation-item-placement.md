# Runtime selection, excavation progress и item placement

Статус реализации: `IMPLEMENTED`, runtime Play Mode verification pending.

Authoritative design: [`../design/runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md).

Tracking: [#387](https://github.com/bageus/Dig/issues/387), [#388](https://github.com/bageus/Dig/issues/388), [#390](https://github.com/bageus/Dig/issues/390), [#398](https://github.com/bageus/Dig/issues/398).

## Исправленные первопричины

### World selection и input priority

Selected-resident movement перехватывал pointer до completed-building selection и части item interactions. Runtime priority resolver теперь обрабатывает completed buildings и world items до свободного movement target. Selection здания открывает функции, переключает Buildings roster и использует единый selected building id для world/HUD/management.

### BuildingBox unpack preview

Representative ghost resolver выбирал `BuildingVisualState.BuildingBox` для `Z0`, поэтому unpacking визуально оставался коробкой. Unpack preview теперь всегда использует completed-building visual profile, а отдельный confirmation path не зависит от front collider под pointer.

### Item pickup, placement и collision

Generic item pickup больше не зависит от `Alt`; `Alt` остаётся обязательным для BuildingBox. World item collider переведён в trigger: он участвует в raycast, но не является физическим препятствием. Inventory single-click создаёт локальный transparent ghost, confirmation вызывает существующий authoritative drop handler, double-click сохраняет immediate drop в клетке resident. После drop используется существующий Inventory/world gravity resolver.

### Excavation quarter ownership

`ExcavationWorkCoordinator` существовал, но runtime jobs обходили его и переводили работу в `Finalize` напрямую. Теперь tunnel и spatial excavation выполняют один authoritative quarter swing на work cadence, остаются в `PerformWork` до 4/4, сохраняют completed mask при reassignment/retry и удаляют состояние только после terrain commit.

### Continuation и cave rooms

Manual connected-zone planning был ограничен radius 4 и XY adjacency. Frontier/cluster resolution теперь учитывает соседние Z layers и весь connected target set. Room preview всегда рисует front silhouette; depth designations и quarter masks синхронизируются из authoritative world/job state.

## Изменённые owners

- `InventoryState` остаётся единственным владельцем item location/quantity/reservations.
- `BuildingsState` и building commands остаются владельцами placement/assembly/packing commits.
- `JobSystem` остаётся владельцем excavation lifecycle/stage/worker.
- `ExcavationWorkCoordinator` владеет per-target completed-quarter mask и active quarter assignments.
- Unity Presentation владеет только selected ids, transparent ghosts, hover/cursor и partial-progress rendering.

## Regression coverage

- input router: BuildingBox selection versus pickup and generic item pickup;
- completed-building selection before movement;
- final-building unpack ghost on Z0;
- inventory item placement and trigger-collider source contracts;
- low-skill quarter assignment stability and 4/4 finalization gate;
- 12-cell horizontal/depth connected cluster;
- room outline and quarter marker synchronization.

## Проверка

Выполнены repository quality, C# compatibility, module-boundary и Unity source-contract checks. `.NET` build/tests выполняются в GitHub Actions. Полный Unity Play Mode workflow остаётся обязательным для перевода связанных runtime systems в `VERIFIED`.
