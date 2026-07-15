# Каталог игрового контента Dig

## Назначение

Индекс authoritative design-документов. Runtime state остаётся в Domain/Application владельцах.

## Главные feature-задачи

- здания/сервисы — #74;
- universal BuildingBox — #118;
- питание — #96;
- continuous needs — #159;
- навыки — #103;
- копание/ресурсы — #87;
- HUD/selection/notifications — #113;
- technology tree — #126;
- exploration implementation — #165;
- systems from legacy scripts — #127–#147 и #149–#152.

## Файлы

- [`buildings.md`](buildings.md) — здания, кухни и services;
- [`products.md`](products.md) — physical outputs;
- [`weapons-and-shields.md`](weapons-and-shields.md) — equipment;
- [`materials.md`](materials.md) — materials, ores, terrain/deposits;
- [`food.md`](food.md) — dishes, bites, desired food и history 10;
- [`alcohol.md`](alcohol.md) — drinks/bar/effects;
- [`skills.md`](skills.md) — stable skill IDs;
- [`../needs-continuous-actions.md`](../needs-continuous-actions.md) — gradual Need effects;
- [`../technology-tree.md`](../technology-tree.md) — approved technology graph;
- [`../scripts-system-gap-backlog.md`](../scripts-system-gap-backlog.md) — recovered systems/issues;
- [`../skills-and-progression.md`](../skills-and-progression.md) — grants/capacity/redistribution;
- [`../resident-inventory-expansion.md`](../resident-inventory-expansion.md) — personal inventory;
- [`../resident-hud-selection-and-notifications.md`](../resident-hud-selection-and-notifications.md) — HUD/input/ticker;
- [`../building-box-placement-and-packing.md`](../building-box-placement-and-packing.md) — box lifecycle;
- [`../world-3d-depth.md`](../world-3d-depth.md) — X,Y,Z world;
- [`../exploration-fog-of-war.md`](../exploration-fog-of-war.md) — 3-state fog, vision sources и remembered items;
- [`../excavation-room-templates-and-deposits.md`](../excavation-room-templates-and-deposits.md) — tunnels/rooms/deposits;
- [`../terrain-resource-output-and-processing.md`](../terrain-resource-output-and-processing.md) — terrain output;
- [`../material-demand-and-hauling.md`](../material-demand-and-hauling.md) — demand/filter/visibility hauling;
- [`../open-questions.md`](../open-questions.md) — decisions and unresolved questions.

## Правила ведения

1. Display names не являются IDs.
2. Явно заданные числа считаются исходным балансом.
3. Остальные коэффициенты data-driven/BALANCE_TBD.
4. World владеет terrain/deposits; Exploration — fog/visibility.
5. Inventory владеет items/locations/boxes.
6. Buildings владеет buildings/functions/places, но не копирует items.
7. Production владеет orders; recipes — inputs/outputs.
8. Technology владеет unlock/research state.
9. Agents владеет needs/actions/skills/history.
10. Jobs владеет lifecycle/reservations.
11. Society/Lifecycle владеет family/age/birth/death.
12. Presentation хранит только local view state.
13. Изменение stable IDs требует migration.
14. Animation callbacks не начисляют effects/items/experience.
15. Legacy scripts дают candidates, а не автоматически утверждённый content.

## Ключевые решения

### Мир

- X,Y,Z, depth `0..3`;
- fog states: Unexplored / ExploredNotVisible / Visible;
- vision блокируется стенами, потолками и закрытыми дверями;
- explored сохраняется после ухода source;
- world items не дают vision, но могут иметь last-known marker;
- hauling требует authoritative visibility policy.

### Еда

- Nutrition `0..100` -> Domain `×100`;
- meal = 3 bites;
- desired dish from researched catalog, затем fallback;
- history = последние 10 meals;
- `>=6 matches -> +UnlockedDishCount`, `>=6 mismatches -> -UnlockedDishCount`;
- personal taste profiles не используются;
- worker только для active cooking order;
- Cooking влияет только на скорость, не на output/effects;
- kitchen output `2/2/3/3`.

### Навыки

- 12 skills в одном pool;
- capacity 100 -> 200, individual max 100;
- mixed grants atomic;
- Production per produced unit, Jobs after completion, Combat per result event;
- overflow losses proportional to donor values with deterministic largest remainder.

### Здания

- каждое placeable building имеет физический BuildingBox;
- recipe коробки является стоимостью здания;
- placement резервирует одну box;
- packing возвращает одну box;
- коробка и полный material-site cost одновременно не применяются.

## Актуальные открытые решения

См. `../open-questions.md`:

- Q-014 — balance values;
- Q-034–Q-037 — research/farm lifecycle;
- Q-040 — food Mood cap 50;
- Q-041 — leisure repetition penalty;
- Q-042 — partnership/pregnancy details;
- Q-043 — school/inheritance details;
- Q-044 — sleep comfort/personal beds;
- Q-045 — ecology special cases;
- Q-046 — doors/liquids/stone variant.

## Связанные issues

- technology/system specs: #126–#147, #149–#152;
- exploration design closed #147, implementation #165;
- taste profiles rejected/closed #144;
- food #96–#101, #159;
- skills #103–#107, #117;
- buildings #74–#82, #108, #118;
- excavation/resources #87–#94, #109–#110;
- HUD #113–#117.