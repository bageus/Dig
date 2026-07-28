# Производство в зданиях и внутреннее снабжение

Статус: `IMPLEMENTED`.

Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

## 1. Назначение

Система позволяет completed building производить предметы и `BuildingBox` других зданий через data-driven definitions. Runtime не содержит веток вида `if campfire` или `if stone_mason`: новое производящее здание, recipe, внутренний stock и будущая animation добавляются content definitions.

Первый вертикальный slice подключает производство к распакованному костру.

## 2. Владение состоянием

- `ProductionContentCatalog` владеет immutable recipes и workstation definitions.
- `ProductionState` владеет очередями, active order, material-step progress и consumed-input ledger.
- `InventoryState` владеет физическими input/output stacks, quantity reservations, building-internal location и resident cargo.
- `BuildingSupplyState` владеет delivery toggles, incoming quantities и active supply request для каждого completed building instance.
- `JobSystem` владеет production/supply jobs, worker claims и position reservations.
- `BuildingsState` владеет существованием, orientation и work/output positions building instance.
- `Presentation` показывает icons, tooltip, queue count, orange shortage, stock toggles/stacks и future animation, но не изменяет authoritative state напрямую.

## 3. Data model

```text
ProductionWorkstationDefinition
- BuildingDefinitionId
- AnimationProfileId             # сохраняется сейчас, используется Presentation позже
- RecipeIds[]
- InternalStockRules[]
- OutputPlacement = FrontFreeCell

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
- MaterialSteps[]                # one step per consumed input unit
- SkillGrantProfile
- SkillGrantScale = PerOrder | PerOutputUnit
- RequiredTool / Technology / Energy

RecipeMaterialStep
- ItemId
- SkillId
- BaseDurationTicks
```

`MaterialSteps` must cover each input quantity exactly. Their order is content order and is stable across save/load.

## 4. Campfire content

Stable IDs:

- `building.campfire`, `building_box.campfire`;
- `building.tent`, `building_box.tent`;
- `building.stone_mason`, `building_box.stone_mason`;
- `building.wood_workshop`, `building_box.wood_workshop`;
- `food.grilled_mushroom`;
- `food.roasted_hamster`.

Campfire animation profile: `production.animation.campfire`. На этом этапе animation не проигрывается, но ID входит в validated content.

### 4.1 Internal stock

| ItemId | Capacity | Delivery default |
|---|---:|---|
| `material.mushroom_cap` | 4 | enabled |
| `material.mushroom_leg` | 4 | enabled |
| `material.stone` | 4 | enabled |
| `creature.hamster` | 2 | enabled |

### 4.2 Recipes

| Icon/order | Inputs | Output | Material timing | Skill grant per completed order |
|---|---|---|---|---|
| Tent | 2 legs + 1 cap | 1 `building_box.tent` | all Woodworking | +2.0 Woodworking |
| Stone mason workshop | 2 legs + 4 stone | 1 `building_box.stone_mason` | legs Woodworking; stone Stonework | +1.0 Woodworking, +3.0 Stonework |
| Wooden workshop | 4 legs + 1 cap | 1 `building_box.wood_workshop` | all Woodworking | +3.0 Woodworking |
| Campfire | 2 legs + 2 stone | 1 `building_box.campfire` | legs Woodworking; stone Stonework | +1.0 Woodworking, +1.0 Stonework |
| Grilled mushroom | 1 cap | 2 `food.grilled_mushroom` | Cooking | +1.2 Cooking |
| Grilled hamster | 1 hamster | 2 `food.roasted_hamster` | Cooking | +1.8 Cooking |

For building recipes mushroom cap and hamster material steps use Woodworking; cooking recipes use Cooking for food inputs.

## 5. UI and commands

Selecting a completed workstation opens its building functions panel.

- Production area contains only product/building icons.
- Hover uses the same recipe snapshot as queue/validation and shows ingredient quantities.
- LMB on an icon enqueues exactly one order.
- Queue count beside the icon equals non-terminal orders for that recipe/building.
- Missing current stock colors the icon orange but never prevents enqueue.
- Internal-stock area contains one icon per `InternalStockRule`; LMB toggles automatic delivery.
- Stock icon shows current/capacity and incoming quantity.
- Every recipe with a non-terminal queue count exposes a visible decrement control. One activation cancels the newest queued order for that recipe; if no queued order exists, it cancels the active order. Cancel releases unconsumed reservations; already consumed material steps remain consumed.

## 6. Supply lifecycle

A supply demand exists when all are true:

1. building is completed and has workstation definition;
2. delivery for ItemId is enabled;
3. `current + incoming < capacity`;
4. no production order at that building is `InProgress` or `ReadyToComplete`.

The demand target is capacity, not only current recipe quantity.

The command handler plans and reserves a deterministic batch from revealed, reachable, unreserved world stacks. After assignment, the resident first travels to the workstation work position and confirms the active reserved supply route before visiting its sources. Internal workstation inventory is a protected automatic source and is excluded from ordinary stockpile/building demands. A direct player pickup remains valid: with one resident selected, ordinary LMB on a visible internal-stock unit creates a quantity-one pickup at the workstation work position. The unit must be currently available rather than reserved by active production. After successful pickup, enabled delivery makes the missing unit eligible for the next supply job.

### 6.1 Batch selection

A supply job may reserve several ItemIds and source stacks. Capacity is limited by the assigned resident's currently free cargo/container slots and item stack limits.

Deterministic preference:

1. stock-rule priority and declaration order;
2. first ItemId is filled as far toward capacity as possible;
3. remaining slots may collect later ItemIds;
4. source choice uses path cost then stable stack ID.

If ten units are missing and only six slots are free, the job commits a valid partial plan. After deposit and demand refresh, another supply job may cover the rest.

The worker follows `workstation check -> every reserved source -> workstation deposit`. It acquires each allocation in deterministic order and deposits stacks into `ItemLocation.InBuilding(buildingId)`. Different ItemIds remain separate authoritative stacks and Presentation projects them at distinct anchors. Internal-stock hit colliders are triggers and do not block navigation.

Failure/cancel/retry releases source quantity, incoming capacity and worker/position reservations atomically.

## 7. Production lifecycle

1. An order may remain queued without inputs.
2. The next queued order becomes preparable only when the complete input set is available in internal stock.
3. Inputs are reserved for that order.
4. A free eligible resident claims one `ProductionWorkJob`.
5. On begin, step durations are calculated from the assigned worker's skills and stored in authoritative order state.
6. Work advances only the current material step.
7. Completing a step consumes one reserved unit of that step's ItemId immediately.
8. The next step begins until all input units are processed.
9. Completion creates outputs, grants skills exactly once, completes the job/order, then refreshes supply demand.

While an order is active, no replenishment demand is created. This prevents materials consumed mid-cycle from triggering delivery before the product finishes.

One worker job produces one recipe order. Output quantity may be greater than one.

## 8. Timing and skill

Base processing duration for each input unit is 15 game minutes. The test/demo profile uses one second without changing the state machine.

For step skill value `S` points:

```text
effectiveDuration = max(1 tick, round(baseDuration * (100 - clamp(S, 0, 100)) / 100))
```

Example: Stonework 25 processes one stone in 11.25 game minutes.

The skill is read when the production job begins and the resolved step durations are saved. Reload or frame rate cannot change the active plan.

## 9. Outputs

Building recipes create ordinary quantity-one `BuildingBox` item entities. Food recipes create ordinary stackable item entities according to output quantity.

Output location is the first deterministic free supported explored cell in front of the building. Search follows orientation-facing row, then increasing lateral distance, then stable XYZ order. A cell occupied by a building/plan or world item is not free. If no output cell is available, order remains `ReadyToComplete` with typed `production.output_space_unavailable`; inputs and exactly-once grants are not duplicated on retry.

## 10. Multiple buildings and conflicts

- Each building instance owns an independent queue, stock toggles, incoming ledger and one active production order.
- One production worker per building.
- One active supply job per building; its batch may contain several inputs.
- A source quantity cannot be reserved by two jobs.
- Direct pickup from workstation stock uses only available quantity through an explicit command; active production reservations remain authoritative and cannot be silently stolen.

## 11. Save/load and migration

Save data includes:

- workstation instance registration and definition ID;
- stock delivery toggles and incoming reservations;
- production queue sequence and status;
- current material-step index/progress/resolved duration;
- consumed inputs and remaining allocation quantities;
- active production/supply job cross-references and batch pickup plan.

Loader validates content references, building existence, order/job/worker links, inventory reservation conservation and `current + incoming <= capacity`. Migration creates default workstation state for existing completed campfires and enables all four stock rules.

## 12. Diagnostics

Inspector/query reports:

- workstation/animation profile/recipe IDs;
- queue counts and active order;
- icon state and missing ingredients;
- stock current/incoming/capacity/toggle;
- source eligibility and batch selection;
- current material step, skill, resolved duration and progress;
- consumed/reserved inputs;
- output candidate/reason;
- typed reasons including `inputs_missing`, `delivery_disabled`, `active_production_suppresses_supply`, `source_hidden`, `resident_capacity_exhausted`, `output_space_unavailable`.

## 13. Acceptance tests

Domain:

- content validation and no runtime campfire branch;
- queue ordering/missing-input orange state;
- stock toggle/capacity/incoming invariants;
- mixed batch partial fill and protected source;
- exact material-step durations at skills 0/25/100;
- progressive consumption and deferred replenishment;
- per-order skill grants with two-output cooking recipes;
- deterministic front output cell and BuildingBox identity.

Integration/deterministic:

- world source -> resident mixed cargo -> campfire stock;
- queued blocked order starts after supply;
- cancel/retry at every supply and production stage;
- supply route starts with a workstation check before the first world source;
- direct quantity-one pickup from available internal stock creates replacement demand;
- queue decrement cancels exactly one newest queued order, then the active order only when no queued order remains;
- two campfires operate independently;
- save/load mid-supply and after each material step without duplicates.

Unity Play Mode:

- select unpacked campfire and see six production icons/four stock icons;
- hover ingredient tooltip, orange shortage, click count;
- resident supply trip starts at the workstation, visits mixed sources, returns, and shows separated internal stacks;
- selected resident can LMB an available internal-stock unit and the next synchronization creates replacement demand;
- visible decrement removes one queued order;
- complete building box and food output appear in front;
- repeated queue item starts the next order;
- future animation profile is projected but no animation asset is required for this slice.

## 14. Out of scope

- final art/animation clips;
- technology unlock balancing beyond existing contracts;
- quality tiers or probabilistic extra outputs;
- production priorities shared across different buildings.

## 15. Decision log

| Date | Decision | Confirmed by |
|---|---|---|
| 2026-07-27 | Generic workstation definitions, campfire recipes, internal capacities/toggles, mixed partial supply, protected stock, progressive consumption, deferred replenishment, per-material skill timing and front-cell outputs. | User |
| 2026-07-28 | Supply workers check the workstation before collection; available internal stock supports quantity-one direct pickup; every recipe exposes explicit one-order decrement. | User |
