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
| Terrain resources и processing | ore, terrain output, refinery | `APPROVED` | [`../design/terrain-resource-output-and-processing.md`](../design/terrain-resource-output-and-processing.md) | [`../implementation/mining-output-save-data-contract.md`](../implementation/mining-output-save-data-contract.md), [#109](https://github.com/bageus/Dig/issues/109) |
| Deposits и depletion | resource veins, coal, crystal | `IMPLEMENTED` | [`../design/excavation-room-templates-and-deposits.md`](../design/excavation-room-templates-and-deposits.md), [`../design/terrain-resource-output-and-processing.md`](../design/terrain-resource-output-and-processing.md) | [`../implementation/issue-91-deterministic-deposits-2026-07-30.md`](../implementation/issue-91-deterministic-deposits-2026-07-30.md), [#91](https://github.com/bageus/Dig/issues/91) |
| Procedural generation | seed, deterministic world | `IMPLEMENTED` | [`../architecture/systems-core.md`](../architecture/systems-core.md#2-мир) | [`../implementation/world-generation.md`](../implementation/world-generation.md), [#3](https://github.com/bageus/Dig/issues/3) |
| Fog of war и exploration | reveal, vision source, hidden hauling | `APPROVED` | [`../design/exploration-fog-of-war.md`](../design/exploration-fog-of-war.md) | [#165](https://github.com/bageus/Dig/issues/165) |

## Navigation и residents

| Заголовок | Aliases | Статус | Authoritative specification | Implementation / issue |
|---|---|---|---|---|
| Traversability, regions и pathfinding | route, walkability, replan | `IMPLEMENTED` | [`../architecture/systems-core.md`](../architecture/systems-core.md#3-навигация) | [`../implementation/navigation.md`](../implementation/navigation.md), [#4](https://github.com/bageus/Dig/issues/4) |
| Resident movement occupancy | shared cell, directional lanes, passing, no swap | `APPROVED` | [`../design/resident-movement-occupancy-and-vertical-traversal.md`](../design/resident-movement-occupancy-and-vertical-traversal.md) | [`../implementation/layered-tunnel-movement.md`](../implementation/layered-tunnel-movement.md), [#386](https://github.com/bageus/Dig/issues/386) |
| Ladders, elevators и mobility | vertical links, personal mobility | `APPROVED` | [`../design/ladders-and-elevators.md`](../design/ladders-and-elevators.md) | [#51](https://github.com/bageus/Dig/issues/51) |
