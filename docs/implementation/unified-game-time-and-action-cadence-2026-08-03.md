# Unified game time and action cadence — 2026-08-03

Status: `IMPLEMENTATION CORRECTION IN PROGRESS`; licensed Unity Play Mode evidence pending.

Authoritative specifications:

- [`../design/unified-game-time-and-action-cadence.md`](../design/unified-game-time-and-action-cadence.md);
- [`../design/game-time-scale-runtime-synchronization-correction-2026-08-03.md`](../design/game-time-scale-runtime-synchronization-correction-2026-08-03.md).

Tracking: [#601](https://github.com/bageus/Dig/issues/601), with linked systems #2, #159, #386, #388, #459, #508 and #559.

## Original implementation map

- `GameTimeCadence` owns the shared `150 ticks/hour`, `3600 ticks/day` constants and one-second normal tick duration.
- Unity demo composition reads tick duration and `DailySchedule.TicksPerDay` from that contract and starts residents with full Nutrition.
- `AgentNeedPolicy` resolves passive need and starvation Health deltas through deterministic cumulative fractions.
- `ResidentMovementModeCatalog` supplies gameplay speed profiles; `ResidentInventoryMovementCadence` resolves a deterministic transition count, including the second run substep.
- The Unity simulation driver performs at most one additional movement-only replan/substep per simulation tick; work, needs, combat and production do not advance twice.
- `ActiveFoodMeal` owns `NextBiteTick`, so cooldown and save/load are Domain state rather than Presentation timing.
- `ProductionStepTiming` applies a 50% minimum duration; campfire content uses 25 ordinary ticks and 50 cooking ticks.
- Demo mining equipment uses a three-tick base impact interval.
- Cave encounter melee profiles use a four-tick resolve cycle.

## Runtime correction — global time-scale owner

User runtime verification after merge #603 exposed two remaining legacy owners:

- `DigGameHudCanvas.Clock` used `24 ticks/day` whenever no resident was selected or hovered;
- passive needs used `AgentState.Schedule.TicksPerDay`, allowing a personal/legacy schedule resolution to redefine hunger, alertness and starvation speed.

The correction introduces one explicit Domain coefficient:

- `24 game seconds / real second` at normal x1;
- one real second produces one simulation tick;
- one tick advances 24 game seconds;
- `GameTimeCadence` projects day/hour/minute/second for HUD and diagnostics;
- clock hands always use the global projection, independent of resident selection;
- schedule overlays remain resident-specific but cannot own current time;
- passive needs use global calendar ticks rather than resident schedule resolution.

## Save/load boundary

Existing simulation tick is retained unchanged. Active meal save data keeps `NextBiteTick`; legacy payloads derive a safe next due tick after load. Movement fractional cadence is derived from tick/mode and is not serialized. Existing production, excavation and combat authoritative progress remains unchanged.

A legacy/custom resident schedule may retain its activity segmentation temporarily, but it cannot alter global needs cadence. Calendar projection always derives from the saved simulation tick and the current `GameTimeCadence` coefficient.

## Regression plan

- explicit real-to-game coefficient and calendar projection;
- clock without resident selection uses global day length;
- selecting/hovering a resident changes only schedule overlay;
- resident with `DailySchedule.TicksPerDay = 24` still follows global 7200/10800/1800 needs periods;
- pause/single-step/x2/x4 produce the same committed-tick relationship for clock and needs;
- exact Nutrition, Alertness and starvation endpoints;
- run/walk/climb fixed-point transition counts;
- bites at `T+1/T+3/T+5` and cooldown save/load;
- cooking duration floor and output quantity two;
- pickaxe three-tick base cadence;
- four-tick melee profiles;
- Unity source/runtime contracts for one clock owner and movement-only second substep;
- checked-in Play Mode scenario for complete repeated workflows.

## Verification boundary

Repository build/tests, headless smoke and deterministic soaks are required before merge. Runtime status remains below `VERIFIED` until licensed Unity Test Runner executes the checked-in Play Mode scenarios and produces result artifacts. The time-scale scenario must include no selection, selected resident, pause, single-step and speed multipliers.
