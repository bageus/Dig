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

For every stock rule used by the next queued recipe, the production refill threshold is `ceil(capacity / 2)`. A stock is below threshold only when `current + incoming < threshold`; equality does not trigger refill. With campfire mushroom-cap capacity `4`, quantities `4`, `3` and `2` may start the next grilled-mushroom unit, while quantity `1` requests supply before another unit.

When a building has a non-terminal production queue:

1. consecutive production units are allowed while the next unit has its complete input set and every required stock is at or above its threshold;
2. after a produced unit closes and the worker releases the building operation, the operation turn becomes `Supply`;
3. the Supply turn is used only when the next unit lacks an input or at least one of its required stocks is below threshold;
4. one supply job takes every currently eligible free world unit it can carry for the next recipe's required item types; it does not wait for unavailable types and does not reserve protected building/resident inventory;
5. unavailable required types create the existing supported extraction/harvest dependency when possible; unsupported types remain an explicit shortage;
6. while the current `Supply` turn has produced no committed delivery, its unresolved supported extraction dependency keeps the next production unit waiting;
7. after any committed supply batch, the operation turn becomes `Production`, so one next unit may start whenever its complete input set exists even if other extraction dependencies are still unresolved or the batch could not restore every required stock to threshold/capacity;
8. unresolved dependencies never reserve the workstation; their eventual deliveries wait for the building operation to become free. If no eligible direct source and no supported extraction dependency can be created, an otherwise runnable unit is not permanently blocked.

Example for grilled mushroom with cap capacity `4`: starting at `4`, three consecutive units may consume the stock `4 -> 3 -> 2 -> 1`; only then does the next Supply turn run. If the world currently contains only part of the depleted recipe inputs, that batch delivers only those units, extraction jobs are created for supported unavailable inputs, and production resumes after the batch when the next recipe still has a complete input set.

If there is no production queue, enabled refill remains continuous: synchronization creates delivery whenever eligible free materials appear and stops only when `current + incoming == capacity`. Supported extraction dependencies may replenish missing source types, but one failed/unreachable batch cannot leave phantom incoming or permanently block later refill.

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
- campfire cap stock at `4`, `3` or `2` allows consecutive grilled-mushroom units; cap stock at `1` gives the next operation to Supply;
- one queued supply batch collects all currently eligible free required materials without waiting for unavailable types;
- a supported extraction dependency blocks the current pre-production Supply turn until a delivery commits;
- after a partial delivery commits, unresolved dependencies no longer block one runnable Production turn;
- if neither direct delivery nor supported extraction can be created, a runnable unit is not permanently blocked;
- supply completion yields one Production turn whenever the next unit has a complete input set, even when stock remains below threshold;
- no production queue allows continuous refill to capacity as eligible materials appear;
- save/load preserves operation turn, active reservations and action progress without duplication;
- Domain, Application, deterministic, headless and Unity Play Mode regressions cover the full workflows.

## Verification boundary

The implementation can be marked `IMPLEMENTED` after final-head Quality, headless and deterministic tests. It is `VERIFIED` only after licensed Unity Play Mode executes cadence, Tent walking/sleep, food-package opening/eating, and production/supply alternation in the real composition.
