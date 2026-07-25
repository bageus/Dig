# Контекстный ввод, курсоры, выбор объектов и синхронизация панелей

Статус: `QUESTIONNAIRE`.

Tracking issue: [#390](https://github.com/bageus/Dig/issues/390).

Связанные issues: [#113](https://github.com/bageus/Dig/issues/113), [#115](https://github.com/bageus/Dig/issues/115), [#118](https://github.com/bageus/Dig/issues/118), [#386](https://github.com/bageus/Dig/issues/386), [#387](https://github.com/bageus/Dig/issues/387), [#388](https://github.com/bageus/Dig/issues/388).

Связанные документы:

- [`resident-hud-selection-and-notifications.md`](resident-hud-selection-and-notifications.md);
- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`excavation-command-execution.md`](excavation-command-execution.md).

## 1. Назначение

Система определяет единый deterministic routing для pointer input, selection, cursor feedback и переключения HUD. Один физический input event не может одновременно создать несколько игровых команд.

## 2. Владение состоянием

- Application/input router владеет решением, какая команда соответствует событию и target snapshot.
- Domain/Application owners валидируют фактическое изменение.
- Presentation владеет hover, cursor animation, selection highlight, active tab и uncommitted placement preview.
- Cursor/ghost отображает тот же resolved decision, который будет использован при клике.

## 3. Подтверждённые режимы курсора

- доступная копка выбранным гномом — слегка анимированная лопата;
- доступный `Alt`-подбор предмета или BuildingBox — анимированная стрелка вверх;
- успешный direct move order — временные анимированные ноги;
- eraser — серый курсор;
- недоступное действие — default cursor и диагностируемый reason при попытке команды.

Pickup cursor без `Alt` запрещён. Feedback cursor не должен оставаться после завершения окна анимации или выхода из режима.

### BuildingBox placement

После выбора BuildingBox обычным ЛКМ пользователь нажимает кнопку «Распаковать» в меню строения. Только после этого включается placement mode.

В placement mode:

- системный 2D cursor скрыт;
- в мире отображается только 3D ghost здания и footprint;
- ghost следует pointer target только внутри игровой зоны;
- отдельный 2D cursor здания не используется;
- ЛКМ по валидному месту создаёт призрачный BuildingPlan;
- ПКМ в любой момент отменяет preview и снимает выбор BuildingBox/строения;
- обычный LMB по коробке без нажатия «Распаковать» только выбирает коробку и не создаёт preview.

Отдельное действие ЛКМ на Z0 пока требует уточнения в Q-INPUT-007 и Q-BBOX-001.

## 4. Подтверждённая синхронизация выбора

- выбор resident из мира или HUD открывает resident roster и подсвечивает того же resident;
- выбор completed building из мира, HUD или management открывает building roster и подсвечивает строку;
- обычный LMB по BuildingBox выбирает её, открывает building roster/menu и подсвечивает BuildingBox row;
- selection BuildingBox показывает кнопку «Распаковать»;
- новый взаимоисключающий выбор снимает предыдущий;
- ПКМ снимает building/BuildingBox selection и active placement preview;
- UI click не проходит в мир;
- selection не изменяет authoritative position или state объекта.

## 5. Подтверждённые приоритеты

После UI shielding:

1. active placement mode: ЛКМ подтверждает preview или выполняет отдельно утверждённое Z0-действие; ПКМ отменяет mode/selection;
2. `Alt + ЛКМ` по BuildingBox/item создаёт pickup order только при реально зажатом `Alt`;
3. обычный LMB по BuildingBox выбирает коробку и открывает menu;
4. object selection обрабатывается раньше excavation ground stroke;
5. selected resident + reachable free ground создаёт move order;
6. active excavation tool обрабатывает terrain target;
7. один event создаёт не более одной command.

Полная таблица overlap-target priorities остаётся открытой в Q-INPUT-003.

## 6. Статусы действий

- копка тоннеля, глубины и комнаты показывает typed status «Копает»;
- pickup order показывает typed pickup/logistics status;
- placement mode показывает definition, orientation, validity и reason code;
- failed routing не запускает success cursor feedback.

## 7. Инварианты

- cursor/ghost resolver и command router используют одну target classification;
- UI shielding выполняется до world raycast command;
- обычный LMB по BuildingBox не запускает unpacking;
- placement mode начинается только после UI action «Распаковать»;
- не существует hidden target, который остаётся кликабельным через stale collider;
- выбор объекта и highlighted roster row определяются одним selected entity id;
- Presentation mode не создаёт Domain state до подтверждённой команды;
- ПКМ очищает preview/selection без расходования коробки.

## 8. Решённые вопросы

- **Q-INPUT-001:** системный 2D cursor скрывается; placement представлен только 3D ghost/footprint.
- **Q-INPUT-002:** используются 3D model ghost и footprint; отдельный 2D cursor здания не нужен.
- **Q-INPUT-004:** обычный LMB по BuildingBox выбирает её; placement запускается отдельной кнопкой «Распаковать».

## 9. Открытые вопросы

- **Q-INPUT-003:** полный порядок overlap targets: BuildingBox/item, building, resident, creature, excavation target, free ground.
- **Q-INPUT-005:** точная duration анимации ног и поведение при rejected move command.
- **Q-INPUT-006:** accessibility option для отключения cursor animation и правила UI scaling.
- **Q-INPUT-007:** что именно делает LMB на Z0 в placement mode: переносит исходную коробку, создаёт delivery target или применяет другую операцию; остаётся ли mode активным?
- **Q-INPUT-008:** после успешного создания BuildingPlan selection переходит на plan, остаётся на BuildingBox или снимается?

До ответов эти пункты не должны закрепляться новой реализацией как окончательные правила.

## 10. Acceptance после закрытия опросника

- world BuildingBox LMB выбирает объект и открывает menu без placement;
- «Распаковать» включает только 3D ghost/footprint;
- Play Mode matrix покрывает все cursor modes;
- overlap targets создают ровно одну ожидаемую command;
- `Alt` gating совпадает для hover и click;
- selection paths world/HUD/management дают одинаковую вкладку и highlight;
- placement visual следует pointer только внутри игровой зоны;
- RMB, stale target и failed command очищают feedback;
- input remains deterministic after save/load session restart.
