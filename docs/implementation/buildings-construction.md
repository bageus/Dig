# Buildings and construction

## 1. State ownership

`BuildingsState` is the authoritative owner of building projects, construction progress, completed-building durability and lifecycle status. It does not own terrain, item stacks, workers or paths.

- World owns terrain and exploration;
- Inventory owns material and BuildingBox stacks, locations and reservations;
- Jobs owns delivery, construction, packing, workers and position reservations;
- Navigation supplies reachable positions;
- Production creates BuildingBox outputs;
- Presentation receives immutable snapshots and owns only preview/selection.

## 2. Current implemented model

The currently implemented flow is material-site construction.

`BuildingDefinition` defines footprint, work positions, material requirements, work cost and durability.

Placement validates:

- world bounds;
- empty explored terrain;
- overlap;
- reachable work position.

The current lifecycle is:

```text
AwaitingMaterials -> ReadyToBuild -> UnderConstruction
-> ReadyToComplete -> Completed -> Damaged -> Completed
```

Typed hauling delivers required quantities to `ItemLocation.InBuilding(buildingId)`. Final completion prevalidates and atomically consumes the full material batch, completes the building and finalizes the job.

Cancellation closes jobs, releases reservations, returns delivered unreserved stacks to the world and releases the footprint.

This remains the factual code contract until #118 is implemented.

## 3. Target universal BuildingBox model

The accepted target design is documented in `docs/design/building-box-placement-and-packing.md`.

Every normally placeable building is produced as one physical `BuildingBox` item. The target flow is:

```text
Box in Inventory/World
-> local placement preview
-> validated ghost plan + box reservation
-> pickup/delivery job
-> box committed at site
-> assembly work
-> completed building
```

Packing reverses the completed building into one box after a confirmed demolition/packing job.

## 4. Migration rule

`BuildingDefinition` receives one explicit construction policy:

```text
ConstructionPolicy
- UniversalBox
- LegacyMaterialSite
```

At runtime one definition may use only one policy.

- `UniversalBox` consumes one produced box and never asks the site for the same recipe materials again.
- `LegacyMaterialSite` retains the implemented material delivery flow until migrated.
- content validation rejects ambiguous definitions that specify both box and full site requirements.

Existing saves/projects keep their original policy/version. Migration must not transform an in-progress material site into a box plan without a deterministic conversion report.

## 5. Target placement validation

Preview is Presentation-only. Confirmation repeats authoritative validation:

- footprint in bounds;
- terrain valid and explored;
- no overlap;
- reachable work position;
- selected box exists;
- box is accessible and not reserved by another plan;
- box matches BuildingDefinition/version.

Only after all checks succeed does one Application transaction reserve the box and create the plan.

## 6. Target box lifecycle

Before site commit, the box remains at its original Inventory location with a plan reservation.

Pickup/delivery:

1. Jobs claims a worker and box;
2. Inventory moves the box to the worker;
3. movement reaches the site;
4. Inventory moves the box to a typed site location;
5. the plan records box commit;
6. assembly advances.

Final assembly:

1. validate plan/job/site box state;
2. consume exactly one committed box;
3. complete authoritative building state;
4. finalize the job;
5. publish events after commit.

No step creates a second copy of the box.

## 7. Target packing lifecycle

Selecting a building opens its functional panel. The right-side pack button sends an Application command.

The command validates:

- building exists and supports packing;
- no non-cancellable operation blocks packing;
- functional places/orders can be closed by policy;
- a reachable packing work position exists;
- output box definition is valid.

A packing job then performs work. At final commit:

1. close/reconcile building functions and reservations;
2. remove the building and release footprint;
3. create exactly one BuildingBox at the site;
4. finalize the job and publish events.

Until final commit the building remains authoritative and no box exists.

## 8. Cancellation and failures

### UniversalBox plan before delivery

- cancel jobs;
- release box reservation;
- leave box in original location;
- release footprint.

### After box delivery but before completion

- cancel jobs and claims;
- convert the committed site payload back into exactly one world/site box;
- release footprint;
- preserve quantity.

Retry cannot reserve the box twice. Missing/stale box creates a typed blocked/failure reason.

## 9. Context UI compatibility

The lower panel is mutually exclusive:

- no selection: excavation palette;
- resident selected: resident inventory;
- building selected: functions and pack button;
- placement active: BuildingPlacement panel.

UI clicks are shielded from world routing. Preview cannot mutate Buildings/Inventory.

## 10. Save/Load

Current saves retain material-site fields. Target saves add:

- construction policy/version;
- BuildingBox ItemId/location;
- box reservation and plan owner;
- box site commit state;
- delivery/assembly/packing stages;
- packing preconditions and reconciliation report.

Presentation preview and selected panel are not authoritative save state.

## 11. Diagnostics

Snapshots/read models expose:

- definition and policy;
- origin/orientation/footprint/work positions;
- status/progress/durability;
- required materials for legacy policy;
- box item/location/reservation/commit for universal policy;
- active jobs and workers;
- last reason/cancellation;
- quantity conservation report.

## 12. Validation

Existing tests continue to cover material delivery, overlap, reachability, atomic material consumption, cancellation, damage and repair.

#118 adds tests for:

- placement from world and resident inventory boxes;
- one box competing between plans;
- invalid preview/confirmation;
- delivery and assembly;
- cancellation before/after site commit;
- packing and repeated placement;
- save/load on every stage;
- deterministic replay and box quantity conservation;
- Unity context panel, preview and input shielding.
