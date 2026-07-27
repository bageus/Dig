# Runtime selection, excavation progress и item placement

Статус: `APPROVED`.

Tracking issues: [#387](https://github.com/bageus/Dig/issues/387), [#388](https://github.com/bageus/Dig/issues/388), [#390](https://github.com/bageus/Dig/issues/390), [#398](https://github.com/bageus/Dig/issues/398).

Этот документ является утверждённым дополнением к:

- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
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

## 4. World item pickup и collision

- `Alt + ЛКМ` для pickup требуется только BuildingBox;
- обычный generic world item подбирается обычным ЛКМ при выбранном resident;
- object target обрабатывается раньше movement target на той же pointer ray;
- hover и click используют одинаковый stack/reachability resolver;
- world item collider остаётся raycastable, но не является физическим препятствием для resident movement;
- предметы не входят в Navigation occupancy и гномы проходят через их visual/collider.

## 5. Размещение предмета из resident inventory

Один ЛКМ по доступному generic item в resident inventory включает локальный item placement mode:

- item visual становится полупрозрачным world-space ghost;
- ghost следует pointer по валидным world cells;
- authoritative stack остаётся в исходном inventory slot до успешной команды;
- ЛКМ по валидной клетке выполняет `DropInventoryStack`;
- invalid target не меняет Inventory и показывает reason;
- RMB отменяет preview;
- успешный drop очищает selection/preview.

Двойной ЛКМ по item в inventory выполняет немедленный drop в authoritative клетке resident. После drop обычная world-item gravity policy автоматически перемещает unsupported item вниз до первой допустимой опоры в vertical tunnel.

BuildingBox inventory action остаётся отдельным unpacking workflow и использует layer-derived box/building ghost, а не generic item ghost.

## 6. Excavation quarter progress

Каждая excavation target cell выполняется через четыре authoritative quarters:

- `UpperLeft`;
- `LowerLeft`;
- `UpperRight`;
- `LowerRight`.

Случайный/deterministic выбор текущего quarter и количества swings не может быть скрытым Presentation-only состоянием.

- quarter completion сохраняется в едином excavation progress owner;
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
10. selected resident -> LMB generic world item -> pickup order без Alt;
11. resident проходит через world item collider;
12. inventory item single LMB -> transparent item ghost -> valid world drop;
13. inventory item double LMB -> drop at resident cell -> fall through open vertical tunnel;
14. horizontal и vertical excavation минимум 10 cells без остановки;
15. 1/4, 2/4, 3/4 progress видим как реально удалённые quarters породы без чёрной заливки;
16. interruption/erase после partial progress оставляет удалённые quarters, а повторное designation продолжает с того же mask;
17. cave-room valid preview видим и child jobs продолжаются до полного plan completion;
18. failure/retry одного excavation job не блокирует другие commands/residents.

Unit/source-contract tests не заменяют Unity Play Mode validation для pointer routing, world/HUD selection synchronization, moving ghost visibility, depth-derived plan kind, partial terrain geometry и длительного workflow.
