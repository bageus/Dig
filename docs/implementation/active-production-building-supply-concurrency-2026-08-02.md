# Active production / building supply concurrency — 2026-08-02

Status: `IMPLEMENTED` on branch `fix/active-production-building-supply`; licensed Unity Play Mode evidence remains required.

Authoritative design: [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).
Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

## Observed regression

A completed workstation had enabled missing internal stock and visible, navigation-connected world materials, but no resident created or executed the refill workflow while production was active.

## Root cause

`ProductionWorkJobDefinition` correctly owns an exclusive `ReservationKey.ForPosition(WorkPosition)` for the craft action. `BuildingSupplyJobDefinition` also requested the same exclusive position even though the approved supply workflow is allowed to run concurrently with production.

`CreateBuildingSupplyJobHandler` reserved the source quantity, resident slot capacity and incoming building capacity, then failed at `JobSystem.Claim` with `ReservationConflict`. The handler rolled those external owners back, and the next synchronization pass repeated the same failed claim. Materials therefore remained visible and reachable without any persistent supply job.

The existing bootstrapped supply test did not expose this conflict because it ran without an active `ProductionWorkJob` and placed the source directly under the worker.

## Correction

- production retains exclusive ownership of the craft/work position;
- building supply reserves the building destination, but not the production work position;
- `BuildingSupplyState.ActiveSupplyJobId` and the destination reservation continue to enforce one active supply batch for the workstation;
- authoritative movement occupancy still serializes residents that approach the same physical cell;
- source reservation, resident slot claims, incoming capacity, workstation check, route/acquire/deposit and cancellation transactions are unchanged.

## Regression coverage

- Domain test proves production and supply jobs for the same building can be claimed by different residents while only production owns the position reservation;
- Application test executes `CreateBuildingSupplyJobHandler` while production already owns the work position and requires source/incoming reservations plus a claimed supply job;
- checked-in Unity Play Mode bootstraps a campfire, starts production from one internal cap, places another cap at a remote resident cell, requires simultaneous production/supply workers, completes the outbound/return supply route and commits stock into `ItemLocation.InBuilding`.

## Verification boundary

Repository build/tests and source contracts can validate reservation ownership and engine-independent transactions. The runtime workflow is not `VERIFIED` until the checked-in Play Mode scenario executes in a licensed Unity Test Runner and retains its XML/log evidence under #511.
