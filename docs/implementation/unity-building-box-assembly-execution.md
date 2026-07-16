# Unity BuildingBox assembly execution

This slice connects a confirmed BuildingBox placement plan to the existing resident, Navigation, Jobs, Inventory, Buildings, and Unity projection loops.

## Ownership

- `InventoryState` remains the only owner of the box stack, location, quantity, and quantity reservation.
- `JobSystem` owns assignment, resident/item/position claims, stages, and terminal cleanup.
- `BuildingsState` owns the plan, footprint, work progress, commit state, and completed building.
- Unity owns no authoritative construction state. It evaluates immutable snapshots and dispatches one Application command per logical tick.

## Reserved transit

A confirmed plan reserves exactly one non-stackable box. During pickup the whole reserved stack moves from its world location to `AgentInventory` while retaining the same quantity reservation. This prevents a second plan from observing the carried box as available.

The reservation is consumed only when `CommitBuildingBoxToSiteHandler` moves the box from the assigned resident into the building site inventory. Finalize consumes the site box exactly once.

## Execution order

For each simulation tick:

1. available assembly jobs receive deterministic resident candidates;
2. the shared assignment handler claims one available resident and common reservations;
3. Navigation routes the worker to the world box, or directly to the work position when the box is already carried by that worker;
4. `BuildingBoxAssemblyExecutionPolicy` chooses at most one step;
5. Application handlers perform start, acquire, stage advance, site commit, one work unit, or finalize;
6. job, building, item, route, and HUD projections refresh from authoritative snapshots.

The logical stages remain:

```text
AcquireItem -> TravelToDestination -> DepositItem -> PerformWork -> Finalize
```

## Visual diagnostics

The existing building renderer now distinguishes lifecycle states by geometry:

- `AwaitingBox`: low rotated plan marker;
- `ReadyToBuild`: delivered site base;
- `UnderConstruction`: taller construction volume;
- `ReadyToComplete`: nearly complete volume;
- `Completed`: full building geometry.

The route overlay labels assembly paths separately from pickup, hauling, packing, and terrain work.

## Regression coverage

Tests verify that:

- a worker away from a world box cannot start the job;
- transit preserves the exact quantity reservation;
- site commit requires the assigned resident to carry the reserved box;
- the policy-driven lifecycle reaches `Completed`;
- the source box is consumed once;
- common job reservations are released;
- terminal evaluation cannot replay Finalize.

## Remaining work

The next UI slice is resident inventory presentation and selecting a carried BuildingBox directly from its inventory slot to enter the same placement mode.
