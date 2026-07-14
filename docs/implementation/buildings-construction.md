# Buildings and construction

## State ownership

`BuildingsState` is the only authoritative owner of building projects, construction progress, completed-building durability and lifecycle status. It does not own terrain, item stacks, worker claims or paths.

The surrounding systems keep their existing ownership:

- World owns terrain cells and exploration state;
- Inventory owns delivered material stacks and their locations;
- Jobs owns delivery and construction work status plus worker and position reservations;
- Navigation supplies reachable work positions;
- Presentation receives immutable snapshots and never becomes a second source of truth.

## Building definitions

`BuildingDefinition` is immutable and identified by a stable `BuildingDefinitionId`. It defines:

- footprint offsets;
- candidate work-position offsets;
- material requirements;
- required construction work;
- maximum durability.

Offsets rotate deterministically around the placement origin for north, east, south and west orientations. Material requirements with the same item id are normalized into one quantity.

## Placement validation

`BuildingPlacementValidator` evaluates a `WorldSnapshot`, the occupied cells of active building projects and a caller-supplied set of reachable cells. Placement fails with a stable diagnostic error when:

- any footprint cell is outside the world;
- terrain is solid;
- terrain is unexplored;
- the footprint overlaps another active project or building;
- no configured empty and explored work position is reachable.

A successful result contains the resolved footprint and one deterministic work position. Only this validated result can create a project.

## Project lifecycle

A project follows explicit status transitions:

```text
AwaitingMaterials -> ReadyToBuild -> UnderConstruction
    -> ReadyToComplete -> Completed -> Damaged -> Completed

AwaitingMaterials / ReadyToBuild / UnderConstruction / ReadyToComplete
    -> Cancelled

Completed / Damaged -> Removed
```

Construction progress is capped at the definition requirement. Reaching the required work changes the project to `ReadyToComplete`; it does not create the final building by itself. Final completion is a separate validated operation and emits `BuildingCompleted` once.

## Material delivery

Building material delivery reuses typed hauling jobs. A delivery job targets `ItemLocation.InBuilding(buildingId)` and reserves an exact quantity from its source stack.

Creation checks that:

- the project is awaiting materials;
- the item is required by the definition;
- delivered plus active incoming quantity does not exceed the requirement;
- the source stack has enough unreserved quantity.

A completed delivery moves only the quantity reserved by that job. `RefreshBuildingMaterialsHandler` derives readiness from Inventory rather than storing a duplicate delivered-material counter in Buildings.

## Construction work

`BuildingWorkJobDefinition` is a typed Jobs definition for construction, repair or demolition work. Construction reserves the selected work position through the common Jobs reservation ledger.

Before creating construction work, the application checks that the current Navigation-derived reachable set still contains the project's work position. If not, the project retains a human-readable diagnostic reason and no job is created.

Construction work advances only while its matching job is in `PerformWork`. When the work requirement is reached, the job advances to `Finalize` and the project becomes `ReadyToComplete`.

## Transactional completion

Final construction performs these steps in order:

1. validate the matching project and job states;
2. prevalidate every required material at the construction-site Inventory location;
3. atomically consume the complete material batch;
4. complete the authoritative building state;
5. advance the work job through its final stage.

Inventory prevalidates the full batch before changing any stack. A missing material therefore cannot cause partial consumption.

## Cancellation, damage and removal

Cancelling an unfinished project:

- cancels every active delivery or building-work job associated with it;
- releases item and job reservations;
- returns already delivered unreserved stacks from the site to the world at the project origin;
- marks the project cancelled and releases its footprint.

Completed buildings support damage and repair through durability. A completed or damaged building can be removed, emitting `BuildingRemoved` and releasing its occupied cells. Scene objects are disposable visual representations of these snapshots.

## Diagnostics and validation

`BuildingSnapshot` exposes definition, origin, orientation, footprint, selected work position, status, work progress, durability and the latest diagnostic reason. `BuildingPresenter` maps it to a read-only inspector model for Unity.

Tests cover placement failures, overlap, reachability, delivery limits, exact material consumption, completion exactly once, cancellation and material return, reservation release, damage, repair and removal. The headless smoke scenario now runs digging, resource creation, stockpile hauling, building delivery and final construction in one deterministic chain.
