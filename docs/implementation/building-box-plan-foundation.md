# BuildingBox plan foundation

## Construction policy

`BuildingDefinition` now selects one mutually exclusive construction policy:

- `LegacyMaterials` keeps the existing material-delivery pipeline;
- `BuildingBox` uses one non-stackable box item, assembly work and future packing work.

A definition cannot contain both legacy material requirements and a `BuildingBoxPolicy`. Existing legacy definitions remain source-compatible because the box policy is optional.

`BuildingBoxPolicy` stores the stable box `ItemId`, required packing work and whether packing is enabled. `BuildingDefinition.RequiredWork` remains the assembly-work requirement.

## Authoritative plan state

A box-enabled plan starts in `BuildingStatus.AwaitingBox` and exposes:

- source stack id;
- owning assembly Job id;
- commit state: `Reserved`, `AtSite` or `Consumed`.

The legacy `Place` and `Complete` methods reject box definitions. Box plans use dedicated transitions:

1. `PlaceBoxPlan` creates the footprint and `Reserved` commitment;
2. `MarkBoxAtSite` changes the commitment to `AtSite` and the project to `ReadyToBuild`;
3. normal construction work changes `ReadyToBuild → UnderConstruction → ReadyToComplete`;
4. `CompleteBoxConstruction` changes `AtSite → Consumed` and completes the building.

Typed `BuildingBoxPlanCreated` and `BuildingBoxCommitChanged` facts carry stable ids. Repeating a site commit is rejected instead of creating another transition.

## Shared Job and reservation model

`BuildingBoxAssemblyJobDefinition` uses the common Job lifecycle and reserves:

- the exact source stack;
- the building destination id;
- the selected work position;
- the normal Job and worker keys added by `JobSystem`.

Stages are:

```text
AcquireItem → TravelToDestination → DepositItem → PerformWork → Finalize
```

The Inventory quantity reservation is owned by the same real Job id. This keeps save/load cross-reference validation compatible and prevents a second plan from reserving the same box.

## Application transaction

`ConfirmBuildingBoxPlacementHandler` repeats authoritative placement validation at click time, then verifies:

- the definition uses BuildingBox policy;
- the source stack exists and has the required box item id;
- the item definition is non-stackable and the stack quantity is exactly one;
- building and Job ids are unused;
- footprint and reachable work position remain valid.

After validation it reserves one quantity, creates the plan, creates the assembly Job and makes the Job available. Invalid placement creates no reservation, plan or Job.

`CommitBuildingBoxToSiteHandler` only runs while the Job is at `DepositItem`. It moves the exact reserved stack into the building site location and commits the plan to `AtSite`.

`AddBuildingBoxAssemblyWorkHandler` accepts work only at `PerformWork` after site commitment. `CompleteBuildingBoxAssemblyHandler` requires `Finalize`, consumes the exact source stack once, completes the building and completes the Job.

## Cancellation and quantity conservation

Before delivery, cancellation releases the Inventory quantity reservation and cancels the Job and project.

After site commitment, cancellation moves the same source stack from the building site back to the plan origin, then cancels the Job and project.

A consumed/completed plan cannot be cancelled. Both paths preserve exactly one box until successful completion consumes it.

## Validation

Tests cover:

- confirmation from world and resident inventory;
- invalid footprint with no partial state;
- competing plans for one box;
- full Job delivery and assembly lifecycle;
- exact source-stack consumption;
- cancellation before delivery;
- cancellation after site commitment;
- legacy/box policy exclusivity;
- idempotent site commitment.

## Remaining #118 slices

This foundation does not close #118. Follow-up work still adds:

- typed packing Jobs and recipes;
- designation/storage filters and tools;
- construction and packing skill awards;
- save codecs and Building snapshot restoration;
- Unity BuildingBox visuals, ghost preview and router command adapter;
- progression-based UI visibility and inventory workflows.