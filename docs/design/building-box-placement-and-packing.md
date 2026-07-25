# Коробки зданий, размещение, сборка и упаковка

Статус: `QUESTIONNAIRE`.

Tracking issues: [#118](https://github.com/bageus/Dig/issues/118), [#390](https://github.com/bageus/Dig/issues/390).

## 1. Назначение

Все размещаемые здания используют единый физический lifecycle коробки. Здание производится как предмет-коробка, выбирается игроком, переводится отдельной кнопкой «Распаковать» в placement mode, размещается как призрачный план, доставляется свободным гномом на площадку, собирается и позднее может быть разобрано обратно в коробку.

Эта модель заменяет для соответствующего `BuildingDefinition` прямую доставку полного списка строительных материалов на площадку. Одновременное применение двух construction policies к одному зданию запрещено.

## 2. Владение состоянием

- `InventoryState` владеет коробкой, количеством, местоположением и reservations.
- `BuildingsState` владеет планом, footprint, orientation, progress, completed state и durability.
- `Jobs` владеет pickup, delivery, assembly и packing lifecycle, исполнителем и позициями.
- `Production` создаёт коробку как обычный output рецепта.
- `Navigation` проверяет достижимость коробки, площадки и рабочих позиций.
- `Presentation` владеет selection, меню коробки, preview и локальным placement mode.

Призрачная модель и footprint не являются авторитетным зданием.

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
- display name и локализованное название не используются как ID;
- рецепт коробки содержит стоимость производства здания.

## 4. Selection и нижняя контекстная панель

В один момент активен ровно один selection/context mode.

### Нет выбранного объекта

Показывается меню копки: тоннель, глубина, шаблоны комнат и eraser незавершённых designations/jobs. Уже выкопанный мир eraser не восстанавливает.

### Выбран гном

Показывается его личный инвентарь.

### Выбрано построенное здание

Показываются функции здания и кнопка упаковки, если definition допускает packing.

### Выбрана BuildingBox

Обычный ЛКМ по world BuildingBox:

1. выбирает коробку;
2. снимает несовместимый предыдущий selection;
3. открывает вкладку/меню строений;
4. подсвечивает строку этой BuildingBox;
5. показывает кнопку «Распаковать».

Обычный ЛКМ сам по себе **не** включает placement mode и не создаёт plan.

`Alt + ЛКМ` по world BuildingBox при выбранном resident не выбирает коробку для размещения, а создаёт pickup order по общей item interaction policy.

ПКМ в любой момент снимает выбор BuildingBox/строения и отменяет активный placement preview без расходования коробки.

## 5. Запуск unpacking/placement mode

Placement mode включается только явным действием пользователя в меню выбранной BuildingBox — кнопкой «Распаковать».

Перед входом Application/Presentation проверяет, что:

- выбранная коробка всё ещё существует;
- это BuildingBox с известным `BuildingDefinitionId`;
- коробка не зарезервирована несовместимой операцией;
- пользователь не переключил selection на другой объект.

Нажатие «Распаковать» не меняет Inventory quantity/location и не резервирует коробку. Оно создаёт только локальный preview mode.

Поведение коробки из resident inventory остаётся открытым в Q-BBOX-002: текущий ответ подтверждает world BuildingBox workflow, но не определяет отдельный inventory activation path.

## 6. Preview и подтверждение

В placement mode:

- системный 2D cursor скрыт;
- под pointer target отображается только 3D ghost здания и его footprint;
- отдельный 2D cursor здания не используется;
- ghost показывается только внутри игровой зоны;
- валидность вычисляется через World, Buildings и Navigation snapshots;
- Presentation не изменяет Domain до команды;
- ЛКМ по валидной позиции создаёт `BuildingPlan`/призрак строения;
- клик по невалидной позиции ничего не создаёт и показывает reason code;
- ПКМ отменяет preview и selection, коробка остаётся на прежнем authoritative location.

Пользователь отдельно указал: «ЛКМ на Z0 размещает коробку». Точная транзакция и отличие от обычного building plan пока не определены и вынесены в Q-BBOX-001; реализация не должна угадывать это правило.

Команда подтверждения building plan атомарно:

1. повторно валидирует footprint, orientation и work positions;
2. проверяет существование и доступность выбранной коробки;
3. резервирует коробку за plan;
4. создаёт призрачный `BuildingPlan`;
5. публикует событие создания плана.

Одна коробка не может быть зарезервирована двумя планами.

## 7. Доставка и сборка

После создания plan он попадает в общий список работ.

Свободный подходящий гном:

1. получает job;
2. идёт к зарезервированной коробке;
3. подбирает её через Inventory transaction;
4. несёт к площадке;
5. фиксирует коробку в site location;
6. выполняет assembly work;
7. завершает здание одной подтверждённой операцией.

Если коробка уже находится в инвентаре назначенного гнома, внешний pickup может быть пропущен, но reservation и commit остаются обязательными.

Коробка не существует одновременно как предмет у гнома, предмет на площадке и завершённое здание. При успешном завершении она расходуется ровно один раз.

## 8. Отмена и ошибки

До доставки коробки отмена плана освобождает reservation, коробка остаётся на прежнем месте.

После доставки, но до завершения сборки отмена:

- закрывает связанные jobs;
- освобождает worker/position claims;
- возвращает одну коробку в допустимое site location;
- не создаёт самостоятельное частично построенное здание;
- не изменяет количество.

Retry не резервирует коробку повторно. Уничтоженная или недоступная коробка переводит plan в диагностируемое blocked/failed состояние.

## 9. Упаковка построенного здания

Кнопка упаковки в панели completed building создаёт `PackBuilding` command.

После проверки:

- здание помечается как ожидающее разборки;
- создаётся общий packing job;
- гном приходит к рабочей позиции и выполняет разборку;
- functional places, active orders и reservations завершаются по явной policy;
- после commit здание освобождает footprint;
- в site location создаётся ровно одна коробка соответствующего `BuildingBoxItemId`.

До commit здание остаётся авторитетным объектом и коробка не существует. После commit здание больше не функционирует.

## 10. Контекстный ввод

После UI shielding применяется подтверждённый порядок:

1. active placement mode: ЛКМ подтверждает preview или выполняет отдельно утверждённое Z0-действие; ПКМ отменяет mode/selection;
2. `Alt + ЛКМ` по world BuildingBox создаёт pickup order выбранному resident;
3. обычный ЛКМ по world BuildingBox выбирает коробку и открывает её building menu;
4. кнопка «Распаковать» в UI включает placement mode;
5. ЛКМ по completed building выбирает его и открывает функции;
6. selected resident + reachable free ground создаёт move order;
7. excavation tools обрабатывают terrain targets только после object targets.

Один pointer event создаёт не более одной команды.

## 11. Save/Load

Сохраняются:

- коробки как Inventory stacks/locations;
- box reservation и owning plan id;
- plan definition, orientation, footprint и progress;
- site commit state;
- active delivery/assembly/packing jobs;
- work/position/item reservations;
- building functional state до начала packing;
- migration/version data.

Selection, открытое меню, preview и cursor не входят в authoritative save.

## 12. Диагностика

Inspector показывает:

- BuildingDefinitionId и BuildingBoxItemId;
- source box location;
- selected entity и active menu mode;
- plan и reservation owner;
- pickup/delivery/assembly/packing stage;
- выбранного worker;
- footprint/work position validation;
- commit state коробки;
- причины blocked/cancel/failure;
- quantity conservation report.

## 13. Инварианты

- одна коробка имеет одно authoritative location;
- обычный LMB selection не создаёт plan и не резервирует коробку;
- placement начинается только через кнопку «Распаковать»;
- одна коробка принадлежит не более чем одному активному plan;
- preview не резервирует и не расходует предмет;
- plan не создаётся без доступной коробки;
- completed building не хранит скрытый дубликат коробки;
- packing создаёт ровно одну коробку;
- cancel/retry/save-load не теряют и не дублируют коробки;
- UI не изменяет Buildings или Inventory напрямую.

## 14. Открытые вопросы

- **Q-BBOX-001:** что точно означает «ЛКМ на Z0 размещает коробку»: перенос той же world box в выбранную Z0-клетку, создание отдельного delivery target, отмена unpacking или другой workflow? Остаётся ли mode активным после этого?
- **Q-BBOX-002:** как запускается unpacking для BuildingBox, находящейся в resident inventory: выбор stack + та же кнопка, перенос в building roster или другой UI path?
- **Q-BBOX-003:** допускается ли building plan на Z0, либо на Z0 всегда разрешено только размещение самой коробки?
- **Q-BBOX-004:** после успешного создания plan selection остаётся на plan, переключается на коробку или полностью снимается?

## 15. Тесты

Обязательны:

- world box LMB выбирает box, но не включает placement;
- кнопка «Распаковать» включает 3D ghost/footprint и скрывает 2D cursor;
- invalid footprint без reservation;
- ПКМ отменяет preview и selection;
- конкурирующие планы за одну коробку;
- доставка другим свободным гномом;
- cancel до и после site commit;
- missing box, unreachable source и blocked work position;
- сборка ровно один раз;
- упаковка и повторное размещение;
- save/load на каждой authoritative стадии;
- deterministic replay и quantity conservation;
- Unity Play Mode для selection, menu, preview, shielding и input priority.
