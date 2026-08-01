# Campfire production stall correction — 2026-08-01

Status: `IMPLEMENTED` on branch `fix/campfire-production-stall`; licensed Unity runtime evidence remains required before `VERIFIED`.

Authoritative design: [`../design/campfire-production-runtime-cadence-correction-2026-08-01.md`](../design/campfire-production-runtime-cadence-correction-2026-08-01.md).
Tracking issue: [#433](https://github.com/bageus/Dig/issues/433).

## Reported workflow

The assigned resident created the unfinished output package, acquired the reserved input from the campfire internal stock and then appeared to stop without completing production.

## Root causes

- `DigBuildingProductionZones.AdvanceProductionJob` applied one material-work tick only on even simulation ticks. Material durations are already resolved in simulation ticks, so the Unity layer silently doubled every production duration.
- `JobOverlayPresenter` did not project `ProductionWorkJobDefinition.WorkPosition` and `JobOverlayViewModel` had no production-work identity. `DigAgentRenderer.WorkFacing` therefore excluded production from stationary work animation, leaving the resident visually idle/carrying during a long active material step.
- Existing tests covered handlers and presentation fragments but did not execute the complete runtime sequence from internal-stock acquisition through package close.

## Correction

- every simulation tick at the workstation now applies one elapsed material-work tick;
- production jobs project their authoritative work position and explicit production-work identity;
- stationary production uses the existing looping `Build` rig action, while travel continues to use movement/carry animation;
- production demo initialization exposes a duration parameter only for executable Play Mode fixtures; normal runtime still uses `CampfireProductionContent.ProductionMaterialTicks`;
- a bootstrapped Play Mode regression uses a one-tick recipe duration and executes synchronization, route planning, resident movement, material acquisition, work, input consumption, finalization and package close.

## Verification boundary

The source contracts and .NET tests establish implementation wiring. Actual Unity EditMode/PlayMode execution is still required before the system status may become `VERIFIED`.
