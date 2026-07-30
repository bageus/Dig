# Issue #88 — authoritative XYZ closure

Status: `IMPLEMENTED` after merge of the linked PR; licensed Unity Play Mode evidence remains required for `VERIFIED`.

Authoritative specification: [`../design/world-3d-depth.md`](../design/world-3d-depth.md).  
Tracking: [#88](https://github.com/bageus/Dig/issues/88), parent [#87](https://github.com/bageus/Dig/issues/87).

## Audit baseline

The project already used `CellId(X,Y,Z)` across most Domain/Application state, but repository-wide closure found several parallel 2D assumptions:

- generated cell buffers allocated only `Width * Height`, while `WorldState` indexed all four depth layers;
- generation fingerprint and overlay visited only Z0;
- generation validation created XY neighbours at hard-coded Z0;
- BuildingBox packing accepted a worker at the same X/Y on the wrong depth;
- traffic swap prevention classified same-height depth traversal as a horizontal X swap;
- two runtime route producers used the legacy `RouteViewModel` overload and dropped work Z;
- route, overlay, selection, stockpile and chunk-version Presentation paths projected or keyed only X/Y;
- v4→v5 migration did not explicitly normalize agents, deposits, job coordinate properties or Position/Designation reservation strings.

These were observable correctness gaps, not a request for a second coordinate type.

## Domain and generation changes

- `GenerationCellBuffer` now requires `WorldSize.CellCount` and indexes cells in stable XYZ order.
- generated solid geology materializes every Z layer; authored initial rooms/corridors remain Z0 unless generation content explicitly places them deeper.
- `WorldState.CreateGenerated` validates the full 3D cell count.
- generation fingerprint includes depth and every mutable/provenance cell field across all layers.
- generation overlays compare/apply every XYZ cell and reject worlds with different depth.
- generation reachability preserves current Z for XY neighbours and examines valid ±Z neighbours.

## Application/runtime changes

- packing work-position comparison uses exact `CellId` equality.
- traffic anti-swap applies only when Y and Z are equal; opposite depth traversals are not mistaken for X swaps.
- building packing and production route view models pass target Z explicitly.
- navigation route renderer projects each route cell through `DigTunnelProjection.RouteWorldPosition(CellId)`.
- overlay chunk cache uses `(X,Y,Z)` and all overlay/selection placement passes exact Z.
- stockpile visuals use the shared depth projection instead of treating logical Y as Unity Z.

## Save migration

`SaveVersionFourCoordinateMigrationNormalizer` is the single v4→v5 normalization boundary. It maps every legacy owner to explicit `Z=0`:

- world chunks/cells;
- world item locations;
- building origin/work positions;
- agent positions;
- deposits;
- every job property group with matching `*.x` and `*.y` but no `*.z`;
- Position and Designation reservation values.

Job properties are returned in stable ordinal order; reservation strings use invariant formatting. Non-coordinate reservation kinds remain unchanged.

## Regression coverage

- generated snapshots contain exactly `Width * Height * 4` unique cells;
- deep cells exist and remain solid until mutated;
- a deep designation changes deterministic fingerprint and round-trips through generation overlay;
- packing rejects same X/Y on the wrong Z;
- opposite depth traversal is not blocked by the horizontal swap rule;
- v4 migration normalizes world, item, building, agent, deposit, job and reservation coordinates;
- source contract protects runtime route/overlay/stockpile/producer use of Z;
- checked-in Unity Play Mode test renders same-XY route cells on Z0 and Z3 at distinct world depths.

## Validation boundary

Executed locally:

- `python3 tools/quality/check_quality.py`;
- `python3 tools/quality/check_unity_source_contracts.py`;
- `python3 tools/quality/check_unity_excavation_playmode_contracts.py`.

The available environment has no .NET SDK or Unity Editor. Release build, .NET tests, smoke, deterministic soaks, Stage 2 exports and Unity compilation/execution must be taken from the exact PR head CI. A green Unity workflow with skipped activation-gated test steps is not runtime evidence.
