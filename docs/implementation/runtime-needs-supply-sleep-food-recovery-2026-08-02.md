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
- building supply state persists the next operation turn. Runnable queued production yields at most one completed supply batch between produced units; missing required inputs may continue supply until production becomes runnable; buildings without a queue still refill to capacity.

## Regression coverage

- Domain/Application tests cover operation-turn persistence, autonomy override ownership and no generic duplicate action progress.
- source contracts cover session-clock cadence, Tent facilities/movement, automatic package Use and `eatAfterPickup`.
- checked-in Unity Play Mode scenarios cover critical Sleep walking to a completed Tent slot before Alertness recovery and free-time hunger breaking a produced food package, picking up one released unit and recovering Nutrition through the real meal workflow.
- deterministic save/restore coverage includes the persisted building operation turn.

## Verification boundary

The implementation remains `IN PROGRESS` until final-head Quality, Release tests, headless smoke and deterministic soaks pass. It remains not `VERIFIED` until licensed Unity EditMode/PlayMode actually executes the new runtime scenarios and records evidence.
