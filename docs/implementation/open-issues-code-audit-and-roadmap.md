# Аудит открытых issues по реализации в коде и план работ

Дата аудита: 2026-07-21  
Репозиторий: `bageus/Dig`  
Проверяемая ветка: `main`

## 1. Назначение документа

Этот документ фиксирует состояние открытых issues по фактической реализации в коде. Статус не определяется только наличием PR, упоминанием issue в commit message или зелёным CI.

При проверке учитывались:

- Domain и Application ownership;
- Unity runtime integration;
- сохранение, восстановление и migration;
- unit, integration, deterministic и source-contract tests;
- полный пользовательский сценарий;
- ручная граница Unity Play Mode там, где её нельзя подтвердить обычным CI.

Используются четыре статуса:

1. **Можно закрывать по коду** — критерии issue присутствуют в текущем `main`.
2. **Код завершён, нужна финальная проверка** — репозиторная часть завершена, но issue требует Unity reimport/Play Mode или устранения небольшого UX-дефекта.
3. **Частично реализовано** — есть фундамент или отдельный vertical slice, но критерии issue не выполнены полностью.
4. **Практически не начато** — есть только общие зависимости, design-документы или базовые системы без конкретной реализации issue.

---

## 2. Issues, которые можно закрывать по реализации в коде

### #65 — слотный layout и активные расширения жителя

Реализовано:

- шесть фиксированных `Main` slots;
- `Main`, `Cargo`, `Weapon` compartments;
- stable slot index;
- одно активное расширение максимального tier в каждой группе;
- одновременная работа Cargo и Weapon;
- category validation;
- запрет расширений вне Main;
- immutable layout snapshot;
- стабильная нормализация старых/неслотированных stacks.

Основные файлы:

- `src/Dig.Domain/Inventory/InventoryState.ResidentLayout.cs`;
- `src/Dig.Domain/Inventory/InventoryState.ResidentLayout.Helpers.cs`;
- `src/Dig.Domain/Inventory/ResidentInventoryValues.cs`;
- `tests/Dig.Tests/ResidentInventoryLayoutTests.cs`.

**Вывод:** issue можно закрывать.

### #67 — предмет в руках, использование и транзакционный spill

Реализовано:

- `HeldItemReference` на исходный stack без переноса количества;
- hold/release/switch;
- проверка доступного quantity;
- rollback при неуспешном switch;
- очистка stale references;
- использование и drop через Application commands;
- spill расширения и содержимого;
- quantity conservation и rollback tests.

Основные файлы:

- `src/Dig.Domain/Inventory/InventoryState.HeldItems.cs`;
- `src/Dig.Domain/Inventory/InventoryState.Spill.cs`;
- `src/Dig.Application/Inventory/ResidentInventoryDropHandler.cs`;
- `src/Dig.Application/Inventory/ResidentInventoryUseHandler.cs`;
- `tests/Dig.Tests/HeldItemReferenceTests.cs`;
- `tests/Dig.Tests/ResidentInventorySpillTests.cs`.

**Вывод:** issue можно закрывать.

### #68 — hauling, слотная ёмкость и штрафы корзин

Реализовано:

- reservation вместимости конкретных resident slots до переноса;
- заполнение compatible partial stack до использования пустой ячейки;
- защита от overbooking;
- release/reconciliation claims;
- Cargo speed multiplier;
- влияние на движение, ETA, Utility AI и job candidate cost;
- Save/Load активных claims;
- deterministic multi-resident tests.

Основные файлы:

- `src/Dig.Domain/Inventory/ResidentInventorySlotClaims.cs`;
- `src/Dig.Application/Inventory/HaulingResidentSlotClaimService.cs`;
- `src/Dig.Application/Inventory/HaulingResidentSlotClaimReconciler.cs`;
- `src/Dig.Domain/Inventory/ResidentInventoryMovementCadence.cs`;
- `src/Dig.Domain/Inventory/ResidentInventoryTravelTiming.cs`;
- `tests/Dig.Tests/HaulingResidentSlotClaimLifecycleTests.cs`;
- `tests/Dig.Tests/ResidentInventorySlotClaimSoakTests.cs`.

**Вывод:** issue можно закрывать.

### #69 — корзины, ножны, разгрузка, рецепты и attachments

Реализовано:

- Basket, Large Basket, Sheath и Weapon Harness definitions;
- группы, tiers, количество дополнительных slots и speed multipliers;
- category filters;
- точные рецепты ножен и разгрузки;
- content validation;
- Cargo/Weapon attachments на resident visual;
- скрытие пустой Cargo-корзины.

Основные файлы:

- `src/Dig.Domain/Content/ResidentInventoryExpansionContent.cs`;
- `src/Dig.Domain/Content/ResidentInventoryExpansionContentValidation.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigAgentRenderer.InventoryAttachments.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigResidentInventoryAttachmentVisual.cs`;
- `tests/Dig.Tests/ResidentInventoryExpansionContentTests.cs`.

**Вывод:** issue можно закрывать.

### #71 — Save/Load, migration, diagnostics и tests слотного инвентаря

Реализовано:

- compartment и slot index каждого resident stack;
- held item references;
- resident slot claims;
- active hauling state;
- deterministic migration старого inventory;
- validation повреждённых references;
- diagnostics и полный набор Domain/integration/save/soak tests.

Основные файлы:

- `src/Dig.Application/Saving/InventorySaveData.cs`;
- `src/Dig.Application/Saving/SaveGameBuilder.cs`;
- `src/Dig.Application/Saving/SaveGameLoader.Inventory.cs`;
- `src/Dig.Application/Saving/SaveGameLoader.ResidentSlotClaims.cs`;
- `src/Dig.Domain/Inventory/ResidentInventoryMigration.cs`;
- `tests/Dig.Tests/HaulingSlotClaimSaveRoundTripTests.cs`;
- `tests/Dig.Tests/ResidentInventoryMigrationTests.cs`.

**Вывод:** issue можно закрывать.

### #207 — building prefab authoring pipeline

Реализовано:

- typed `BuildingVisualProfile` lookup;
- один visual root на здание вместо primitive на каждую footprint cell;
- orientation, footprint, pivot и collider validation;
- BuildingBox, Assembly, Completed, Damaged и Packing states;
- worker, visitor, input, output, storage и VFX anchors;
- Campfire, Furnace/Forge/workshop и Arsenal/Storage representatives;
- fallback visual;
- measured LOD, shared meshes и instanced material budgets;
- selection через child colliders с сохранением при rebuild.

Основные файлы:

- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigBuildingRenderer.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigBuildingVisual.cs`;
- representative building library и visual catalog files;
- quality contracts для building prefabs.

Связанные PR: #229 и #232.

**Вывод:** scope issue выполнен в production path. Финальные artist-authored assets могут заменять representatives без изменения архитектуры и не должны блокировать закрытие #207.

### #233 — рабочая копка глубины

Реализовано:

- `SpatialDigJobDefinition`;
- authoritative `SpatialCellId` target и work cell;
- Z-aware Position и Designation reservations;
- стадии `TravelToTarget -> PerformWork -> Finalize`;
- изменение terrain только после завершения работы;
- ручное назначение выбранных residents;
- назначение в Work и FreeTime;
- распределение группы по независимым jobs;
- сохранение назначений невыбранных workers;
- поддержка tunnel и completed room sources на Z0–Z3;
- deterministic tests.

Основные файлы:

- `src/Dig.Domain/Jobs/SpatialDigJobDefinition.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainSpatialExcavation.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainSpatialExcavation.Assignment.cs`;
- `tests/Dig.Tests/SpatialDigJobTests.cs`.

Связанный PR: #242.

**Вывод:** issue можно закрывать.

### #246 — восстановление residents, excavation controls, minimap и buildings

Реализовано:

- HUD, interaction, simulation и minimap инициализируются до optional presentation stages;
- ошибки отдельных renderers изолированы;
- visual failure не отключает playable shell;
- resident имеет selectable Capsule fallback при ошибке composite rig;
- equipment presentation не может сорвать запуск simulation;
- exact failed stage выводится как warning.

Основные файлы находятся в Unity bootstrap, agent renderer и runtime composition.

Связанный PR: #248.

**Вывод:** по коду regression устранён. Перед закрытием следует оставить в issue короткий комментарий с приложенным Play Mode evidence, если он ещё не добавлен.

---

## 3. Код завершён или почти завершён, но issue пока не закрывать

### #64 — общий epic слотного личного инвентаря

Domain, Application, Save/Load, hauling и visual attachments реализованы. Блокеры закрытия:

- дочерняя #70 имеет responsive layout defect;
- общий DoD требует финального Unity Play Mode прохода;
- родительскую issue следует закрывать после закрытия #65, #67, #68, #69, #70 и #71.

### #70 — окно личного инвентаря и управление мышью

Реализованы:

- resident inventory panel;
- Weapon/Main/Cargo slots;
- Alt-use;
- double-click/current-cell drop;
- targeted drop;
- BuildingBox placement;
- reserved/held/active-expansion states;
- UI click shielding.

Оставшийся дефект:

- фиксированная высота нижней панели и фиксированные размеры секций/ячеек приводят к обрезанию UI на небольшом Game View.

Нужно:

1. заменить фиксированный root height на adaptive layout;
2. добавить horizontal/vertical scroll fallback;
3. проверить 1280×720, 1600×900, 1920×1080 и ultrawide;
4. добавить source contract на отсутствие обрезания активных compartments;
5. выполнить Unity Play Mode input matrix.

### #212 — URP, stylized lighting и pooled effects

Репозиторная реализация завершена:

- URP package и generated pipeline assets;
- shared Lit/Unlit/Overlay material ownership;
- vertex colour/AO terrain path;
- pooled VFX и realtime lights;
- construction, production, excavation, combat и status projections;
- local-space budgeting;
- diagnostics;
- blocking quality contracts.

Связанные PR: #252, #257, #258 и #262.

Осталась acceptance evidence:

1. clean Unity 6000.0.71f1 reimport;
2. проверить shader compilation;
3. проверить overlay readability во всех lighting states;
4. снять VFX/light budget counters в representative scene;
5. подтвердить чистую Console.

После этого #212 можно закрывать без дополнительной архитектурной работы.

### #250 — authoritative ручное движение Z0–Z3

Реализовано:

- удалён destination teleport;
- удалена decorative whole-route playback model;
- один authoritative manual spatial order на resident;
- один adjacent open `SpatialCellId` за simulation tick;
- player movement interrupt active work;
- manual order имеет приоритет над work/autonomy;
- local failure не отключает global simulation;
- newly excavated cells синхронизируются перед route validation.

Связанный PR: #251.

Осталась ручная проверка:

- busy resident перенаправляется в любой момент;
- маршрут проходит Z0→Z1→Z2→Z3 и обратно;
- нет clipping через solid terrain;
- logical и visual position совпадают на каждом шаге;
- новый tunnel сразу доступен;
- selection и управление не теряются после failed route.

---

## 4. Частично реализованные issues

### 4.1. World, 3D coordinates, excavation и resources

Issues: **#87, #88, #89, #90, #91, #92, #93, #94, #109, #110**.

Что уже есть:

- `SpatialCellId` и Z0–Z3 movement/digging slice;
- cave-room preview foundation;
- deterministic sparse deposits;
- deposit depletion;
- basic terrain materials;
- job lifecycle и reservations;
- front-layer и deep excavation effects.

Главные пробелы:

- одновременно существуют 2D `CellId` и spatial `SpatialCellId` paths;
- World, Inventory, Buildings, Jobs, Save/Load и Presentation ещё не используют одну authoritative XYZ model;
- нет полного 3D template mask/provenance/save lifecycle;
- нет полного depth-aware deposit persistence;
- нет atomic output/hauling commit для всех deep cells;
- #110 блокируется отсутствием authoritative fog #165;
- Unity preview и inspectors не покрывают все criteria #93.

### 4.2. HUD, input, notifications и overlays

Issues: **#14, #113, #115, #116, #117, #211**.

Что уже есть:

- adaptive gameplay HUD;
- Residents/Buildings/Jobs tabs;
- world/HUD resident selection;
- marquee selection;
- hover/selected highlights;
- excavation palette;
- BuildingBox placement;
- typed overlay manager;
- needs и top-five skills projection.

Пробелы:

- hostile click/attack path не завершён в Unity input router;
- excavation eraser не является единым atomic batch command;
- нет persistent `GameNotification` queue со stable IDs и navigation targets;
- нет полного skills capacity/redistribution report;
- selection, excavation, buildings, deposits и fog ещё не полностью сведены в один overlay pipeline;
- epics #14 и #113 содержат больше scope, чем текущий HUD vertical slice.

### 4.3. Food, eating, schedule и continuous needs

Issues: **#96, #97, #98, #99, #100, #101, #159, #234**.

Что уже есть:

- authoritative needs;
- basic Eat/Sleep/Leisure actions;
- Inventory food reservations;
- Bed/Leisure place reservations;
- Utility AI foundation;
- interruption cleanup.

Пробелы:

- нет meal из трёх bites;
- нет committed meal payload и lost remainder;
- нет полного каталога блюд четырёх kitchen tiers;
- нет desired dish selection и history последних десяти meals;
- нет interval-based Health/Nutrition/Alertness/Mood effects;
- нет соответствующих Save/Load codecs и UI;
- #234 не реализует полный deterministic FreeTime priority chain и social/reproduction reservations.

### 4.4. Skills и progression

Issues: **#103, #104, #105, #106, #107, #117**.

Что уже есть:

- skill IDs и values;
- work efficiency;
- roster top-five projection;
- basic combat result events;
- foundational Society/Production/Jobs integration points.

Пробелы:

- единый `TotalSkillCapacity` 100–200;
- atomic mixed grant bundle;
- proportional donor loss;
- largest-remainder rounding;
- idempotent grants для jobs/production/combat/services;
- Save/Load и migration полного progression state;
- detailed UI report.

### 4.5. Production, Technology и Energy

Issues: **#75, #108, #126, #128, #127**.

Что уже есть:

- общий Production owner;
- recipe inputs/outputs;
- Job workers;
- prerequisite graph foundation;
- BuildingBox lifecycle;
- energy availability interface.

Пробелы:

- полный catalog всех BuildingBox recipes;
- Furnace/Foundry/Sandstone content и active orders;
- authoritative technology tree;
- research queue и orange/yellow/white states;
- research UI;
- три класса энергии и четыре concrete generators;
- source binding, stock, refill и paused production persistence.

### 4.6. Combat, Society, ecology и appearance

Issues: **#129, #132, #138, #139, #145, #149, #151**.

Что уже есть:

- Faction state и diplomacy foundation;
- deterministic damage/cooldown/status foundation;
- basic tactical and strategic decisions;
- partnerships, pregnancies, births and family graph;
- composite resident rig;
- creature visual families and pooling.

Пробелы:

- полный combat equipment/recipe/technology catalog;
- universal status stacking/immunity/save/UI;
- full combat encounter FSM;
- expedition party/base perimeter FSM;
- complete meeting/reproduction Application flow;
- authoritative ecology individuals, lifecycle and adapters;
- resident sex/age/headwear authoritative bridge и Save/Load.

### 4.7. Visual production epic

Issues: **#203, #208, #209, #210, #211**.

Статус:

- #207 можно закрывать;
- #208 имеет infrastructure slice, но не полный representative item catalog, badges и measured LOD;
- #209 имеет rig foundation, но нет authored skinned prefabs, Animator controllers и 64+ measured budgets;
- #210 имеет presentation foundation, но нет ecology/combat adapter и authored creature assets;
- #211 имеет overlay foundation, но не все runtime layers migrated;
- поэтому #203 остаётся открытой.

---

## 5. Практически не начатые issues

Следующие tasks имеют design или общие зависимости, но не имеют полного concrete runtime state/commands/tests.

### Buildings и services

- #74 — общий epic новых зданий;
- #76 — guard post, alarm и emergency weapon issue;
- #77 — Arsenal collection/storage/manual issue;
- #78 — game rooms;
- #79 — cinema/service/fertility modifier;
- #80 — Distillery и alcohol recipes;
- #81 — Bar serving modes;
- #82 — University capacity progression.

### Health, consumables и defence

- #130 — Hospital treatment lifecycle;
- #131 — potion catalog/use/effects;
- #133 — traps and automatic defence.

### Navigation, factions и world visibility

- #136 — doors and access modes;
- #137 — ladders, lifts and personal mobility;
- #140 — faction clan content;
- #141 — ownership, theft and diplomacy reaction;
- #165 — authoritative three-state fog and vision flood-fill.

### Needs, lifecycle и society

- #142 — sleep quality and personal beds;
- #143 — leisure variety history;
- #146 — childhood, school and inheritance;
- #150 — graves, rejuvenation and return;
- #152 — conversation topics and social memory.

### Backlog

- #177 — additional fantasy/creature equipment from legacy scripts. Оставить backlog и не включать в ближайший critical path.

---

## 6. Рекомендуемый порядок реализации

План построен по зависимостям, а не по номеру issue. Каждый этап должен завершаться отдельным mergeable vertical slice и exact-head Quality run.

## Этап 0. Очистка tracker и фиксация baseline

Закрыть после короткого повторного подтверждения:

- #65;
- #67;
- #68;
- #69;
- #71;
- #207;
- #233;
- #246.

Действия:

1. оставить в каждой issue комментарий со ссылками на production files и tests;
2. обновить checkboxes родительских #64 и #203;
3. пометить manual validation issues отдельным label;
4. не закрывать parent epic, пока не закрыты обязательные children.

## Этап 1. Стабилизировать текущий playable vertical slice

Приоритет: максимальный.

Issues:

- #70;
- #212;
- #250;
- затем #64.

Работы:

1. сделать нижнюю inventory panel responsive;
2. выполнить clean Unity reimport и representative Play Mode для URP/VFX;
3. выполнить movement matrix Z0–Z3;
4. записать Play Mode evidence;
5. закрыть #70, #212, #250 и parent #64.

Definition of Done:

- чистая Console;
- HUD не обрезается на минимальном поддерживаемом разрешении;
- residents можно выбирать и перенаправлять на любой глубине;
- effects/lights не меняют simulation result;
- все CI checks зелёные.

## Этап 2. Одна authoritative XYZ-модель

Приоритет: критический архитектурный.

Issues:

- #88;
- затем foundation для #87.

Порядок:

1. определить единственный authoritative coordinate contract;
2. перевести World cells/chunks;
3. перевести Agent position и occupancy;
4. перевести ItemLocation;
5. перевести building footprints/places;
6. перевести Jobs/reservations;
7. перевести Navigation;
8. перевести snapshots/hash/save;
9. добавить deterministic `X,Y -> X,Y,Z=0` migration;
10. удалить production parallel 2D paths.

Definition of Done:

- одинаковые X/Y с разным Z всегда различаются во всех owners;
- не существует отдельной 2D authoritative mutation path;
- save/load сохраняет exact Z для agents/items/jobs/buildings/deposits;
- stale-route и chunk invalidation tests учитывают Z.

## Этап 3. Полный excavation/resource vertical slice

Issues:

- #89;
- #90;
- #91;
- #92;
- #109;
- #94;
- #93;
- #165;
- #110;
- закрытие #87.

Порядок:

1. terrain/material/output catalog (#109);
2. template definitions и unlock (#89);
3. atomic 3D template instances (#90);
4. deposits and generation (#91);
5. mining output transaction (#92);
6. full save/migration/diagnostics (#94);
7. Unity previews, arches, layers and inspectors (#93);
8. authoritative fog/visibility (#165);
9. demand/fog-aware hauling (#110);
10. end-to-end Play Mode: designate → dig → drop → reveal → haul.

Definition of Done:

- один cell/deposit не выдаёт output дважды;
- hidden source не создаёт hauling;
- template placement atomic;
- одинаковый seed/version даёт одинаковый world/output;
- Save/Load восстанавливает partial plans и deposits;
- #87 закрывается только после полного end-to-end slice.

## Этап 4. Завершить input, HUD, notifications и overlays

Issues:

- #115;
- #116;
- #117;
- #211;
- #113;
- #14.

Порядок:

1. завершить deterministic input priority matrix;
2. подключить attack order;
3. сделать eraser atomic batch command;
4. реализовать persistent notification queue;
5. добавить navigation targets и focus actions;
6. добавить capacity/redistribution UI;
7. мигрировать все overlay layers в общий manager;
8. выполнить Play Mode tests для selection, UI shielding, notifications и rebuild;
9. закрыть child issues, затем #113 и #14.

## Этап 5. FreeTime, food и continuous needs

Issues:

- #234;
- #97;
- #98;
- #99;
- #159;
- #101;
- #100;
- #96;
- затем #142, #143 и часть #145.

Порядок:

1. единый deterministic FreeTime coordinator;
2. three-bite meal payload;
3. kitchen/food catalog;
4. desired dish и diet history;
5. interval-based Eat/Sleep/Leisure effects;
6. Save/Load active actions/history;
7. Unity UI и animation projection;
8. personal beds/sleep quality;
9. leisure variety/social partner reservation;
10. integration с reproduction eligibility.

Definition of Done:

- interruption не дублирует effects;
- один meal создаёт одну history entry;
- Save/Load не reroll selection;
- residents не берут shared work вне Work без manual order;
- social pair reservation атомарна.

## Этап 6. Skills, research, production и energy

Issues:

- #104;
- #105;
- #106;
- #107;
- #103;
- #126;
- #128;
- #75;
- #108;
- #127;
- #82;
- #117 integration.

Порядок:

1. implement capacity/mixed grant math;
2. connect Jobs/Production/Combat/Services;
3. Save/Load and UI progression;
4. authoritative technology graph;
5. research queue/building UI;
6. complete BuildingBox recipes;
7. furnace/foundry/sandstone content;
8. energy sources/binding/pause lifecycle;
9. University progression.

Definition of Done:

- grant order не влияет на результат;
- replay не начисляет XP повторно;
- research completion idempotent;
- production progress сохраняется без энергии;
- recipes and technology references полностью валидируются.

## Этап 7. Buildings и services catalog

Issues:

- #74;
- #76;
- #77;
- #78;
- #79;
- #80;
- #81;
- #130.

Рекомендуемые vertical slices:

1. Arsenal + Guard Post;
2. Game Rooms + leisure integration;
3. Distillery + Bar;
4. Cinema + Society modifier;
5. Hospital.

Каждый slice должен включать:

- content definition;
- recipe/technology placement;
- functional places;
- jobs/reservations;
- Save/Load;
- UI;
- representative visuals;
- deterministic integration tests;
- Unity Play Mode test.

## Этап 8. Combat, ecology, lifecycle и society

Issues:

- #129;
- #132;
- #138;
- #139;
- #145;
- #149;
- #151;
- #140;
- #141;
- #146;
- #150;
- #152.

Порядок:

1. combat equipment catalog;
2. universal status system;
3. encounter FSM;
4. ecology authoritative state and creature adapter;
5. resident appearance/headwear bridge;
6. complete conception/pregnancy Application flow;
7. Strategic AI expeditions;
8. faction content and theft;
9. childhood/school;
10. death/graves/return;
11. social memory/conversations.

## Этап 9. Secondary navigation and defence systems

Issues:

- #136;
- #137;
- #131;
- #133.

Порядок зависит от предыдущих owners:

- doors после authoritative XYZ Navigation и перед финальным fog integration;
- ladders/lifts после stable spatial occupancy;
- potions после universal status system;
- traps после Combat + Status + faction ownership.

## Этап 10. Завершить visual production epic

Issues:

- #208;
- #209;
- #210;
- #211;
- #203.

Работы:

1. representative/authored item catalog;
2. badges, selected/last-known overlays и measured item LOD;
3. authored resident prefabs/Animator controllers;
4. measured 64+ resident budgets;
5. ecology/combat creature adapters и authored families;
6. complete overlay migration;
7. cross-family Editor gallery и representative Play Mode profiling;
8. закрыть #203 после всех children.

---

## 7. Правила для следующих реализаций

### Размер PR

Один PR должен завершать один проверяемый vertical slice. Нельзя смешивать в одном PR:

- новую authoritative модель;
- массовую миграцию presentation;
- несколько независимых content families;
- unrelated regressions.

### Обязательные проверки

Для каждого implementation issue:

- Domain unit tests;
- Application integration tests;
- Save/Load или явное обоснование отсутствия state;
- deterministic replay/idempotency;
- source contracts только как дополнительная защита, не вместо runtime tests;
- exact-head Quality run;
- Unity Play Mode evidence для input/rendering/composition tasks.

### Закрытие issue

Issue закрывается только когда:

1. критерии приёмки присутствуют в production code;
2. нет параллельного placeholder path, который обходит новую систему;
3. tests проверяют failure/interruption/replay;
4. migration определена для существующих saves/state;
5. Unity-specific behavior подтверждено Play Mode там, где это требуется;
6. в issue оставлен итоговый комментарий с файлами, PR и validation evidence.

---

## 8. Краткий итог

### Закрыть по коду

`#65 #67 #68 #69 #71 #207 #233 #246`

### Закрыть после небольшой доработки или ручной проверки

`#64 #70 #212 #250`

### Оставить активными как частичную реализацию

`#14 #75 #87 #88 #89 #90 #91 #92 #93 #94 #96 #97 #98 #99 #100 #101 #103 #104 #105 #106 #107 #108 #109 #110 #113 #115 #116 #117 #126 #128 #129 #132 #138 #139 #145 #149 #151 #159 #203 #208 #209 #210 #211 #234`

### Практически не начаты или остаются design/content tasks

`#74 #76 #77 #78 #79 #80 #81 #82 #127 #130 #131 #133 #136 #137 #140 #141 #142 #143 #146 #150 #152 #165`

### Backlog

`#177`
