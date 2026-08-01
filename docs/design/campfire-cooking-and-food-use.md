# Campfire cooking and direct food use

Status: `IMPLEMENTED`

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

One order consumes exactly one mushroom cap from that campfire internal stock and creates two separate ordinary world entities. Each grilled mushroom output has `quantity = 1`, occupies its own finished-output cell and never stacks with the second produced unit.

The single material step has a base duration of 15 game minutes. The existing production duration contract remains authoritative:

`effectiveDuration = max(1, round(baseDuration * (100 - clamp(Cooking, 0, 100)) / 100))`.

Cooking skill changes speed only. Completion grants the existing recipe Cooking progression exactly once.

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

The resident assigned to `ProductionWorkJob` remains the authoritative worker through `Finalize`. That worker performs output placement; no anonymous completion adapter or second hauling job creates the food.

The result is two distinct stacks with `quantity = 1`. The generic building-production placement owner requires two supported, explored, unoccupied cells in the right finished-output zone. Candidate order is `right edge + 1`, then `+2` and onward; front, left and rear fallback are forbidden.

If fewer than two valid right-side output cells exist, the order remains ready to complete with `production.output_space_unavailable`. Retry must not consume another cap, commit only one mushroom, duplicate output, repeat progression, or lose the assigned completion owner.

The grilled-mushroom product cell shows a no-text fill overlay only while cooking is active. It starts at zero when production work begins, fills with actual resolved cooking progress, remains full while the cook moves to the output zone, and disappears only when both outputs, the terminal order/job transition and the product counter decrement commit together. Cancellation removes it immediately.

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

`Eat` является stationary action: meal может начаться и продолжаться только когда resident стоит в клетке с полной ровной actor support surface. Shaft gap, воздух и partial-support клетка запрещены. Guard выполняется до reservation/consume, поэтому direct inventory use на неподдерживаемой позиции возвращает `resident.food_meal.unsupported_standing_position`, сохраняет порцию и не создаёт active meal. После world pickup тот же guard либо начинает meal на поддерживаемой source cell, либо оставляет порцию в inventory для безопасного retry. Если опора исчезает во время meal, action прерывается до следующего bite по обычному interruption contract.

A grilled mushroom portion provides 15 Nutrition points (`1500` fixed-point units) over exactly three bites. The portion is removed from Inventory at meal start. Each completed bite applies exactly one third (`500`) to Nutrition. The first bite is the start of consumption for history/diagnostics. Interruption preserves already-applied bites and loses the consumed remainder; restart requires another portion.

While a meal is active, autonomous replanning cannot replace it. A later explicit direct command may interrupt it through the normal direct-command preparation path.

## 7. Multiple residents, conflicts and retries

- Inventory reservations remain the only source ownership lock.
- One world cap or food stack cannot feed two jobs.
- Different residents may harvest, deliver, cook, pick up and eat concurrently when they own different entities/jobs.
- A source that becomes reserved, moved, hidden or unreachable before commit blocks/cancels through existing typed job handling.
- If the selected resident has no compatible free inventory slot, direct pickup/eat is rejected before job creation and the food remains in the world.

## 8. Save/load and migration

Existing production, supply, mushroom and pickup job codecs retain their authority. The post-pickup action is stored in `WorldItemPickupJobDefinition` and its codec. Save format v9 stores resident needs, active food item, original meal start tick, completed bite count and remaining bite plan.

Loading restores the exact holder and completed bite progress without replaying pickup, consuming another portion, applying completed Nutrition again, repeating progression or output completion. Older saves migrate to an empty agent-runtime section and therefore load with no active meal. Older pickup-job payloads without `completion_action` decode as `None`.

## 9. Presentation and diagnostics

Observable state includes:

- order status and missing cap reason in the campfire panel;
- supply, dependency-chop and production jobs in the shared job overlay;
- resident statuses for harvesting, delivering, cooking, picking up food and eating;
- right finished-output-zone blocked reason;
- animated pickup arrow and green mouth cursor;
- active meal item, completed bites and remaining bites in diagnostics.

## 10. Acceptance

Domain/application tests must cover recipe quantity, protected source filtering, one dependency chop, deterministic right-zone multi-cell placement, duration at Cooking 0/25/100, exactly-once completion, direct pickup/eat reservation, supported-standing guard before consume, three bites, support-loss interruption and retry.

Integration tests must cover:

`enabled missing cap/leg -> no free world source -> chop job + dependent supply -> drop -> source binding -> internal stock refill`;

`queued order -> no cap -> refill/dependency chain -> internal stock -> cook overlay -> two quantity-one outputs -> LMB pickup`;

and:

`queued order -> cook -> Alt+LMB -> approach -> pickup -> three bites -> Nutrition +15`.

Unity Play Mode must verify cursor priority/animation colour, resident movement, continuous refill during active production, deferred resident fallback, stale dependency recovery, per-product overlay start/fill/full/clear lifecycle, two distinct output entities/cells, pickup commit, eating animation/status, repeated orders, full output zone, cancellation and the next repeated interaction.

## 11. Implementation evidence

- Core production, input and meal workflow: [#464](https://github.com/bageus/Dig/pull/464).
- Persisted pickup completion action, save v9 active-meal restoration and full workflow regressions: [#485](https://github.com/bageus/Dig/pull/485).
- Continuous cap/leg refill and deferred resolver recovery: `docs/implementation/campfire-supply-dependency-recovery-2026-08-01.md`.
- GitHub Quality covers architecture boundaries, build, domain/application/integration tests, headless smoke and deterministic soak.
- `CampfireFoodWorkflowPlayModeTests` contains executable dependency-chain and pickup-to-three-bites scenarios. The hosted Unity workflow currently records them as skipped when Unity activation is unavailable, so the system is `IMPLEMENTED`, not `VERIFIED`.
