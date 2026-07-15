# BuildingBox packing lifecycle

## Scope

This slice implements the authoritative reverse transition from a completed box-built building back to one `BuildingBox` inventory stack.

It does not add the Unity building-functions panel or save-format support for an active packing plan. Those remain follow-up slices of issue #118.

## Ownership

- `BuildingsState` owns the packing plan, work progress and the final removal of the building.
- `JobSystem` owns the worker, lifecycle stages and reservations.
- `InventoryState` owns the output box stack and its world location.
- Application handlers coordinate the single-threaded transaction after validating every expected failure.
- Presentation may request packing, render progress and display reasons, but does not mutate Buildings, Jobs or Inventory directly.

## Lifecycle

A completed building with an enabled `BuildingBoxPolicy` can start one packing plan:

```text
Completed building
  -> available BuildingBoxPacking job
  -> TravelToDestination
  -> PerformWork
  -> Finalize
  -> Removed building + one BuildingBox stack at origin
```

The building remains `Completed`, authoritative and footprint-occupying until the successful final commit. The active `BuildingPackingPlanSnapshot` distinguishes that state without introducing a second building object or a transient replacement status.

The packing plan stores stable ids for:

- the owning Job;
- the future output Inventory stack;
- completed packing work;
- active, completed or cancelled commitment.

## Reservations

`BuildingBoxPackingJobDefinition` reserves:

- the assigned resident through the common Job claim;
- the building as a destination;
- the building work position.

Completion and cancellation use normal `JobSystem` terminal transitions, so worker and position reservations are released by the common ledger.

## Completion invariants

Before any mutation, `CompleteBuildingBoxPackingHandler` verifies:

- the Job belongs to the building packing plan;
- the Job is in `Finalize`;
- the plan is active and has all required work;
- the future output stack id is unused;
- the configured box item is non-stackable.

The commit then:

1. creates exactly one box at the building origin;
2. marks the packing plan completed;
3. removes the building and releases its footprint;
4. completes the Job and releases reservations.

A replay is rejected before Inventory mutation because the building is removed and the Job is terminal. The preselected output stack id also prevents a second stack from being created.

## Cancellation

Cancellation is valid only while the packing plan and Job are non-terminal. It:

- cancels the Job;
- releases common reservations;
- marks the packing plan cancelled;
- leaves the building completed and footprint-occupying;
- creates no output item.

A later packing request may replace the terminal cancelled plan with a new stable Job/output id pair.

## Protected transitions

While packing is active, direct building damage and direct removal are rejected. This prevents the building from becoming damaged or disappearing while a Job still owns its packing destination and work position.

## Validation

Regression tests cover:

- starting packing without creating an output box;
- complete Job progression through travel, work and finalize;
- one-box quantity conservation;
- footprint release only after final commit;
- cancellation with no output and released reservations;
- completion replay without duplication;
- damage and direct removal rejection while packing is active.
