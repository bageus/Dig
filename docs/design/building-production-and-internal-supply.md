# Производство в зданиях и внутреннее снабжение

Статус: `APPROVED` для revised spatial workflow от 2026-07-29; предыдущая реализация `IMPLEMENTED`, runtime verification остаётся обязательной.

Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

Связанные документы:

- [`campfire-cooking-and-food-use.md`](campfire-cooking-and-food-use.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-movement-occupancy-and-vertical-traversal.md`](resident-movement-occupancy-and-vertical-traversal.md).

## 1. Назначение

Система позволяет completed workstation производить предметы и BuildingBox через data-driven recipes, автоматически пополнять защищённый внутренний запас и показывать полный spatial workflow в мире. Runtime не содержит отдельных веток вида `if campfire` или `if stone_mason`: новые здания, stock rules и recipes добавляются content definitions.

## 2. Владение состоянием

- `ProductionContentCatalog` владеет immutable recipes и workstation definitions.
- `ProductionState` владеет очередями, active order, material-step progress и consumed-input ledger.
- `InventoryState` владеет physical item entities, quantities, reservations, resident cargo, `ItemLocation.InBuilding` и world outputs.
- `BuildingSupplyState` владеет delivery toggles, incoming quantities и active supply request каждого building instance.
- `JobSystem` владеет production/supply jobs, worker claims и position/item reservations.
- `BuildingsState` владеет footprint, orientation и work position.
- `Agents` владеет authoritative resident position и skills.
- `Presentation` показывает icons, counters, zones, piles, hover и post-work pose, но не меняет authoritative production/inventory state.

## 3. Data model

```text
ProductionWorkstationDefinition
- BuildingDefinitionId
- AnimationProfileId
- RecipeIds[]
- InternalStockRules[]
- OutputPlacement = RightSideOutputZone

InternalStockRule
- ItemId
- Capacity
- DefaultDeliveryEnabled
- Priority

RecipeDefinition
- RecipeId
- WorkstationId
- Inputs[]
- Outputs[]
- MaterialSteps[]
- SkillGrantProfile
- SkillGrantScale = PerOrder | PerOutputUnit
- RequiredTool / Technology / Energy

RecipeMaterialStep
- ItemId
- SkillId
- BaseDurationTicks
```

`MaterialSteps` покрывают каждую единицу input точно один раз. Порядок content-stable и сохраняется через save/load.

## 4. Campfire content

Stable IDs:

- `building.campfire`, `building_box.campfire`;
- `building.tent`, `building_box.tent`;
- `building.stone_mason`, `building_box.stone_mason`;
- `building.wood_workshop`, `building_box.wood_workshop`;
- `food.grilled_mushroom`;
- `food.roasted_hamster`.

Внутренний запас campfire:

| ItemId | Capacity | Delivery default |
|---|---:|---|
| `material.mushroom_cap` | 4 | enabled |
| `material.mushroom_leg` | 4 | enabled |
| `material.stone` | 4 | enabled |
| `creature.hamster` | 2 | enabled |

Recipes:

| Product | Inputs | Output | Skill |
|---|---|---|---|
| Tent | 2 legs + 1 cap | 1 tent BuildingBox | Woodworking |
| Stone mason | 2 legs + 4 stone | 1 stone-mason BuildingBox | Woodworking + Stonework |
| Wooden workshop | 4 legs + 1 cap | 1 wooden-workshop BuildingBox | Woodworking |
| Campfire | 2 legs + 2 stone | 1 campfire BuildingBox | Woodworking + Stonework |
| Grilled mushroom | 1 cap | 2 grilled mushrooms in one stack | Cooking |
| Grilled hamster | 1 hamster | 2 roasted hamsters in one stack | Cooking |

## 5. Building UI и queue counter

Selecting a completed workstation opens its building functions panel.

- Production area contains only product/building icons.
- Hover shows recipe input quantities from the same snapshot used by validation.
- LMB on a product icon enqueues exactly one order.
- RMB on the same icon cancels exactly one order: newest queued order first, active order only if no queued order remains.
- RMB at projected count `0` is a consumed no-op; no command is sent and the count cannot become negative.
- A separate decrement/minus icon is forbidden.
- Queue count equals all non-terminal orders for that recipe/building.
- After one order reaches terminal `Completed` or `Cancelled`, the projected counter decreases by exactly one on the next refresh.
- Missing current stock colors the product icon orange but does not block enqueue.
- Internal-stock icons show current/capacity/incoming and toggle automatic delivery.

## 6. Authoritative spatial zones

Every completed workstation exposes two distinct presentation zones in screen/world X order, independent of building orientation:

1. **Left zone — internal input storage.** Its anchor starts immediately left of the leftmost building footprint cell and expands farther left when several material piles are shown.
2. **Right zone — finished output.** Its candidate strip starts immediately right of the rightmost footprint cell and expands farther right in deterministic order.

Both zones are visible even when empty. They are derived from the building footprint and are not separate saved entities.

### 6.1 Internal-storage zone

- Physical stock remains authoritative `ItemLocation.InBuilding(buildingId)`.
- Presentation renders one visible pile/unit per current quantity, grouped by ItemId, inside the left zone.
- Stock hit colliders are triggers and do not block Navigation.
- With one resident selected, ordinary LMB on an available unit creates a quantity-one pickup from the left-zone pickup cell.
- Only `AvailableQuantity` can be taken; active production reservations cannot be stolen.
- A successful direct pickup may create replacement demand on the next synchronization when delivery remains enabled.
- An item already in `ItemLocation.InBuilding` is never an eligible source for another automatic delivery, stockpile demand or building-supply job.
- Resident inventory is not an arbitrary automatic supply source; it is valid only as reserved transit cargo of its owning supply job.

### 6.2 Finished-output zone

- Production outputs are ordinary world item stacks in `ItemLocation.InWorld(outputCell)`.
- The output cell must be explored, open, supported, inside the world, outside every building footprint and not already occupied by a world item.
- Candidate order is `right edge + 1`, then `right edge + 2`, and so on, with stable Y/Z tie-break for multi-cell footprints.
- No side/rear fallback is allowed. If the right zone has no free supported candidate within the configured distance, the order remains `ReadyToComplete` with `production.output_space_unavailable` and retries without duplicate inputs, outputs or skill grants.
- Because finished products are ordinary world items, existing selection/pickup/hauling rules apply. They can be manually picked up.

## 7. Supply lifecycle

A demand exists when all are true:

1. building is completed and registered as a workstation;
2. delivery for ItemId is enabled;
3. `current + incoming < capacity`;
4. no active production order at that building is `InProgress` or `ReadyToComplete`.

Demand target is capacity. The planner reads only revealed, reachable, unreserved world stacks.

Worker flow:

```text
workstation check -> every reserved world source -> workstation deposit
```

The command handler reserves a deterministic mixed batch before movement. Partial valid plans are allowed when resident capacity is insufficient. Deposit commits reserved transit cargo to `ItemLocation.InBuilding(buildingId)`. Different ItemIds remain separate stacks. Cancel/failure/retry releases source quantity, incoming capacity and worker/position claims atomically.

For grilled mushroom, when internal stock and eligible world stock contain no cap but a visible/reachable Large mushroom exists, the composing food workflow may create one ordinary mushroom-chop dependency. The resulting world cap still enters this normal supply lifecycle.

## 8. Production lifecycle

1. An order may remain queued without inputs.
2. The next queued order becomes preparable only when the complete input set is available in internal stock.
3. Inputs are reserved for that order.
4. One eligible resident claims one `ProductionWorkJob`.
5. On begin, material-step durations are resolved from that worker's skills and stored authoritatively.
6. Work advances only at the workstation work position.
7. Completing a material step consumes one reserved input unit exactly once.
8. The same assigned resident remains owner through `Finalize`.
9. During `Finalize`, the output cell is resolved in the right finished-output zone and becomes the resident's movement target.
10. After reaching that cell, the assigned resident commits the output stack there, completes job/order, grants skills exactly once and releases reservations.
11. The completed order leaves the non-terminal queue projection, so the building counter decreases by one.
12. After placement, the resident keeps the output cell as authoritative logical position, shifts a small presentation-only distance away from the building and waits facing the camera until another authoritative movement/work state takes priority.

While an order is active, replenishment is deferred so mid-cycle consumption does not start a competing supply trip.

One production job produces one recipe order; output quantity may be greater than one in the same stack.

## 9. Timing and skills

For step skill `S` in points:

```text
effectiveDuration = max(1 tick, round(baseDuration * (100 - clamp(S, 0, 100)) / 100))
```

Resolved durations are saved at job start. Frame rate and reload cannot change an active plan. Skill grants are committed exactly once after output creation succeeds.

## 10. Multiple buildings and conflicts

- Each building instance owns independent queue, stock toggles, incoming ledger and active order.
- One production worker per building.
- One active supply job per building; its batch may include several ItemIds.
- A source quantity cannot be reserved by two jobs.
- Internal stock can be removed only by explicit direct pickup from available quantity or by its owning production transaction.
- Two buildings may not place outputs into the same world cell; current Inventory occupancy is rechecked at finalize.

## 11. Save/load and migration

Save data includes:

- workstation registration and definition ID;
- delivery toggles and incoming reservations;
- production queue sequence/status;
- current material-step index/progress/resolved duration;
- consumed inputs and remaining allocations;
- active production/supply job references and supply pickup batch.

The left/right zone geometry and resident presentation offset/facing are derived and are not saved. Output stacks are saved through their ordinary world `ItemLocation`. On load, routes and presentation poses are rebuilt; committed outputs/skill grants are never repeated.

## 12. Diagnostics

Diagnostics expose:

- building/workstation/recipe IDs;
- non-terminal queue count and active order;
- stock current/incoming/capacity/toggle;
- source eligibility and rejection reason;
- current material step, skill, duration and progress;
- consumed/reserved inputs;
- left input-zone anchor and right output candidates;
- chosen output cell or `production.output_space_unavailable`;
- assigned worker, finalize movement target and post-work pose owner.

## 13. Acceptance tests

Domain/Application:

- content validation and exact campfire recipe matrix;
- queue count increases/decreases by one and never becomes negative;
- internal stock remains protected from automatic delivery source selection;
- direct quantity-one pickup uses available internal stock and recreates demand;
- right-zone candidates are deterministic and never fall back to left/front/rear cells;
- occupied/unsupported right candidates are skipped, full zone blocks without duplicate commit;
- outputs remain ordinary pickup-capable world stacks;
- same assigned worker owns work, finalize movement and output commit;
- save/load mid-supply, mid-material step and ReadyToComplete is exactly-once;
- two workstations operate independently.

Unity Play Mode:

- completed workstation visibly shows an empty/filled left internal-storage zone and an empty/right finished-output zone;
- supplied materials appear in the left zone and remain clickable;
- an internal-stock item cannot become another automatic supply source;
- selected resident can take one available left-zone unit;
- LMB/RMB product icon count behavior remains correct;
- worker performs production at the building, walks to the chosen right-zone cell and places the output there;
- building queue counter decreases after completion;
- output can be selected/picked up through ordinary world-item workflow;
- worker then stands slightly away from the building facing the camera;
- repeat order uses the next free right-zone candidate;
- blocked output zone retries safely;
- save/load rebuilds both zones and does not duplicate output.

## 14. Out of scope

- final production art/animation clips;
- technology balancing beyond existing contracts;
- quality tiers or probabilistic bonus outputs;
- shared production priority across different buildings.

## 15. Decision log

| Date | Decision | Confirmed by |
|---|---|---|
| 2026-07-27 | Generic workstation definitions, recipes, protected internal stock, progressive consumption, deferred replenishment and per-material timing. | User |
| 2026-07-28 | Supply begins with workstation check; available internal stock supports quantity-one direct pickup. | User |
| 2026-07-28 | Assigned cook finalizes one quantity-two food output stack. | User |
| 2026-07-29 | LMB/RMB on the same product icon add/cancel one order; separate minus icon removed. | User |
| 2026-07-29 | Buildings expose a visible left internal-storage zone and right finished-output zone; internal stock is not an automatic delivery source; the assigned worker carries finalize to the right zone; completed output is pickup-capable; the queue count decreases on completion; worker waits slightly away facing the camera. | User |
