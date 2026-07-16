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
- [`products.md`](products.md) — physical outputs, grave/return/rejuvenation recipes;
- [`weapons-and-shields.md`](weapons-and-shields.md) — equipment;
- [`materials.md`](materials.md) — materials, ores, terrain/deposits;
- [`food.md`](food.md) — dishes, bites, desired food и history 10;
- [`alcohol.md`](alcohol.md) — drinks/bar/effects;
- [`skills.md`](skills.md) — stable skill IDs;
- [`../needs-continuous-actions.md`](../needs-continuous-actions.md) — gradual Need effects;
- [`../leisure-variety-and-selection.md`](../leisure-variety-and-selection.md) — выбор досуга и repeat penalty;
- [`../partnership-pregnancy-and-birth.md`](../partnership-pregnancy-and-birth.md) — пары и рождение;
- [`../childhood-school-and-inheritance.md`](../childhood-school-and-inheritance.md) — дети и школа;
- [`../sleep-comfort-and-bed-assignment.md`](../sleep-comfort-and-bed-assignment.md) — sleep tiers и кровати;
- [`../ecology-creatures-and-special-drops.md`](../ecology-creatures-and-special-drops.md) — существа и drops;
- [`../doors-access-and-lifecycle.md`](../doors-access-and-lifecycle.md) — двери;
- [`../resident-role-headwear.md`](../resident-role-headwear.md) — cosmetic role hats и aging hair;
- [`../death-graves-resurrection-and-rejuvenation.md`](../death-graves-resurrection-and-rejuvenation.md) — identity cap, graves, return и rejuvenation;
- [`../energy-generation-and-production-pausing.md`](../energy-generation-and-production-pausing.md) — energy sources и paused Production;
- [`../research-availability-duration-and-ui.md`](../research-availability-duration-and-ui.md) — queued research, duration и UI;
- [`../ladders-and-elevators.md`](../ladders-and-elevators.md) — ladder tiers и elevator queue;
- [`../technology-tree.md`](../technology-tree.md) — approved technology graph;
- [`../scripts-system-gap-backlog.md`](../scripts-system-gap-backlog.md) — recovered systems/issues;
- [`../skills-and-progression.md`](../skills-and-progression.md) — grants/capacity/redistribution;
- [`../resident-inventory-expansion.md`](../resident-inventory-expansion.md) — personal inventory;
- [`../resident-hud-selection-and-notifications.md`](../resident-hud-selection-and-notifications.md) — HUD/input/ticker;
- [`../building-box-placement-and-packing.md`](../building-box-placement-and-packing.md) — box lifecycle;
- [`../world-3d-depth.md`](../world-3d-depth.md) — X,Y,Z world;
- [`../exploration-fog-of-war.md`](../exploration-fog-of-war.md) — fog/vision;
- [`../excavation-room-templates-and-deposits.md`](../excavation-room-templates-and-deposits.md) — tunnels/rooms/deposits;
- [`../terrain-resource-output-and-processing.md`](../terrain-resource-output-and-processing.md) — terrain output;
- [`../material-demand-and-hauling.md`](../material-demand-and-hauling.md) — demand/filter hauling;
- [`../open-questions.md`](../open-questions.md) — Q-001–Q-052 summary;
- [`../open-questions-047-051.md`](../open-questions-047-051.md) — detailed Q-047–Q-052.

## Правила ведения

1. Display names не являются IDs.
2. Явно заданные числа считаются исходным балансом.
3. Остальные коэффициенты data-driven/BALANCE_TBD.
4. World владеет terrain/deposits; Exploration — fog/visibility.
5. Inventory владеет items/locations/boxes и identity cap.
6. Buildings владеет buildings/functions/places, но не копирует items.
7. Production владеет orders; recipes — inputs/outputs.
8. Technology владеет unlock/research state.
9. Agents владеет needs/actions/skills/history/appearance state.
10. Jobs владеет lifecycle/reservations.
11. Society/Lifecycle владеет family/age/birth/death/return.
12. Presentation хранит только local view state.
13. Animation callbacks не начисляют effects/items/experience.
14. Legacy scripts дают candidates; явно принятые recipes становятся approved content.

## Ключевые решения

### Еда

- Nutrition `0..100` -> Domain `×100`;
- meal = 3 bites;
- desired dish затем fallback;
- history = 10 meals;
- matched food Mood и positive diversity могут поднимать Mood выше 50 до MoodMaximum;
- Cooking влияет только на скорость;
- kitchen output `2/2/3/3`.

### Досуг, сон и общество

- 5 leisure repeats в предыдущих 10 → Mood gain 50%;
- sleep multipliers `1/.75/.5/.25`;
- exclusive male/female pair, conception guaranteed;
- pregnancy 1 day, cooldown 2 days;
- школа 1 teacher/4 students, 24/7, 12 skills.

### Appearance и lifecycle

- role hats cosmetic only;
- любая работа ждёт проверки/смены шапки;
- одежда постоянна, old hair grey, old males bald;
- death создаёт identity cap, не текущую role hat;
- grave = 3 stone + cap, permanent and non-packable;
- temple return = cap + hamster + 4 gold + 2 crystal ore;
- rejuvenation potion = hamster + crystal + iron ore + 2 gold;
- repeated rejuvenation/return cycles разрешены.

### Research

- orange/yellow research можно queue заранее;
- worker должен соответствовать requirements;
- materials не расходуются, только задают duration;
- ores weight 2;
- research не начисляет XP;
- completed technology не relock при падении skills.

### Transport

- carts/rails/wheelbarrows excluded;
- ladders: wooden 12, metal 16, crystal 24;
- elevators capacity 4, classes 1/2/3;
- ждут подошедших в radius 1 без timeout;
- power loss запускает emergency wall climb;
- cargo только в personal Inventory.

## Актуальные открытые решения

- Q-014 — balance values;
- Q-034 — coal weight, no-material recipe и research slots;
- Q-036 — requirements lost during active research;
- Q-037 — farm action model;
- Q-049 — Energy allocation/source lifecycle;
- Q-050 — busy researcher color;
- Q-051 — emergency elevator climb destination;
- Q-052 — partnership conflict after return.

## Связанные issues

- technology/energy/research: #126–#128;
- transport/doors: #136–#137;
- lifecycle/appearance: #145, #150–#151;
- food #96–#101, #159;
- skills #103–#107, #117;
- buildings #74–#82, #108, #118;
- excavation/resources #87–#94, #109–#110;
- HUD #113–#117.