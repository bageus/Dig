# Runtime supply, food interaction and resident HUD recovery — 2026-08-04

Status: `APPROVED`.

Tracking issue: [#612](https://github.com/bageus/Dig/issues/612).

This correction is authoritative together with:

- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`campfire-cooking-and-food-use.md`](campfire-cooking-and-food-use.md);
- [`hamsters-and-grubs-ecology.md`](hamsters-and-grubs-ecology.md);
- [`resident-hud-selection-and-notifications.md`](resident-hud-selection-and-notifications.md);
- [`item-interaction-capabilities.md`](item-interaction-capabilities.md).

## Confirmed observable behavior

### Resident HUD

The expanded resident row keeps the five highest skill metrics in their existing deterministic order, but renders no separate `TOP 5 SKILLS` heading. Removing the heading does not change skill composition, values or progress bars.

### Production and BuildingSupply inventory ownership

- A production raw unit is visible in resident inventory only while physically travelling from internal stock to the workstation.
- Workbench staging removes the exact order-reserved raw unit from resident inventory before processing begins.
- A successful BuildingSupply deposit removes every job-reserved carried unit and places the complete batch in `ItemLocation.InBuilding`.
- A cancelled, blocked, route-failed or direct-command-replaced BuildingSupply job first materializes already acquired job-reserved units exactly once at the assigned resident's current supported world cell. Only after that recovery may it release source reservations, resident-slot claims, incoming capacity and building operation ownership.
- A carried-raw ProductionWork interruption applies the same recovery to the exact order-reserved raw unit. Staged or processed material follows the existing loss-on-forced-move rule.
- Cancellation must not leave unreserved supply/raw units hidden in resident inventory.

### Continuous refill and direct food use

- After delivery, cancellation, route failure or direct-command replacement, the next synchronization pass re-evaluates enabled missing stock. A previous batch cannot permanently suppress later refill.
- Direct LMB pickup and Alt+LMB pickup-then-eat may replace BuildingSupply or carried-raw ProductionWork through the specialized atomic recovery above.
- A valid pickup/mouth cursor and click resolve the same world food stack and must not fail because of a stale supply assignment, route, reservation or incoming ledger.

### Hamster supply

Hamster stock remains opt-in and capacity `2`. When enabled, a visible, reachable, unreserved free hamster is an ordinary BuildingSupply world source. While that item is reserved by a supply job, ecology movement treats it as temporarily blocked and does not call `MoveAvailable`; the source cell remains stable until committed pickup or cancellation. Pickup then changes Inventory location and ordinary ecology reconciliation marks the hamster stored.

## Invariants

- one job/order reservation owner per recovered unit;
- no duplicate world materialization on retry or repeated cancel;
- no orphan source reservation, resident-slot claim, incoming ledger, route or non-terminal building-operation blocker;
- refill and food interaction remain deterministic across repeated execution and save/load;
- Presentation does not own inventory, jobs, ecology or meal state.

## Verification

Domain/Application/integration regressions must cover acquired-unit cancellation, carried-raw interruption, repeated refill, direct food pickup/eat after supply replacement and enabled hamster delivery. Checked-in Unity Play Mode must exercise the same full workflow and a Console-error-free representative scene. Licensed execution remains required before any affected system is marked `VERIFIED`.
