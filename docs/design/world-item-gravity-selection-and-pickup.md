# Мировые предметы: падение, видимость, выбор и подбор

Статус: `QUESTIONNAIRE`.

Tracking issue: [#387](https://github.com/bageus/Dig/issues/387).

Связанные issues: [#7](https://github.com/bageus/Dig/issues/7), [#64](https://github.com/bageus/Dig/issues/64), [#67](https://github.com/bageus/Dig/issues/67), [#113](https://github.com/bageus/Dig/issues/113), [#118](https://github.com/bageus/Dig/issues/118), [#390](https://github.com/bageus/Dig/issues/390), [#396](https://github.com/bageus/Dig/issues/396).

Связанные документы:

- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`entity-fall-knockback-and-vertical-shafts.md`](entity-fall-knockback-and-vertical-shafts.md).

## 1. Назначение

Система задаёт lifecycle свободного world item после появления, стабилизации на опоре, отображения, selection и pickup. BuildingBox использует тот же authoritative Inventory location и дополнительные BuildingBox interactions.

Общая будущая система падения residents/enemies и combat knockback описывается отдельно в `entity-fall-knockback-and-vertical-shafts.md`.

## 2. Владение состоянием

- Inventory владеет ItemStack, quantity, reservations и `ItemLocation`.
- World/Navigation предоставляет immutable support/traversability snapshot.
- Jobs/Agents владеют pickup action и worker assignment.
- Presentation владеет visual offset, animation, collider, selection highlight и hover feedback, но не местоположением предмета.

## 3. Подтверждённый workflow стабилизации предмета

1. Свободный, не удерживаемый и не зарезервированный world item проверяет опору.
2. В открытом vertical tunnel item должен оказаться на первой допустимой плоской поверхности.
3. Inventory атомарно изменяет world cell ровно один раз.
4. Renderer обновляет visual в той же projected cell над floor, а collider следует visual.
5. Item остаётся видимым и доступным для raycast.

Коробки и обычные предметы используют общую item gravity/support policy, если item definition не задаёт исключение.

Точное время и визуальный процесс падения пока не утверждены. Demo-коробка костра на текущем этапе сразу находится в нижней пещере и не используется как демонстрация падения.

## 4. Подтверждённый pickup contract

- BuildingBox pickup требует выбранного resident и `Alt + ЛКМ`;
- обычный ЛКМ не создаёт pickup order;
- pickup cursor появляется только при `Alt`, доступном quantity и допустимом target;
- command использует фактический ItemId выбранной коробки, а не hardcoded тип;
- worker должен прийти в точную logical XYZ target cell;
- quantity/location/reservation меняются только authoritative Inventory transaction.

## 5. BuildingBox selection и список строений

Обычный LMB по world BuildingBox:

1. выбирает BuildingBox;
2. снимает несовместимый resident/building selection;
3. открывает building roster/menu;
4. подсвечивает строку выбранной коробки;
5. показывает кнопку «Распаковать».

Обычный LMB не включает placement mode. Placement начинается только после кнопки «Распаковать» согласно [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md).

BuildingBox остаётся Inventory item. Его строка в building roster является Presentation/read-model classification и не превращает коробку в completed `BuildingState`.

## 6. Видимость и spatial consistency

- visual не может быть скрыт floor geometry при наличии selectable logical item;
- collider не может оставаться в старой клетке после стабилизации/падения;
- visual front offset является производным Presentation параметром;
- root transform не должен повторно применять terrain rotation к уже спроецированной world position;
- при rebuild visual полностью восстанавливается из Inventory snapshot;
- selection raycast и отображаемая позиция обязаны указывать на одну logical cell.

## 7. Инварианты

- один stack имеет ровно одно authoritative location;
- падение/стабилизация не меняет quantity;
- reserved/held/site item не падает без явной policy;
- hidden-but-clickable и visible-but-stale states запрещены;
- pickup hover и pickup command используют одинаковую target availability;
- BuildingBox не проектируется одновременно как generic item и отдельный duplicate stack visual;
- обычный LMB selection не создаёт pickup order и не запускает placement.

## 8. Решённые вопросы

- **Q-ITEM-001:** обычный LMB по world BuildingBox только выбирает коробку; placement запускается кнопкой «Распаковать» в building menu.
- **Q-ITEM-002:** выбор BuildingBox является взаимоисключающим selection и переключает HUD на выбранную коробку.
- **Q-ITEM-006 (частично):** текущая demo-сцена не обязана показывать процесс падения; generalized visual/actor fall оформлен отдельной системой #396.

## 9. Открытые вопросы

- **Q-ITEM-003:** обычные generic items получают информационный selection по LMB или не реагируют без `Alt`?
- **Q-ITEM-004:** policy нескольких предметов в одной клетке: visual slots, capacity или world pile entity?
- **Q-ITEM-005:** точное определение допустимой плоской опоры.
- **Q-ITEM-006:** item fall выполняется мгновенной Domain-транзакцией с visual animation или существует authoritative falling state на несколько ticks?
- **Q-ITEM-007:** можно ли выбирать/распаковывать BuildingBox из resident inventory через тот же building menu?

## 10. Save/Load

Сохраняются authoritative stack locations, quantity и reservations. Selection, hover, visual slot и front offset не сохраняются. Если будет утверждено отдельное authoritative falling state, его save contract определяется в #396; до этого нельзя самостоятельно выбирать модель.

## 11. Диагностика и тесты

Диагностика показывает stack id, item id, source/landing cell, support reason, reservation/held state, selected entity, visual projected position и collider owner.

Acceptance включает:

- стабилизацию через несколько открытых клеток;
- остановку на первой опоре;
- несколько item types, включая campfire BuildingBox;
- visibility + raycast после landing;
- world box LMB selection без pickup/placement;
- `Alt` hover/click parity;
- pickup arrival по XYZ;
- save/load и repeated render rebuild.
