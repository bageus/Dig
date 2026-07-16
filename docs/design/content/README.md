# Каталог игрового контента Dig

## Назначение

Индекс authoritative design-документов. Runtime state остаётся в Domain/Application владельцах.

## Content

- [`buildings.md`](buildings.md) — здания и services;
- [`products.md`](products.md) — outputs;
- [`weapons-and-shields.md`](weapons-and-shields.md) — equipment;
- [`materials.md`](materials.md) — materials, ores, terrain;
- [`food.md`](food.md) — food, bites и variety;
- [`alcohol.md`](alcohol.md) — drinks;
- [`skills.md`](skills.md) — skill IDs.

## Resident и Needs

- [`../needs-continuous-actions.md`](../needs-continuous-actions.md);
- [`../leisure-variety-and-selection.md`](../leisure-variety-and-selection.md);
- [`../partnership-pregnancy-and-birth.md`](../partnership-pregnancy-and-birth.md);
- [`../childhood-school-and-inheritance.md`](../childhood-school-and-inheritance.md);
- [`../sleep-comfort-and-bed-assignment.md`](../sleep-comfort-and-bed-assignment.md);
- [`../resident-role-headwear.md`](../resident-role-headwear.md);
- [`../resident-inventory-expansion.md`](../resident-inventory-expansion.md);
- [`../resident-hud-selection-and-notifications.md`](../resident-hud-selection-and-notifications.md);
- [`../skills-and-progression.md`](../skills-and-progression.md).

## Technology, Energy и Navigation

- [`../technology-tree.md`](../technology-tree.md);
- [`../research-availability-duration-and-ui.md`](../research-availability-duration-and-ui.md);
- [`../energy-generation-and-production-pausing.md`](../energy-generation-and-production-pausing.md);
- [`../ladders-and-elevators.md`](../ladders-and-elevators.md);
- [`../building-box-placement-and-packing.md`](../building-box-placement-and-packing.md);
- [`../doors-access-and-lifecycle.md`](../doors-access-and-lifecycle.md).

## World

- [`../world-3d-depth.md`](../world-3d-depth.md);
- [`../exploration-fog-of-war.md`](../exploration-fog-of-war.md);
- [`../excavation-room-templates-and-deposits.md`](../excavation-room-templates-and-deposits.md);
- [`../terrain-resource-output-and-processing.md`](../terrain-resource-output-and-processing.md);
- [`../material-demand-and-hauling.md`](../material-demand-and-hauling.md);
- [`../ecology-creatures-and-special-drops.md`](../ecology-creatures-and-special-drops.md).

## Registers

- [`../scripts-system-gap-backlog.md`](../scripts-system-gap-backlog.md);
- [`../open-questions.md`](../open-questions.md) — основной реестр;
- [`../open-questions-047-051.md`](../open-questions-047-051.md) — подробности текущих вопросов.

## Владение состоянием

1. World — terrain/deposits; Exploration — visibility.
2. Inventory — items/locations/boxes.
3. Buildings — buildings/functions/places.
4. Production — orders/recipes/progress.
5. Technology — eligibility/research/unlocks.
6. Energy — source/batch/allocation state.
7. Navigation/Transport — routes/cabin/queue.
8. Agents — needs/actions/skills/history/role visuals.
9. Society/Lifecycle — family/age/identity transitions.
10. Presentation — local view state only.

## Новые подтверждённые правила

- qualified resident лично выполняет Research job в Work schedule;
- Research duration выводится из weighted production recipe;
- active Energy catalog содержит два class-1, один class-2 и один class-3 source;
- Production progress сохраняется при временном отсутствии питания;
- carts/rails исключены, ladders отличаются длиной;
- elevator обслуживает requests по направлению;
- role headwear отражает current/last work role.

## Открыто

- Q-014 — balance;
- Q-034, Q-036, Q-037 — research/farm;
- Q-047–Q-051 — role visuals, lifecycle item, Energy allocation, Research UI/experience, ladders/elevators.
