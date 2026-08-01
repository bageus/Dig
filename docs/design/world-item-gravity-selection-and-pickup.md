# Мировые предметы: падение, видимость, выбор и подбор

Статус: `QUESTIONNAIRE`.

Tracking issue: [#387](https://github.com/bageus/Dig/issues/387).

Связанные issues: [#7](https://github.com/bageus/Dig/issues/7), [#64](https://github.com/bageus/Dig/issues/64), [#67](https://github.com/bageus/Dig/issues/67), [#113](https://github.com/bageus/Dig/issues/113), [#118](https://github.com/bageus/Dig/issues/118), [#390](https://github.com/bageus/Dig/issues/390), [#396](https://github.com/bageus/Dig/issues/396).

Связанные документы:

- [`building-box-placement-and-packing.md`](building-box-placement-and-packing.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`entity-fall-knockback-and-vertical-shafts.md`](entity-fall-knockback-and-vertical-shafts.md).

## 1. Назначение

Система задаёт lifecycle свободного world item после появления, автоматической реакции на потерю опоры, landing, отображения, selection и pickup. BuildingBox использует тот же authoritative Inventory location и дополнительные BuildingBox interactions.

Общая система landing для residents/enemies и combat knockback описывается отдельно в `entity-fall-knockback-and-vertical-shafts.md`. В отличие от предметов, actors не начинают падение только из-за потери опоры: для них требуется внешнее воздействие.

## 2. Владение состоянием

- Inventory владеет ItemStack, quantity, reservations, `ItemLocation` и authoritative item-fall transition.
- World/Navigation предоставляет immutable support/traversability snapshot.
- Jobs/Agents владеют pickup action и worker assignment.
- Presentation владеет visual offset, animation, collider, selection highlight и hover feedback, но не местоположением предмета.

## 3. Подтверждённый workflow падения и стабилизации предмета

1. Свободный, не удерживаемый и не зарезервированный world item проверяет допустимую опору.
2. При обнаружении потери опоры item автоматически начинает fall workflow; отдельный удар, приказ или interaction не требуется.
3. В открытом vertical tunnel fall resolver выбирает первую допустимую плоскую поверхность.
4. Inventory изменяет authoritative world location по утверждённой timing policy ровно один раз.
5. Renderer воспроизводит падение/landing и показывает item в той же projected cell над floor.
6. Collider следует visual и authoritative location.
7. Item остаётся видимым и доступным для raycast.

Коробки и обычные предметы используют общую item gravity/support policy. Любой новый `ItemDefinition`, включая материал, инструмент, оружие, еду и BuildingBox, автоматически наследует grounded world-item behavior без отдельного `lies_on_ground`, `is_grounded`, ItemId allowlist или Unity override. Исключение разрешено только как явно утверждённая специальная spatial policy и не может быть обязательным полем для обычного нового предмета.

Presentation также применяет одну geometry-derived grounding policy: после создания, scale и rotation фактическая нижняя граница активных renderers совмещается с projected floor. Pivot prefab, высота mesh и наличие отдельного visual profile не должны заставлять автора вручную задавать vertical offset. Та же policy используется для обычного world stack, internal stock, inventory placement ghost и BuildingBox relocation preview. Carry sockets, tethered creatures и другие не-world projections используют свои отдельные presentation owners.

Если опора исчезла из-за полного excavation commit, support check использует уже обновлённый authoritative World/topology snapshot и запускается до новых pickup/hauling reservations. Ошибка обновления производной Navigation-проекции не восстанавливает удалённую породу и не отменяет обнаружение потери опоры. Это уточнение не закрывает Q-ITEM-006: текущий runtime может использовать существующую атомарную relocation, а будущая multi-tick animation/state policy остаётся отдельным решением.

«Сразу после потери опоры» означает отсутствие отдельного trigger-воздействия. Остаётся открытым, выполняется ли authoritative relocation атомарно или существует falling state на несколько simulation ticks. Demo-коробка костра на текущем этапе сразу находится в нижней пещере и не используется как демонстрация падения.

## 4. Подтверждённый pickup/use contract

- generic/material/tool/weapon/ordinary food pickup требует выбранного resident и обычный `ЛКМ`;
- BuildingBox pickup требует выбранного resident и `Alt + ЛКМ`, а ordinary LMB сохраняет selection/menu workflow;
- food с `ItemFoodUseDefinition` ordinary LMB подбирается, а `Alt + ЛКМ` создаёт pickup-then-use;
- cursor/highlight и click используют один resolved exact stack/action snapshot;
- command использует фактический `StackId`, `ItemDefinition` и profile, а не hardcoded ItemId/prefix;
- worker должен прийти в точную logical XYZ target cell;
- quantity/location/reservation меняются только authoritative Inventory transaction;
- item target с недоступным action возвращает typed reason и не превращается в ground move/excavation.

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
- collider не может оставаться в старой клетке после падения/landing;
- visual front offset является производным Presentation параметром;
- нижняя geometry bound каждого обычного world-item visual автоматически совмещается с projected floor после scale/rotation;
- новое item/material content не требует per-item ground flag, pivot convention или vertical offset;
- root transform не должен повторно применять terrain rotation к уже спроецированной world position;
- при rebuild visual полностью восстанавливается из Inventory snapshot;
- selection raycast и отображаемая позиция обязаны указывать на одну logical cell.

## 7. Инварианты

- один stack имеет ровно одно authoritative location;
- свободный unsupported world item автоматически входит в fall workflow;
- падение/landing не меняет quantity;
- reserved/held/site item не падает без явной policy;
- hidden-but-clickable и visible-but-stale states запрещены;
- pickup/use hover и command используют одинаковые target, modifier и availability facts;
- ordinary generic item LMB не создаёт informational selection: он создаёт pickup order либо typed rejection;
- BuildingBox не проектируется одновременно как generic item и отдельный duplicate stack visual;
- обычный LMB selection не создаёт pickup order и не запускает placement;
- support-loss detection не зависит от Unity frame rate;
- excavation commit не может оставить item на удалённой опоре из-за stale Navigation/presentation state;
- fall/support reconciliation выполняется раньше новых pickup/hauling reservations;
- grounded presentation является default для всех новых `ItemDefinition`; отсутствие visual profile не отменяет grounding;
- per-item grounded allowlist и ItemId-based vertical offsets запрещены.

## 8. Решённые вопросы

- **Q-ITEM-001:** обычный LMB по world BuildingBox только выбирает коробку; placement запускается кнопкой «Распаковать» в building menu.
- **Q-ITEM-002:** выбор BuildingBox является взаимоисключающим selection и переключает HUD на выбранную коробку.
- **Q-ITEM-003:** обычные generic items ordinary LMB создают pickup order; informational generic selection не используется.
- **Q-ITEM-009:** item interaction определяется definition-owned profile; Presentation ID/prefix classifiers запрещены.
- **Q-ITEM-006 (trigger):** свободный item автоматически начинает падение после потери опоры без отдельного воздействия; timing/state model остаётся открытым.
- **Q-ITEM-008:** текущая demo-сцена не обязана показывать процесс падения; generalized visual/actor fall оформлен отдельной системой #396.
- **Q-ITEM-009:** все новые item/material definitions по умолчанию являются grounded world items. Author не задаёт отдельный ground flag или vertical offset; Presentation совмещает фактическую нижнюю geometry bound с floor автоматически.

## 9. Открытые вопросы

- **Q-ITEM-004:** policy нескольких предметов в одной клетке: visual slots, capacity или world pile entity?
- **Q-ITEM-005:** точное определение допустимой плоской опоры.
- **Q-ITEM-006 (timing):** item fall выполняется мгновенной Domain-транзакцией с visual animation или существует authoritative falling state на несколько ticks?
- **Q-ITEM-007:** можно ли выбирать/распаковывать BuildingBox из resident inventory через тот же building menu?

## 10. Save/Load

Сохраняются authoritative stack locations, quantity и reservations. Selection, hover, visual slot и front offset не сохраняются. Если будет утверждено отдельное authoritative falling state, его save contract определяется в #396; до этого нельзя самостоятельно выбирать модель.

## 11. Диагностика и тесты

Диагностика показывает stack id, item id, source/landing cell, `trigger = SupportLost`, support reason/version, reservation/held state, selected entity, visual projected position и collider owner.

Acceptance включает:

- автоматический fall trigger сразу после потери опоры без отдельного воздействия;
- стабилизацию через несколько открытых клеток;
- остановку на первой опоре;
- удаление опоры excavation commit-ом запускает тот же fall workflow без erase/redraw или дополнительного interaction;
- support reconciliation происходит до новых pickup/hauling reservations;
- несколько item types, включая campfire BuildingBox;
- незарегистрированный новый material/tool item автоматически использует common gravity и floor grounding;
- centered-pivot и bottom-pivot prefab касаются одной и той же floor plane без per-item metadata;
- world stack, internal stock и placement/relocation ghosts используют один grounding owner;
- visibility + raycast после landing;
- world box LMB selection без pickup/placement;
- ordinary pickup и `Alt` special-action hover/click parity;
- generic, food, tool/weapon и BuildingBox используют один profile-driven resolver;
- pickup arrival по XYZ;
- save/load и repeated render rebuild.
