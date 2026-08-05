# World-item floor pose and work-time automatic planning correction

Status: `APPROVED`.

Tracking issue: [#650](https://github.com/bageus/Dig/issues/650).

Related authoritative specifications:

- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md);
- [`resident-schedule-needs-actions.md`](resident-schedule-needs-actions.md);
- [`unified-game-time-and-action-cadence.md`](unified-game-time-and-action-cadence.md);
- [`presentation-input-ui-and-diagnostics.md`](presentation-input-ui-and-diagnostics.md).

## 1. Confirmed observable behavior

### Loose world-item pose

- ordinary loose world items, materials, tools, weapons and food rest flat on the floor instead of standing vertically;
- the floor-rest pose is applied before geometry-derived grounding, so the visible renderer bounds and the interaction collider still touch the authoritative floor plane;
- internal building-stock units use the same world-item floor-rest projection;
- held, carried and equipped projections keep their authored sockets and do not inherit the loose-world pose;
- a BuildingBox keeps its separate upright container projection;
- the decision is based on the definition-owned world interaction/profile, not an ItemId allowlist or per-item vertical offset;
- newly added ordinary item definitions inherit the flat floor-rest pose by default.

### Clock colors

- orange schedule sectors mean `Work` time;
- blue schedule sectors mean rest/free time;
- the clock only projects the selected or hovered resident schedule and does not redefine simulation time.

### Automatic planning

- `AutomaticPlanningEnabled` controls automatic Job acquisition only during `ScheduleActivity.Work`;
- during Work with AUTO ON, a resident may claim newly available automatic Jobs;
- during Work with AUTO OFF, a resident with no already-owned Job, direct order or combat action remains Idle;
- automatic Eat, Sleep and Leisure are unavailable during Work; a direct player feed/order and combat keep their existing priority;
- outside Work, AUTO ON/OFF is ignored for Job acquisition: no new automatic Job is assigned, while ordinary Eat, Sleep and Leisure behavior is allowed;
- an already claimed or in-progress Job is not cancelled by an AUTO toggle or by the schedule leaving Work;
- direct commands remain available regardless of AUTO state or schedule.

## 2. Ownership

- Inventory remains authoritative for item identity, location, quantity and reservations.
- Presentation owns the loose-world floor-rest rotation, geometry-derived grounding and collider projection.
- `AgentState` owns schedule and `AutomaticPlanningEnabled`.
- Utility AI owns intent selection; Jobs owns claims and assignments.
- Presentation clock colors are read-only schedule projection.

## 3. Full workflow

### Work + AUTO ON

1. The schedule projects Work and the clock sector is orange.
2. Automatic candidate production includes the resident.
3. When a Job appears, normal deterministic scoring may assign it.
4. The resident keeps an already-owned Job through later AUTO/schedule changes.
5. When no Job exists, the resident stays Idle rather than starting automatic needs/leisure behavior.

### Work + AUTO OFF

1. The schedule projects Work and the clock sector is orange.
2. Candidate production excludes the resident from new automatic Jobs.
3. Utility AI rejects automatic Eat, Sleep and Leisure.
4. With no direct order, combat action or already-owned Job, the resident remains Idle until rest/free time.

### Rest/free time

1. The clock sector is blue.
2. Candidate production excludes new automatic Jobs regardless of AUTO state.
3. Ordinary Eat, Sleep and Leisure evaluate through their existing deterministic rules.
4. Direct commands and already-owned Jobs retain their existing lifecycle.

## 4. Failure, retry and persistence

- missing work produces Idle, not a fabricated Job;
- a blocked need action outside Work keeps its existing typed reason and retry behavior;
- toggling AUTO does not release current Job reservations;
- save/load preserves the AUTO preference and schedule, and candidate eligibility is recomputed from both after restore;
- the floor-rest pose is derived Presentation state and is not saved.

## 5. Acceptance

- ordinary loose world items and internal stock are visibly flat and geometry-grounded;
- BuildingBox, carry and equipment projections remain separate;
- collider bounds match the flat visible pose;
- orange maps to Work and blue maps to rest/free;
- Work + AUTO OFF + no owned work selects Idle with Eat/Sleep/Rest unavailable;
- Work + AUTO ON can acquire newly available Jobs;
- rest/free time cannot acquire new automatic Jobs for either AUTO state;
- existing Job, direct order/feed and combat priority are preserved;
- Domain, Presentation, deterministic/source-contract and Unity Play Mode regressions cover repeated schedule/AUTO transitions.

## 6. Verification boundary

Repository tests may raise the correction to `IMPLEMENTED`. `VERIFIED` requires licensed Unity Play Mode evidence for the visible item pose, collider grounding, clock colors and runtime schedule/AUTO transitions.
