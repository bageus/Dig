# Контекстный ввод, курсоры, выбор объектов и синхронизация панелей

Статус: `QUESTIONNAIRE`.

Tracking issue: [#390](https://github.com/bageus/Dig/issues/390).

Связанные issues: [#113](https://github.com/bageus/Dig/issues/113), [#115](https://github.com/bageus/Dig/issues/115), [#118](https://github.com/bageus/Dig/issues/118), [#387](https://github.com/bageus/Dig/issues/387), [#388](https://github.com/bageus/Dig/issues/388).

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
- Cursor не является доказательством доступности команды: он отображает тот же resolved decision, который будет использован при клике.

## 3. Подтверждённые режимы курсора

- доступная копка выбранным гномом — слегка анимированная лопата;
- доступный `Alt`-подбор предмета или BuildingBox — анимированная стрелка вверх;
- успешный direct move order — временные анимированные ноги;
- eraser — серый курсор;
- BuildingBox placement — модель/ghost соответствующего здания в игровой зоне под pointer target;
- недоступное действие — default cursor и диагностируемый reason при попытке команды.

Pickup cursor без `Alt` запрещён. Cursor не должен оставаться после завершения feedback window или выхода из режима.

## 4. Подтверждённая синхронизация выбора

- выбор resident из мира или HUD открывает resident roster и подсвечивает того же resident;
- выбор completed building из мира, HUD или management открывает building roster и подсвечивает строку;
- выбор BuildingBox также открывает building roster и подсвечивает BuildingBox row;
- новый взаимоисключающий выбор снимает предыдущий;
- UI click не проходит в мир;
- selection не изменяет authoritative position или state объекта.

## 5. Подтверждённые приоритеты

Из существующих спецификаций подтверждены:

1. active placement mode обрабатывает подтверждение или отмену preview;
2. `Alt`-interaction не должен превращаться в обычный pickup без реально зажатого `Alt`;
3. BuildingBox target обрабатывается раньше excavation ground stroke;
4. selected resident + reachable ground создаёт move order;
5. active excavation tool обрабатывает ground target;
6. один event создаёт не более одной command.

Полная таблица overlap-target priorities остаётся открытой в Q-INPUT-003.

## 6. Статусы действий

- копка тоннеля, глубины и комнаты показывает typed status «Копает»;
- pickup order показывает typed pickup/logistics status;
- placement mode показывает definition, orientation, validity и reason code;
- failed routing не запускает success cursor feedback.

## 7. Инварианты

- cursor resolver и command router используют одну target classification;
- UI shielding выполняется до world raycast command;
- не существует hidden target, который остаётся кликабельным через stale collider;
- выбор объекта и highlighted roster row определяются одним selected entity id;
- Presentation mode не создаёт Domain state до подтверждённой команды.

## 8. Открытые вопросы

- **Q-INPUT-001:** placement заменяет системный 2D cursor моделью здания или системный cursor скрывается, а модель существует только как 3D ghost?
- **Q-INPUT-002:** нужны одновременно model ghost и отдельный footprint overlay или один visual?
- **Q-INPUT-003:** полный порядок при overlap: BuildingBox/item, building, resident, creature, excavation target, free ground.
- **Q-INPUT-004:** повторный LMB по уже выбранной BuildingBox запускает placement, меняет selection или подтверждает preview?
- **Q-INPUT-005:** точная duration анимации ног и поведение при rejected move command.
- **Q-INPUT-006:** accessibility option для отключения cursor animation и правила UI scaling.

До ответов эти пункты не должны закрепляться новой реализацией как окончательное правило.

## 9. Acceptance после закрытия опросника

- Play Mode matrix для всех cursor modes;
- overlap targets создают ровно одну ожидаемую command;
- `Alt` gating совпадает для hover и click;
- selection paths world/HUD/management дают одинаковую вкладку и highlight;
- placement visual следует pointer только внутри игровой зоны;
- cancel, stale target и failed command очищают feedback;
- input remains deterministic after save/load session restart.
