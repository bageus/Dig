# Каталог игрового контента Dig

## Назначение

Каталог хранит согласованные design-правила зданий, продукции, оружия, материалов, еды, алкоголя, навыков, дерева технологий, HUD и размещения. Runtime-реализация остаётся в Domain/Application системах.

Главные feature-задачи:

- здания и сервисы — #74;
- универсальные коробки зданий — #118;
- питание — #96;
- навыки — #103;
- копание и ресурсы — #87;
- HUD гномов, выбор и уведомления — #113;
- полное дерево технологий — #126;
- системы, найденные в legacy scripts, — #127–#147 и #149–#152.

## Файлы

- [`buildings.md`](buildings.md) — здания, сервисы, кухни и production capabilities;
- [`products.md`](products.md) — physical outputs и переработка;
- [`weapons-and-shields.md`](weapons-and-shields.md) — боевое снаряжение;
- [`materials.md`](materials.md) — материалы, руды, terrain и deposits;
- [`food.md`](food.md) — блюда, укусы и разнообразие;
- [`alcohol.md`](alcohol.md) — напитки, бар и effects;
- [`skills.md`](skills.md) — skill IDs;
- [`../technology-tree.md`](../technology-tree.md) — authoritative согласованная часть дерева технологий;
- [`../scripts-system-gap-backlog.md`](../scripts-system-gap-backlog.md) — индекс найденных систем и созданных issues;
- [`../skills-and-progression.md`](../skills-and-progression.md) — каталог 12 навыков, capacity 100→200 и grants;
- [`../resident-inventory-expansion.md`](../resident-inventory-expansion.md) — личный inventory, expansions и BuildingBox input;
- [`../resident-hud-selection-and-notifications.md`](../resident-hud-selection-and-notifications.md) — roster, panels, input и notifications;
- [`../building-box-placement-and-packing.md`](../building-box-placement-and-packing.md) — universal box placement, assembly и packing;
- [`../world-3d-depth.md`](../world-3d-depth.md) — authoritative X,Y,Z;
- [`../excavation-room-templates-and-deposits.md`](../excavation-room-templates-and-deposits.md) — tunnels, rooms и deposits;
- [`../terrain-resource-output-and-processing.md`](../terrain-resource-output-and-processing.md) — terrain outputs;
- [`../material-demand-and-hauling.md`](../material-demand-and-hauling.md) — demand/filter/fog hauling;
- [`../open-questions.md`](../open-questions.md) — реестр решений.

## Правила ведения

1. Display name не используется как ссылка; definitions получают stable IDs.
2. Явно заданные числа считаются исходным балансом.
3. Непредоставленные числа остаются data-driven TBD.
4. World владеет terrain/deposit state.
5. Inventory владеет items, quantities, locations и boxes.
6. Buildings владеет plans/buildings/functions, но не копирует items.
7. Production владеет orders; RecipeDefinition — inputs/outputs.
8. Technology владеет состоянием исследований и открытиями.
9. Agents/Skills владеет 12 skill values и TotalSkillCapacity.
10. Jobs владеет work lifecycle и reservations.
11. Society/Lifecycle владеет sex, age, birth/death и family rules.
12. Presentation хранит только local selection, panels, hover, preview, scroll и ticker animation.
13. Localized strings не являются state keys.
14. Typed IDs разных систем не смешиваются.
15. Изменение stable ID требует migration.
16. Конфликты фиксируются как Q-XXX.
17. UI не зависит только от цвета.
18. Один BuildingDefinition использует только одну construction policy.
19. Legacy scripts являются источником кандидатов, но не автоматически утверждённым balance/content.

## Принятые решения

### Мир и ресурсы

- world — X,Y,Z, depth 0..3;
- Stonework thresholds 20/40/60;
- existing excavation plan survives later skill loss;
- room trapezoid repeated on Z layers, no mirror;
- deposits replace terrain cells;
- terrain-specific output tables;
- hauling only by demand/filter and revealed source;
- iron/metal = material.iron;
- official names «Песчаник» and «Рудная порода».

### Технологии

- skill threshold не открывает technology автоматически;
- eligibility появляется, когда один живой гном одновременно удовлетворяет всем требованиям;
- игрок запускает изучение иконкой в соответствующей постройке;
- недоступная иконка оранжевая, доступная белая, но status дублируется текстом;
- Лесопилка и Пилорама — разные узлы;
- Пилорама производит Винокурню и Университет;
- Мебельная мастерская производит три игровые комнаты;
- Оружейная кузница изучает и производит Арсенал;
- Плавильня из scripts = Горн;
- Литейный цех — продвинутая плавка железа/золота на угле;
- Песчаник обрабатывает кристаллическую руду;
- водолазный колокол исключён;
- `Dojo` трактуется как направление «Кулачный бой»;
- «Грибной самогон» = legacy name Огненной воды, отдельного ItemId нет;
- продолжение дерева после согласованного участка остаётся `TBD_OWNER`.

### HUD и resident

- no selection panel: tunnel, room templates, eraser;
- resident panel: Weapon → Main → Cargo;
- building panel: functions + packing button;
- BuildingBox panel: placement preview;
- selected resident synchronized HUD/world;
- direct display name «Бодрость» for Alertness;
- Mood sad 0–25, neutral 26–75, joy 76–100;
- status text built from typed descriptors.

### Навыки

- one pool contains all 12 skills;
- TotalSkillCapacity base 100, university max 200;
- individual max 100;
- value 120 removed as erroneous;
- exact redistribution formula remains Q-029.

### Buildings

- every placeable building has a physical BuildingBox;
- Production recipe is the building cost;
- LMB world/inventory box enters placement;
- Alt+LMB world box assigns pickup;
- valid plan reserves one box;
- worker carries and assembles it;
- packing creates exactly one box;
- no simultaneous box and full-material consumption for one definition.

### Notifications

- hunger threshold Nutrition <15 UI / <1500 Domain;
- event on downward crossing;
- repeat allowed after recovery and another crossing;
- simultaneous messages remain separate;
- active until LMB view/focus or RMB dismiss;
- no history.

## Открытые решения

Актуальный реестр: [`../open-questions.md`](../open-questions.md).

Основные оставшиеся вопросы:

- Q-014 — balance values;
- Q-015–Q-018 — food scaling, variety, interruption и kitchen workers;
- Q-019–Q-021 — mixed grants, equipment/defense и timing;
- Q-029 — deterministic skill redistribution formula;
- research duration/cost/worker и policy потери requirements во время progress — описаны как TBD в `technology-tree.md`/#128.

## Связанные issues

### Технологии и новые system specs

- #126 — authoritative дерево технологий;
- #127 — энергия;
- #128 — skill eligibility и запуск исследования;
- #129–#147, #149–#152 — отдельные функции и системы из legacy scripts;
- полный индекс — [`../scripts-system-gap-backlog.md`](../scripts-system-gap-backlog.md).

### Здания и сервисы

- #75 — BuildingBox recipes и подтверждённые мастерские;
- #76–#82 — guard, arsenal, leisure, cinema, alcohol, bar, university;
- #108 — furnace/foundry/crystal processor;
- #118 — universal box placement/packing.

### Еда

- #97–#101;
- #144 — вкусовые профили и выбор еды.

### Навыки

- #103–#107;
- #117;
- #128.

### Копание и ресурсы

- #88–#94;
- #109–#110.

### HUD

- #113–#117;
- #70;
- #118.
