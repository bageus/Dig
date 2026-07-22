# Logical world implementation

## Status

This document describes the implementation for GitHub Issue #3. The authoritative development rules remain in `docs/development-rules.md`.

## State ownership

`WorldState` is the single owner of mutable logical world state:

- cell material references;
- dig designations;
- exploration flags;
- damage and temperature values;
- world version;
- per-chunk version stamps;
- the derived dirty-chunk queue.

Meshes, colliders, navigation graphs and UI are not sources of world state. They must consume immutable snapshots and can be discarded and rebuilt.

## Grid and chunks

The gameplay topology is a finite three-dimensional integer grid with exactly four depth layers.

- `CellId` identifies one logical cell by `X`, `Y` and bounded `Z=0..3`;
- `ChunkId` identifies one chunk-layer by `X`, `Y` and `Z`;
- `WorldSize` defines valid XYZ bounds and requires depth four;
- `ChunkLayout` maps cells to chunk-layers and handles partial chunks at world edges.

A changed cell invalidates its owning chunk-layer and only neighbouring chunk/layers whose mesh or navigation sampling can observe the changed boundary. Cells with the same X/Y and different Z remain distinct state, hash and reservation keys.

This conservative rule allows future mesh and navigation builders to sample border data without silently using stale neighbors.

## Materials and cell state

`MaterialCatalog` is immutable after construction. Each `MaterialDefinition` owns:

- stable `MaterialId`;
- `IsSolid`;
- hardness.

Cells do not duplicate solidity or hardness. `CellState` stores only mutable per-cell values:

- material reference;
- designation;
- exploration;
- damage;
- temperature.

`CellSnapshot` resolves solidity and hardness from the catalog at snapshot creation time. This keeps material properties as one source of truth.

The initial implementation supports solid and non-solid materials but does not yet simulate liquids, gases, support or temperature propagation.

## Atomic mutations

`ApplyTerrainChanges` validates an entire batch before changing any cell.

Validation includes:

- cell bounds;
- duplicate cells in one batch;
- material existence;
- designation compatibility with the target material.

If any entry is invalid, the world version, cells, dirty chunks and event buffer remain unchanged.

Successful changes are sorted by `CellId` before application. Therefore event order and mutation order do not depend on the input collection implementation.

A successful batch increments the world version once, even when many cells change.

Convenience operations currently include:

- setting or clearing a dig designation;
- excavation into a specified non-solid material.

## Versions and invalidation

Every successful mutation batch receives one new world version.

Every affected chunk receives that same value as its chunk version stamp. Consumers can reject stale asynchronous work by comparing the snapshot chunk version with the current world chunk version.

Dirty chunks are stored as a derived set:

- `PeekDirtyChunks` returns a sorted immutable copy;
- `DrainDirtyChunks` returns the same data and clears the derived queue;
- clearing dirty chunks does not change the world version.

The full set of chunk snapshots can always be regenerated from authoritative cells and material definitions.

## Snapshots

`ChunkSnapshot` contains:

- chunk identity and bounds;
- source world version;
- chunk version;
- an immutable copy of row-major cell snapshots.

`WorldSnapshot` contains all chunks in stable order. Existing snapshots never change when `WorldState` changes later.

Navigation and rendering systems must retain the snapshot version used for their work and verify it before applying results.

## Events

World mutations publish facts:

- `CellChanged` for every changed cell;
- `ChunkInvalidated` once for every affected chunk.

Events contain immutable values and identifiers. They do not contain mutable references to the world or cells.

## Application boundary

The Application layer exposes:

- `DesignateDiggingCommand`;
- `ExcavateCellCommand`;
- `GetCellQuery`;
- `GetChunkSnapshotQuery`.

Command handlers delegate validation and mutation to `WorldState`, persist the same authoritative world instance and publish its domain events. Query handlers return snapshots without changing the world version.

## Validation

Automated tests cover:

- interior and boundary invalidation;
- four-chunk corner invalidation;
- atomic rejection of invalid batches;
- duplicate-cell rejection;
- one version increment for bulk changes;
- deterministic event ordering;
- immutable snapshots;
- dirty-queue draining;
- complete snapshot reconstruction for partial edge chunks;
- invalid dig designation on non-solid cells;
- command publication and side-effect-free queries.

The headless host creates a world, designates one cell, excavates it and verifies that its snapshot becomes non-solid.


## Authoritative XYZ integration

World, Agents, Inventory, Buildings, Jobs, reservations and Navigation use the same `CellId(X,Y,Z)`. Navigation and Unity meshes are derived from immutable World snapshots and are not state owners. Save format v5 stores exact Z for world cells, residents, world items, buildings, jobs and terrain deposits. The v4-to-v5 migration deterministically maps every legacy `X,Y` coordinate to `Z=0`.
