# Campfire serialized production lifecycle correction — 2026-08-02

Status: `IMPLEMENTED` in branch `fix/campfire-serialized-production-lifecycle`; licensed Unity Play Mode evidence remains required.

Authoritative correction: [`../design/campfire-serialized-operation-and-demo-workbench-correction-2026-08-02.md`](../design/campfire-serialized-operation-and-demo-workbench-correction-2026-08-02.md).

Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

## Reported behavior

- a picked inventory expansion remained in visible Main slot 3 after slots 1/2 became free;
- production and internal-stock supply could target the same building concurrently;
- the worker appeared to stop after staging raw material on the virtual workbench;
- no explicit temporary workbench visual, processed carry or return-to-building completion was visible.

## Root causes

- `AcquireReservedIntoResidentSlots` released its incoming slot claim but did not normalize the resulting resident layout;
- PR #564 deliberately removed the shared building reservation from production, which conflicts with the latest confirmed serialized-operation rule;
- demo composition still used the full `ProductionMaterialTicks` duration, so a staged material waited hundreds of simulation ticks;
- output close completed the production job at the package cell, releasing ownership before the worker returned;
- workbench and processed material were authoritative phases without corresponding derived presentation.

## Correction

- successful ingress normalizes immediately after releasing slot claims;
- production reserves both the building destination and its work position; supply reserves the same building destination;
- supply/dependency synchronization runs before production preparation and skips buildings with active production;
- production preparation skips active/non-terminal supply;
- default demo material duration uses `TestProductionMaterialTicks` (`1`);
- output close completes the order and enters `TravelToDestination`; the job terminal-izes only after arrival at `WorkPosition`;
- active production projects a collider-free small log at the work position;
- carried raw and `ProcessedAwaitingPackage` phases project a derived material carry without creating an Inventory item;
- after return, enabled reachable refill is planned before the next production order.

## Regression coverage

- Domain/Application: building reservation conflict and release, ingress compaction, order-close versus job-return lifecycle;
- source contracts: scheduler ordering, shared reservation, return stage, one-tick demo timing, workbench and processed carry;
- Unity Play Mode: no simultaneous production/supply, active workbench, package close, worker return, then remote refill to capacity.

Actual licensed Unity compilation and Play Mode execution remain required before `VERIFIED`.
