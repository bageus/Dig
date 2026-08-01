# Контекстный ввод, курсоры, выбор объектов и синхронизация панелей

Статус: `QUESTIONNAIRE`.

Tracking issue: [#390](https://github.com/bageus/Dig/issues/390).

Связанные issues: [#113](https://github.com/bageus/Dig/issues/113), [#115](https://github.com/bageus/Dig/issues/115), [#118](https://github.com/bageus/Dig/issues/118), [#386](https://github.com/bageus/Dig/issues/386), [#387](https://github.com/bageus/Dig/issues/387), [#388](https://github.com/bageus/Dig/issues/388), [#398](https://github.com/bageus/Dig/issues/398).

Связанные документы:

- [`resident-hud-selection-and-notifications.md`](resident-hud-selection-and-notifications.md);
- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`excavation-command-execution.md`](excavation-command-execution.md).

## 1. Назначение

Система определяет единый deterministic routing pointer input, selection, cursor feedback и HUD. Один физический input event не создаёт несколько игровых команд.

## 2. Владение состоянием

- Application/input router владеет resolved action для event и target snapshot.
- Domain/Application owners валидируют изменение.
- Presentation владеет hover, cursor animation, selection, active tab и uncommitted preview.
- Cursor/ghost отображает тот же resolved decision, который будет использован при клике.

## 3. Режимы курсора

- доступная копка выбранным гномом — слегка анимированная лопата;
- доступный ordinary pickup generic item — анимированная стрелка вверх;
- доступный `Alt`-pickup BuildingBox — та же анимированная стрелка вверх;
- успешный direct move order — временные анимированные ноги;
- доступная закрытая production package `food`/`weapon`/`tool` — слегка анимированный cursor использования;
- eraser — серый cursor;
- недоступное действие — default cursor и reason code.

Generic item pickup cursor показывается без `Alt`. `Alt` обязателен только для pickup BuildingBox и для direct use предмета, чей `ItemInteractionProfile` это определяет. Production package никогда не показывает generic pickup cursor: её world interaction задаётся explicit content-owned profile.

## 4. BuildingBox selection и unpacking

Обычный LMB по BuildingBox:

1. выбирает коробку;
2. открывает building roster/menu;
3. подсвечивает BuildingBox row;
4. показывает кнопку «Распаковать».

Он не включает placement mode и не создаёт plan.

Кнопка «Распаковать» для world BuildingBox включает placement mode. Обычный LMB по BuildingBox в resident inventory включает этот же mode сразу, без промежуточной кнопки.

В placement mode:

- системный 2D cursor скрывается;
- в мире отображаются 3D ghost и footprint;
- ghost следует pointer только внутри игровой зоны;
- отдельный 2D cursor здания не используется.

## 5. Подтверждение placement

В active placement mode:

- LMB по невалидной позиции не создаёт command и показывает reason;
- LMB по валидной Z0-позиции создаёт выбранный intent: box-placement plan либо building-assembly plan;
- успешное создание plan закрывает interactive placement mode;
- до authoritative pickup source BuildingBox остаётся физически видимой в своей location;
- target продолжает показывать planned ghost: box ghost для переноса либо building ghost/footprint для assembly;
- source box остаётся selected и подсвечивается синим в мире/списке строений либо в resident inventory slot;
- box-placement completion оставляет ту же коробку в target cell;
- assembly plan после delivery автоматически продолжает сборку конечного здания;
- RMB отменяет preview и снимает BuildingBox/building selection;
- отмена не меняет quantity/location коробки.

## 6. Синхронизация выбора

- resident selection открывает resident roster и подсвечивает resident;
- completed building selection открывает building roster и подсвечивает building row;
- BuildingBox selection открывает building roster/menu и подсвечивает box row;
- новый взаимоисключающий selection снимает предыдущий;
- UI click не проходит в мир;
- selection не изменяет authoritative state объекта.

## 7. Input priority

После UI shielding:

1. active placement mode: LMB подтверждает preview, RMB отменяет mode/selection;
2. exact item target разрешается единым `ItemInteractionProfile` resolver-ом до ground/building/movement/excavation fallback;
3. generic/material/tool/weapon ordinary LMB создаёт pickup; food ordinary LMB создаёт pickup, `Alt + LMB` — pickup-then-use;
4. BuildingBox ordinary LMB выбирает коробку, `Alt + LMB` создаёт pickup;
5. closed production package LMB создаёт direct use/break command;
6. object selection обрабатывается раньше excavation stroke;
7. selected resident + reachable free ground создаёт move order;
8. active excavation tool обрабатывает terrain target;
9. один event создаёт не более одной command.

Полная таблица overlap targets остаётся открытой в Q-INPUT-003.

## 8. Статусы

- tunnel/depth/room excavation показывает «Копает»;
- pickup order показывает pickup/logistics status;
- placement mode показывает definition, orientation, validity и reason;
- failed routing не запускает success feedback.

## 9. Инварианты

- world hover, world click, inventory hover и inventory click используют один definition-owned interaction profile;
- hover/click resolver использует exact `StackId`, modifier и availability одного target snapshot;
- production package hover/click используют одну package identity/version и никогда не маршрутизируются в generic pickup;
- UI shielding выполняется до world command;
- LMB по BuildingBox не запускает unpacking;
- placement начинается только через «Распаковать»;
- valid Z0 confirmation создаёт выбранный plan kind и закрывает interactive mode;
- до pickup source physical box и target planned ghost отображаются одновременно без дублирования authoritative entity;
- hidden stale target не остаётся кликабельным;
- selection и highlighted row определяются одним selected entity id;
- Presentation не создаёт Domain state до command;
- RMB очищает preview/selection без расходования коробки.

## 10. Решённые вопросы

- **Q-INPUT-001:** системный 2D cursor скрыт.
- **Q-INPUT-002:** используются 3D ghost и footprint.
- **Q-INPUT-004:** LMB по BuildingBox только выбирает её; unpacking запускается кнопкой.
- **Q-INPUT-007:** valid Z0 confirmation создаёт выбранный box-placement или building-assembly plan и закрывает interactive placement mode.
- **Q-INPUT-008:** после успешного plan source BuildingBox остаётся selected и получает синюю planned-подсветку до pickup/commit/cancel.
- **Q-INPUT-009:** inventory BuildingBox LMB сразу запускает placement mode.
- **Q-INPUT-010:** generic item ordinary LMB подбирает; только BuildingBox pickup требует `Alt`; item target поглощает event до movement/excavation.
- **Q-INPUT-011:** new item behavior определяется `ItemDefinition.ItemInteractionProfile`, без Unity ID/prefix hardcode.

## 11. Открытые вопросы

- **Q-INPUT-003:** полный порядок overlap targets.
- **Q-INPUT-005:** duration анимации ног и rejected move feedback.
- **Q-INPUT-006:** accessibility option и UI scaling cursor animations.

## 12. Acceptance

- world BuildingBox LMB выбирает объект без preview;
- «Распаковать» для world box и обычный inventory LMB включают один 3D ghost/footprint workflow;
- valid Z0 confirmation создаёт выбранный plan kind, закрывает interactive mode и сохраняет синюю planned-подсветку source box;
- до pickup source physical box остаётся видимой, target planned ghost сохраняется;
- box-placement plan не создаёт completed building, assembly plan автоматически продолжает assembly после delivery;
- Play Mode matrix покрывает cursor modes, включая animated use для food/weapon/tool package;
- overlap targets создают одну command;
- ordinary/`Alt` gating совпадает для hover и click;
- generic item first LMB создаёт pickup либо typed rejection и не уходит в ground action;
- новый item profile подключается к hover/click без изменения Unity router;
- food/weapon/tool package LMB создаёт ровно один direct-use command, а BuildingBox сохраняет обычный selection/unpack workflow;
- world/HUD/management selection дают одинаковую вкладку и highlight;
- ghost существует только в игровой зоне;
- RMB, stale target и failed command очищают feedback;
- input deterministic после restart/save-load session.
