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

Обычный ЛКМ по занятому BuildingBox slot немедленно запускает тот же placement mode. Отдельная кнопка `Unpack` не требуется.

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

## 6. Intent определяется depth layer

Отдельного UI selector или modifier для выбора intent нет.

### Z0: relocation BuildingBox

- ghost автоматически меняется на модель BuildingBox;
- footprint равен одной target cell;
- valid confirmation создаёт relocation/hauling job для той же коробки;
- completed building и assembly plan не создаются;
- после delivery та же BuildingBox entity получает world location target Z0 cell.

### Z1–Z3: unpack/assembly building

- ghost показывает конечную модель здания и footprint;
- valid confirmation создаёт BuildingBox assembly plan и job;
- worker доставляет коробку к site, выполняет unpack/assembly и расходует коробку ровно один раз при completion;
- после completion ghost заменяется completed-building visual.

Это depth rule является единственным owner выбора plan kind и закрывает прежний вопрос `Move as box` versus `Unpack as building`.

## 7. Placement validity

Общие blocking conditions:

- target/footprint выходит за world bounds;
- terrain target/footprint solid;
- cell unexplored;
- footprint пересекает active building или building plan;
- отсутствует reachable target/work position;
- source box отсутствует, зарезервирована несовместимой операцией либо больше не quantity-one unit.

Не блокируют placement:

- resident или creature в target cell;
- loose world item, включая другую не-authoritative визуальную проекцию предмета;
- presentation overlays и cursor markers.

Для Z0 relocation target также должен быть открытой explored reachable cell. Для Z1–Z3 assembly применяется footprint/surface policy соответствующего `BuildingDefinition`.

ЛКМ по invalid preview не создаёт reservation, plan или job и показывает reason code.

## 8. Confirmation и planned projection

Успешный ЛКМ атомарно:

1. повторно валидирует source, layer-derived intent и target;
2. резервирует конкретную BuildingBox за одним job/plan;
3. создаёт relocation job либо assembly plan/job;
4. публикует события;
5. закрывает interactive placement mode и восстанавливает системный cursor;
6. сохраняет target planned ghost до authoritative delivery/assembly commit;
7. сохраняет source selection/planned indication.

До фактического pickup:

- world source box остаётся физически видимой в своей authoritative cell;
- inventory source box остаётся в своём resident slot;
- target показывает planned ghost результата;
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

Коробка перемещается в target Z0 world cell, reservation/job завершаются, BuildingBox остаётся доступной для последующего выбора и `Unpack`.

### Assembly completion

После delivery автоматически выполняется unpack/assembly. Коробка расходуется только при успешном completion здания.

## 10. Cancel, failure и retry

- RMB отменяет unconfirmed preview, восстанавливает cursor и не меняет quantity/location.
- Cancel confirmed job до pickup освобождает reservation; коробка остаётся в source location.
- Cancel после pickup возвращает коробку в допустимое world location и не меняет quantity.
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
- ghost origin, footprint, validity и reason;
- target, job/plan id, stage и assigned worker;
- reservation owner;
- carried-by resident;
- commit/cancel/retry/failure state;
- quantity conservation result.

## 15. Инварианты

- одна BuildingBox имеет одно authoritative location;
- preview не резервирует и не мутирует Domain;
- Z0 всегда означает relocation box, Z1–Z3 всегда означают assembly building;
- resident/creature/loose item не блокируют placement footprint;
- holder-owned box job назначается только holder resident;
- relocation не создаёт completed building и не расходует box;
- assembly расходует box только при completion;
- cancel/retry/save-load не теряют и не дублируют коробки;
- UI не изменяет Inventory/Buildings/Jobs напрямую.

## 16. Acceptance

Обязательны unit, integration, deterministic и Unity Play Mode scenarios:

- world LMB selection/menu без preview;
- `Unpack` и inventory BuildingBox LMB запускают один placement workflow;
- system cursor скрыт, moving ghost следует pointer по нескольким cells/layers;
- valid green / invalid red preview;
- Z0 показывает box ghost и создаёт relocation job;
- Z1–Z3 показывают completed-building ghost и создают assembly plan/job;
- resident и loose item в target cell не блокируют valid preview;
- world source подбирается свободным worker и переносится;
- inventory source job получает только holder resident;
- carried reserved box синяя в inventory;
- RMB cancel и invalid LMB не меняют authoritative state;
- cancel/retry/save-load на каждой authoritative стадии;
- relocation сохраняет entity id и quantity;
- assembly расходует box ровно один раз;
- repeated placement/packing и deterministic replay.
