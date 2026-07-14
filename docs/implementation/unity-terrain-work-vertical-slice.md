# Unity terrain work vertical slice

## Purpose

This slice closes the first observable digging loop in the Unity host. A resident receives a real digging job, follows a path calculated by Navigation, reaches an adjacent work cell, advances the authoritative job stages, excavates World terrain and creates a real Inventory stack at the changed cell.

Unity remains a presentation and composition host. None of the scene objects owns logical terrain, job lifecycle, item quantity or path topology.

## State ownership

- `JobSystem` owns job status, stage, assigned resident and reservation release.
- `WorldState` owns the target cell, material, designation, versions and dirty chunks.
- `InventoryState` owns the output item stack and quantity.
- `NavigationMap` owns derived walkability, regions and route versions for one traversal profile.
- `AgentState` owns the resident's confirmed logical cell.
- Unity owns only route lines, item meshes, interpolation and local selection.

Deleting and rebuilding every renderer does not change any of these authoritative owners.

## Completion use case

`CompleteTerrainWorkCommandHandler` is the Application boundary for the final stage. Before changing state it verifies:

- the job exists and is a digging job;
- the job is `InProgress` at `Finalize`;
- the target is still solid and designated;
- the replacement material exists and is non-solid;
- the output item exists;
- the output stack id is unused;
- the output quantity fits the item definition.

After the preflight succeeds, the deterministic single-threaded commit performs these steps:

1. `WorldState.Excavate` replaces the material and clears the designation;
2. `InventoryState.AddStack` creates the resource in the target world cell;
3. `JobSystem.Complete` releases the Job, Agent, Position and Designation reservations;
4. repositories are saved and domain events are published.

Rejected commands return before the first mutation. Tests assert that world, inventory and job versions remain unchanged when output validation fails. The current in-memory simulation executes this coordinator on one thread; a persistent or concurrent adapter will require a real transaction or unit-of-work boundary with the same preflight contract.

## Route planning and movement

`TerrainWorkRoutePlanner` does not pathfind into a solid target. It evaluates the target's four cardinal neighbours that are walkable in the current `NavigationSnapshot`, calculates a path to each candidate and selects the lowest total cost with stable cell ordering as the tie-breaker.

The Unity demo uses the free-mover traversal profile so the existing open cavern and resident spawn cells are all represented consistently. The selected path is converted to an immutable route view. Each simulation tick advances the assigned resident by at most one confirmed logical cell through `MoveAgentCommandHandler`. Frame interpolation occurs only after that command succeeds.

A job can leave `TravelToTarget` only when its assigned resident is at the selected work cell. Work and finalization remain simulation-tick operations; animation callbacks do not complete the job.

## Navigation refresh

Excavation invalidates the affected World chunks. The terrain work session drains those dirty chunk ids and calls `RefreshNavigationCommandHandler` with the new world snapshot. Only changed chunks are rebuilt by `NavigationMap`; later routes use the new navigation version.

Designation-only updates can also refresh a chunk version, but do not create new demo jobs. Dynamic designation-to-job creation is a later command workflow.

## Visuals and controls

- cyan lines show current successful Navigation routes;
- `F4` hides or restores the route root without changing the simulation;
- job markers and reservation links remain controlled by `F3`;
- grey-blue spheres show authoritative world item stacks created by excavation;
- completed job markers remain visible, while reservation links disappear after release;
- the excavated cell is rebuilt from the refreshed immutable world view.

## Current limitations

- only the two initial demo designations become jobs;
- resource output is a fixed demo rock item and quantity;
- dropped resources are not yet connected to the Unity storage/hauling scenario;
- residents without an assigned terrain job continue using the earlier deterministic demo movement;
- Unity Play Mode still needs local verification because CI validates the shared C# contracts and compatibility baseline without launching the editor.

## Validation

Engine-independent tests cover successful three-owner completion, rejected-command atomicity, reservation cleanup, cheapest adjacent work-cell selection and stable world-item presentation. The normal quality workflow also runs architecture checks, the C# 9 compatibility gate, Release build, all tests, headless smoke and both deterministic soak profiles.
