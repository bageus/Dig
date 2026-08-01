# Индекс систем Dig

Статус: обязательная точка входа для поиска требований.

Tracking issues: [#385](https://github.com/bageus/Dig/issues/385), [#393](https://github.com/bageus/Dig/issues/393), [#394](https://github.com/bageus/Dig/issues/394), [#403](https://github.com/bageus/Dig/issues/403).

Актуальные аудиты:

- полнота описания, реализации и test evidence: [`../implementation/implemented-systems-audit-2026-07-26.md`](../implementation/implemented-systems-audit-2026-07-26.md);
- готовность issues к закрытию и status synchronization: [`../implementation/open-issues-closure-audit-2026-07-28.md`](../implementation/open-issues-closure-audit-2026-07-28.md), обновлено 2026-07-29.

Статусы отражают текущий `main`. Открытый PR не повышает status до merge.

## Как использовать индекс

1. Найти заголовок или alias, совпадающий с запросом.
2. Открыть authoritative specification.
3. Открыть tracking issue и implementation map.
4. При `QUESTIONNAIRE` не придумывать открытые бизнес-правила — задать перечисленные вопросы.
5. Если подходящего заголовка нет, использовать [`../development/system-specification-template.md`](../development/system-specification-template.md) и процесс [`../development/system-specification-workflow.md`](../development/system-specification-workflow.md).

Статусы:

- `DRAFT` — отдельная полная спецификация ещё не утверждена;
- `QUESTIONNAIRE` — есть открытые вопросы, влияющие на observable behavior;
- `APPROVED` — требования утверждены;
- `IMPLEMENTED` — требования реализованы и покрыты автоматическими тестами;
- `VERIFIED` — дополнительно проверен полный runtime/Play Mode workflow.

Статус относится к полноте описания и evidence, а не гарантирует отсутствие багов. Issue может оставаться открытой при `IMPLEMENTED`, если её собственный acceptance требует `VERIFIED` evidence.

## Системы, описанные в текущих проектных чатах

| Заголовок | Aliases | Статус | Authoritative specification | Tracking |
|---|---|---|---|---|
| Контекстный ввод, курсоры и selection | cursor, shovel, pickup arrow, movement feet, erase, input priority, roster tab | `QUESTIONNAIRE` | [`contextual-input-cursors-and-selection.md`](../design/contextual-input-cursors-and-selection.md), [`runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md) | [#390](https://github.com/bageus/Dig/issues/390), [#398](https://github.com/bageus/Dig/issues/398) |
| BuildingBox selection, unpacking, placement, assembly и packing | коробка здания, меню строения, кнопка распаковать, final-building ghost, pack | `IMPLEMENTED` | [`building-box-placement-and-packing.md`](../design/building-box-placement-and-packing.md), [`runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md) + input/item contracts | [#118](https://github.com/bageus/Dig/issues/118), [#390](https://github.com/bageus/Dig/issues/390), [#398](https://github.com/bageus/Dig/issues/398), [#495](https://github.com/bageus/Dig/pull/495) |
| Мировые предметы: gravity, visibility, selection, pickup и placement | падение коробки, предмет на полу, Alt+LMB, item ghost, drop | `QUESTIONNAIRE` | [`world-item-gravity-selection-and-pickup.md`](../design/world-item-gravity-selection-and-pickup.md), [`runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md), [`runtime-stacking-barrel-and-room-recovery-decisions.md`](../design/runtime-stacking-barrel-and-room-recovery-decisions.md) | [#387](https://github.com/bageus/Dig/issues/387), [#390](https://github.com/bageus/Dig/issues/390) |
| Падение предметов, гномов и врагов | vertical shaft, knockback, fall damage, landing | `QUESTIONNAIRE` | [`entity-fall-knockback-and-vertical-shafts.md`](../design/entity-fall-knockback-and-vertical-shafts.md) | [#396](https://github.com/bageus/Dig/issues/396) |
| Многоклеточная копка и прямые приказы | connected zone, tunnel chain, depth, room, direct excavation, quarter progress | `APPROVED` | [`excavation-command-execution.md`](../design/excavation-command-execution.md), [`runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md) | [#388](https://github.com/bageus/Dig/issues/388) |
| Excavation plans, rooms, depth и deposits | тоннель, глубина, комната, eraser, жила | `APPROVED` | [`excavation-room-templates-and-deposits.md`](../design/excavation-room-templates-and-deposits.md), [`runtime-stacking-barrel-and-room-recovery-decisions.md`](../design/runtime-stacking-barrel-and-room-recovery-decisions.md) | [#87](https://github.com/bageus/Dig/issues/87) |
| Resident movement, directional lanes и vertical climbing | обход гномов, shared cell, right/left lane, карабканье | `APPROVED` | [`resident-movement-occupancy-and-vertical-traversal.md`](../design/resident-movement-occupancy-and-vertical-traversal.md) | [#386](https://github.com/bageus/Dig/issues/386) |
| Пространственное выполнение боя | engagement, approach, pursuit, assist, retreat, combat LoS | `QUESTIONNAIRE` | [`combat-spatial-execution.md`](../design/combat-spatial-execution.md) | [#508](https://github.com/bageus/Dig/issues/508) |
| Resident/building roster synchronization | открыть вкладку, подсветить строку, world/HUD selection | `QUESTIONNAIRE` | [`contextual-input-cursors-and-selection.md`](../design/contextual-input-cursors-and-selection.md) | [#390](https://github.com/bageus/Dig/issues/390) |
| Demo campfire + packed box | нижняя пещера, готовый костёр, коробка костра | `QUESTIONNAIRE` | [`demo-starting-scenario.md`](../design/demo-starting-scenario.md) | [#389](https://github.com/bageus/Dig/issues/389) |
| Рост и прямая рубка грибов | гриб, mushroom, дерево, рубка, топор, regrowth, mushroom cap, mushroom leg, woodworking | `IMPLEMENTED` | [`mushroom-growth-and-chopping.md`](../design/mushroom-growth-and-chopping.md) | [#423](https://github.com/bageus/Dig/issues/423) |
| Разрушаемые бочки с содержимым и падением | barrel, бочка, sword cursor, attack barrel, loot, safe falling | `IMPLEMENTED` | [`destructible-barrels.md`](../design/destructible-barrels.md), [`runtime-stacking-barrel-and-room-recovery-decisions.md`](../design/runtime-stacking-barrel-and-room-recovery-decisions.md) | [#443](https://github.com/bageus/Dig/issues/443) |
| Производство в зданиях и внутреннее снабжение | костёр, recipe icon, очередь, internal stock, refill, output package, food, weapon, tool, forced move | `APPROVED` | [`building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md) | [#433](https://github.com/bageus/Dig/issues/433), [#501](https://github.com/bageus/Dig/pull/501) |
| Приготовление и использование пищи в костре | grilled mushroom, гриль-гриб, шляпка гриба, Cooking, pickup arrow, зелёный рот, Alt+LMB, три укуса | `IMPLEMENTED` | [`campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md) | [#459](https://github.com/bageus/Dig/issues/459) |
| Simulation/job fault isolation | один job не останавливает всех гномов | `QUESTIONNAIRE` | [`excavation-command-execution.md`](../design/excavation-command-execution.md), [`../implementation/simulation-runtime.md`](../implementation/simulation-runtime.md) | [#388](https://github.com/bageus/Dig/issues/388) |
| Presentation host, input, UI и diagnostics | bootstrap, renderer, inspector, read model, notification ticker, debug overlay, 64 residents | `IMPLEMENTED` | [`presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md) | [#14](https://github.com/bageus/Dig/issues/14), [#511](https://github.com/bageus/Dig/issues/511) |

## Runtime foundation

| Заголовок | Aliases | Статус | Authoritative specification | Implementation / issue |
|---|---|---|---|---|
| Simulation loop и fixed ticks | runtime, time, scheduler, cadence | `IMPLEMENTED` | [`../architecture/systems-core.md`](../architecture/systems-core.md#1-runtime-и-фундамент) | [`../implementation/simulation-runtime.md`](../implementation/simulation-runtime.md), [#2](https://github.com/bageus/Dig/issues/2) |
| Entity identity | stable ID, entity registry | `IMPLEMENTED` | [`../architecture/systems-core.md`](../architecture/systems-core.md#1-runtime-и-фундамент) | [#1](https://github.com/bageus/Dig/issues/1) |
| Commands, events и queries | CQRS, command pipeline, event journal | `IMPLEMENTED` | [`../development-rules.md`](../development-rules.md#6-команды-события-и-запросы) | [`../architecture/module-contracts.md`](../architecture/module-contracts.md) |
| Ошибкоустойчивость simulation execution | driver failure, job exception, fault isolation | `DRAFT` | [`excavation-command-execution.md`](../design/excavation-command-execution.md) | [#388](https://github.com/bageus/Dig/issues/388) |

## Мир, копание и exploration

| Заголовок | Aliases | Статус | Authoritative specification | Implementation / issue |
|---|---|---|---|---|
| 3D cell world и глубина | XYZ, Z0..3, layered world | `IMPLEMENTED` | [`../design/world-3d-depth.md`](../design/world-3d-depth.md) | [`../implementation/world-state.md`](../implementation/world-state.md), [`../implementation/issue-88-authoritative-xyz-closure-2026-07-30.md`](../implementation/issue-88-authoritative-xyz-closure-2026-07-30.md), [#88](https://github.com/bageus/Dig/issues/88) |
| Excavation plans и cave templates | tunnel, room, depth designation | `IMPLEMENTED` | [`../design/excavation-room-templates-and-deposits.md`](../design/excavation-room-templates-and-deposits.md), [`../design/runtime-stacking-barrel-and-room-recovery-decisions.md`](../design/runtime-stacking-barrel-and-room-recovery-decisions.md) | [`../implementation/z0-excavation-planning.md`](../implementation/z0-excavation-planning.md), [#87](https://github.com/bageus/Dig/issues/87), [#89](https://github.com/bageus/Dig/issues/89), [#90](https://github.com/bageus/Dig/issues/90), [PR #520](https://github.com/bageus/Dig/pull/520) |
| Excavation execution | direct dig, connected zone, automatic continuation, quarter progress | `APPROVED` | [`../design/excavation-command-execution.md`](../design/excavation-command-execution.md), [`../design/runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md) | [`../implementation/unity-terrain-work-vertical-slice.md`](../implementation/unity-terrain-work-vertical-slice.md), [`../implementation/runtime-selection-excavation-item-placement.md`](../implementation/runtime-selection-excavation-item-placement.md), [#388](https://github.com/bageus/Dig/issues/388) |
| Excavation cadence profiles | hardness, Stonework, pickaxe, posture, quarter skill grant | `IMPLEMENTED` | [`../design/excavation-cadence-profiles.md`](../design/excavation-cadence-profiles.md) | [`../implementation/excavation-cadence-profiles-2026-07-29.md`](../implementation/excavation-cadence-profiles-2026-07-29.md), [#388](https://github.com/bageus/Dig/issues/388), [PR #506](https://github.com/bageus/Dig/pull/506) |
| Entity falling и vertical shafts | unsupported item, actor knockback, landing | `QUESTIONNAIRE` | [`../design/entity-fall-knockback-and-vertical-shafts.md`](../design/entity-fall-knockback-and-vertical-shafts.md) | [#396](https://github.com/bageus/Dig/issues/396) |
| Terrain resources и output commit | ore, terrain profile, mining drop, output ledger | `IMPLEMENTED` | [`../design/terrain-resource-output-and-processing.md`](../design/terrain-resource-output-and-processing.md), [`../design/content/materials.md`](../design/content/materials.md) | [`../implementation/mining-output-save-data-contract.md`](../implementation/mining-output-save-data-contract.md), [`../implementation/issues-92-109-terrain-output-2026-07-30.md`](../implementation/issues-92-109-terrain-output-2026-07-30.md), [`../implementation/issue-541-demo-terrain-regions-and-deposits-2026-08-01.md`](../implementation/issue-541-demo-terrain-regions-and-deposits-2026-08-01.md), [#92](https://github.com/bageus/Dig/issues/92), [#109](https://github.com/bageus/Dig/issues/109), [#541](https://github.com/bageus/Dig/issues/541), [PR #544](https://github.com/bageus/Dig/pull/544) |
| Deposits и depletion | resource veins, coal, crystal | `IMPLEMENTED` | [`../design/excavation-room-templates-and-deposits.md`](../design/excavation-room-templates-and-deposits.md), [`../design/terrain-resource-output-and-processing.md`](../design/terrain-resource-output-and-processing.md) | [`../implementation/issue-91-deterministic-deposits-2026-07-30.md`](../implementation/issue-91-deterministic-deposits-2026-07-30.md), [`../implementation/issue-541-demo-terrain-regions-and-deposits-2026-08-01.md`](../implementation/issue-541-demo-terrain-regions-and-deposits-2026-08-01.md), [#91](https://github.com/bageus/Dig/issues/91), [#541](https://github.com/bageus/Dig/issues/541), [PR #544](https://github.com/bageus/Dig/pull/544) |
| Procedural generation | seed, deterministic world | `IMPLEMENTED` | [`../architecture/systems-core.md`](../architecture/systems-core.md#2-мир) | [`../implementation/world-generation.md`](../implementation/world-generation.md), [#3](https://github.com/bageus/Dig/issues/3) |
| Fog of war и exploration | reveal, vision source, hidden hauling | `APPROVED` | [`../design/exploration-fog-of-war.md`](../design/exploration-fog-of-war.md) | [#165](https://github.com/bageus/Dig/issues/165) |

## Navigation и residents

| Заголовок | Aliases | Статус | Authoritative specification | Implementation / issue |
|---|---|---|---|---|
| Navigation и pathfinding | A*, region, traversal link | `IMPLEMENTED` | [`../architecture/systems-core.md`](../architecture/systems-core.md#3-навигация-и-перемещение) | [`../implementation/navigation-pathfinding.md`](../implementation/navigation-pathfinding.md), [#4](https://github.com/bageus/Dig/issues/4) |
| Resident movement, directional lanes и vertical traversal | shared cells, edge swap, climb, Y transition, forced fast movement | `APPROVED` | [`../design/resident-movement-occupancy-and-vertical-traversal.md`](../design/resident-movement-occupancy-and-vertical-traversal.md) | [`../implementation/resident-movement.md`](../implementation/resident-movement.md), [#386](https://github.com/bageus/Dig/issues/386) |
| Resident autonomy, schedules и needs | work, eat, sleep, mood, health | `IMPLEMENTED` | [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#1-жители) | [`../implementation/resident-agents.md`](../implementation/resident-agents.md), [#6](https://github.com/bageus/Dig/issues/6) |
| Skills progression | 12 skills, cap, redistribution, university | `IMPLEMENTED` | [`../design/skills-progression.md`](../design/skills-progression.md) | [`../implementation/resident-agents.md`](../implementation/resident-agents.md), [#64](https://github.com/bageus/Dig/issues/64), [#421](https://github.com/bageus/Dig/issues/421), [PR #455](https://github.com/bageus/Dig/pull/455) |
| Resident inventory, baskets и weapon slots | six main slots, cargo, speed penalty, C+LMB quick drop, sheath, harness | `APPROVED` | [`../design/resident-inventory-expansion.md`](../design/resident-inventory-expansion.md), [`../design/item-interaction-capabilities.md`](../design/item-interaction-capabilities.md) | [`../implementation/inventory.md`](../implementation/inventory.md), [#67](https://github.com/bageus/Dig/issues/67), [#69](https://github.com/bageus/Dig/issues/69), [#210](https://github.com/bageus/Dig/issues/210) |
| Resident management UI | profile, family, health, needs, schedule, skills, job | `IMPLEMENTED` | [`../design/settlement-resident-management-ui.md`](../design/settlement-resident-management-ui.md) | [`../implementation/settlement-resident-management-ui.md`](../implementation/settlement-resident-management-ui.md), [#428](https://github.com/bageus/Dig/issues/428) |

## Предметы, hauling и storage

| Заголовок | Aliases | Статус | Authoritative specification | Implementation / issue |
|---|---|---|---|---|
| Inventory и world items | stacks, reservation, held item, drop | `APPROVED` | [`../design/world-item-gravity-selection-and-pickup.md`](../design/world-item-gravity-selection-and-pickup.md), [`../design/item-interaction-capabilities.md`](../design/item-interaction-capabilities.md) | [`../implementation/inventory.md`](../implementation/inventory.md), [#64](https://github.com/bageus/Dig/issues/64), [#67](https://github.com/bageus/Dig/issues/67), [#387](https://github.com/bageus/Dig/issues/387) |
| Generic item interaction capabilities | pickup, use, place, quick drop, BuildingBox select/Alt-pickup, food Alt-use | `APPROVED` | [`../design/item-interaction-capabilities.md`](../design/item-interaction-capabilities.md) | [#67](https://github.com/bageus/Dig/issues/67), [#387](https://github.com/bageus/Dig/issues/387), [#390](https://github.com/bageus/Dig/issues/390) |
| Storage zones и filters | priority, capacity, hauling | `IMPLEMENTED` | [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#2-предметы-и-логистика) | [`../implementation/hauling-storage.md`](../implementation/hauling-storage.md), [#7](https://github.com/bageus/Dig/issues/7) |
| Hauling jobs | reserve, pickup, deposit, retry | `IMPLEMENTED` | [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#2-предметы-и-логистика) | [`../implementation/hauling-storage.md`](../implementation/hauling-storage.md), [#7](https://github.com/bageus/Dig/issues/7) |
| Resident equipment | held tool, work rate, auto/suggest mode | `IMPLEMENTED` | [`../design/resident-equipment.md`](../design/resident-equipment.md) | [`../implementation/resident-equipment.md`](../implementation/resident-equipment.md), [#70](https://github.com/bageus/Dig/issues/70) |

## Строительство и производство

| Заголовок | Aliases | Статус | Authoritative specification | Implementation / issue |
|---|---|---|---|---|
| Building lifecycle | placement, delivery, construction, repair, removal | `IMPLEMENTED` | [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#3-строительство) | [`../implementation/building-construction.md`](../implementation/building-construction.md), [#11](https://github.com/bageus/Dig/issues/11) |
| BuildingBox lifecycle | pack, unpack, relocate, world/inventory box | `IMPLEMENTED` | [`../design/building-box-placement-and-packing.md`](../design/building-box-placement-and-packing.md) | [`../implementation/building-box-placement-and-packing.md`](../implementation/building-box-placement-and-packing.md), [#118](https://github.com/bageus/Dig/issues/118) |
| Generic production and internal supply | queue, recipes, internal stock, staged package, output zones | `APPROVED` | [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md) | [`../implementation/building-production-and-internal-supply.md`](../implementation/building-production-and-internal-supply.md), [#433](https://github.com/bageus/Dig/issues/433) |
| Campfire cooking and food use | grilled mushroom, roasted hamster, three bites, food cursors | `IMPLEMENTED` | [`../design/campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md) | [`../implementation/campfire-cooking-and-food-use.md`](../implementation/campfire-cooking-and-food-use.md), [#459](https://github.com/bageus/Dig/issues/459) |

## Социальные системы, бой и AI

| Заголовок | Aliases | Статус | Authoritative specification | Implementation / issue |
|---|---|---|---|---|
| Family graph и reproduction | parents, relatives, pregnancy, inheritance | `IMPLEMENTED` | [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#5-общество-и-прогресс) | [`../implementation/society.md`](../implementation/society.md), [#13](https://github.com/bageus/Dig/issues/13) |
| Combat и health | attack, damage, status effects, healing | `IMPLEMENTED` | [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#4-конфликт) | [`../implementation/combat-factions-strategy.md`](../implementation/combat-factions-strategy.md), [#10](https://github.com/bageus/Dig/issues/10), [#12](https://github.com/bageus/Dig/issues/12) |
| Factions, diplomacy и territory | allies, enemies, claims | `IMPLEMENTED` | [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#4-конфликт) | [`../implementation/combat-factions-strategy.md`](../implementation/combat-factions-strategy.md), [#12](https://github.com/bageus/Dig/issues/12) |
| Strategic AI | goals, plans, deterministic cadence | `IMPLEMENTED` | [`../architecture/systems-gameplay.md`](../architecture/systems-gameplay.md#6-ai) | [`../implementation/strategic-ai.md`](../implementation/strategic-ai.md), [#15](https://github.com/bageus/Dig/issues/15) |

## Сохранение и presentation

| Заголовок | Aliases | Статус | Authoritative specification | Implementation / issue |
|---|---|---|---|---|
| Save/load и migrations | save slot, autosave, corruption, schema migration | `IMPLEMENTED` | [`../design/save-load-and-migrations.md`](../design/save-load-and-migrations.md) | [`../implementation/save-load.md`](../implementation/save-load.md), [#9](https://github.com/bageus/Dig/issues/9) |
| Presentation adapters | HUD, overlays, world view, diagnostics | `IMPLEMENTED` | [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md) | [`../implementation/vertical-slice-presentation.md`](../implementation/vertical-slice-presentation.md), [#14](https://github.com/bageus/Dig/issues/14) |
| Runtime diagnostics | job inspector, performance, invariant reports | `IMPLEMENTED` | [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md) | [`../implementation/simulation-runtime.md`](../implementation/simulation-runtime.md), [#14](https://github.com/bageus/Dig/issues/14) |

## Source-of-truth rules

При конфликте источников применяется порядок из `docs/development-rules.md`: latest confirmed decision in authoritative design/issue → approved design → issue acceptance → ADR/rules → implementation notes → code.
