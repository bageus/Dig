# Resident free-time needs actions — implementation note

**Status:** IMPLEMENTED  
**Date:** 2026-08-02  
**Authoritative design:** `docs/design/resident-schedule-needs-actions.md`  
**Tracking:** #159, #142, #113  
**Implementation PR:** #569

## Implementation

- `AgentDecisionCandidates` disables automatic `Eat` during `ScheduleActivity.Work`, including critical hunger.
- Work-time hunger does not replace Work or an active direct player order and does not create a food reservation.
- An already active/manual Eat remains eligible during Work, preserving the direct-feed workflow.
- The existing `AgentNeedThresholdCrossed(Hunger)` event remains the sole hunger-notification source.
- `AgentActivityTargetKind.FloorSleep` represents sleeping on the ground without a building reservation.
- Sleep target resolution uses an available Bed first and FloorSleep when no Bed can be reserved.
- Targeted Eat/Sleep/Leisure effects are divided into deterministic intervals and committed while the action is active.
- Interruption preserves committed intervals; completion clears the action/target without applying the full effect a second time.
- Floor sleep suppresses positive Mood and caps Alertness at `7500`.
- Idle no longer restores Alertness, Mood or Health.
- Headless settlement and soak schedules include bounded free-time windows between Work periods, exercising the new schedule gate without inventing automatic Work-time consumption.

## Regression coverage

### Domain and Application

- Work hunger preserves a direct order and marks automatic Eat unavailable.
- An active direct Eat continues during Work.
- Free-time critical hunger selects Eat.
- One food unit is reserved/consumed by only one resident.
- Work-time hunger with zero food emits one hunger event, creates no reservation and does not block Work.
- One available Bed is preferred while a competing tired resident falls back to FloorSleep.
- FloorSleep has no positive Mood effect and cannot raise Alertness above 75%.
- Leisure and other targeted need effects are applied progressively.
- Missing target reservation keeps already committed intervals and blocks without duplicate completion.

### Unity

`ResidentFreeTimeNeedsPlayModeTests` is checked in and covers Work-time hunger, no automatic food reservation, the hunger event and FloorSleep projection through the real settlement composition.

## Final-head evidence

Code head `aa635ac84627f069154fe232e2cdcb2aaf34860d` passed Quality run `30744477464`:

- architecture, file-size, C# compatibility, compiler, dependency and domain-boundary checks;
- Unity source and presentation contract checks;
- Release build: `0` warnings, `0` errors;
- .NET tests: `1339/1339` passed;
- headless smoke passed;
- standard deterministic soak: 8 residents, 4 workers, 2000 ticks plus drain to 2020, replay verified, no invariant/budget violations, hash `84DF20CCAE6B6CD42CB9B3B07415D468D45E117F8F3B6A1A675DA0A329CB3479`;
- large deterministic soak: 64 residents, 16 workers, 1000 ticks plus drain to 1020, replay verified, no invariant/budget violations, hash `28CF96B7C7F7FC12CD859AB20E837FAC091FA3FF7B6F20E1B693AA340A303F0C`;
- Stage 2 v2/v3 source exports passed.

## Verification boundary

Unity workflow `30744477450` completed with blocked evidence only. `Run Unity EditMode and PlayMode tests` and runtime-evidence validation were skipped because no usable licensed Unity activation was available. Therefore the implementation is `IMPLEMENTED`, not `VERIFIED`; actual Bed preference, Floor fallback, direct feeding, notification and HUD refresh still require licensed Unity Play Mode execution.