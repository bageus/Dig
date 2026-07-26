# Коробки зданий, размещение, сборка и упаковка

Статус: `QUESTIONNAIRE`.

Tracking issues: [#118](https://github.com/bageus/Dig/issues/118), [#390](https://github.com/bageus/Dig/issues/390), [#398](https://github.com/bageus/Dig/issues/398).

## 1. Назначение

Все размещаемые здания используют единый физический lifecycle коробки. Здание производится как предмет-коробка, выбирается игроком, переводится отдельной кнопкой «Распаковать» в placement mode, размещается через подтверждённый plan workflow, доставляется свободным гномом, собирается и позднее может быть разобрано обратно в коробку.

Эта модель заменяет для соответствующего `BuildingDefinition` прямую доставку полного списка строительных материалов. Одновременное применение двух construction policies к одному зданию запрещено.

## 2. Владение состоянием

- `InventoryState` владеет коробкой, количеством, местоположением и reservations.
- `BuildingsState` владеет plans, footprint, orientation, progress, completed state и durability.
- `Jobs` владеет pickup, delivery, assembly и packing lifecycle, исполнителями и рабочими позициями.
- `Production` создаёт коробку как обычный output рецепта.
- `Navigation` проверяет достижимость коробки, площадки и рабочих позиций.
- `Presentation` владеет selection, меню коробки, preview и локальным placement mode.

Ghost и footprint не являются авторитетным зданием или коробкой.

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

`BuildingBoxItemId` обозначает обычный предмет Inventory:

- одна коробка представляет одно здание;
- коробки не складываются друг с другом;
- коробка не является вложенным контейнером материалов;
- display name не используется как ID;
- рецепт коробки содержит стоимость производства здания.

## 4. Selection и меню

### Обычный ЛКМ по world BuildingBox

1. выбирает коробку;
2. снимает несовместимый предыдущий selection;
3. открывает вкладку/меню строений;
4. подсвечивает строку этой BuildingBox;
5. показывает кнопку «Распаковать».

Обычный ЛКМ сам по себе не включает placement mode, не создаёт plan и не резервирует коробку.

### `Alt + ЛКМ`

При выбранном resident создаёт pickup order. Он не выбирает коробку для размещения и не запускает preview.

### ПКМ

В любой момент отменяет активный preview и снимает selection BuildingBox/строения. Quantity и authoritative location коробки не меняются.

## 5. Запуск unpacking/placement mode

Placement mode включается только кнопкой «Распаковать» в меню выбранной BuildingBox.

Перед входом проверяется, что:

- коробка существует;
- её `BuildingDefinitionId` известен;
- она не зарезервирована несовместимой операцией;
- selection всё ещё указывает на ту же коробку.

Нажатие кнопки не меняет Inventory quantity/location и не создаёт reservation. Оно создаёт только локальный preview mode.

Для BuildingBox в resident inventory отдельная кнопка не требуется: обычный LMB по занятому inventory slot сразу включает тот же placement mode с 3D ghost конечного здания и footprint. Коробка остаётся в inventory до успешного authoritative command.

## 6. Preview

В placement mode:

- системный 2D cursor скрыт;
- под pointer отображаются 3D ghost и footprint;
- ghost существует только внутри игровой зоны;
- validity вычисляется через World, Buildings и Navigation snapshots;
- Presentation не изменяет Domain до команды;
- клик по невалидной позиции ничего не создаёт и показывает reason code;
- ПКМ отменяет preview и selection.

## 7. Подтверждение на Z0

ЛКМ по валидной позиции Z0 создаёт `BuildingPlan`, назначением которого является размещение самой BuildingBox в выбранной клетке.

Этот plan:

- не является plan строительства или сборки конечного здания;
- не создаёт completed building;
- не расходует коробку как материал здания;
- резервирует конкретную BuildingBox для доставки/размещения;
- после выполнения оставляет ту же коробку обычным world BuildingBox в целевой клетке;
- закрывает placement mode сразу после успешного создания plan.

В документации это называется **BuildingBox placement plan**. Точное имя enum/type в коде должно следовать существующей архитектуре, но observable contract остаётся указанным выше.

Команда подтверждения атомарно:

1. повторно валидирует целевую Z0-клетку;
2. проверяет существование и доступность выбранной коробки;
3. резервирует коробку за plan;
4. создаёт BuildingBox placement plan;
5. публикует событие создания plan;
6. закрывает preview mode.

Одна коробка не может быть зарезервирована двумя plans.

## 8. Доставка BuildingBox placement plan

Свободный подходящий гном:

1. получает обычный delivery job;
2. идёт к зарезервированной коробке;
3. подбирает её через Inventory transaction;
4. несёт к целевой Z0-клетке;
5. размещает коробку в world location;
6. завершает plan.

После commit коробка остаётся BuildingBox и может быть выбрана или подобрана. Дальнейший переход от доставленной коробки к сборке конечного здания остаётся отдельным открытым workflow Q-BBOX-006.

## 9. Поверхность и будущая сборка здания

Для текущего BuildingBox workflow валидной поверхностью подтверждения является только Z0. Поверхности других depth layers и любые специальные direct-assembly surfaces не разрешены.

Подтверждение placement на Z0 создаёт BuildingBox placement plan из раздела 7, а не completed building. Как именно доставленная коробка затем переводится в building assembly workflow, должно быть отдельно подтверждено в Q-BBOX-006. До этого direct building assembly не должен молча запускаться из placement confirmation.

## 10. Отмена и ошибки

До доставки отмена plan освобождает reservation, коробка остаётся на прежнем месте.

После pickup, но до commit, отмена:

- закрывает связанные jobs;
- освобождает worker/position claims;
- возвращает одну коробку в допустимое world location;
- не меняет quantity.

Retry не резервирует коробку повторно. Уничтоженная или недоступная коробка переводит plan в диагностируемое blocked/failed состояние.

## 11. Упаковка completed building

Кнопка упаковки создаёт `PackBuilding` command.

После проверки:

- здание помечается как ожидающее разборки;
- создаётся общий packing job;
- гном выполняет разборку;
- functional places, active orders и reservations завершаются по policy;
- после commit здание освобождает footprint;
- в site location создаётся ровно одна BuildingBox.

До commit здание остаётся авторитетным объектом и коробка не существует. После commit здание больше не функционирует.

## 12. Контекстный ввод

После UI shielding применяется порядок:

1. active placement mode: ЛКМ подтверждает preview, ПКМ отменяет mode/selection;
2. `Alt + ЛКМ` по world BuildingBox создаёт pickup order выбранному resident;
3. обычный ЛКМ по world BuildingBox выбирает коробку и открывает building menu;
4. кнопка «Распаковать» включает placement mode;
5. ЛКМ по completed building выбирает его и открывает функции;
6. selected resident + reachable free ground создаёт move order;
7. excavation tools обрабатывают terrain targets после object targets.

Один pointer event создаёт не более одной команды.

## 13. Save/Load

Сохраняются:

- BuildingBox Inventory location и quantity;
- reservation и owning plan id;
- plan kind, target, footprint/orientation при необходимости и progress;
- active delivery/assembly/packing jobs;
- work/position/item reservations;
- migration/version data.

Selection, меню, cursor и uncommitted preview не сохраняются.

## 14. Диагностика

Inspector показывает:

- BuildingDefinitionId и BuildingBoxItemId;
- source и target location;
- selected entity и active menu mode;
- plan kind: box placement или building assembly;
- reservation owner;
- pickup/delivery/assembly/packing stage;
- worker;
- validation result;
- commit state коробки;
- blocked/cancel/failure reason;
- quantity conservation report.

## 15. Инварианты

- одна коробка имеет одно authoritative location;
- обычный ЛКМ selection не создаёт plan;
- placement начинается только через кнопку «Распаковать»;
- preview не резервирует и не расходует коробку;
- одна коробка принадлежит не более чем одному active plan;
- Z0 BuildingBox placement plan не создаёт completed building;
- успешный Z0 plan сохраняет коробку как BuildingBox в target cell;
- building assembly расходует коробку только при completion;
- cancel/retry/save-load не теряют и не дублируют коробки;
- UI не изменяет Buildings или Inventory напрямую.

## 16. Решённые вопросы

- **Q-BBOX-001:** ЛКМ на валидной Z0-позиции создаёт BuildingBox placement plan, а не plan конечного здания.
- **Q-BBOX-002:** обычный LMB по BuildingBox в resident inventory сразу включает placement mode с 3D ghost/footprint.
- **Q-BBOX-003:** Z0 confirmation размещает коробку через plan; после успешного создания plan placement mode закрывается.
- **Q-BBOX-004:** после успешного создания plan исходная коробка остаётся selected; world visual, building row или inventory slot подсвечиваются синим как объект запланированного действия.
- **Q-BBOX-005:** BuildingBox placement разрешён только на Z0; другие поверхности не принимают confirmation.

## 17. Открытые вопросы

- **Q-BBOX-006:** после доставки BuildingBox placement plan коробка остаётся ожидающим отдельной команды объектом или тот же workflow автоматически запускает сборку конечного здания?

## 18. Тесты

Обязательны:

- world box LMB выбирает box, но не включает placement;
- inventory BuildingBox LMB сразу включает тот же 3D ghost/footprint mode;
- кнопка «Распаковать» для world box включает 3D ghost/footprint и скрывает 2D cursor;
- valid Z0 click создаёт BuildingBox placement plan, закрывает mode и оставляет source box selected с синей planned-подсветкой;
- Z0 plan после delivery оставляет box, а не completed building;
- invalid target не создаёт reservation;
- ПКМ отменяет preview и selection;
- конкурирующие plans за одну коробку;
- доставка другим свободным гномом;
- cancel до и после pickup;
- missing box, unreachable source и blocked target;
- building assembly расходует box ровно один раз;
- packing и повторное размещение;
- save/load на каждой authoritative стадии;
- deterministic replay и quantity conservation;
- Unity Play Mode для selection, menu, preview, confirmation, shielding и input priority.
