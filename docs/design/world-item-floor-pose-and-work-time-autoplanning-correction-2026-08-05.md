# World-item floor pose and work-time automatic planning correction

Status: `APPROVED`.

Decision date: 2026-08-05.

Tracking issue: [#650](https://github.com/bageus/Dig/issues/650).

Related authoritative specifications:

- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-schedule-needs-actions.md`](resident-schedule-needs-actions.md);
- [`game-time-scale-runtime-synchronization-correction-2026-08-03.md`](game-time-scale-runtime-synchronization-correction-2026-08-03.md);
- [`unified-game-time-and-action-cadence.md`](unified-game-time-and-action-cadence.md).

This correction supersedes older rules that allowed emergency automatic Sleep during `Work` and any implementation that treats `AutomaticPlanningEnabled` as an all-day work eligibility flag.

## 1. Loose world-item floor pose

- Ordinary loose world items, materials, food, tools and weapons rest flat on the floor instead of standing vertically.
- Presentation applies the deterministic floor-rest rotation before the existing geometry-derived grounding pass. The active renderer bounds and interaction collider therefore still touch the authoritative projected floor plane after scale and rotation.
- The same ordinary world projection is used for loose world stacks and internal building-stock units.
- Held, equipped and carried projections keep their authored socket pose.
- Entity-shaped cargo containers, including BuildingBox, keep their authored upright container pose.
- The distinction is profile/policy based. ItemId allowlists, per-item ground flags and manual vertical offsets remain forbidden.
- Selection, hover, pickup identity, quantity, reservations, gravity and authoritative `ItemLocation` are unchanged.

## 2. Clock schedule colours

- Orange schedule sectors mean `ScheduleActivity.Work`.
- Blue schedule sectors mean rest/free time: every schedule activity outside `Work`.
- Colours project the selected or hovered resident schedule only. They do not own global time, day length or simulation cadence.

## 3. Automatic planning scope

`AgentState.AutomaticPlanningEnabled` controls eligibility for new automatic Jobs only during `ScheduleActivity.Work`.

### Work time

- `AUTO ON`: a living resident without a higher-priority direct order/combat owner is eligible for newly appearing automatic Jobs.
- `AUTO OFF`: no new automatic Job is assigned. If the resident has no already-owned Job, direct order or combat owner, the resident remains `Idle` until rest/free time.
- Automatic Eat, Sleep and Leisure/Rest are unavailable during Work, including critical Nutrition, Alertness or Mood.
- Direct player feeding and other explicit orders remain valid during Work and keep normal priority.
- Combat/flee retains its existing emergency priority.
- Changing AUTO or entering/leaving Work does not cancel an already claimed or in-progress Job.

### Rest/free time

- New automatic Jobs are unavailable regardless of AUTO state.
- AUTO ON/OFF is ignored for needs/leisure selection.
- Automatic Eat, Sleep and Leisure/Rest use the existing deterministic utility, target, reservation, interruption and retry workflow.
- Direct player orders remain available.

### Schedule transition

- When an autonomous targeted Eat/Sleep/Leisure action reaches a Work boundary, the action is interrupted, its target reservation is released exactly once, and the resident becomes eligible for Work according to AUTO.
- A direct player meal is not an autonomous targeted action and may continue under the existing direct-feed rule.
- Already committed need intervals/bites are preserved; no effect is rolled back or duplicated.

## 4. Ownership

- `AgentState` owns schedule, AUTO preference, active action and needs.
- Jobs owns claimed/in-progress work and reservations.
- Utility AI owns candidate selection but cannot bypass schedule eligibility.
- Presentation `AgentViewModel.IsAvailableForAutomaticPlanning` projects the same Domain rule used by candidate producers.
- Clock Presentation owns only sector colours.
- World-item Presentation owns floor pose and geometry grounding; Inventory remains authoritative for item state.

## 5. Acceptance

- ordinary loose item/material/tool/weapon/food visuals are visibly flat and geometry-grounded;
- internal stock uses the same floor pose; BuildingBox/cargo and held/equipped visuals retain their separate pose;
- orange sectors map to Work and blue sectors map to non-Work schedule time;
- Work + AUTO OFF + no owned Job selects Idle and exposes Eat/Sleep/Rest as unavailable;
- Work + AUTO ON admits newly available Jobs;
- rest/free time never admits new automatic Jobs, regardless of AUTO;
- claimed/in-progress Jobs survive AUTO toggles and schedule transitions;
- Work-boundary interruption releases autonomous needs reservations and preserves committed effects;
- direct orders/feed and combat keep priority;
- Domain, Application/Presentation, deterministic/source-contract and Unity Play Mode regressions cover the complete workflow;
- `VERIFIED` requires executed Unity evidence, not source inspection alone.
