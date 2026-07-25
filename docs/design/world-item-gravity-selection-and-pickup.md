# Мировые предметы: падение, видимость, выбор и подбор

Статус: `QUESTIONNAIRE`.

Tracking issue: [#387](https://github.com/bageus/Dig/issues/387).

Связанные issues: [#7](https://github.com/bageus/Dig/issues/7), [#64](https://github.com/bageus/Dig/issues/64), [#67](https://github.com/bageus/Dig/issues/67), [#113](https://github.com/bageus/Dig/issues/113), [#118](https://github.com/bageus/Dig/issues/118), [#390](https://github.com/bageus/Dig/issues/390).

## 1. Назначение

Система задаёт lifecycle свободного world item после появления, падения, отображения, selection и pickup. BuildingBox использует тот же authoritative Inventory location и дополнительные BuildingBox interactions.

## 2. Владение состоянием

- Inventory владеет ItemStack, quantity, reservations и `ItemLocation`.
- World/Navigation предоставляет immutable support/traversability snapshot.
- Jobs/Agents владеют pickup action и worker assignment.
- Presentation владеет visual offset, animation, collider и hover feedback, но не местоположением предмета.

## 3. Подтверждённый workflow падения

1. Свободный, не удерживаемый и не зарезервированный world item проверяет опору.
2. В открытом vertical tunnel item опускается до первой допустимой плоской поверхности.
3. Inventory атомарно изменяет world cell ровно один раз.
4. Renderer обновляет visual в той же projected cell над floor, а collider следует visual.
5. Item остаётся видимым и доступным для raycast.

Коробки и обычные предметы подчиняются общей gravity policy, если конкретный item definition не задаёт исключение.

## 4. Подтверждённый pickup contract

- BuildingBox pickup требует выбранного resident и `Alt + ЛКМ`;
- обычный ЛКМ не создаёт pickup order;
- pickup cursor появляется только при `Alt`, доступном quantity и допустимом target;
- command использует фактический ItemId выбранной коробки, а не hardcoded тип;
- worker должен прийти в точную logical XYZ target cell;
- quantity/location/reservation меняются только authoritative Inventory transaction.

## 5. BuildingBox и списки строений

BuildingBox остаётся Inventory item, но показывается в building roster/management как объект, из которого можно начать placement. Это Presentation/read-model classification и не превращает коробку в completed `BuildingState`.

## 6. Видимость и spatial consistency

- visual не может быть скрыт floor geometry при наличии selectable logical item;
- collider не может оставаться в старой клетке после падения;
- visual front offset является производным Presentation параметром;
- root transform не должен повторно применять terrain rotation к уже спроецированной world position;
- при rebuild visual полностью восстанавливается из Inventory snapshot.

## 7. Инварианты

- один stack имеет ровно одно authoritative location;
- падение не меняет quantity;
- reserved/held/site item не падает без явной policy;
- hidden-but-clickable и visible-but-stale states запрещены;
- pickup hover и pickup command используют одинаковую target availability;
- BuildingBox не проектируется одновременно как generic item и отдельный duplicate stack visual.

## 8. Открытые вопросы

- **Q-ITEM-001:** обычный LMB по world BuildingBox сразу включает placement или сначала только выбирает коробку?
- **Q-ITEM-002:** если resident выбран, обычный LMB сохраняет resident selection или переключает на BuildingBox/placement?
- **Q-ITEM-003:** обычные generic items получают информационный selection по LMB или не реагируют без `Alt`?
- **Q-ITEM-004:** policy нескольких предметов в одной клетке: visual slots, capacity или world pile entity?
- **Q-ITEM-005:** точное определение допустимой плоской опоры.
- **Q-ITEM-006:** Domain fall происходит мгновенно, но нужна ли отдельная visual falling animation?

## 9. Save/Load

Сохраняются authoritative stack locations, quantity и reservations. Falling animation, visual slot и front offset не сохраняются. После загрузки unsupported position повторно стабилизируется deterministic gravity policy без duplication.

## 10. Диагностика и тесты

Диагностика показывает stack id, item id, source/landing cell, support reason, reservation/held state, visual projected position и collider owner.

Acceptance включает:

- падение через несколько открытых клеток;
- остановку на первой опоре;
- несколько item types, включая campfire BuildingBox;
- visibility + raycast после падения;
- `Alt` hover/click parity;
- pickup arrival по XYZ;
- save/load и repeated render rebuild.
