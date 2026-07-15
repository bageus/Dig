# Unity building functions slice

This slice connects completed BuildingBox buildings to the interactive Unity demo without moving simulation authority into UI code.

## Ownership

- `BuildingsState` remains the authoritative source for building status, durability, footprint, and packing progress.
- The existing shared demo `JobSystem` owns assembly and packing jobs and reservations.
- A dedicated BuildingBox inventory aggregate owns the non-stackable box item catalog used by packing.
- `BuildingWorldPresenter` and `BuildingFunctionsPresenter` publish immutable read models.
- `DigBuildingRenderer`, `DigWorldInteraction`, and `DigHudOverlay` only render, select, route input, and dispatch typed commands.

## Selection and panel behavior

A raycast hit on a completed building is routed through `ContextInputRouter` as `CompletedBuilding`. The resulting `SelectBuilding` effect clears resident, cell, and job selection before showing the building-functions panel. Selecting another supported object clears building selection, so the contextual modes remain mutually exclusive.

Selection is not color-only: the selected building also becomes taller and wider, while the HUD names the selected object and reports its status.

## Pack command boundary

The Pack button reads the immutable action state. Disabled actions show a typed reason code and dispatch no command. An enabled click creates one deterministic job/output id pair and invokes `StartBuildingBoxPackingCommand` through `BuildingFunctionsCommandAdapter`.

After a successful command, the Unity adapter reloads building and job projections. The building remains selected, the Pack action becomes disabled as active, and the new packing job appears in the existing jobs/reservations data source.

## Remaining work

The demo currently starts the packing job but does not yet route a free resident through its travel/work/finalize stages. That execution adapter is a later issue #14/#118 slice; the Domain, Application, save-format, and presentation contracts are already authoritative.
