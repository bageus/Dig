# Runtime cadence, needs actions, workstation interleaving and shelter/food recovery

**Status:** IMPLEMENTED  
**Decision date:** 2026-08-02  
**Tracking:** #2, #159, #142, #433, #459  
**Implementation PR:** #573

## Scope

This specification records the confirmed runtime correction for four observable workflows:

- resident needs must change slowly enough to be readable at normal speed;
- queued production and enabled internal-stock refill must share one workstation without refill starving production;
- a completed Tent is a real sleep facility and must be preferred over Floor sleep when a free reachable slot exists;
- a hungry resident outside Work may open a closed temporary food package and then eat the released food.

It is an authoritative addendum to `systems-core.md`, `resident-schedule-needs-actions.md`, `sleep-comfort-and-bed-assignment.md`, `building-production-and-internal-supply.md` and `campfire-cooking-and-food-use.md`.

## Simulation cadence and readable needs

- `SimulationState.Clock.TickDuration` is the only normal-speed cadence authority.
- The Unity driver reads that duration from the active session; it does not keep an independent serialized normal tick.
- The demo normal tick duration is `2.0` real seconds.
- Pause and single-step remain exact. Fast and very-fast playback remain deterministic multipliers of the same authoritative tick duration.
- Passive Nutrition, Alertness and Mood decay is committed once per simulation tick. HUD values refresh from the committed snapshot; Presentation never predicts or applies need changes.
- A frame catch-up may execute several due ticks, but the existing maximum-ticks-per-frame boundary remains in force.

Save data stores simulation tick/time as before; changing the real-time cadence does not rewrite saved game ticks.

## Workstation operation interleaving

A completed production building still has one authoritative building-level operation owner. Production and BuildingSupply never run simultaneously for the same building.

When a building has a non-terminal production queue and enabled missing internal stock:

1. if the next production unit has its complete required input set, `Production` owns the next turn;
2. after one produced unit closes and the worker releases the building operation, at most one successful `BuildingSupply` batch owns the next turn;
3. after that supply batch commits or releases, `Production` owns the next turn again;
4. the cycle repeats `Production -> one Supply batch -> Production` while both kinds of work remain eligible;
5. supply does not fill the whole internal capacity before returning the turn to production.

If the next queued unit is not runnable, Supply may continue one batch at a time until the required input set becomes runnable. As soon as it is runnable, Production wins the next turn. If there is no production queue, ordinary enabled refill continues to capacity. If no reachable supply source exists, the turn cannot block an otherwise runnable production unit.

The per-building operation turn is authoritative Domain state, included in snapshots/save/load, and advanced only by committed operation lifecycle transitions. Retry, cancellation, route failure and load cannot create simultaneous owners or permanently starve either queue.

## Tent sleep workflow

- Every completed `building.tent` publishes exactly two deterministic `Bed` facility slots at supported cells inside its footprint.
- Packed, destroyed, incomplete or unreachable tents do not publish usable slots.
- A resident selecting Sleep resolves the nearest reachable free Bed slot, reserves it, walks to its cell and only then advances Sleep intervals.
- A reserved slot cannot be used by another resident.
- If no reachable free Bed slot exists, the resident uses reservation-free `FloorSleep` at the current supported cell.
- Tent sleep uses sleep tier `Tent`; Floor keeps Mood `0` positive gain and Alertness cap `7500`.
- Completion, interruption, death, packing, destruction or route invalidation releases the reservation exactly once.

## Automatic food-package recovery

Outside `ScheduleActivity.Work`, a hungry resident resolves food in this order:

1. an edible unit already carried by that resident;
2. a reachable, unreserved loose world food unit;
3. a reachable, unreserved closed output package with package kind `food`.

For case 3 the resident claims the existing production-package Use workflow, walks to the package, breaks it once, and materializes its manifest in the package cell. A later deterministic planning pass claims one released food unit through the existing world-pickup `eatAfterPickup` workflow, then the authoritative three-bite meal applies Nutrition.

Automatic package opening:

- is disabled during Work, matching automatic Eat;
- never applies Nutrition by itself;
- never materializes a package twice;
- never steals a package or food stack reserved by another resident/job;
- prefers loose food over breaking another package;
- uses stable resident/distance/entity ordering;
- releases claims on route failure, interruption, death or target loss.

Direct player package Use remains unchanged and has input priority over automatic planning.

## Diagnostics and UI

Diagnostics expose authoritative tick duration, playback multiplier, resident need action/target, Tent slot/reservation, food plan stage, workstation operation turn and the reason either Production or Supply is currently blocked.

## Acceptance

- normal Unity playback advances one simulation tick every `2.0` real seconds from the session clock;
- pause/single-step/speed changes keep deterministic tick order;
- Work-time hunger still creates no automatic food/package job;
- free-time hunger consumes loose food when available and otherwise opens one food package then eats one released unit;
- two tired residents use two free Tent slots; a third uses Floor; packing the Tent releases slots;
- Sleep effects do not advance before arrival at the reserved Tent slot;
- with runnable queued production and missing enabled stock, execution alternates one production unit and at most one refill batch;
- a missing required input allows refill until runnable, then immediately yields to production;
- no production queue allows continuous refill to capacity;
- save/load preserves operation turn, active reservations and action progress without duplication;
- Domain, Application, deterministic, headless and Unity Play Mode regressions cover the full workflows.

## Verification boundary

The implementation can be marked `IMPLEMENTED` after final-head Quality, headless and deterministic tests. It is `VERIFIED` only after licensed Unity Play Mode executes cadence, Tent walking/sleep, food-package opening/eating, and production/supply alternation in the real composition.
