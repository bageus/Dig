# Resident schedule-gated needs actions

**Status:** IMPLEMENTED  
**Decision date:** 2026-08-02  
**Tracking:** #159, #142, #113  
**Implementation:** `docs/implementation/resident-free-time-needs-actions-2026-08-02.md`, PR #571

## Scope

This specification clarifies the observable schedule, needs-action, sleep-target and notification workflow shared by:

- `docs/design/needs-continuous-actions.md`;
- `docs/design/content/food.md`;
- `docs/design/sleep-comfort-and-bed-assignment.md`;
- `docs/design/resident-hud-selection-and-notifications.md`.

Where an older implementation or test allows automatic eating during Work or blocks Sleep when no bed is available, this document is authoritative.

## Authoritative state ownership

- `AgentState` owns Nutrition, Alertness, Mood, Health, schedule and the active action.
- Utility AI selects an intention but does not mutate needs.
- Inventory owns food identity, quantity and reservations.
- Building facilities own bed/leisure availability and reservations.
- Domain/Application action intervals commit need effects.
- Presentation projects snapshots/events and never applies need effects or creates an automatic Eat command from a notification.

## Schedule and input priority

### Working time

- Automatic `Eat` is unavailable while `ScheduledActivity == ScheduleActivity.Work`, including critical hunger.
- Hunger does not interrupt Work or an active direct player order.
- A manual/direct feed command is allowed during working time and owns the real bite-based `Eat` workflow.
- An already active direct meal may continue after the schedule enters Work.
- Critical Alertness may still select Sleep; the user only disabled automatic working-time eating, not emergency sleep.

### Free time

- Outside Work, Nutrition can select automatic Eat when food is available.
- Sleep and Leisure compete through the existing deterministic utility rules and schedule bonuses.
- Work remains possible off schedule only through the existing lower off-schedule score or a direct order.

## Needs are changed by real actions

- Nutrition improvement is committed by confirmed food bites or targeted Eat intervals.
- Alertness improvement is committed by active Sleep intervals.
- Mood improvement is committed by data-driven food, Sleep or Leisure intervals.
- Idle does not restore Alertness, Mood or Health by itself.
- Work, movement and combat do not produce positive natural recovery.
- Passive decay/critical penalties remain authoritative and continue to run on simulation ticks.

Targeted Eat, Sleep and Leisure distribute their configured total action effect across deterministic intervals. Integer remainder is assigned to the earliest intervals. Interruption preserves committed intervals. Completion clears the action and reservation but applies no duplicate full-action effect.

The active action elapsed interval is the save/replay cursor. Restore continues from the next unapplied interval.

## Sleep target priority

1. Resolve and reserve an available Bed facility.
2. When no Bed is available, create a reservation-free `FloorSleep` target for that resident.
3. Do not report `bed_unavailable` while Floor sleep is possible.
4. Floor sleep has no positive Mood effect and cannot raise Alertness above `7500`.
5. A Bed reservation is released exactly once on completion, interruption, death or target loss. Floor owns no building reservation.

An available Bed always wins over Floor for the resident being resolved. With one Bed and two tired residents, one uses the Bed and the other sleeps on the Floor without double reservation.

## Hunger notification

- The existing downward crossing `Nutrition < 1500` emits `AgentNeedThresholdCrossed(Hunger)` during Work and free time.
- The notification is informational/navigation UI only.
- It does not reserve food, start Eat, cancel Work or override a player order.
- Recovery above the threshold followed by a new downward crossing can notify again.
- Duplicate source events remain suppressed.

## Full workflow

### Success path

1. Simulation advances passive needs.
2. A threshold crossing emits a typed event.
3. Utility AI evaluates schedule-gated candidates.
4. Automatic Eat is rejected during Work or acquires food outside Work.
5. Sleep reserves a Bed when available, otherwise uses FloorSleep.
6. Each active interval commits its proportional need delta.
7. Completion consumes/releases the authoritative target and clears the action without duplicating effects.
8. HUD/notifications refresh from snapshots/events.

### Interruption, failure and retry

- Missing reserved food/facility blocks the targeted action and preserves prior intervals.
- Direct commands may replace the resident action according to existing input priority.
- Bed destruction/packing/target loss releases the reservation; a later Sleep decision retries Bed then Floor.
- Food unavailability outside Work remains a typed blocked reason.
- FloorSleep never fabricates a building reservation.

### Multiple residents

- Food and Bed reservations prevent double ownership.
- Resolution order remains stable by resident and target identifiers.
- Residents unable to obtain the same Bed independently fall back to Floor.

## Acceptance

- [x] Work-schedule critical hunger leaves automatic Eat unavailable and preserves Work/player order.
- [x] Direct feeding during Work starts and advances the authoritative meal.
- [x] Free-time hunger reserves and consumes one real food unit.
- [x] Work-time hunger emits one notification without automatic reservation/consumption.
- [x] Partial targeted Eat/Sleep/Leisure produces partial need changes.
- [x] Interruption preserves applied intervals and completion does not duplicate them.
- [x] Available Bed is preferred; no Bed produces FloorSleep.
- [x] Floor applies Mood 0 positive gain and Alertness cap 7500.
- [x] Save/replay cursor remains the active action elapsed interval.
- [x] Domain, Application and deterministic regressions cover the workflow.
- [x] Unity Play Mode regression is checked in.
- [ ] Licensed Unity EditMode/PlayMode actually executes the complete workflow.

## Verification boundary

PR #571 code head `aa635ac84627f069154fe232e2cdcb2aaf34860d` passed repository Quality, Release build/tests, headless smoke and standard/large deterministic soaks. The system is `IMPLEMENTED`, not `VERIFIED`: the Unity workflow recorded blocked evidence and skipped the actual EditMode/PlayMode test step because no usable licensed activation was available.