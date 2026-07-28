# Коробки зданий, размещение, сборка и упаковка

Статус: `APPROVED`.

Tracking issues: [#118](https://github.com/bageus/Dig/issues/118), [#390](https://github.com/bageus/Dig/issues/390), [#398](https://github.com/bageus/Dig/issues/398).

## 1. Назначение

Все размещаемые здания используют единый физический lifecycle коробки:

```text
Production -> BuildingBox item -> placement preview -> confirmed plan/job
-> pickup/carry -> box relocation на Z0 либо unpack/assembly на Z1–Z3
-> completed building -> Pack -> BuildingBox item
```

Одна физическая BuildingBox представляет одно здание и существует ровно в одном authoritative location. Для одного `BuildingDefinition` нельзя одновременно использовать BuildingBox policy и legacy material-delivery policy.

## 2. Владение состоянием

- `InventoryState` владеет BuildingBox entity, item id, quantity, location и item reservations.
- `BuildingsState` владеет assembly plan, footprint, orientation, progress, completed state и durability.
- `JobSystem` владеет relocation/delivery/assembly/packing lifecycle, worker claim и position reservations.
- `World` владеет terrain cells и фактами физической опоры под footprint.
- `Navigation` проверяет достижимость source box, target и work position.
- `Presentation` владеет selection, меню, системным cursor visibility, moving ghost, footprint и локальным placement mode.

Ghost, cursor и footprint не являются authoritative коробкой, зданием или job.

## 3. Content model

Каждое размещаемое здание имеет стабильные ссылки:

```text
BuildingDefinition
- BuildingDefinitionId
- BuildingBoxItemId
- PlacementPolicy
- Footprint
- WorkPositions
- AssemblyWork
- PackingWork
- FunctionalCapabilities
```

BuildingBox:

- является quantity-one unit item;
- не складывается с другими коробками;
- не является контейнером материалов;
- определяется стабильными ids, а не display name;
- расходуется только при успешном completion assembly plan;
- после relocation plan остаётся той же BuildingBox entity.

## 4. Запуск placement mode

### World BuildingBox

Обычный ЛКМ:

1. выбирает конкретный `StackId`;
2. открывает Buildings roster/menu;
3. подсвечивает только renderer-ы этой коробки и её строку;
4. показывает кнопку `Unpack`;
5. не создаёт preview, reservation, plan или job.

Кнопка `Unpack` запускает placement mode для выбранной коробки.

`Alt + ЛКМ` при выбранном resident остаётся отдельным direct pickup order и не запускает placement mode.

### BuildingBox в inventory resident

Обычный ЛКМ по занятому BuildingBox slot немедленно запускает тот же placement mode. Отдельная кнопка `Unpack` не требуется. Системный cursor скрывается, а 3D ghost конечного здания становится игровым cursor.

До успешного confirmation коробка остаётся в исходном authoritative location и не резервируется.

## 5. Cursor и preview

При входе в placement mode:

- системный 2D cursor скрывается;
- 3D ghost становится игровым cursor и непрерывно следует pointer в world-space;
- изменение pointer cell немедленно обновляет origin, footprint, validity, tint и reason code;
- ghost не участвует в raycast, physics, Navigation или occupancy;
- valid preview зелёный, invalid preview красный;
- при выходе из mode системный cursor восстанавливается;
- preview не меняет Inventory, Buildings или Jobs.

Если pointer находится над объектом, который не должен блокировать placement, resolver продолжает искать world cell под ним либо проецирует pointer на текущий depth layer. Resident, creature и loose world item не удерживают ghost на старой клетке.

Interactive ghost отображается только тогда, когда под всем опорным краем его footprint существует реальная terrain-плоскость. Если target находится над пустотой либо хотя бы одна требуемая опора отсутствует, placement mode остаётся активным, но ghost скрывается и confirmation запрещён. Это правило одинаково для box ghost на Z0 и building ghost на Z1–Z3.

## 6. Intent определяется depth layer

Отдельного UI selector или modifier для выбора intent нет.

### Z0: relocation BuildingBox

- ghost автоматически меняется на точную визуальную модель исходного BuildingBox item;
- размер, asset, scale, floor offset и depth offset Z0 ghost совпадают с фактической коробкой в world;
- footprint равен одной target cell;
- valid confirmation создаёт relocation/hauling job для той же коробки;
- completed building и assembly plan не создаются;
- после delivery та же BuildingBox entity получает world location target Z0 cell.

### Z1–Z3: unpack/assembly building

- ghost показывает конечную модель здания и footprint;
- BuildingBox-enabled demo content, включая campfire, должно иметь placement profile, который разрешает supported placement на каждом слое Z1–Z3; визуальный размер `1.5 x 1.5` сам по себе не расширяет logical occupancy за пределы утверждённого building footprint;
- valid confirmation создаёт BuildingBox assembly plan и job;
- worker доставляет коробку к site, выполняет unpack/assembly и расходует коробку ровно один раз при completion;
- после completion ghost заменяется completed-building visual.

Это depth rule является единственным owner выбора plan kind и закрывает прежний вопрос `Move as box` versus `Unpack as building`.

## 7. Placement validity

Общие blocking conditions:

- target/footprint выходит за world bounds;
- terrain target/footprint solid;
- cell unexplored;
- под опорным краем box/building footprint отсутствует solid terrain support;
- опорные клетки footprint находятся на разных высотах при policy `RequiresFlatSurface`;
- footprint пересекает active building или building plan;
- отсутствует reachable target/work position;
- source box отсутствует, зарезервирована несовместимой операцией либо больше не quantity-one unit.

Для side-view footprint опорным краем является нижняя occupied-клетка каждого horizontal/depth column; каждая такая клетка должна иметь solid support непосредственно под ней. Верхние occupied-клетки footprint должны оставаться открытыми, но не требуют отдельной опоры внутри самого footprint.

Не блокируют placement:

- resident или creature в target cell;
- loose world item, включая другую не-authoritative визуальную проекцию предмета;
- presentation overlays и cursor markers.

Для Z0 relocation target также должен быть открытой explored reachable cell с solid support непосредственно под ней. Для Z1–Z3 assembly применяется footprint/surface policy соответствующего `BuildingDefinition`.

ЛКМ по invalid preview не создаёт reservation, plan или job и показывает reason code. Preview и authoritative confirmation используют одни и те же World support facts; stale preview не может разместить коробку или здание после исчезновения опоры.

## 8. Confirmation и planned projection

Успешный ЛКМ атомарно:

1. повторно валидирует source, layer-derived intent, target и terrain support;
2. резервирует конкретную BuildingBox за одним job/plan;
3. создаёт relocation job либо assembly plan/job;
4. публикует события;
5. закрывает interactive placement mode и восстанавливает системный cursor;
6. сохраняет target planned ghost до authoritative delivery/assembly commit;
7. сохраняет source selection/planned indication.

До фактического pickup:

- world source box остаётся физически видимой в своей authoritative cell;
- inventory source box остаётся в своём resident slot;
- target показывает planned ghost результата; для relocation это точная item-проекция коробки того же размера, которая остаётся до authoritative deposit/cancel/failure;
- source world visual, Buildings row или inventory slot отображаются синим как зарезервированный объект запланированного действия.

Одна коробка не может принадлежать двум active jobs/plans.

## 9. Worker assignment и execution

### Source box в world

Обычный matching выбирает свободного подходящего resident. Он:

1. идёт к source box;
2. подбирает ту же entity в inventory;
3. несёт её к target/work position;
4. выполняет relocation deposit либо assembly workflow.

### Source box в resident inventory

Candidate set содержит только resident, чей `AgentInventory` владеет source stack. Job не может получить другой resident до тех пор, пока authoritative location остаётся этим inventory.

Во время carry зарезервированная BuildingBox отображается синим в inventory.

### Relocation completion

Worker не обязан входить в target cell коробки. Runtime выбирает ближайшую reachable work cell, ортогонально соседнюю с destination на том же Z0; сама destination используется только как fallback, если соседняя позиция отсутствует. Resident или loose item в destination не блокируют delivery. Когда worker с зарезервированной коробкой достигает допустимой work cell, все немедленные stage transitions дренируются в том же simulation tick, коробка перемещается в target Z0 world cell, reservation/job завершаются, BuildingBox остаётся доступной для последующего выбора и `Unpack`.

### Assembly completion

После delivery автоматически выполняется unpack/assembly. Коробка расходуется только при успешном completion здания.

В текущем demo/test-профиле после достижения worker-ом assembly work position runtime дренирует все немедленно выполнимые переходы `DepositItem -> PerformWork -> Finalize` в том же simulation tick. Три логические unpack-итерации, три per-iteration Logistics grant, authoritative work progress, exact-once consumption коробки и terminal cleanup сохраняются; ускорение не разрешает обходить pickup, carry, site commit или проверки work position. Production-duration balancing остаётся отдельным правилом content/balance profile.

## 10. Cancel, failure и retry

- RMB отменяет unconfirmed preview, восстанавливает cursor и не меняет quantity/location.
- Cancel confirmed job до pickup освобождает reservation; коробка остаётся в source location.
- Обычный explicit cancel после pickup возвращает коробку в допустимое world location и не меняет quantity.
- Если assigned resident уже несёт зарезервированную коробку в своём inventory и получает принудительный direct-move command, active relocation/assembly job и незавершённый plan отменяются, все item/worker/position reservations освобождаются, planned target ghost исчезает, синяя inventory-подсветка снимается, а та же quantity-one коробка остаётся в inventory этого resident.
- Forced-move cancellation применяется только пока box ещё не committed `AtSite`; после site commit действует обычная explicit-cancel policy.
- Temporary route/source/target block сохраняет job и reservation для retry.
- Missing/destroyed source или permanently invalid target переводят workflow в typed blocked/failed state.
- Retry не резервирует коробку повторно и не создаёт duplicate entity.

## 11. Packing completed building

`Pack` создаёт packing job. До commit здание остаётся authoritative и функциональным согласно policy. После успешного completion:

- building освобождает footprint;
- создаётся ровно одна BuildingBox;
- completed building больше не функционирует;
- quantity conservation сохраняется.

## 12. Input priority

После UI shielding:

1. active BuildingBox placement: LMB confirm, RMB cancel;
2. `Alt + LMB` world BuildingBox: direct pickup выбранному resident;
3. обычный LMB world BuildingBox: selection/menu;
4. `Unpack`: placement mode;
5. inventory BuildingBox LMB: placement mode;
6. completed building selection;
7. resident movement;
8. excavation terrain input.

Один pointer event создаёт не более одной authoritative command.

## 13. Save/Load

Сохраняются:

- BuildingBox entity/location/reservation;
- relocation job destination и stage;
- assembly plan, footprint/orientation/progress и job stage;
- worker/item/position reservations;
- packing lifecycle;
- migration/version data.

Selection, cursor visibility, interactive preview и hover не сохраняются. После load confirmed planned projection восстанавливается из authoritative job/plan state.

## 14. Диагностика

Diagnostics/Inspector показывают:

- source `StackId`, item id и authoritative location;
- selected source и active placement layer/intent;
- ghost origin, footprint, visibility, support facts, validity и reason;
- target, job/plan id, stage и assigned worker;
- reservation owner;
- carried-by resident;
- commit/cancel/retry/failure state;
- quantity conservation result.

## 15. Инварианты

- одна BuildingBox имеет одно authoritative location;
- preview не резервирует и не мутирует Domain;
- Z0 всегда означает relocation box, Z1–Z3 всегда означают assembly building;
- box/building никогда не подтверждаются без solid support под полным опорным краем;
- flat-surface policy сравнивает реальные высоты terrain support, а не depth layer id;
- resident/creature/loose item не блокируют placement footprint;
- holder-owned box job назначается только holder resident;
- forced direct movement carried-holder-а удаляет active plan/job/reservations, но не перемещает и не дублирует коробку;
- relocation не создаёт completed building и не расходует box;
- assembly расходует box только при completion;
- cancel/retry/save-load не теряют и не дублируют коробки;
- UI не изменяет Inventory/Buildings/Jobs напрямую.

## 16. Acceptance

Обязательны unit, integration, deterministic и Unity Play Mode scenarios:

- world LMB selection/menu без preview;
- `Unpack` и inventory BuildingBox LMB запускают один placement workflow;
- inventory LMB немедленно скрывает system cursor и показывает moving building ghost;
- system cursor скрыт, moving ghost следует pointer по нескольким cells/layers;
- ghost скрывается над unsupported air и снова появляется над supported plane;
- valid green / invalid red preview;
- uneven terrain support отклоняет flat-surface building и не создаёт plan/job;
- Z0 показывает box ghost точно того же размера и с тем же item asset/scale, что фактическая коробка, и создаёт relocation job только на supported cell;
- после Z0 confirmation planned box ghost остаётся видимым до deposit, затем без скачка размера заменяется фактической коробкой;
- Z1–Z3 показывают completed-building ghost и создают assembly plan/job;
- resident и loose item в target cell не блокируют valid preview или relocation deposit; worker использует соседнюю reachable work cell;
- world source подбирается свободным worker и переносится;
- inventory source job получает только holder resident;
- carried reserved box синяя в inventory;
- forced direct movement во время carry отменяет job/plan, освобождает reservations, убирает planned ghost/blue tint и оставляет ту же коробку в holder inventory;
- RMB cancel и invalid LMB не меняют authoritative state;
- cancel/retry/save-load на каждой authoritative стадии;
- relocation сохраняет entity id и quantity;
- после site delivery demo/test unpack дренирует три work iterations и finalize в том же tick, завершает building/job, расходует source box ровно один раз и освобождает reservations/routes;
- assembly расходует box ровно один раз;
- repeated placement/packing и deterministic replay.
