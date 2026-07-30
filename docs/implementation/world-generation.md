# Deterministic world generation

Issue #10 introduces an engine-independent first generation pipeline in `Dig.Domain.Generation`.

## Contract

A generation request contains:

- `WorldSeed`;
- a stable profile id and `GeneratorVersion`;
- world and chunk dimensions;
- material ids for empty terrain, biome rock and biome resources;
- data parameters for zones, rooms, corridors, starting resources, points of interest and layers.

Only `WorldGenerator.CurrentGeneratorVersion` is accepted. Existing saves must retain their recorded generator version and generated-world metadata; a future algorithm change requires a new version and explicit migration policy.

## Stages and random streams

Generation uses independent named streams derived from the world seed:

1. start location;
2. zone locations;
3. biome assignment;
4. zone connections;
5. resource placement;
6. points of interest;
7. optional terrain deposits with separate named streams for origin, definition, cluster size, stable instance identity and neighbour order.

Changing a later stage does not consume random values from an earlier stage. Deposit host cells and definitions are sorted before rolls, so input enumeration order is irrelevant. The logical fingerprint is calculated from the seed, version, profile id, ordered cell state and the complete ordered deposit snapshot, without Unity APIs or runtime hash codes.

## Generated world

The current version creates a solid biome field, carves a safe explored start room, carves additional zone rooms, and connects every zone to the nearest earlier zone. This forms a deterministic connected macro graph. Resource cells are placed around the start room before additional zone resources, and POIs are selected only from reachable empty cells outside the start area.

The initial `WorldState` is created directly at version zero. Optional deposit generation validates mineable solid host materials, creates hidden per-cell instances across `Z=0..3`, and attaches them without dirty chunks or domain events because generation is creation, not a sequence of player mutations.

## Validation

`WorldGenerationValidator` checks:

- dimensions, profile id and version;
- exact zone and POI counts;
- empty in-bounds start cell;
- reachability of all zone centers and POIs;
- minimum starting resources with valid solid resource materials;
- pristine initial world version, dirty set and event queue.

The test suite includes a 128-seed sweep, reproducibility, version rejection, stream isolation, deposit host/version/input-order determinism and overlay replay.

## Changes over the base world

`WorldGenerationOverlay` captures only cells that differ from a regenerated base world. Applying the ordered overrides through the normal atomic terrain mutation path reconstructs player changes without rerunning or rewriting the base generation result.

The core generation contract now includes optional versioned deposits. Exact frequency/yield/effort values remain data-driven `BALANCE_TBD` under Q-014; terrain output, hauling and processing continue in #92/#108–#110.
