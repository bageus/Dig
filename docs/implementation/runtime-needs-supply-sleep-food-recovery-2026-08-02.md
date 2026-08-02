# Runtime needs, production/supply, Tent sleep and food-package recovery

**Status:** IN PROGRESS  
**Date:** 2026-08-02  
**Authoritative design:** `docs/design/runtime-needs-supply-sleep-food-recovery.md`  
**Tracking:** #2, #159, #142, #433, #459  
**Implementation PR:** #573

## Implementation scope

- Unity normal speed reads `SimulationState.Clock.TickDuration`; the demo clock is two real seconds per simulation tick.
- `AgentAutonomySystem` supports an Application-level intent execution override so the Unity composition can own physical Eat/Sleep execution without a second generic need delta.
- completed `building.tent` snapshots publish two deterministic Bed facilities; Sleep reserves the nearest reachable slot, movement routes to it and intervals advance only after arrival; FloorSleep remains the fallback.
- free-time Eat selects carried food, reachable loose food or a reachable closed `food` package. A package uses the existing exactly-once package Use lifecycle, then the released item uses ordinary pickup with `eatAfterPickup` and authoritative meal bites.
- building supply state persists the next operation turn. Queued refill uses `ceil(capacity / 2)` low-water thresholds for the next recipe inputs, permits consecutive production at or above threshold, collects only currently eligible target materials, creates supported extraction dependencies for unavailable targets and returns one runnable Production turn after a partial delivery; buildings without a queue still refill all enabled stocks to capacity.

## Regression coverage

- Domain/Application tests cover operation-turn persistence, half-capacity thresholds, partial targeted supply, extraction target filtering, autonomy override ownership and no generic duplicate action progress.
- source contracts cover session-clock cadence, Tent facilities/movement, automatic package Use and `eatAfterPickup`.
- checked-in Unity Play Mode scenarios cover critical Sleep walking to a completed Tent slot before Alertness recovery, free-time hunger breaking a produced food package and the campfire sequence `4 -> 3 -> 2 -> 1 -> Supply -> resumed Production` without concurrent building operation owners.
- deterministic save/restore coverage includes the persisted building operation turn.

## Pending verification

The threshold amendment supersedes the previous strict alternation evidence. Final-head Release build, full .NET suite, headless smoke, deterministic soaks and source exports must be rerun after the scheduler, planner and Play Mode regression changes are published.

## Verification boundary

The amendment remains `IN PROGRESS` until final-head automated checks pass. It remains not `VERIFIED` until licensed Unity Play Mode executes cadence, Tent walking/sleep, food-package opening/eating and the threshold-driven production/supply sequence in the real composition.
