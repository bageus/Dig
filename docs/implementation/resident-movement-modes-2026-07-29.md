# Resident movement modes — 2026-07-29

Status: `IMPLEMENTED`; licensed Unity Play Mode verification pending.

Authoritative specifications:

- [`../design/resident-movement-modes.md`](../design/resident-movement-modes.md);
- [`../design/resident-movement-occupancy-and-vertical-traversal.md`](../design/resident-movement-occupancy-and-vertical-traversal.md);
- [`../design/ladders-and-elevators.md`](../design/ladders-and-elevators.md).

Tracking: [#386](https://github.com/bageus/Dig/issues/386), [#137](https://github.com/bageus/Dig/issues/137).

## Implementation

- `ResidentMovementModeCatalog` owns data-driven speed and visual-duration values.
- `ResidentMovementModeResolver` resolves `Normal`, `Tired`, `ForcedFast`, `Fleeing`, `Carrying`, `Mobility` and `Climbing`.
- automatic, manual and spatial-work movement call the same fixed-tick cadence gate.
- repeat is derived from replacement of an active route by the same destination.
- BuildingBox is resolved from authoritative Inventory category and blocks fast mobility.
- Hoverboard priority and Reithamster fallback are implemented in the resolver; runtime item activation remains disabled until stable definitions and Q-014 values exist.
- Presentation receives a typed mode view model, applies transition-duration multiplier and keeps Carry action while moving.
- cancellation, completion and failure publish typed interruption reasons.
- obsolete global movement-target filter/cadence composition was removed to avoid a second movement-speed authority.

## Determinism and save/load

The resolver reads Agent, Inventory, command and traversal state. It uses no frame time or wall clock. Authoritative movement remains limited to one cell per fixed tick. Mode and interpolation are derived and are not persisted.

## Regression coverage

- pure resolver priority and Q-014 boundary tests;
- repeat/replacement typed-reason source contracts;
- automatic/manual/spatial-work composition contracts;
- moving-carry visual projection test;
- Unity Play Mode fixture for mode duration and carrying projection.

## Verification boundary

Quality/source contracts and .NET tests can prove the checked-in structure and deterministic policies. The system is not `VERIFIED` until Unity Test Runner actually executes the Play Mode fixture and publishes results; a skipped licensed step is not evidence.
