# Resident schedule-gated needs actions

**Status:** APPROVED  
**Decision dates:** 2026-08-02, cadence correction 2026-08-03  
**Tracking:** #159, #142, #113, #601

## Scope

This specification clarifies the observable schedule, needs-action, sleep-target and notification workflow shared by:

- `docs/design/needs-continuous-actions.md`;
- `docs/design/content/food.md`;
- `docs/design/sleep-comfort-and-bed-assignment.md`;
- `docs/design/resident-hud-selection-and-notifications.md`;
- [`unified-game-time-and-action-cadence.md`](unified-game-time-and-action-cadence.md).

Where an older implementation or test allows automatic eating during Work or blocks Sleep when no bed is available, this document is authoritative. Where an older timing value differs, the 2026-08-03 unified cadence specification is authoritative.

## Authoritative state ownership

- `AgentState` owns Nutrition, Alertness, Mood, Health, schedule and active action.
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
- Critical Alertness may still select Sleep; the user disabled automatic working-time eating, not emergency sleep.

### Free time

- Outside Work, Nutrition can select automatic Eat when food is available.
- Sleep and Leisure compete through the existing deterministic utility rules and schedule bonuses.
- Work remains possible off schedule only through the existing lower off-schedule score or a direct order.

## Runtime cadence

Unity normal playback uses only `SimulationState.Clock.TickDuration`; demo default is `1.0` real second per simulation tick. Driver keeps no independent normal cadence. The demo calendar uses `150 ticks/hour` and `3 600 ticks/day`. Passive needs and action intervals change only on committed simulation ticks, so pause/single-step/x2/x4 preserve deterministic order. Full contract: [`runtime-needs-supply-sleep-food-recovery.md`](runtime-needs-supply-sleep-food-recovery.md) and [`unified-game-time-and-action-cadence.md`](unified-game-time-and-action-cadence.md).

Fresh demo residents begin with full Nutrition. Without recovery, full Nutrition/Alertness last `7 200/10 800` ticks. Continuous critical hunger depletes full Health over `1 800` ticks through proportional deterministic deltas.

## Needs are changed by real actions

- Nutrition improvement is committed by confirmed food bites or targeted Eat intervals.
- A standard three-bite meal commits at `T+1`, `T+3` and `T+5`; cooldown ticks apply no Nutrition.
- Alertness improvement is committed by active Sleep intervals.
- Mood improvement is committed by data-driven food, Sleep or Leisure intervals.
- Idle does not restore Alertness, Mood or Health by itself.
- Work, movement and combat do not produce positive natural recovery.
- Passive decay/critical penalties remain authoritative and continue on simulation ticks.
- Travelling to a Bed does not count as recovery: while `Sleep.ElapsedTicks == 0`, critical Alertness and Nutrition use ordinary passive damage.
- After the first Sleep interval is committed, critical Alertness alone does not apply passive Health/Mood damage while that same Sleep action remains active; Sleep Health recovery is therefore monotonic for exhaustion-only recovery.
- Critical Nutrition remains a survival-critical damage source during Sleep and can still reduce Health.

Targeted Eat, Sleep and Leisure distribute configured total action effects across deterministic intervals. Integer remainder is assigned to earliest intervals. Interruption preserves committed intervals. Completion clears action/reservation but applies no duplicate full-action effect.

The active action elapsed interval is the save/replay cursor. For meals, `NextBiteTick` is also authoritative so restore continues after cooldown without replay.

## Sleep target priority

1. Resolve and reserve an available Bed facility.
2. When no Bed is available, create a reservation-free `FloorSleep` target for that resident.
3. Do not report `bed_unavailable` while Floor sleep is possible.
4. Floor sleep has no positive Mood effect and cannot raise Alertness above `7500`.
5. A Bed reservation is released exactly once on completion, interruption, death or target loss. Floor owns no building reservation.

An available Bed always wins over Floor for the resident being resolved. With one Bed and two tired residents, one uses the Bed and the other sleeps on the Floor without double reservation.

## Hunger notification

- The downward crossing `Nutrition < 1500` emits `AgentNeedThresholdCrossed(Hunger)` during Work and free time.
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
6. Each active interval/bite commits its proportional need delta.
7. Completion consumes/releases the authoritative target and clears the action without duplicating effects.
8. HUD/notifications refresh from snapshots/events.

### Interruption, failure and retry

- Missing reserved food/facility blocks the targeted action and preserves prior intervals.
- Direct commands may replace resident action according to existing input priority.
- Bed destruction/packing/target loss releases reservation; a later Sleep decision retries Bed then Floor.
- Food unavailability outside Work remains a typed blocked reason.
- FloorSleep never fabricates a building reservation.
- Meal interruption preserves completed bites and discards the consumed remainder; cooldown does not create a hidden fourth effect.

### Multiple residents

- Food and Bed reservations prevent double ownership.
- Resolution order remains stable by resident and target identifiers.
- Residents unable to obtain the same Bed independently fall back to Floor.

## Acceptance

- Work-schedule critical hunger leaves automatic Eat unavailable and preserves Work/player order.
- Direct feeding during Work starts and advances the authoritative meal.
- Free-time hunger reserves and consumes one real food unit; when loose food is unavailable but a reachable closed `food` package exists, resident automatically breaks it through package Use and then picks up/eats one released unit.
- Work-time hunger emits one notification without automatic reservation/consumption.
- Partial targeted Eat/Sleep/Leisure produces partial need changes.
- A committed Sleep interval prevents Alertness-only critical Health loss until interruption/completion, but does not suppress critical Nutrition damage; walking to a Bed has no such protection.
- Bites commit at `T+1/T+3/T+5`; cooldown save/load resumes without replay.
- Interruption preserves applied intervals and completion does not duplicate them.
- Available Bed is preferred; no Bed produces FloorSleep.
- Floor applies Mood 0 positive gain and Alertness cap 7500.
- Save/load resumes at the next interval/bite without replay or simulation-tick scaling.
- Domain, Application, deterministic and Unity Play Mode regressions cover the complete workflow.

## Verification boundary

The implementation may be marked `IMPLEMENTED` after repository Quality, Release tests, smoke and deterministic soaks pass. It may be marked `VERIFIED` only after licensed Unity EditMode/PlayMode actually executes calendar cadence, work-hunger notification, direct feed with bite cooldowns, Bed preference, Floor fallback and HUD refresh.
