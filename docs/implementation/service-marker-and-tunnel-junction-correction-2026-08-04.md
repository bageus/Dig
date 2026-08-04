# Service marker visibility and tunnel reinforcement correction — 2026-08-04

Status: `IMPLEMENTED IN BRANCH`.

Authoritative correction: [`../design/service-markers-and-tunnel-overwrite-correction.md`](../design/service-markers-and-tunnel-overwrite-correction.md).

Related specifications:

- [`../design/world-item-gravity-selection-and-pickup.md`](../design/world-item-gravity-selection-and-pickup.md);
- [`../design/contextual-input-cursors-and-selection.md`](../design/contextual-input-cursors-and-selection.md);
- [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md);
- [`../design/room-purposes-upgrades-and-tunnel-reinforcement.md`](../design/room-purposes-upgrades-and-tunnel-reinforcement.md).

Tracking issues: [#390](https://github.com/bageus/Dig/issues/390), [#574](https://github.com/bageus/Dig/issues/574).

## Report

Unity screenshots showed large grey cylinders above ordinary world items, a selection circle above a completed building and a clickable cylinder at a vertical/horizontal tunnel junction. The junction cylinder opened the real `TUNNELAUTOMATIC:JUNCTIONSTONETRIM` job panel.

The visible cylinder was the bug. The automatic junction job itself is required. The user also confirmed that automatic horizontal wooden supports remain required, both automatic job kinds use a maximum range of 30 cells from a completed-building footprint cell, and material from reinforcement removed by later excavation is destroyed.

## Root causes

- `DigJobRenderer` created a visible cylinder for every surface job target.
- the Jobs overlay was visible by default in Release/Debug profiles, so ordinary pickup/haul/production targets looked like item interaction cylinders.
- `DigWorldOverlayRenderer` created one elevated selection cylinder for every footprint cell of the selected building.
- the first correction incorrectly removed automatic `JunctionStoneTrim` behavior instead of removing only its Presentation marker.
- automatic range still used the superseded value `20`.
- topology overwrite already removed reinforcement provenance without producing inventory output, but this destructive rule had not been authoritative.

## Implemented correction

- Jobs world markers start hidden and remain an explicit `F3` diagnostic toggle.
- both `WoodenSupport` and `JunctionStoneTrim` jobs are typed as tunnel infrastructure and never create world markers even when Jobs diagnostics are enabled.
- item interaction continues through its invisible trigger collider and visible item geometry; no service cylinder is introduced.
- selected buildings retain model tint/scale, roster-row highlight and panel state but no overhead footprint cylinders.
- automatic `JunctionStoneTrim` synchronization, source reservation, movement, completion, stone consumption and Stonework grant are restored.
- automatic `WoodenSupport` synchronization and completion remain active.
- both automatic job kinds use the shared inclusive 30-cell 3D Manhattan range.
- completed infrastructure still publishes collider-free tunnel geometry.
- when later topology/excavation removes completed reinforcement, no world stack or inventory output is created; the consumed material is destroyed.

## Regression coverage

- overlay visibility contract: Jobs hidden by default and opt-in by user toggle;
- job projection marks both automatic tunnel kinds explicitly and renderer rejects their world markers;
- source contract rejects selected-building overhead cylinders and world-item cylinder geometry;
- Application tests cover automatic junction creation, late source reservation, cancellation/replacement and automatic completion;
- planner and support synchronization tests cover inclusive range 30 and rejection at 31;
- topology overwrite tests cover removal of completed wooden support and junction trim without recovered material output;
- Unity runtime source contract requires both automatic support and junction synchronization paths.

## Verification boundary

Repository Quality, Release build, full .NET suite, headless smoke, deterministic soaks and Stage 2 exports are required on the final PR head. Checked-in Unity interaction regressions are runtime evidence only when a licensed Unity Test Runner actually executes them; blocked activation does not produce `VERIFIED` evidence.
