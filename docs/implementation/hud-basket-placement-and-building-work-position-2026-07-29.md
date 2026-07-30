# HUD, basket placement и building side work position — 2026-07-29

Status: `IMPLEMENTED` after merge of the linked PR; licensed Unity Play Mode evidence remains required for `VERIFIED`.

Implementation PR: [#519](https://github.com/bageus/Dig/pull/519).

Authoritative specifications:

- [`../design/resident-inventory-expansion.md`](../design/resident-inventory-expansion.md);
- [`../design/runtime-selection-excavation-item-placement-decisions.md`](../design/runtime-selection-excavation-item-placement-decisions.md);
- [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md);
- [`../design/building-production-and-internal-supply.md`](../design/building-production-and-internal-supply.md).

Tracking: #67, #69, #70, #113, #387, #433, #511.

## Implementation map

- `DigGameHudCanvas.Inventory` resolves compartment columns from capacity, hides Weapon/Cargo text headings and fills exactly two rows column-first (`1/2`, `3/4`, `5/6`).
- `DigGameHudCanvas.Layout` owns one responsive bottom HUD height shared by minimap, context panel and clock; context builders only choose internal compact metrics.
- `ResidentInventoryPlacementHandlers` allow inventory expansions and commit active basket placement through an Inventory-owned reserved spill transaction.
- `InventoryState.Spill` preserves quantity/reservation invariants while moving the active expansion and its compartment contents atomically.
- campfire/workstation content exposes side-only work positions; placement/demo resolution requires same-plane open supported cells.
- save/load fixtures and building definitions preserve the same side work-position candidates, so restored construction and production jobs never reintroduce an above-building worker cell.

## Regression boundary

Automated coverage includes:

- Main/Cargo/Weapon two-row layouts without Weapon/Cargo text titles and with column-major slot order `1/2`, `3/4`, `5/6`;
- fixed outer HUD bounds and adaptive inner controls;
- empty and occupied basket placement jobs, reservation cleanup and quantity-safe spill;
- supported-surface target validation;
- side work position content/placement contracts;
- checked-in Unity Play Mode scenarios for HUD geometry and worker position.

Actual Unity Test Runner execution remains owned by #511. Source contracts or a green activation-blocked workflow do not promote this implementation to `VERIFIED`.
