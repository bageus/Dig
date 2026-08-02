# Campfire serialized operation and demo workbench correction — 2026-08-02

Status: `APPROVED`.

Parent authoritative specifications:

- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`resident-inventory-expansion.md`](resident-inventory-expansion.md).

Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

This addendum records the latest confirmed user decision and supersedes the concurrent production/supply rule introduced by PR #564.

## Building operation ownership

- One completed production building has at most one active resident operation.
- `ProductionWorkJob` and `BuildingSupplyJob` for the same building share a building-level reservation and cannot be claimed or active concurrently.
- A production job keeps ownership through output close and the worker's return to the building `WorkPosition`.
- A supply job keeps ownership through committed internal-stock deposit or cancellation.
- After production close and return, synchronization checks enabled missing stock first. Reachable refill is scheduled before the next production order.
- If no supply job can be created, the next eligible production order may start.

## Production material workflow

The observable sequence is:

`internal stock -> carried raw material -> temporary log/workbench -> one-tick demo processing -> derived processed carry -> unfinished package -> close -> return to WorkPosition -> terminal/idle`.

- The small log/workbench is derived Presentation only. It has no entity, collider, reservation or save state and exists only while production is active.
- Demo/test material processing temporarily completes in one simulation tick. The data-driven production-duration overload remains available for deterministic phase tests.
- `ProcessedAwaitingPackage` remains authoritative `ProductionState`; the carried processed-material visual is derived and does not create a second Inventory item.
- Package deposit remains an explicit exactly-once commit. Processing time alone cannot complete a material step.
- The last deposit closes the output package and completes the order, but the job remains active until the worker returns to `WorkPosition`.

## Resident inventory ingress correction

- Successful ingress commits into its claimed slot, releases the incoming claim, then normalizes the resident layout in the same authoritative operation.
- Newly activated expansions may therefore rebalance compatible items immediately.
- A temporary high-index claimed slot must not remain visible when a lower valid slot is free; Main/Weapon/Cargo low-index priority remains authoritative.

## Acceptance

- pickup of an expansion into temporary Main slot 3 compacts it into the first valid Main slot after ingress;
- production and supply for one building cannot be active at the same time;
- production reaches workbench processing, processed carry, package deposit, package close and return-to-building completion;
- after return, reachable enabled internal-stock demand creates supply before another production order;
- workbench log appears only during active production and has no collider;
- default demo processing uses one tick;
- unit, application, deterministic and checked-in Unity Play Mode regressions cover the full sequence;
- actual licensed Unity Play Mode execution is still required before `VERIFIED`.
