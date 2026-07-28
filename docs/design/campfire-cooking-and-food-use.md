# Campfire cooking and direct food use

Status: `APPROVED`

Tracking issue: [#459](https://github.com/bageus/Dig/issues/459)

Related authoritative systems:

- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`mushroom-growth-and-chopping.md`](mushroom-growth-and-chopping.md);
- [`content/food.md`](content/food.md);
- [`needs-continuous-actions.md`](needs-continuous-actions.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md).

## 1. Scope

This specification owns the vertical workflow that turns a queued campfire food order into a world food item and lets a selected resident pick it up or eat it directly. It composes the existing Production, BuildingSupply, Mushroom, Inventory, Jobs, AgentNeeds and contextual-input owners; it must not create duplicate inventories, nutrition state or Unity-only job truth.

Stable identifiers:

- workstation: `building.campfire`;
- recipe: `recipe.campfire.food.grilled_mushroom`;
- input: `material.mushroom_cap`;
- output: `food.grilled_mushroom`;
- cooking skill: `skill.cooking`.

## 2. Recipe and timing

One order consumes exactly one mushroom cap from that campfire internal stock and creates one ordinary world stack containing two grilled mushrooms.

The single material step has a base duration of 15 game minutes. The existing production duration contract remains authoritative:

`effectiveDuration = max(1, round(baseDuration * (100 - clamp(Cooking, 0, 100)) / 100))`.

Cooking skill changes speed only. Completion grants the existing recipe Cooking progression exactly once.

## 3. Dependency chain

For every queued grilled-mushroom order the runtime evaluates one deterministic chain:

1. If the campfire internal stock contains one unreserved mushroom cap, prepare the production job immediately.
2. Otherwise, if a revealed, reachable, unreserved world mushroom cap exists, create the ordinary protected BuildingSupply job.
3. Otherwise, if a revealed and reachable `Large` mushroom exists without an active chop attempt, create exactly one ordinary mushroom-chop job as the dependency.
4. Mushroom drops remain world items. On following synchronization ticks the normal BuildingSupply planner reserves a cap, delivers it, and the normal production preparation starts.

A cap in another building internal stock is protected by that building owner and is never a candidate. A cap in another resident inventory is also not an automatic source. Delivery and cooking may be completed by the same resident only when that resident independently wins both normal assignments; no special affinity is required.

At most one active dependency chop may be created per blocked campfire order. Existing world caps always take priority over creating another chop.

## 4. Cooking completion and output placement

The resident assigned to `ProductionWorkJob` remains the authoritative worker through `Finalize`. That worker performs output placement; no anonymous completion adapter or second hauling job creates the food.

The result remains one stack with `quantity = 2`. The placement resolver searches supported, explored, unoccupied cells around the completed campfire in deterministic front-first order:

1. front centre and front lateral cells;
2. side cells;
3. rear cells;
4. expanding perimeter rings when the nearest ring is full.

If no valid surrounding cell exists, the order remains ready to complete with `production.output_space_unavailable`. Retry must not consume another cap, duplicate output, repeat progression, or lose the assigned completion owner.

## 5. World interaction

When at least one living resident is selected and the pointer is over an available `food.grilled_mushroom` world stack:

- without `Alt`, the cursor is the existing animated upward pickup arrow; LMB creates the existing direct `WorldItemPickup` job for the selected resident;
- with `Alt`, the cursor is an animated green mouth; `Alt+LMB` creates the same authoritative approach/pickup job plus a post-pickup eat intent;
- the stack remains in the world until the resident reaches it and pickup commits;
- a failed or cancelled pickup clears the post-pickup eat intent and does not consume food;
- after successful pickup, the exact carried food unit starts the authoritative meal action immediately.

BuildingBox input and ordinary non-food item routing are unchanged.

## 6. Meal execution

Direct and autonomous food use share one Agent meal owner.

A grilled mushroom portion provides 15 Nutrition points (`1500` fixed-point units) over exactly three bites. The portion is removed from Inventory at meal start. Each completed bite applies exactly one third (`500`) to Nutrition. The first bite is the start of consumption for history/diagnostics. Interruption preserves already-applied bites and loses the consumed remainder; restart requires another portion.

While a meal is active, autonomous replanning cannot replace it. A later explicit direct command may interrupt it through the normal direct-command preparation path.

## 7. Multiple residents, conflicts and retries

- Inventory reservations remain the only source ownership lock.
- One world cap or food stack cannot feed two jobs.
- Different residents may harvest, deliver, cook, pick up and eat concurrently when they own different entities/jobs.
- A source that becomes reserved, moved, hidden or unreachable before commit blocks/cancels through existing typed job handling.
- If the selected resident has no compatible free inventory slot, direct pickup/eat is rejected before job creation and the food remains in the world.

## 8. Save/load and migration

Existing production, supply, mushroom and pickup job codecs retain their authority. The post-pickup eat marker and active meal bite progress must be persisted with stable identifiers. Loading must restore the exact source/holder, completed bite count and remaining nutrition without replaying pickup, consumption, progression or output completion.

Older saves without meal state load as no active meal.

## 9. Presentation and diagnostics

Observable state includes:

- order status and missing cap reason in the campfire panel;
- supply, dependency-chop and production jobs in the shared job overlay;
- resident statuses for harvesting, delivering, cooking, picking up food and eating;
- front-first output blocked reason;
- animated pickup arrow and green mouth cursor;
- active meal item, completed bites and remaining bites in diagnostics.

## 10. Acceptance

Domain/application tests must cover recipe quantity, protected source filtering, one dependency chop, deterministic output-ring placement, duration at Cooking 0/25/100, exactly-once completion, direct pickup/eat reservation, three bites, interruption and retry.

Integration tests must cover:

`queued order -> no cap -> Large mushroom chop -> cap drop -> supply -> internal stock -> cook -> output quantity 2 -> LMB pickup`;

and:

`queued order -> cook -> Alt+LMB -> approach -> pickup -> three bites -> Nutrition +15`.

Unity Play Mode must verify cursor priority/animation colour, resident movement, pickup commit, eating animation/status, repeated orders, full output ring, cancellation and the next repeated interaction.