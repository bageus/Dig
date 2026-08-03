# Runtime supply, food interaction and HUD recovery — 2026-08-04

Status: `IMPLEMENTED`; licensed Unity Play Mode evidence pending.

Tracking: [#612](https://github.com/bageus/Dig/issues/612). Implementation PR: [#616](https://github.com/bageus/Dig/pull/616).

Authoritative specification: [`../design/runtime-supply-food-hud-recovery-correction-2026-08-04.md`](../design/runtime-supply-food-hud-recovery-correction-2026-08-04.md), composed with the production, food, ecology and resident HUD specifications.

## Runtime report

The local Unity workflow exposed one linked recovery failure: BuildingSupply could be generically unassigned by a direct command while its job-owned items, reservations and incoming lifecycle were still active. Cancel paths also released reservations before recovering already acquired resident items. This left protected material in resident inventory, could suppress later refill, and could reject an otherwise valid world-food pickup/eat command after the cursor had already resolved.

A separate ecology boundary let a reserved free hamster continue attempting `MoveAvailable`, so an active supply reservation could turn normal hamster movement into a runtime failure.

The HUD issue was presentation-only: the five skill rows remain, but the redundant `TOP 5 SKILLS` label is removed.

## Implementation map

- `ProductionReservedResidentRecovery` drops fully job/order-reserved resident units at the assigned resident current world cell before releasing ownership;
- BuildingSupply and carried-raw ProductionWork direct-command replacement use their specialized cancel/interrupt handlers;
- source loss, route failure and blocked-job recovery provide the same authoritative resident recovery cell;
- completed supply validates that no job-owned unit remains in resident inventory;
- the next synchronization pass can plan a replacement refill batch after recovery;
- ecology treats a reserved world hamster as temporarily movement-blocked until pickup or cancellation;
- resident expanded rows render the same top-five metrics without a heading;
- regressions cover acquired-unit cancellation, replacement refill, carried-raw interruption, direct food routing, reserved hamster stability and the HUD contract.

## Final-head repository evidence

Implementation commit `36da7187b55955108c6f1b8f07a33a86695f8909`:

- Quality run `30862699478` / run 9007: success — architecture/file-size/C# compatibility, Unity source contracts, Release build, full test suite, headless smoke, standard deterministic soak and large-settlement soak;
- Stage 2 v2 run `30862699485`: success;
- Stage 2 v3 run `30862699447`: success;
- Unity workflow `30862699451` / run 838: workflow success, but actual EditMode/PlayMode and executed-evidence validation were skipped because activation was unavailable; a blocked evidence manifest was recorded.

## Verification boundary

Repository implementation is complete. Actual Unity EditMode/PlayMode execution must still verify repeated refill, food pickup/eat, hamster delivery, resident inventory cleanup and a Console-error-free representative scene before the affected runtime workflows may be marked `VERIFIED`.
