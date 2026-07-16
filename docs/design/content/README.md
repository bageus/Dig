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
- systems from legacy scripts — #127–#147, #149–#152 и deferred #177.

## Файлы

- [`buildings.md`](buildings.md) — здания и функции;
- [`products.md`](products.md) — производственные выходы;
- [`weapons-and-shields.md`](weapons-and-shields.md) — основной colony-mode equipment catalog, recipes, skills и slots;
- [`legacy-combat-equipment-appendix.md`](legacy-combat-equipment-appendix.md) — deferred fantasy classes и окончательные special-mode exclusions;
- [`materials.md`](materials.md) — материалы и руды;
- [`food.md`](food.md) — питание;
- [`alcohol.md`](alcohol.md) — напитки;
- [`skills.md`](skills.md) — skill IDs;
- [`../technology-tree.md`](../technology-tree.md) — дерево технологий;
- [`../research-availability-duration-and-ui.md`](../research-availability-duration-and-ui.md) — research queue, UI, duration и slots;
- [`../energy-generation-and-production-pausing.md`](../energy-generation-and-production-pausing.md) — энергия;
- [`../ladders-and-elevators.md`](../ladders-and-elevators.md) — лестницы, лифты и mobility tools;
- [`../partnership-pregnancy-and-birth.md`](../partnership-pregnancy-and-birth.md) — связи жителей и lifecycle семьи;
- [`../death-graves-resurrection-and-rejuvenation.md`](../death-graves-resurrection-and-rejuvenation.md) — смерть, могилы и возвращение;
- [`../resident-role-headwear.md`](../resident-role-headwear.md) — внешний вид;
- [`../needs-continuous-actions.md`](../needs-continuous-actions.md) — Needs;
- [`../leisure-variety-and-selection.md`](../leisure-variety-and-selection.md) — досуг;
- [`../childhood-school-and-inheritance.md`](../childhood-school-and-inheritance.md) — школа;
- [`../sleep-comfort-and-bed-assignment.md`](../sleep-comfort-and-bed-assignment.md) — сон;
- [`../ecology-creatures-and-special-drops.md`](../ecology-creatures-and-special-drops.md) — экология;
- [`../doors-access-and-lifecycle.md`](../doors-access-and-lifecycle.md) — двери;
- [`../exploration-fog-of-war.md`](../exploration-fog-of-war.md) — исследование мира;
- [`../skills-and-progression.md`](../skills-and-progression.md) — прогрессия;
- [`../resident-inventory-expansion.md`](../resident-inventory-expansion.md) — инвентарь;
- [`../resident-hud-selection-and-notifications.md`](../resident-hud-selection-and-notifications.md) — HUD;
- [`../building-box-placement-and-packing.md`](../building-box-placement-and-packing.md) — коробки зданий;
- [`../excavation-room-templates-and-deposits.md`](../excavation-room-templates-and-deposits.md) — копка;
- [`../terrain-resource-output-and-processing.md`](../terrain-resource-output-and-processing.md) — выходы грунта;
- [`../material-demand-and-hauling.md`](../material-demand-and-hauling.md) — доставка;
- [`../scripts-system-gap-backlog.md`](../scripts-system-gap-backlog.md) — backlog scripts;
- [`../open-questions.md`](../open-questions.md) — основной реестр;
- [`../open-questions-047-051.md`](../open-questions-047-051.md) — Q-047–Q-052.

## Правила ведения

1. Display names не являются IDs.
2. Явно заданные числа считаются стартовым балансом.
3. Остальные коэффициенты data-driven/BALANCE_TBD.
4. Legacy scripts дают candidates; принятыми становятся только явно согласованные правила.
5. Animation callback не создаёт authoritative result.
6. Изменение stable IDs требует migration.
7. Deferred content не входит в runtime до отдельного owner decision.
8. Excluded content является validation error при colony reference.

## Последние закрытые решения

- Q-034/Q-036/Q-050: research materials не расходуются; уголь и руды имеют вес 2; одно здание имеет один active slot; busy state белый; начатая работа продолжается после снижения skill; zero-input fallback мгновенный.
- Q-051: emergency exit идёт к целевой площадке; Reithamster/Hoverboard используют legacy `speedtype 3/2`; отдельных числовых скоростей в TCL нет.
- Q-052: новая текущая связь сохраняется; прежняя остаётся исторической.
- Q-053: основной каталог ограничен десятью предметами; skill/slot/ammo/no-wear policies утверждены; 32 fantasy classes перенесены в #177; modern special-mode content исключён.

## Актуальные открытые решения

- Q-014 — balance values, включая combat coefficients и числовой mobility multiplier;
- Q-037 — runtime model действий фермы.

## Связанные issues

- technology/energy/research: #126–#128;
- основной combat equipment: #129;
- deferred fantasy equipment: #177;
- transport/doors: #136–#137;
- lifecycle/appearance: #145, #150–#151;
- food #96–#101, #159;
- skills #103–#107, #117;
- buildings #74–#82, #108, #118;
- excavation/resources #87–#94, #109–#110;
- HUD #113–#117.