# Runtime selection, excavation progress и item placement

Статус: `APPROVED`.

Tracking issues: [#67](https://github.com/bageus/Dig/issues/67), [#70](https://github.com/bageus/Dig/issues/70), [#387](https://github.com/bageus/Dig/issues/387), [#388](https://github.com/bageus/Dig/issues/388), [#390](https://github.com/bageus/Dig/issues/390), [#398](https://github.com/bageus/Dig/issues/398), [#459](https://github.com/bageus/Dig/issues/459).

Этот документ является утверждённым дополнением к:

- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-inventory-expansion.md`](resident-inventory-expansion.md);
- [`campfire-cooking-and-food-use.md`](campfire-cooking-and-food-use.md);
- [`excavation-command-execution.md`](excavation-command-execution.md).

При расхождении по перечисленным ниже пунктам это дополнение имеет приоритет как более позднее подтверждённое решение пользователя.

## 1. Выбор построенного здания

Обычный ЛКМ по completed building:

1. выбирает именно здание до обработки movement/excavation target под ним;
2. открывает нижнее меню функций здания;
3. переключает правую панель на список Buildings;
4. подсвечивает строку того же здания;
5. снимает несовместимый resident, job, BuildingBox и inventory selection.

World selection, HUD selection и management selection используют один selected building id. Один ЛКМ не может одновременно выбрать здание и выдать приказ перемещения.

## 2. Выбор BuildingBox и синхронизация мира со списком

Обычный ЛКМ по world BuildingBox обязан завершаться через игровой runtime router и единый selection path:

1. выбирается конкретный `StackId` коробки;
2. world visual этой же коробки получает selection highlight;
3. правая панель переключается на Buildings и подсвечивает строку той же коробки;
4. открывается меню BuildingBox с действием `Unpack`;
5. movement, excavation и placement command этим же кликом не создаются.

Selection highlight изменяет только renderers фактической модели выбранной коробки. Запрещено создавать collider-sized cube/surface вокруг stack, подсвечивать клетку, пол, соседние world items или другие коробки в той же клетке. Подсветка строки Buildings roster остаётся отдельной HUD-проекцией и не расширяет world highlight.

Выбор BuildingBox из Buildings roster/management использует тот же selected `StackId`, вызывает тот же runtime selection path и немедленно подсвечивает соответствующую физическую коробку в мире. World click и HUD click не могут владеть разными selected ids. Incompatible resident, job, completed-building, cell и inventory selection очищаются вместе с переключением на BuildingBox.

## 3. BuildingBox unpacking и placement cursor

Кнопка `Unpack` выбранной world BuildingBox и обычный LMB по BuildingBox в resident inventory включают один placement mode.

- системный 2D cursor скрывается;
- полупрозрачный 3D ghost становится игровым cursor и непрерывно следует pointer в world-space;
- ghost не участвует в raycast/physics/occupancy;
- valid preview зелёный, invalid preview красный и содержит reason code;
- preview не меняет Inventory/Buildings/Jobs;
- invalid LMB не создаёт reservation или job;
- RMB отменяет preview и восстанавливает системный cursor.

Intent определяется target depth без отдельного selector:

- `Z0` показывает ghost BuildingBox и создаёт relocation/hauling job той же коробки;
- `Z1–Z3` показывают ghost конечного здания с footprint и создают BuildingBox assembly plan/job.

Resident, creature и loose world item в target cell не блокируют placement. Solid/unexplored/out-of-bounds terrain, active building/plan overlap и отсутствие reachable target/work position блокируют.

После confirmation source box остаётся в authoritative location до pickup, target planned ghost остаётся видимым до commit, а зарезервированная source box отображается синим в world/Buildings/inventory projection. Если source box уже в `AgentInventory`, candidate set содержит только resident-holder. Если box лежит в world, обычный matching выбирает свободного worker, который подбирает её в inventory и несёт к target.

## 3.1 Единый item interaction source of truth

Все переносимые предметы получают `ItemInteractionProfile` из authoritative `ItemDefinition`. Default resolver использует category `building.box`, `ItemFoodUseDefinition`, `IsTool`, затем generic fallback. World/inventory Presentation не классифицирует gameplay behavior по ItemId, строковым prefix или Unity override dictionary. Полный contract: [`item-interaction-capabilities.md`](item-interaction-capabilities.md).

## 4. World item pickup, direct use и collision

Для любой pickup/use команды обязателен один выбранный живой resident. Без выбранного resident world item не показывает action cursor/highlight и click не создаёт pickup/use job.

- обычный generic world item, material, food, potion или drink подбирается обычным ЛКМ;
- hover доступного для pickup non-BuildingBox stack показывает анимированную стрелку вверх и подсвечивает только renderers этого stack;
- hover highlight является Presentation-состоянием и не заменяет BuildingBox selection highlight;
- `Alt + ЛКМ` для pickup требуется только BuildingBox;
- world BuildingBox показывает стрелку вверх и pickup hover highlight только пока удерживается `Alt`; без `Alt` обычный ЛКМ сохраняет BuildingBox selection/unpack workflow;
- food, potion и drink без `Alt` используют обычный pickup contract;
- при удержании `Alt` доступный food, potion или drink показывает анимированный рот; `Alt + ЛКМ` создаёт direct pickup-then-use command для точного stack;
- конкретный эффект consumable принадлежит его authoritative action/content owner: food использует meal owner, а potion/drink не должны реализовывать эффект в Presentation;
- object target обрабатывается раньше movement target на той же pointer ray;
- hover и click используют одинаковый stack/availability resolver;
- full Inventory, reserved/unavailable stack или stale selected resident возвращают reason и не создают скрытый move/pickup/use;
- world item collider остаётся raycastable, но не является физическим препятствием для resident movement;
- предметы не входят в Navigation occupancy и гномы проходят через их visual/collider.

## 5. Размещение и использование предмета из resident inventory

Один ЛКМ по любому доступному profile с `PlaceItem` (generic/material/food/tool/weapon, корзина или большая корзина) сразу включает полноценный item placement mode. Profile `PlaceBuilding` запускает BuildingBox placement/assembly mode:

- системный 2D cursor скрывается;
- item visual становится полупрозрачным world-space ghost и непрерывно следует pointer;
- ghost не участвует в raycast/physics/occupancy;
- target допустим в любой explored reachable open cell непосредственно над твёрдой ровной walkable support surface;
- valid preview зелёный, invalid preview красный и содержит reason code;
- authoritative stack остаётся в исходном inventory slot до фактического выполнения работы;
- ЛКМ по valid cell создаёт `ResidentInventoryPlacementJob` для exact resident, stack, available quantity и destination cell, но не переносит stack немедленно;
- job резервирует exact quantity и destination, привязан к тому же resident и проходит `TravelToDestination -> DepositItem`;
- зарезервированный для placement stack остаётся видимым в inventory и получает синюю подкраску с числовым reservation marker до deposit/cancel/failure cleanup;
- несколько placement jobs одного resident образуют deterministic dependency chain по порядку создания и выполняются этим resident последовательно;
- следующая работа становится available только после terminal-success предыдущей; cancel/failure предыдущей освобождает её reservations и явно разблокирует либо отменяет dependents по общей job policy;
- destination, ставшая недопустимой до deposit, отменяет job при прибытии, освобождает reservation и оставляет stack в inventory того же resident без потери/дублирования;
- completion active Cargo expansion использует reserved spill-aware Inventory transaction: expansion и всё содержимое Cargo перемещаются в destination, дополнительные slots исчезают в том же refresh, quantity сохраняется;
- cancel/failure не проливает Cargo и освобождает reservation, пока deposit не committed;
- invalid target не меняет Inventory и не создаёт job;
- RMB отменяет preview и восстанавливает системный cursor;
- успешное создание job очищает selection/preview и восстанавливает системный cursor, но stack остаётся видимым синим как reserved до deposit;
- save/load восстанавливает definition, exact resident binding, dependency order, reservation, destination и текущий stage.

Quick drop использует отдельный явный modifier:

- пока удерживается `C`, hover любого доступного quick-drop-enabled inventory stack, включая BuildingBox, показывает анимированную стрелку вниз;
- `C + ЛКМ` немедленно выполняет `DropInventoryStack` в authoritative current resident cell без placement job;
- `D` больше не является quick-drop modifier и остаётся правым направлением camera pan;
- double click и RMB больше не являются quick-drop input;
- reserved/held/unavailable stack не выбрасывается и возвращает typed reason;
- после quick drop обычная world-item gravity policy автоматически перемещает unsupported item вниз до первой допустимой опоры в vertical tunnel;
- движение камеры полностью дублируется стрелками: `LeftArrow/RightArrow` дублируют `A/D`, `DownArrow/UpArrow` дублируют `S/W`.

Использование consumable/tool сохраняет отдельный priority:

- `Alt + ЛКМ` по доступному food, potion, drink, tool или weapon отправляет typed use command;
- при удержании `Alt` consumable slot показывает анимированный рот, а tool/weapon использует свой action feedback;
- `Alt` use имеет приоритет над generic placement; `C` quick drop имеет приоритет только без `Alt`;
- BuildingBox ordinary LMB остаётся отдельным placement/assembly workflow и использует layer-derived box/building ghost; `C + LMB` использует общий exact-stack quick drop.

## 6. Excavation quarter progress

Каждая excavation target cell выполняется через четыре authoritative quarters:

- `UpperLeft`;
- `LowerLeft`;
- `UpperRight`;
- `LowerRight`.

Случайный/deterministic выбор текущего quarter и количества swings не может быть скрытым Presentation-only состоянием.

- completed-quarter mask, target-owned cut pattern и source-material provenance сохраняются в authoritative World `CellState`; Jobs/worker coordinator хранит только незавершённый swing cadence и reservations;
- cut pattern определяется plan kind target, а не текущей work position: vertical front-slice target всегда использует `HorizontalRows`, horizontal target использует ближайшую к resident `VerticalColumns`, depth target использует `DepthFace`;
- пока в target-owned ближайшей строке/колонке остаётся хотя бы один unfinished quarter, один swing не может одновременно перейти на дальнюю строку/колонку даже при высоком mining skill; смена side/depth/climbing work position не меняет pattern vertical target;
- Job не переходит в `Finalize`, пока не завершены все четыре quarters;
- completed quarter немедленно отображается и на designation overlay, и на самой породе;
- завершённая четверть удаляет/скрывает соответствующую геометрию породы и открывает пространство за ней;
- завершённую четверть запрещено имитировать чёрной заливкой или непрозрачной чёрной пластиной;
- оставшиеся четверти сохраняют material/tint породы, а designation остаётся отдельным overlay;
- 1/4, 2/4 и 3/4 должны визуально отличаться;
- retry/reassignment не сбрасывает completed quarters;
- interruption, release, cancel или Eraser удаляют только оставшуюся работу: уже завершённые quarters не восстанавливаются и остаются отсутствующими в terrain visual;
- повторное designation частично выкопанной клетки продолжает с сохранённого completed-quarter mask, а не начинает 0/4;
- completion одной клетки не удаляет remaining designations/jobs connected zone;
- Z0 tunnel, vertical/depth excavation и cave-room child cells используют тот же observable progress contract;
- ошибка одной клетки не прекращает simulation loop и не блокирует остальных residents.

### World-owned excavation progress and typed traversal

- `CompletedQuarterMask` и `ExcavationCutPattern` являются частью authoritative `CellState`; Unity-local coordinator может хранить только swing/reservation cadence и обязан гидратироваться из World.
- vertical front-slice tunnel использует `HorizontalRows` независимо от resident/work-cell approach; horizontal tunnel использует `VerticalColumns`, depth — `DepthFace`.
- каждый completed quarter сначала коммитится в World. Четвёртый quarter в том же mutation делает cell empty и снимает designation; последующий job/output cleanup idempotent.
- cursor, terrain renderer, standing support, work-position replan и save/load читают World mask.
- любой partial cut клетки под ногами отменяет full actor support и вызывает side/depth replan либо stationary climbing stance.
- horizontal movement через open shaft floor gap является `ShaftGapTraverse`; route без gap, включая обход по depth, имеет лексикографический приоритет над более коротким gap route.

## 7. Cave-room preview и execution

При активном room tool и валидном entrance:

- полный front silhouette room preview всегда видим до клика;
- valid preview не исчезает из-за отключения invalid outline;
- click создаёт child designations/jobs для полного plan mask;
- pending room cells отображаются как excavation designations;
- room остаётся видимой как plan до фактического excavation completion;
- progress room вычисляется из completed child cells/quarters, а не из локальной анимации.

## 8. Acceptance

Обязательные regression scenarios:

1. resident selected -> LMB completed building -> building functions + Buildings tab + highlighted row, без move order;
2. world BuildingBox LMB -> выбран тот же `StackId`, подсвечены только renderers модели этой коробки + Buildings row + `Unpack`, без collider-sized highlight surface, preview, движения или копки;
3. Buildings roster BuildingBox click -> только та же физическая коробка подсвечена в runtime; клетка, пол и соседние items не меняют tint/geometry;
4. selected world BuildingBox -> `Unpack` -> hidden system cursor + moving ghost;
5. inventory BuildingBox LMB -> тот же moving ghost placement mode;
6. pointer по Z0 -> box ghost -> relocation job; pointer по Z1–Z3 -> completed-building ghost/footprint -> assembly plan/job;
7. resident/creature/loose item в target cell не блокируют valid placement;
8. inventory-held source назначается только holder resident и отображается синим while reserved/carried;
9. world source подбирается worker, переносится в inventory и доставляется к target;
10. selected resident -> hover generic/material/food item -> exact stack highlight + animated pickup arrow -> LMB pickup order без Alt;
11. world BuildingBox без Alt не показывает pickup arrow; Alt hover показывает arrow, Alt+LMB создаёт pickup;
12. selected resident -> Alt hover food/consumable -> animated mouth -> Alt+LMB exact pickup-then-use; plain LMB по тому же stack остаётся pickup;
13. resident проходит через world item collider;
14. inventory generic item LMB -> hidden system cursor + transparent moving ghost -> green valid flat/walkable target -> resident-bound placement job, stack остаётся синим и reserved в slot до deposit;
15. два и более placement jobs одного resident выполняются строго в порядке создания, без параллельного claim, потери или дублирования items;
16. destination, ставшая недопустимой к моменту прибытия, отменяет job, снимает stale blue tint и оставляет предмет в inventory resident; save/load сохраняет очередь и stage до этой проверки;
17. inventory item C hover -> animated down arrow -> C+LMB immediate drop at resident cell -> fall through open vertical tunnel;
18. D+LMB не создаёт quick drop; camera pan остаётся доступен через `A/D/S/W` и `Left/Right/Down/Up`;
19. double click/RMB не создают quick drop; Alt use, `C` quick drop и BuildingBox ordinary placement сохраняют profile-defined priority;
20. horizontal и vertical excavation минимум 10 cells без остановки;
21. 1/4, 2/4, 3/4 progress видим как реально удалённые quarters породы без чёрной заливки; при копке сверху вниз состояние 2/4 является полностью удалённой верхней половиной, а не вертикальной колонкой;
22. interruption/erase после partial progress оставляет удалённые quarters, а повторное designation продолжает с того же mask;
23. cave-room valid preview видим и child jobs продолжаются до полного plan completion;
24. failure/retry одного excavation или item-placement job не блокирует другие commands/residents.

Unit/source-contract tests не заменяют Unity Play Mode validation для pointer routing, world/HUD selection synchronization, animated cursor/highlight, moving ghost visibility, ordered resident-bound placement jobs, depth-derived plan kind, partial terrain geometry и длительного workflow.
