# Runtime selection, excavation progress и item placement

Статус реализации: `IMPLEMENTED`, runtime Play Mode verification pending.

Authoritative design: [`../design/runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md), [`../design/building-box-placement-and-packing.md`](../design/building-box-placement-and-packing.md).

Tracking: [#387](https://github.com/bageus/Dig/issues/387), [#388](https://github.com/bageus/Dig/issues/388), [#390](https://github.com/bageus/Dig/issues/390), [#398](https://github.com/bageus/Dig/issues/398), [#118](https://github.com/bageus/Dig/issues/118).

## Исправленные первопричины

### World selection и input priority

Selected-resident movement перехватывал pointer до completed-building selection и части item interactions. Runtime priority resolver теперь обрабатывает completed buildings и world items до свободного movement target. Selection здания открывает функции, переключает Buildings roster и использует единый selected building id для world/HUD/management.

### BuildingBox selection и world/HUD sync

`ContextInputRouter` возвращал `SelectBuildingBox`, но Unity `ApplyEffects` не обрабатывал этот effect. Поэтому world click потреблялся без фактического runtime selection. Effect подключён; world click и Buildings roster используют один `StackId` и очищают несовместимые selections.

Первый physical highlight создавал отдельный полупрозрачный cube по размеру interaction collider. Он визуально захватывал пространство вокруг коробки и мог выглядеть как подсветка клетки/соседних объектов. Отдельная surface удалена. Selection теперь меняет tint только существующих renderer-ов visual instances выбранной коробки; pool reset снимает tint, а HUD row остаётся независимой проекцией того же `StackId`.

### BuildingBox moving placement cursor

Старый hover path обновлял preview только при успешном physics hit. Когда pointer шёл над resident, loose item или открытым участком, target cell не разрешалась и ghost оставался в предыдущей позиции. Placement mode теперь скрывает system cursor, использует 3D ghost как игровой cursor, ищет terrain/tunnel cell за неблокирующими объектами и при отсутствии hit проецирует pointer на текущий depth layer. Cancel и successful confirmation восстанавливают прежнюю visibility системного cursor.

Placement intent больше не выбирается скрытым условием renderer-а:

- Z0 создаёт `RelocateBox`, показывает BuildingBox visual и подтверждается typed relocation job;
- Z1–Z3 создают `AssembleBuilding`, показывают Completed visual/footprint и подтверждаются существующим assembly plan/job.

Validation получает building-plan occupancy, но не resident/creature/loose-item occupancy. Поэтому такие объекты не замораживают ghost и не делают пустую terrain cell invalid. Valid preview остаётся зелёным, invalid — красным с reason code.

### BuildingBox terrain support и forced movement

Причина размещения в воздухе состояла в том, что logical placement проверял только open/explored footprint и reachable work position. Surface projector также ошибочно использовал depth `Z` как elevation и не требовал реальную solid terrain cell под footprint.

Теперь BuildingBox preview и authoritative confirmation используют один terrain-support contract:

- для каждой horizontal/depth column выбирается нижняя occupied cell;
- непосредственно под ней в `Y + 1` должна существовать explored solid terrain cell;
- все support cells должны иметь одинаковую фактическую Y-высоту;
- unsupported preview получает `building.placement.surface_missing`, становится `IsVisible == false`, renderer очищает ghost, а confirmation не создаёт reservation/plan/job;
- Z0 relocation повторяет тот же support check в authoritative relocation handler.

Это применяется ко всем BuildingBox-enabled definitions; packable content дополнительно проходит собственную conservative physical-footprint policy.

World-source relocation использует обычный BuildingBox candidate policy и pickup/carry pipeline. Inventory source сразу claim-ится resident-holder. Exact box reservation сохраняется во время carry; reserved BuildingBox slot отображается синим. Relocation save codec хранит destination и holder-start stage, сохраняя backward compatibility с direct pickup snapshots.

Z0 interactive и confirmed planned ghost теперь используют тот же item visual resolver, asset, world scale и floor/depth projection, что и физическая BuildingBox в `DigWorldItemRenderer`. Planned relocation projection строится из active authoritative relocation jobs и остаётся видимой до deposit/cancel/failure. Pointer target resolver учитывает normal-mode tunnel movement surfaces, поэтому placement cursor получает реальные Z1–Z3 cells даже при отключённых dig-cell proxy colliders. Relocation execution переведён на детерминированную step policy: held box запускает job до travel, world box проходит pickup, а carried box в `DepositItem` атомарно перемещается в destination world cell и завершает job.

Повторная runtime-проверка выявила две оставшиеся причины. Campfire placement profile консервативно округлял визуальные `1.5 x 1.5` до логических `2 x 2`, из-за чего footprint включал solid floor cell и presenter возвращал invisible `surface_missing` на Z1–Z3; одновременно content запрещал tunnel placement. Campfire теперь занимает одну logical anchor cell, требует support, но разрешает supported Z1–Z3. Relocation больше не требует входа worker в destination: path выбирает ближайшую ортогональную work cell, policy разрешает deposit из неё, а runtime дренирует start/stage/deposit transitions в одном tick после прибытия.

Direct movement теперь включает active BuildingBox relocation/assembly в interruption set. Пока box plan ещё не committed `AtSite`, direct move отменяет job/plan, освобождает item/worker/position reservations, удаляет planned building projection и route, но не перемещает и не пересоздаёт коробку: та же quantity-one entity остаётся в inventory holder resident. Immediate Jobs/Buildings/items/agent HUD refresh снимает синий reserved tint и planned ghost в том же interaction. После `AtSite` сохраняется существующая explicit-cancel policy.

Inventory BuildingBox LMB продолжает немедленно маршрутизироваться через `ResidentInventory` input surface в `BeginResidentInventoryBuildingPlacement`; системный cursor скрывается, а building ghost становится игровым cursor.

### Item pickup, placement и collision

Generic item pickup больше не зависит от `Alt`; `Alt` остаётся обязательным для BuildingBox. World item collider переведён в trigger: он участвует в raycast, но не является физическим препятствием. Inventory ordinary LMB создаёт локальный transparent ghost и resident-bound placement job; `C + LMB` выполняет immediate exact-stack drop в клетке resident. Double-click/RMB quick drop запрещены. После drop используется существующий Inventory/world gravity resolver.

### Excavation quarter ownership и geometry

`ExcavationWorkCoordinator` существовал, но runtime jobs обходили его и переводили работу в `Finalize` напрямую. Теперь tunnel и spatial excavation выполняют один authoritative quarter swing на work cadence, остаются в `PerformWork` до 4/4, сохраняют completed mask при reassignment/retry и удаляют состояние только после terrain commit.

Старый quarter marker закрашивал completed part почти чёрным кубом. Solid cell переключается на четыре части породы при первом completed quarter; завершённая часть геометрии отключается, а remaining parts сохраняют material/tint породы. Designation overlay отдельно скрывает completed quarter и больше не создаёт чёрную пластину.

Исправлен второй слой той же ошибки: combined terrain mesh продолжал рисовать полный solid cube под per-cell quarters, поэтому фактически удалённая часть оставалась закрыта общей геометрией. Частично выкопанная клетка исключается из combined mesh, remaining quarter geometry остаётся видимой даже после снятия designation, а повторное назначение продолжает с сохранённого mask. Full 4/4 commit помечает World changed до Navigation refresh; dirty chunks дренируются только после успешного rebuild, поэтому ошибка derived navigation больше не оставляет завершённую клетку визуально целой до ручного redraw.

Оставался третий слой рассинхронизации: ordinary commit обновлял World/Navigation repository, но не пересобирал `DigAgentSession.TunnelVolume` и `DigTunnelDemoRenderer` movement surfaces до следующего interaction refresh. Spatial commit мог изменить World, затем упасть на job/topology cleanup; повторная попытка снова вызывала excavation уже пустой клетки, а общий tick пропускал item gravity. Runtime теперь после любого authoritative terrain commit в том же tick пересобирает Navigation, resident tunnel topology и movement surfaces, затем запускает существующий item support resolver до pickup/hauling reservations. Spatial excavation retry принимает уже открытую authoritative cell как idempotent success и завершает derived cleanup вместо вечного stall.

### Nearest automatic excavation и drag-stroke batching

PR #414 сравнивал Navigation route до work position, но ordinary tunnel tool по-прежнему вызывал `SynchronizeDesignations` после каждой нарисованной клетки. Первый painted/created job мог быть claimed до появления остальных клеток stroke, поэтому порядок рисования скрыто переопределял nearest rule. Особенно заметно это было на вертикальном front-slice tunnel: нижняя правая клетка могла назначаться раньше ближайшей верхней.

Tunnel drag теперь сначала stage-ит все World designations, а на LMB release один раз выполняет job reconciliation и assignment полного stroke batch. Planner среди reachable jobs сначала сравнивает 3D Manhattan distance от текущей клетки resident до самой target cell, затем Navigation route cost до work position, `CellId` и `JobId`. Одинаковая/shared work position больше не делает нижний target равным верхнему. Единый automatic pool применяет тот же порядок к ordinary и spatial excavation.

### Unity compiler guards

Quarter work явно переводит signed generation seed в его 32-bit unsigned representation перед расширением до `ulong`. Skill resolution использует единственный актуальный source, привязанный через `BindExcavationSkillSource`; ссылка на удалённый legacy `_manualExcavationMiningSkill` устранена.

После разделения tunnel-stroke batching на partial-файл Unity не видел `ExcavationStrokeAxis`, потому что `using` directives не распространяются между partial declarations. `DigWorldInteraction.ExcavationStrokeBatch.cs` явно импортирует `Dig.Application.Jobs`. Nullable diagnostics устранены в тех же runtime paths: assigned agent извлекается через `GetValueOrDefault()` после `HasValue`, cave-room overlay field помечен non-null после `EnsureResources`, а nullable resident id передаётся в `EntityId.Parse` только через явный non-null fallback после guard.

После persistent-quarter изменения interaction начал вызывать `ClearExcavationQuarterProgress`, но соответствующий метод отсутствовал у `DigExcavationCursorRenderer`. Renderer теперь сбрасывает progress всех существующих tunnel designation markers в `ExcavationQuarter.None` перед проекцией актуального authoritative snapshot; stale quarters не остаются на overlay и Unity compile больше не получает `CS1061`.

`Dig.Unity.PlayModeTests` является отдельной assembly и не имеет доступа к `internal DigTunnelProjection`. BuildingBox cursor regression теперь проверяет observable transform renderer-а без обращения к runtime-internal helper, поэтому Unity Test Runner компилирует test assembly без `CS0122`. Nullable `ResidentInventoryLayoutSlotViewModel.StackId` теперь явно проверяется и только затем передаётся в `EntityId.Parse`, устраняя `CS8604` без изменения valid inventory workflow.

Тот же assembly boundary применяется к `internal DigBuildingBoxGhostRenderer.RenderPlans`: Play Mode regression вызывает projection через существующий reflection helper вместо прямого compile-time вызова. Это устраняет `CS1061` в `Dig.Unity.PlayModeTests`, не раскрывая runtime presentation API только ради теста.

`PostExcavationTopologyPlayModeTests` больше не полагается на отсутствующий в public Domain contract helper `CellId.Offset`. Четыре горизонтальных соседа строятся через публичный `CellId(x, y, z)` constructor. Это сохраняет black-box проверку topology/movement surfaces и устраняет `CS1061` в Unity test assembly без расширения Domain API ради теста.

### Continuation и cave rooms

Manual connected-zone planning был ограничен radius 4 и XY adjacency. Frontier/cluster resolution учитывает соседние Z layers и весь connected target set. Если назначенный ordinary Dig job после refresh не имеет успешного route/work cell, assignment снимается, quarter state сохраняется, а job возвращается в общий pool вместо вечного `Claimed/InProgress` без движения.

Room commit раньше передавал `VolumeCells` в atomic `SetDigDesignations`, хотя volume включал уже открытую entrance cell; Domain корректно отклонял весь batch. План разделяет полный `VolumeCells`, открытый `BaseTunnelCells` и фактический `ExcavationCells`. Commit назначает только rock mask. Planner требует полный сквозной base tunnel, проверяет mineability каждой остальной 3D-клетки и возвращает per-cell diagnostics. Runtime разрешает pointer на породе над тоннелем и preview красит конкретные missing/unmineable/protected cells.

### Повторный runtime defect: frontier entry, Y-axis и near-side quarter

После PR #432 screenshot/runtime проверка показала три отдельные первопричины, которые source-contract topology rebuild не покрывал:

- `ExcavationApproachSide` инвертировал Y-down координату и при target ниже worker выбирал дальние lower quarters вместо ближайших upper quarters;
- `TunnelNavigationVolume` требовал vertical provenance одновременно у horizontal entry cell и shaft cell, поэтому первый шаг в вертикальный тоннель оставался unreachable;
- grounded `NavigationMap` искал floor support по `Y - 1`, тогда как World renderer, tunnel topology и item gravity используют authoritative Y-down направление `Y + 1`.

Approach resolution перенесён в Domain. Shaft entry/exit сохраняет открытый transition endpoint рядом с planned vertical cell и принимает vertical provenance у shaft endpoint. Grounded support приведён к `Y + 1`, а фактическая Inventory relocation unsupported items вынесена в `WorldItemGravitySettlement` и покрыта integration tests. Play Mode fixture остаётся обязательным для финальной runtime verification.

Повторная runtime-проверка после merge #439 обнаружила, что projection всё ещё получала только `plannedVerticalCells`: завершённые обычные planned tunnel cells без floor support выпадали из `TunnelNavigationVolume`, поэтому маршрут продолжался только через первый endpoint. Rebuild теперь принимает полный authoritative `PlannedTunnelCells`; vertical subset определяет только Y transitions. Quarter planner stage-ит ближайшую строку/колонку и не разливает high-skill swing на дальнюю половину. Spatial depth policy выбирает достижимую side-horizontal cell, затем adjacent open depth cell, затем shaft fallback. Unsupported shaft fallback рендерится как stationary climbing-work pose спиной к камере.

### World-owned excavation state и atomic 4/4 commit

После повторных runtime-рассинхронизаций completed-quarter mask перенесён из Unity session в `CellState`. Каждый authoritative swing завершает конкретный quarter через `WorldState.CommitExcavationQuarter`; renderer, cursor, support, job reconciliation и save/load читают один World snapshot. Target-owned `ExcavationCutPattern` отделяет форму разрезания от текущей work position: front-slice vertical tunnel всегда использует `HorizontalRows`, horizontal tunnel — `VerticalColumns`, depth — `DepthFace`.

Четвёртый quarter в том же World mutation переводит материал в empty, снимает `Dig` designation и оставляет idempotent provenance исходного материала для mining output/finalize cleanup. Поэтому ошибка после 4/4 не может оставить визуально пустую клетку solid, повторно показывать shovel cursor или создавать новый Dig job. Save format v8 добавляет mask/pattern/source material поверх существующей v7 building-production migration.

Work-position planning получает World snapshot и типизированный posture. Частичная потеря полной опоры запрещает `Standing`: planner сначала ищет supported side/depth position, затем разрешает stationary `Climbing`. Tunnel routes содержат `SupportedWalk`, `VerticalClimb`, `ShaftGapTraverse` и `DepthTraverse`; path cost сначала минимизирует shaft-gap transitions, затем длину, поэтому доступный depth detour предпочтительнее прямого пересечения шахты. Неизбежное горизонтальное пересечение shaft gap проецируется climbing pose спиной к камере.

## Изменённые owners

- `InventoryState` остаётся единственным владельцем item location/quantity/reservations.
- `BuildingsState` и building commands остаются владельцами placement/assembly/packing commits.
- `World` владеет support terrain facts; presentation не может считать projected layer достаточной опорой.
- `JobSystem` владеет excavation и BuildingBox relocation/assembly lifecycle/stage/worker.
- `World` владеет per-cell completed-quarter mask, cut pattern и source material; `ExcavationWorkCoordinator` хранит только swing cadence и краткоживущие worker reservations.
- Unity Presentation владеет только selected ids, renderer tint, transparent ghosts, hover/cursor и partial-progress geometry.

## Regression coverage

- input router: BuildingBox selection versus pickup and generic item pickup;
- world/HUD BuildingBox selection effect и shared StackId;
- Play Mode: selection не создаёт дополнительную geometry и меняет tint только физической коробки;
- moving BuildingBox ghost follows cells/layers through enabled movement surfaces, stays IgnoreRaycast and collider-disabled;
- Z0 preview/planned projection matches actual BuildingBox bounds and survives confirmation until deposit;
- supported/unsupported Z0 and assembly preview, hidden ghost over air and confirmation parity;
- flat lower-footprint support projected from real solid `Y + 1` cells;
- Z0 relocation handler rejects unsupported air before reservation/job creation;
- forced direct movement cancels pre-site carried-box plan/job, releases reservations and preserves holder inventory identity/quantity;
- inventory BuildingBox LMB remains the immediate placement-mode entry;
- Z0 relocation preview/job versus Z1–Z3 assembly preview/plan;
- world-source relocation reservation, holder-only inventory assignment, pickup/carry/deposit identity conservation;
- relocation save codec round-trip и blue reserved inventory projection source contract;
- Play Mode test assembly не обращается к runtime-internal projection helpers; nullable inventory stack ids guard-ятся до parsing;
- post-excavation Play Mode topology fixture использует только public `CellId` construction и не вызывает отсутствующий `Offset`;
- completed-building selection before movement;
- inventory item placement and trigger-collider source contracts;
- low-skill quarter assignment stability and 4/4 finalization gate;
- completed quarter removes rock geometry and does not use black fill;
- full commit rebuilds Navigation, resident tunnel topology and movement surfaces from complete planned tunnel provenance before pickup/reservation work;
- high-skill quarter plans finish the near horizontal row/vertical column before the far band;
- depth excavation work-cell selection covers side horizontal, adjacent depth and shaft climbing fallback;
- stationary mining in an unsupported shaft uses climbing pose instead of standing in air;
- spatial retry accepts an already-open authoritative cell and item support loss is settled before reservations;
- shared-work-cell spatial assignment выбирает ближайшую target cell, даже если дальний job имеет меньший id;
- source contract: tunnel drag stage-ит designations и reconciles jobs только после release;
- automatic ordinary/spatial selection использует target-distance → route-cost → CellId → JobId;
- source contract требует namespace import для `ExcavationStrokeAxis`, cursor progress-reset method и non-null projections для agent, overlay и inventory ids;
- unroutable assigned Dig job releases its worker reservation without losing quarter progress;
- room commit uses `ExcavationCells`, full base-tunnel validation and per-cell invalid preview diagnostics;
- Unity quarter seed type normalization и current excavation skill source;
- 12-cell horizontal/depth connected cluster;
- room outline and quarter marker synchronization.

## Проверка

Repository quality, C# compatibility, module-boundary, Unity source-contract и `.NET` build/tests выполняются в GitHub Actions. Полный интерактивный Unity Play Mode workflow placement/relocation/assembly, unsupported-air cursor transitions, forced direct-move cancellation и vertical tunnel stroke остаётся обязательным для перевода runtime systems в `VERIFIED`.


## Definition-owned item interaction follow-up — 2026-08-01

World hover/click and resident inventory actions now consume one `ItemInteractionProfile` from `ItemDefinition`. Generic fallback, food, tool/weapon and BuildingBox behavior no longer depend on Unity ItemId/prefix branches. The exact item resolver runs before movement/excavation and consumes rejected item clicks with a typed reason.
