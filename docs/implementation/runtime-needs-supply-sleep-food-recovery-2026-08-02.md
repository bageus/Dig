# Runtime needs, production/supply, Tent sleep and food-package recovery

**Status:** IMPLEMENTED  
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

## Final-head evidence

Code head `21d55ea46236829e410a5e0bd41496bda420a710` passed Quality run `30758434752`:

- architecture, file-size, C# compatibility, compiler, dependency and Domain-boundary checks;
- Unity source and presentation contract checks;
- Release build: `0` warnings, `0` errors;
- .NET tests: `1343/1343` passed;
- headless smoke passed;
- standard deterministic soak: 8 residents, 4 workers, 2000 ticks plus drain to 2020, deterministic replay matched, no invariant/budget violations, hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak: 64 residents, 16 workers, 1000 ticks plus drain to 1020, deterministic replay matched, no invariant/budget violations, hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Stage 2 v2/v3 source exports passed.

## Verification boundary

Unity workflow `30758434766` recorded blocked evidence. `Run Unity EditMode and PlayMode tests` and executed-runtime validation were skipped because no usable licensed Unity activation was available. Therefore the slice is `IMPLEMENTED`, not `VERIFIED`; the checked-in cadence, Tent walking/sleep, food-package opening/eating and runtime production/supply scenarios still require licensed Unity Play Mode execution.
