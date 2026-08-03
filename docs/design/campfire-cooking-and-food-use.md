# Campfire cooking and direct food use

Status: `IMPLEMENTED`

Tracking issues: [#459](https://github.com/bageus/Dig/issues/459), [#601](https://github.com/bageus/Dig/issues/601)

Related authoritative systems:

- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`mushroom-growth-and-chopping.md`](mushroom-growth-and-chopping.md);
- [`content/food.md`](content/food.md);
- [`needs-continuous-actions.md`](needs-continuous-actions.md);
- [`contextual-input-cursors-and-selection.md`](contextual-input-cursors-and-selection.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`unified-game-time-and-action-cadence.md`](unified-game-time-and-action-cadence.md).

## 1. Scope

This specification owns the vertical workflow that turns a queued campfire food order into world food and lets a selected resident pick it up or eat it directly. It composes Production, BuildingSupply, Mushroom, Inventory, Jobs, AgentNeeds and contextual-input owners; it must not create duplicate inventories, nutrition state or Unity-only job truth.

Stable identifiers:

- workstation: `building.campfire`;
- recipe: `recipe.campfire.food.grilled_mushroom`;
- input: `material.mushroom_cap`;
- output: `food.grilled_mushroom`;
- cooking skill: `skill.cooking`.

## 2. Recipe and timing

One order consumes exactly one mushroom cap from that campfire internal stock and creates two separate ordinary world entities. Each grilled mushroom output has `quantity = 1`, occupies its own finished-output cell and never stacks with the second produced unit.

The single Cooking material step has a base duration of `50 simulation ticks`. Ordinary non-food material processing uses `25 ticks`; Cooking intentionally uses two base processing units.

Cooking skill changes processing speed only. The effective duration is:

`effectiveDuration = round(baseDuration × (100 - min(CookingPoints, 50)) / 100)`.

For base `50`, Cooking `0/25/50/75/100` resolves to `50/38/25/25/25 ticks`. The minimum processing duration is therefore 50% of base; skill never skips package, movement, material-acquire, staging, deposit or close phases.

With short internal-stock/workbench/output routes, the complete one-cap order normally occupies roughly `60–75 ticks`. This is an expected spatial total, not a second timer or hard completion deadline.

Completion grants the existing recipe Cooking progression exactly once per order, not once per output unit.

## 3. Dependency chain

Campfire mushroom supply is a continuous internal-stock chain, not a recipe-only shortcut:

1. Enabled cap/leg stock with missing capacity first uses revealed, reachable, unreserved world items through ordinary protected BuildingSupply.
2. If the highest-priority missing cap/leg has no eligible world source and a revealed/reachable `Large` mushroom exists without an active chop attempt, runtime creates exactly one mushroom-chop job and one dependent BuildingSupply job in the same synchronization pass.
3. This planning is independent of a queued grilled-mushroom order and continues while another production order is active.
4. The dependent supply job stays source-unresolved until chop completion. Resolver then tries every available resident in deterministic distance/id order; one resident without compatible capacity cannot block the others.
5. If the completed chop no longer has the requested world drop, the stale dependent supply is cancelled so a later pass can create a replacement pair. Other cap/leg drops remain ordinary world sources and are collected by following supply batches until capacity.
6. A queued grilled-mushroom order prepares immediately once one unreserved internal cap exists.

A cap or leg in another building internal stock is protected by that building owner and is never a candidate. Material in another resident inventory is also not an automatic source. Delivery and cooking may be completed by the same resident only when that resident independently wins both normal assignments; no special affinity is required.

At most one active dependency chop/delivery pair may exist per campfire. Existing eligible world material always takes priority over creating another pair. Repeated synchronization cannot create duplicate pending delivery jobs.

## 4. Cooking completion and output placement

The resident assigned to `ProductionWorkJob` remains the authoritative worker through `Finalize`. That worker performs the complete spatial cycle; no anonymous completion adapter or second hauling job creates the food.

For grilled mushroom the exact one-step cycle is:

1. create the unfinished food package in the right output zone; package placement is a separate committed one-tick stage;
2. approach internal stock;
3. acquire one exact reserved mushroom cap into a resident slot; the acquire commit is one tick after arrival;
4. return to the campfire;
5. commit the cap onto the virtual workbench so it disappears from resident inventory;
6. process the staged cap for the Cooking-resolved `25–50` ticks;
7. approach the same unfinished package;
8. deposit the processed step;
9. close the package and step away into the normal post-work pose.

The product segment is committed only by the package deposit, not by raw pickup or timer completion.

The result is two distinct stacks with `quantity = 1`. The generic building-production placement owner requires two supported, explored, unoccupied cells in the right finished-output zone. Candidate order is `right edge + 1`, then `+2` and onward; front, left and rear fallback are forbidden.

If fewer than two valid right-side output cells exist, the order remains ready to complete with `production.output_space_unavailable`. Retry must not consume another cap, commit only one mushroom, duplicate output, repeat progression or lose the assigned completion owner.

The grilled-mushroom product cell shows a no-text fill overlay only while cooking is active. It starts at zero when production work begins, fills from actual resolved processing progress, remains full while the cook moves to the output zone and disappears only when both outputs, the terminal order/job transition and product-counter decrement commit together. Cancellation removes it immediately.

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

`Eat` is a stationary action: meal can begin and continue only while the resident stands on a full flat actor-support surface. Shaft gap, air and partial support are forbidden. The guard runs before reservation/consume; direct inventory use from an unsupported position returns `resident.food_meal.unsupported_standing_position`, preserves the portion and creates no active meal. After world pickup the same guard either starts the meal on the supported source cell or leaves the portion in Inventory for safe retry. Support loss during the meal interrupts it before the next bite.

A grilled mushroom portion provides 15 Nutrition points (`1500` fixed-point units) over exactly three bites. The portion is removed from Inventory at meal start. Each completed bite applies exactly one third (`500`) to Nutrition.

If meal start is tick `T`, bites commit at `T+1`, `T+3` and `T+5`. Ticks `T+2` and `T+4` are cooldown ticks and apply no Nutrition. The first bite starts consumption for history/diagnostics. Interruption preserves already-applied bites and loses the consumed remainder; restart requires another portion.

While a meal is active, autonomous replanning cannot replace it. A later explicit direct command may interrupt it through the normal direct-command preparation path.

## 7. Multiple residents, conflicts and retries

- Inventory reservations remain the only source ownership lock.
- One world cap or food stack cannot feed two jobs.
- Different residents may harvest, deliver, cook, pick up and eat concurrently when they own different entities/jobs.
- A source that becomes reserved, moved, hidden or unreachable before commit blocks/cancels through existing typed job handling.
- If the selected resident has no compatible free inventory slot, direct pickup/eat is rejected before job creation and the food remains in the world.

## 8. Save/load and migration

Existing production, supply, mushroom and pickup job codecs retain their authority. The post-pickup action is stored in `WorldItemPickupJobDefinition` and its codec. Agent runtime save data stores resident needs, active food item, original meal start tick, completed bite count and `NextBiteTick`.

Loading restores the exact holder, completed bite progress and cooldown phase without replaying pickup, consuming another portion, applying completed Nutrition again, repeating progression or output completion. A legacy active-meal payload without `NextBiteTick` receives the first safe due tick after load. Existing simulation tick is never multiplied or divided. Older saves without agent-runtime state load with no active meal. Older pickup-job payloads without `completion_action` decode as `None`.

## 9. Presentation and diagnostics

Observable state includes:

- order status and missing-cap reason in the campfire panel;
- supply, dependency-chop and production jobs in the shared job overlay;
- resident statuses for harvesting, delivering, cooking, picking up food and eating;
- base/effective Cooking duration and current material phase;
- right finished-output-zone blocked reason;
- animated pickup arrow and green mouth cursor;
- active meal item, completed bites, remaining bites and next due bite tick.

## 10. Acceptance

Domain/application tests cover recipe quantity, protected source filtering, one dependency chop, deterministic right-zone multi-cell placement, duration at Cooking `0/25/50/75/100`, the 50% duration floor, exactly-once completion, direct pickup/eat reservation, supported-standing guard before consume, bites at `T+1/T+3/T+5`, support-loss interruption and retry.

Integration tests cover:

`enabled missing cap/leg -> no free world source -> chop job + dependent supply -> drop -> source binding -> internal stock refill`;

`queued order -> no cap -> refill/dependency chain -> internal stock -> package placement -> cap pickup -> 50-tick base cook -> two quantity-one outputs -> LMB pickup`;

and:

`queued order -> cook -> Alt+LMB -> approach -> pickup -> bite/cooldown/bite/cooldown/bite -> Nutrition +15`.

Unity Play Mode must verify cursor priority/animation colour, resident movement, continuous refill during active production, deferred resident fallback, stale dependency recovery and the full production route `package placement -> internal-stock pickup -> workbench stage/removal from inventory -> processing -> package deposit -> package close -> post-work step-away`. It must also verify product-overlay lifecycle, two distinct output entities/cells, pickup commit, eating animation/status, cooldown ticks, repeated orders, full output zone, cancellation and the next repeated interaction.

## 11. Implementation evidence

- Core production, input and meal workflow: [#464](https://github.com/bageus/Dig/pull/464).
- Persisted pickup completion action, save v9 active-meal restoration and full workflow regressions: [#485](https://github.com/bageus/Dig/pull/485).
- Continuous cap/leg refill and deferred resolver recovery: `docs/implementation/campfire-supply-dependency-recovery-2026-08-01.md`.
- Unified production and bite cadence: [#601](https://github.com/bageus/Dig/issues/601), [PR #603](https://github.com/bageus/Dig/pull/603).
- GitHub Quality covers architecture boundaries, build, Domain/Application/integration tests, headless smoke and deterministic soak.
- Checked-in Play Mode tests remain runtime evidence only when a licensed Unity runner executes them rather than recording a skip.
