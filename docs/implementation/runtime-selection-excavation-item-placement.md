# Runtime selection, excavation progress и item placement

Статус реализации: `IMPLEMENTED`, runtime Play Mode verification pending.

Authoritative design: [`../design/runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md).

Tracking: [#387](https://github.com/bageus/Dig/issues/387), [#388](https://github.com/bageus/Dig/issues/388), [#390](https://github.com/bageus/Dig/issues/390), [#398](https://github.com/bageus/Dig/issues/398).

## Исправленные первопричины

### World selection и input priority

Selected-resident movement перехватывал pointer до completed-building selection и части item interactions. Runtime priority resolver теперь обрабатывает completed buildings и world items до свободного movement target. Selection здания открывает функции, переключает Buildings roster и использует единый selected building id для world/HUD/management.

### BuildingBox selection и world/HUD sync

`ContextInputRouter` возвращал `SelectBuildingBox`, но Unity `ApplyEffects` не обрабатывал этот effect. Поэтому world click потреблялся без фактического runtime selection. Effect теперь подключён; world click и Buildings roster используют один `StackId`, очищают несовместимые selections и включают один selection highlight на физической коробке. Roster row вычисляет highlight из того же `SelectedBuildingBox`, поэтому world→HUD и HUD→world больше не расходятся.

### BuildingBox unpack preview

Representative ghost resolver выбирал `BuildingVisualState.BuildingBox` для `Z0`, поэтому unpacking визуально оставался коробкой. Unpack preview теперь всегда использует completed-building visual profile, а отдельный confirmation path не зависит от front collider под pointer. После успешного plan source box остаётся selected, если она ещё существует в authoritative world location.

### Item pickup, placement и collision

Generic item pickup больше не зависит от `Alt`; `Alt` остаётся обязательным для BuildingBox. World item collider переведён в trigger: он участвует в raycast, но не является физическим препятствием. Inventory single-click создаёт локальный transparent ghost, confirmation вызывает существующий authoritative drop handler, double-click сохраняет immediate drop в клетке resident. После drop используется существующий Inventory/world gravity resolver.

### Excavation quarter ownership и geometry

`ExcavationWorkCoordinator` существовал, но runtime jobs обходили его и переводили работу в `Finalize` напрямую. Теперь tunnel и spatial excavation выполняют один authoritative quarter swing на work cadence, остаются в `PerformWork` до 4/4, сохраняют completed mask при reassignment/retry и удаляют состояние только после terrain commit.

Старый quarter marker закрашивал completed part почти чёрным кубом. Solid cell теперь переключается на четыре части породы при первом completed quarter; завершённая часть геометрии отключается, а remaining parts сохраняют material/tint породы. Designation overlay отдельно скрывает completed quarter и больше не создаёт чёрную пластину.

### Nearest automatic excavation

Generic assignment обходил jobs в repository order и использовал per-job Manhattan candidate cost, поэтому vertical/depth job мог быть назначен раньше более близкой клетки. Automatic horizontal и spatial excavation теперь используют существующие `DirectJobAssignmentPlanner`/`DirectSpatialJobAssignmentPlanner`: для каждого свободного resident выбирается минимальный фактический Navigation route с deterministic `CellId`/`JobId` tie-break. Оставшиеся excavation candidates блокируются для generic matcher в том же tick и переоцениваются на следующем.

### Unity compiler guards

Quarter work явно переводит signed generation seed в его 32-bit unsigned representation перед расширением до `ulong`. Skill resolution использует единственный актуальный source, привязанный через `BindExcavationSkillSource`; ссылка на удалённый legacy `_manualExcavationMiningSkill` устранена. Inventory routing проверяет nullable selected resident id до `EntityId.Parse` и после guard явно передаёт non-null значение для Unity Roslyn. Это устраняет diagnostics `CS1503`, `CS0103` и `CS8604` без изменения authoritative gameplay state.

### Continuation и cave rooms

Manual connected-zone planning был ограничен radius 4 и XY adjacency. Frontier/cluster resolution теперь учитывает соседние Z layers и весь connected target set. Room preview всегда рисует front silhouette; depth designations и quarter masks синхронизируются из authoritative world/job state.

## Изменённые owners

- `InventoryState` остаётся единственным владельцем item location/quantity/reservations.
- `BuildingsState` и building commands остаются владельцами placement/assembly/packing commits.
- `JobSystem` остаётся владельцем excavation lifecycle/stage/worker.
- `ExcavationWorkCoordinator` владеет per-target completed-quarter mask и active quarter assignments.
- Unity Presentation владеет только selected ids, transparent ghosts, hover/cursor, selection highlight и partial-progress geometry.

## Regression coverage

- input router: BuildingBox selection versus pickup and generic item pickup;
- world/HUD BuildingBox selection effect, shared StackId и physical highlight;
- completed-building selection before movement;
- final-building unpack ghost on Z0;
- inventory item placement and trigger-collider source contracts;
- low-skill quarter assignment stability and 4/4 finalization gate;
- completed quarter removes rock geometry and does not use black fill;
- automatic horizontal/spatial assignment invokes route-nearest planners;
- Unity quarter seed type normalization, current excavation skill source и guarded non-null resident IDs;
- 12-cell horizontal/depth connected cluster;
- room outline and quarter marker synchronization.

## Проверка

Repository quality, C# compatibility, module-boundary, Unity source-contract и `.NET` build/tests выполняются в GitHub Actions. Полный Unity Play Mode workflow остаётся обязательным для перевода связанных runtime systems в `VERIFIED`.
