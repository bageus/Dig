# Unity building functions slice

This slice connects completed BuildingBox buildings to the interactive Unity demo without moving simulation authority into UI code.

## Ownership

- `BuildingsState` remains the authoritative source for building status, durability, footprint, and packing progress.
- The existing shared demo `JobSystem` owns assembly and packing jobs and reservations.
- A dedicated BuildingBox inventory aggregate owns the non-stackable box item catalog and packed output stacks.
- Navigation owns the logical route to the building work position.
- `BuildingWorldPresenter`, `BuildingFunctionsPresenter`, and `InventoryWorldPresenter` publish immutable read models.
- Unity renderers, interaction, HUD, and the simulation driver only select, display, and dispatch typed Application commands.

## Selection and panel behavior

A raycast hit on a completed building is routed through `ContextInputRouter` as `CompletedBuilding`. The resulting `SelectBuilding` effect clears resident, cell, and job selection before showing the building-functions panel. Selecting another supported object clears building selection, so the contextual modes remain mutually exclusive.

Selection is not color-only: the selected building also becomes taller and wider, while the HUD names the selected object and reports its status.

## Pack command boundary

The Pack button reads the immutable action state. Disabled actions show a typed reason code and dispatch no command. An enabled click creates one deterministic job/output id pair and invokes `StartBuildingBoxPackingCommand` through `BuildingFunctionsCommandAdapter`.

After a successful command, the Unity adapter reloads building and job projections. The building remains selected, the Pack action becomes disabled as active, and the new packing job appears in the existing jobs/reservations data source.

## Resident execution

On each logical simulation tick, the Unity composition performs the following orchestration through existing authoritative handlers:

1. active available packing jobs receive deterministic resident candidates;
2. `AssignAvailableJobsHandler` claims one free living resident and the common destination/position reservations;
3. the Navigation pathfinder moves the resident by at most one logical cell per tick toward the building work position;
4. `BuildingBoxPackingExecutionPolicy` chooses at most one execution step from immutable Job and Building snapshots;
5. the normal Job handler starts and advances `TravelToDestination`, `PerformWork`, and `Finalize`;
6. the normal packing handlers add one work unit per tick and atomically complete the packing operation.

Logical worker position, not an animation callback, gates every execution step. A worker away from the work position produces no mutation.

At Finalize, `CompleteBuildingBoxPackingHandler` creates exactly one non-stackable BuildingBox stack at the building origin, marks the building removed, completes the Job, and releases its common reservations. The next presentation refresh removes the building visual and includes the new box in the combined world-item projection.

## Diagnostics and replay safety

- Packing routes use the existing navigation-route overlay.
- Job status, stage, worker, and reservations remain visible in the existing job inspector.
- Terminal jobs produce no additional execution step.
- Output-stack identity is predetermined by the Pack command, so replay cannot create a second box.
- Rebuilding Unity visuals does not change assignments, routes, work progress, inventory, or building state.

## Remaining work

BuildingBox world-item selection, normal click-to-place, Alt-click pickup, resident-inventory placement, and local Unity Play Mode coverage remain follow-up work for #14, #115, and #118.
