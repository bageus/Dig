# Каталог игрового контента Dig

## Назначение

Каталог хранит согласованные design-правила зданий, продукции, оружия, материалов, еды, алкоголя, навыков, HUD и размещения. Runtime-реализация остаётся в Domain/Application системах.

Главные feature-задачи:

- здания и сервисы — #74;
- универсальные коробки зданий — #118;
- питание — #96;
- навыки — #103;
- копание и ресурсы — #87;
- HUD гномов, выбор и уведомления — #113.

## Файлы

- [`buildings.md`](buildings.md) — здания, сервисы, кухни и production capabilities;
- [`products.md`](products.md) — physical outputs и переработка;
- [`weapons-and-shields.md`](weapons-and-shields.md) — оружие и щиты;
- [`materials.md`](materials.md) — материалы, руды, terrain и deposits;
- [`food.md`](food.md) — блюда, укусы и разнообразие;
- [`alcohol.md`](alcohol.md) — напитки, бар и effects;
- [`skills.md`](skills.md) — skill IDs;
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
8. Agents/Skills владеет 12 skill values и TotalSkillCapacity.
9. Jobs владеет work lifecycle и reservations.
10. Society/Lifecycle владеет sex, age, birth/death и family rules.
11. Presentation хранит только local selection, panels, hover, preview, scroll и ticker animation.
12. Localized strings не являются state keys.
13. Typed IDs разных систем не смешиваются.
14. Изменение stable ID требует migration.
15. Конфликты фиксируются как Q-XXX.
16. UI не зависит только от цвета.
17. Один BuildingDefinition использует только одну construction policy.

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
- Q-019–Q-021 — mixed grants, weapon/defense и timing;
- Q-029 — deterministic skill redistribution formula.

## Связанные issues

### Здания и сервисы

- #75 — BuildingBox recipes;
- #76–#82 — guard, arsenal, leisure, cinema, alcohol, bar, university;
- #108 — furnace/foundry/crystal processor;
- #118 — universal box placement/packing.

### Еда

- #97–#101.

### Навыки

- #103–#107;
- #117.

### Копание и ресурсы

- #88–#94;
- #109–#110.

### HUD

- #113–#117;
- #70;
- #118.
