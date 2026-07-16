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
- [`../leisure-variety-and-selection.md`](../leisure-variety-and-selection.md) — выбор досуга, история 10 и repeat penalty;
- [`../partnership-pregnancy-and-birth.md`](../partnership-pregnancy-and-birth.md) — пары, зачатие, беременность и рождение;
- [`../childhood-school-and-inheritance.md`](../childhood-school-and-inheritance.md) — детство, школа и наследование;
- [`../sleep-comfort-and-bed-assignment.md`](../sleep-comfort-and-bed-assignment.md) — sleep tiers и личные кровати;
- [`../ecology-creatures-and-special-drops.md`](../ecology-creatures-and-special-drops.md) — существа, reproduction, eggs и drops;
- [`../doors-access-and-lifecycle.md`](../doors-access-and-lifecycle.md) — режимы дверей и automatic lifecycle;
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

### Досуг и сон

- leisure использует weighted random по редкости активности;
- 5 повторений одного вида в предыдущих 10 уменьшают Mood gain до 50%;
- current leisure добавляется в history после расчёта и первого effect interval;
- sleep tier gaps `0/1/2/3+` используют multipliers `1.00/0.75/0.50/0.25`;
- lower sleep tier имеет Mood cap 50;
- Floor имеет Alertness cap 75 и Mood gain 0;
- каждая кровать имеет два связанных slots;
- personal bed недоступна при отсутствии пути или path length >30.

### Общество и образование

- пары только мужчина+женщина, эксклюзивны до смерти партнёра;
- successful eligible meeting гарантирует conception;
- pregnancy длится 1 день, postpartum cooldown 2 дня, child count без лимита;
- childhood длится 2 дня;
- школа: 1 teacher, 4 students, 24/7;
- curriculum поддерживает все 12 навыков;
- inheritance выполняет отдельный random penalty 10–20% для каждого skill;
- повышенная TotalSkillCapacity наследуется и не может быть ниже суммы inherited skills.

### Экология и двери

- creature populations имеют обязательные data-driven caps;
- tamed Vuker не размножается;
- swallowed demon item выпадает после смерти;
- spider egg может вылупиться внутри inventory/storage;
- omelet permanently/stackably повышает один случайный Need maximum на 10 и может превысить 100;
- door variants: wooden/metal/crystal, stone excluded;
- AutomaticOwnOnly закрывается после 2 clear simulation ticks;
- liquids/switches не входят в первый scope;
- destroyed door оставляет свободный проход.

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
- Q-040 — food Mood cap 50.

Q-041–Q-046 закрыты и перенесены в соответствующие design-файлы.

## Связанные issues

- technology/system specs: #126–#147, #149–#152;
- exploration design closed #147, implementation #165;
- taste profiles rejected/closed #144;
- food #96–#101, #159;
- leisure/sleep/society/education #142–#146;
- ecology #149;
- doors #136;
- skills #103–#107, #117;
- buildings #74–#82, #108, #118;
- excavation/resources #87–#94, #109–#110;
- HUD #113–#117.
