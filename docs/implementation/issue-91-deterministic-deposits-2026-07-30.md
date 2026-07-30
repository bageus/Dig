# Issue #91 — deterministic deposits and depletion

Status: `IMPLEMENTED` after merge of the linked PR. Licensed Unity EditMode/PlayMode execution remains required for `VERIFIED`.

Authoritative specifications:

- [`../design/excavation-room-templates-and-deposits.md`](../design/excavation-room-templates-and-deposits.md);
- [`../design/terrain-resource-output-and-processing.md`](../design/terrain-resource-output-and-processing.md);
- [`../design/world-3d-depth.md`](../design/world-3d-depth.md).

Tracking: [#87](https://github.com/bageus/Dig/issues/87), [#91](https://github.com/bageus/Dig/issues/91), [#92](https://github.com/bageus/Dig/issues/92), [#94](https://github.com/bageus/Dig/issues/94).

## Authoritative owners

- `WorldState.TerrainDeposits` owns every deposit cell and generator version.
- `TerrainDepositCatalog` owns immutable definitions, stable ids/versions, output, host constraints, skill profile and effort multiplier.
- `TerrainDepositGenerator` owns seed/version-based layout and stable instance ids.
- `WorldState.Excavate` owns terrain opening, stale-plan validation, depletion, six-axis reveal, events and chunk/layer invalidation.
- `MiningOutputResolver` and `MiningOutputCommitState` retain mutually exclusive output planning and exactly-once ledger ownership.
- Unity consumes immutable snapshots and never mutates an independent deposit collection.

## Generation

`WorldGenerationRequest` optionally accepts a deposit catalog and settings as one atomic configuration. Generation:

- enumerates all mineable solid XYZ host cells;
- validates data-driven allowed host `MaterialId` values;
- sorts hosts and definitions before any roll;
- uses named streams for origin, definition, cluster size, stable instance id and neighbour order;
- creates independent hidden cells in clusters of at most four;
- stores the deposit generator version in World;
- includes the complete ordered deposit snapshot in the generated-world fingerprint.

The representative Unity demo uses the same generator and world-owned state. Its existing yields/weights remain content fixtures. The neutral effort multiplier is not an approved balance value; Q-014 still owns exact density, yield and effort tuning.

## Runtime lifecycle

Before terrain mutation, a deposit mining command carries the resolved instance id and expected yield. A mismatch returns `world.terrain_deposit.stale` without changing terrain, deposits or output.

A successful excavation:

1. opens the exact XYZ cell;
2. depletes only the deposit occupying that cell;
3. leaves neighbouring yields and identities unchanged;
4. reveals only hidden deposits in the six orthogonal XYZ neighbours;
5. publishes typed reveal/depletion events;
6. invalidates affected chunk/layer snapshots;
7. lets the existing Navigation rebuild see the cell as open.

## Save/load and migration

Save document version 11 stores:

- deposit snapshot format version;
- deposit generator version;
- instance id;
- definition id and definition version;
- exact `X,Y,Z`;
- hidden/revealed state;
- remaining yield/depletion;
- per-cell version.

Migration `save.v10_to_v11.terrain_deposit_contract` preserves legacy coordinates, state and yield, supplies definition version 1, and records the existing generator version. Load rejects unknown definitions, unavailable definition versions, duplicate cells/ids, invalid hosts and depleted deposits in solid cells.

## Diagnostics and regression coverage

Coverage includes:

- seed/version and input-order determinism across four depth layers;
- data-driven host constraints and independent neighbouring cells;
- stale identity/yield preflight;
- World-owned depletion and six-axis reveal events;
- Navigation after deep depletion;
- generated-world fingerprint participation;
- exact-Z save/load and v10→v11 migration;
- generator/definition version mismatch handling;
- integrity diagnostics for host, depletion and output-ledger consistency;
- Unity source contracts and checked-in Play Mode reveal/depletion projection.

## CI regression correction

The first complete Quality run exposed five test-contract defects rather than a compile or architecture failure. The corrected head now:

- returns the expected stale-deposit `Result` before any inventory or job mutation instead of converting that controlled domain rejection into an exception;
- includes `save.v10_to_v11.terrain_deposit_contract` in legacy migration-order expectations;
- preserves the explicit deposit generator version in the domain snapshot round trip;
- passes the loaded terrain-deposit generator version when rebuilding a save context, avoiding metadata/world-version drift;
- keeps the expanded migration regression fixture within the repository 350-line source limit without reducing coverage.

## Verification boundary

Normal Quality CI must pass Release compilation, the complete .NET suite, source contracts, smoke and both deterministic soaks. The checked-in Unity lifecycle scenario must execute on a licensed Unity runner before status can move from `IMPLEMENTED` to `VERIFIED`.
