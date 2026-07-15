# BuildingBox packing save format v3

## Scope

Save format version 3 persists the reverse BuildingBox lifecycle introduced by the packing runtime slice. It extends version 2 without changing World, Inventory, assembly-plan or generic Job ownership.

## Serialized state

Each building record may contain one optional packing plan:

- owning packing Job id;
- predetermined output stack id;
- completed packing work;
- commitment state: `Active`, `Completed` or `Cancelled`.

The packing Job uses the stable codec id `job.building_box_packing.v1` and stores:

- building id;
- output stack id;
- work position;
- common Job priority, creation tick, retry policy and dependencies.

Assigned worker, current stage, retry state and all reservations continue to use the generic Jobs save data.

## Migration

The migration sequence is now:

```text
v0 -> v1 metadata
v1 -> v2 Buildings
v2 -> v3 packing plans
```

`save.v2_to_v3.packing` is additive. Existing v2 building records deserialize with no packing plan and retain their previous behavior. Applying the migration pipeline to an already current document remains a no-op.

## Owner restore

`BuildingsState.RestoreWithPacking` first restores normal building and assembly state through the existing owner restore. It then validates and installs packing plans inside the Buildings aggregate.

A restored packing plan must match its definition policy and building status:

- `Active`: building remains `Completed` and work is within the configured packing cost;
- `Cancelled`: building remains `Completed`;
- `Completed`: building is `Removed` and work equals the configured packing cost.

## Cross-system validation

After World, Inventory, Jobs and Buildings restore, the loader validates every packing reference.

### Active

- the referenced Job is a matching `BuildingBoxPackingJobDefinition`;
- the Job is non-terminal;
- the predetermined output stack does not exist.

### Cancelled

- the matching Job is `Cancelled`;
- the output stack does not exist.

### Completed

- the matching Job is `Completed`;
- exactly one stack with the definition's box item id exists under the predetermined id;
- the stack is at the former building origin;
- the output stack has no reservations.

A premature output item, dangling Job, mismatched ids, invalid commitment or missing building definition returns a controlled invalid-save result.

## Deterministic continuation

The acceptance test saves an assigned packing Job at `PerformWork` with partial progress. After load it verifies:

- the same worker remains assigned;
- the same Job stage and reservations are restored;
- packing work and stable output id are unchanged;
- no output item exists before Finalize.

The loaded state then receives the remaining work, advances to `Finalize` and creates one box. Its final building, Job and Inventory state matches the uninterrupted path.
