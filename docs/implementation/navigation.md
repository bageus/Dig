# Navigation implementation

## Status

This document describes the implementation for GitHub Issue #4. It is subordinate to `docs/development-rules.md` and the architecture documents.

## State ownership

`WorldState` remains the only source of truth for terrain cells and chunk versions.

`NavigationMap` owns only derived data for one `TraversalProfile`:

- per-chunk walkability caches;
- navigation chunk versions;
- the current region graph;
- normalized traversal links;
- immutable navigation snapshots;
- diagnostics from the last refresh.

Rendering meshes, physics colliders and engine scenes are not navigation inputs.

## Traversal profiles

A navigation map is built for exactly one immutable profile. The initial profiles are:

- grounded dwarf: requires an empty cell with solid support, permits configured steps and can use ladders and elevators;
- free mover: traverses empty cells in four cardinal directions without support.

Future profiles may add body size, hazard tolerance and movement costs without changing world ownership.

## Chunk extraction

A full rebuild extracts walkability from every world chunk.

A refresh rebuilds only:

- explicitly invalidated chunks;
- chunks whose source version differs from the cached version;
- chunks containing old or new endpoints of changed traversal links.

`NavigationUpdateDiagnostics` reports the exact rebuilt chunk IDs and extracted cell count. This allows tests and profiling tools to detect accidental full-map terrain extraction.

After chunk extraction, the region graph is recalculated from cached navigation cells. It does not rescan terrain state.

## Regions

Regions are deterministic weakly connected components of the current topology. Weak connectivity is intentional: a one-way elevator keeps both endpoints in the same coarse region, while the detailed path search still enforces its direction.

Region IDs are assigned from cells sorted by `CellId`. They are diagnostics and snapshot-local identifiers, not persistent entity IDs.

A region mismatch rejects an unreachable request before node expansion.

## Traversal links

`TraversalLink` represents a derived special transition supplied by another authoritative system, such as future construction or transport state.

Each link has:

- a stable string ID;
- from and to cells;
- ladder or elevator kind;
- traversal cost;
- directionality;
- source version.

Navigation validates unique IDs and in-bounds endpoints. Link changes increment `LinkVersion` and rebuild endpoint chunks because allowed endpoints may become standable for grounded profiles.

Navigation does not own the future building or elevator state that produced a link.

## Path requests

`NavigationPathfinder` performs deterministic cost-ordered graph search. It uses a binary heap with stable tie-breaking by total cost, heuristic and `CellId`.

The current implementation uses a zero heuristic, making it Dijkstra search, which is an A*-equivalent and remains correct with cheap long-distance elevator links.

A request contains:

- start and goal cells;
- the expected navigation snapshot version;
- a maximum expanded-node budget.

Failures return an explicit `PathFailureReason` and diagnostics instead of retrying indefinitely.

## Stale result handling

A request targeting an old snapshot is rejected before search.

A successful `NavigationPath` stores:

- traversal profile ID;
- world and navigation versions used for diagnostics;
- traversal-link version;
- version stamps for every chunk touched by the route.

`NavigationPath.IsValidFor` accepts a path after an unrelated chunk update, but rejects it when a touched chunk or any traversal link has changed.

This is the acceptance check for future background pathfinding jobs.

## Application boundary

Application commands build and refresh navigation maps. Queries read snapshots and find paths.

`INavigationRepository` stores one `NavigationMap` per `TraversalProfileId`. The in-memory implementation is only an adapter and does not duplicate map state.

## Headless validation

`Dig.Headless` now:

1. runs the deterministic simulation;
2. changes logical world cells;
3. creates a grounded corridor;
4. builds navigation without Presentation or an engine;
5. finds a route across the corridor.

## Current limitations

This stage intentionally does not include:

- asynchronous worker scheduling;
- path request queues for hundreds of agents;
- reservations or crowd avoidance;
- dynamic agent occupancy;
- hierarchical portal search between chunks;
- fluids, falling or jump arcs;
- construction-owned ladder and elevator registries.

The snapshot and version contracts are designed so these systems can be added without making navigation a second source of terrain truth.
