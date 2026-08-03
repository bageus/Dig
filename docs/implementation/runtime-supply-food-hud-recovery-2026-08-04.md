# Runtime supply, food interaction and HUD recovery — 2026-08-04

Status: `IMPLEMENTATION IN PROGRESS`.

Tracking: [#612](https://github.com/bageus/Dig/issues/612).

Authoritative specifications:

- [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md);
- [`../design/campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md);
- [`../design/hamsters-and-grubs-ecology.md`](../design/hamsters-and-grubs-ecology.md);
- [`../design/resident-hud-selection-and-notifications.md`](../design/resident-hud-selection-and-notifications.md).

## Runtime report

The local Unity workflow exposed one linked recovery failure: BuildingSupply could be generically unassigned by a direct command while its job-owned items, reservations and incoming lifecycle were still active. Cancel paths also released reservations before recovering already acquired resident items. This left protected material in resident inventory, could suppress later refill, and could reject an otherwise valid world-food pickup/eat command after the cursor had already resolved.

A separate ecology boundary let a reserved free hamster continue attempting `MoveAvailable`, so an active supply reservation could turn normal hamster movement into a runtime failure.

The HUD issue is presentation-only: the five skill rows remain, but the redundant `TOP 5 SKILLS` label is removed.

## Implementation map

- shared production reservation recovery drops fully job/order-reserved resident units at the assigned resident current world cell before releasing ownership;
- direct-command routing specializes BuildingSupply and carried-raw ProductionWork interruption;
- route/source/blocked supply cancellation supplies the same recovery cell;
- ecology treats reserved living-material world units as temporarily movement-blocked;
- resident expanded row renders top-five metrics without a heading;
- regressions cover cancellation after pickup, repeated refill, direct food replacement, enabled hamster supply and HUD source contract.

## Verification boundary

Repository build/tests, headless smoke and deterministic soaks are required before merge. Actual Unity EditMode/PlayMode execution and a representative Console-error-free workflow remain required for `VERIFIED`.
