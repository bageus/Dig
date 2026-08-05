# Resident schedule-gated needs actions

**Status:** APPROVED  
**Decision dates:** 2026-08-02, cadence correction 2026-08-03, autoplanning correction 2026-08-05  
**Tracking:** #159, #142, #113, #601, #650

## Scope

This specification clarifies the observable schedule, needs-action, sleep-target and notification workflow shared by:

- `docs/design/needs-continuous-actions.md`;
- `docs/design/content/food.md`;
- `docs/design/sleep-comfort-and-bed-assignment.md`;
- `docs/design/resident-hud-selection-and-notifications.md`;
- [`unified-game-time-and-action-cadence.md`](unified-game-time-and-action-cadence.md);
- [`world-item-floor-pose-and-work-time-autoplanning-correction-2026-08-05.md`](world-item-floor-pose-and-work-time-autoplanning-correction-2026-08-05.md).

Where an older implementation or test allows automatic eating, sleep, leisure or new automatic Job assignment during the wrong schedule segment, this document is authoritative. Where an older timing value differs, the 2026-08-03 unified cadence specification is authoritative.

## Authoritative state ownership

- `AgentState` owns Nutrition, Alertness, Mood, Health, schedule and active action.
- Utility AI selects an intention but does not mutate needs.
- Inventory owns food identity, quantity and reservations.
- Building facilities own bed/leisure availability and reservations.
- Jobs owns automatic claims and assignments.
- Domain/Application action intervals commit need effects.
- Presentation projects snapshots/events and never applies need effects or creates an automatic Eat command from a notification.

## Schedule and input priority

### Working time

- Orange clock sectors mean `ScheduleActivity.Work`; blue sectors mean rest/free time.
- Automatic `Eat`, `Sleep` and `Leisure` are unavailable during Work, including critical needs.
- Hunger, tiredness and Mood do not interrupt an already-owned Job or an active direct player order.
- A manual/direct feed command is allowed during working time and owns the real bite-based `Eat` workflow.
- An already active direct meal may continue after the schedule enters Work.
- With AUTO ON, the resident may acquire newly available automatic Jobs.
- With AUTO OFF, no new automatic Job is assigned. If no Job/direct order/combat action is already owned, the resident remains Idle until rest/free time.
- Changing AUTO or leaving Work does not cancel an already claimed or in-progress Job.

### Free time

- Outside Work, Nutrition can select automatic Eat when food is available.
- Sleep and Leisure compete through the existing deterministic utility rules and schedule bonuses.
- AUTO ON/OFF is ignored outside Work and no new automatic Job is assigned.
- Direct player orders and already-owned Jobs keep their existing lifecycle.

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
3. Utility AI evaluates schedule-gated candidates and AUTO eligibility.
4. During Work, automatic needs/leisure are rejected and automatic Work is available only with AUTO ON.
5. Outside Work, automatic Jobs are unavailable; Eat/Sleep/Leisure use their ordinary target workflows. Sleep reserves a Bed when available, otherwise uses FloorSleep.
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
- AUTO or schedule transition does not release an already-owned Job reservation.

### Multiple residents

- Food and Bed reservations prevent double ownership.
- Resolution order remains stable by resident and target identifiers.
- Residents unable to obtain the same Bed independently fall back to Floor.
- Automatic Job candidate production independently evaluates each resident's Work schedule and AUTO flag.

## Acceptance

- Work-schedule critical hunger/tiredness leaves automatic Eat/Sleep/Leisure unavailable and preserves owned Job/player order.
- Work + AUTO OFF + no owned Job selects Idle until free time.
- Work + AUTO ON exposes new automatic Job candidates; outside Work neither AUTO state exposes new Job candidates.
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

The implementation may be marked `IMPLEMENTED` after repository Quality, Release tests, smoke and deterministic soaks pass. It may be marked `VERIFIED` only after licensed Unity EditMode/PlayMode actually executes calendar cadence, Work/AUTO transitions, work-hunger notification, direct feed with bite cooldowns, Bed preference, Floor fallback and HUD refresh.
