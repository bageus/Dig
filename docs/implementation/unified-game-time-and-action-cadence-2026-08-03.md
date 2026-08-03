# Unified game time and action cadence — 2026-08-03

Status: `IMPLEMENTATION IN PROGRESS`; licensed Unity Play Mode evidence pending.

Authoritative specification: [`../design/unified-game-time-and-action-cadence.md`](../design/unified-game-time-and-action-cadence.md).

Tracking: [#601](https://github.com/bageus/Dig/issues/601), with linked systems #2, #159, #386, #388, #459, #508 and #559.

## Implementation map

- `GameTimeCadence` owns the shared `150 ticks/hour`, `3600 ticks/day` constants and one-second normal tick duration.
- Unity demo composition reads tick duration and `DailySchedule.TicksPerDay` from that contract and starts residents with full Nutrition.
- `AgentNeedPolicy` resolves passive need and starvation Health deltas through deterministic cumulative fractions.
- `ResidentMovementModeCatalog` supplies gameplay speed profiles; `ResidentInventoryMovementCadence` resolves a deterministic transition count, including the second run substep.
- The Unity simulation driver performs at most one additional movement-only replan/substep per simulation tick; work, needs, combat and production do not advance twice.
- `ActiveFoodMeal` owns `NextBiteTick`, so cooldown and save/load are Domain state rather than Presentation timing.
- `ProductionStepTiming` applies a 50% minimum duration; campfire content uses 25 ordinary ticks and 50 cooking ticks.
- Demo mining equipment uses a three-tick base impact interval.
- Cave encounter melee profiles use a four-tick resolve cycle.

## Save/load boundary

Existing simulation tick is retained unchanged. Active meal save data gains `NextBiteTick`; legacy payloads derive a safe next due tick after load. Movement fractional cadence is derived from tick/mode and is not serialized. Existing production, excavation and combat authoritative progress remains unchanged.

## Regression plan

- calendar constants and hour/day wrap;
- exact Nutrition, Alertness and starvation endpoints;
- run/walk/climb fixed-point transition counts;
- bites at `T+1/T+3/T+5` and cooldown save/load;
- cooking duration floor and output quantity two;
- pickaxe three-tick base cadence;
- four-tick melee profiles;
- Unity source/runtime contracts for one clock owner and movement-only second substep;
- checked-in Play Mode scenario for complete repeated workflows.

## Verification boundary

Repository build/tests and deterministic soaks are required before merge. Runtime status remains below `VERIFIED` until licensed Unity Test Runner executes the checked-in Play Mode scenarios and produces result artifacts.
