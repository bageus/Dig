# Unity visual asset pipeline

## Purpose

Unity presentation resolves low-poly assets from stable gameplay identifiers without moving asset references into Domain or Application. Scene objects remain rebuildable projections of immutable read models.

## Runtime flow

```text
Presentation read model
    -> stable material/building/item/resident/creature id
    -> typed DigVisualCatalog
    -> DigVisualAsset
    -> DigVisualPrefabFactory
    -> rebuildable Unity visual
```

`DigVisualCatalog` is a Unity `ScriptableObject`. Each serialized entry contains:

- a stable gameplay id;
- an optional prefab;
- an optional material;
- a base tint.

A prefab entry must contain `DigVisualPrefabRoot`. Missing, duplicate or invalid entries are reported by catalog validation. Runtime lookup keeps the first serialized duplicate deterministically and returns the configured fallback for an invalid or unknown id.

## Current terrain integration

`DigTerrainVisualCatalog` is loaded from `Resources/Dig/VisualCatalogs/Terrain` when it is not assigned explicitly.

The terrain presentation has four responsibilities:

1. `WorldViewModel` supplies authoritative material and state for the front `Z=0` layer.
2. `TerrainDepthVolumePresenter` projects the current deep solid-rock mask from `TunnelNavigationVolume`, explicit material facts and explicit excavation cells.
3. `DigTerrainRenderSnapshotBuilder` merges both immutable projections and derives dirty chunks.
4. `DigTerrainChunkRenderer` creates collider-free meshes for the resulting `Z=0..3` chunks.

Existing per-cell primitives stay hidden and are retained only as bounded tunnel-dig interaction proxies. The renderer does not walk those objects in `LateUpdate`. World, cutaway, protected-cell, depth-volume, deposit-volume or catalog changes explicitly refresh the terrain projection.

The former `DigRockVolumeRenderer` has been removed. Front terrain and deep rock now use one material resolver, mesh builder, signature calculation, diagnostics counter and dirty-invalidation path.

### Terrain visual profile contract

`DigTerrainVisualCatalog` resolves a stable gameplay material id to a typed `DigTerrainVisualProfile`. Every profile declares one of six required visual families:

- Sand;
- Stone;
- MetalBearing;
- Crystalline;
- Lava;
- Unmineable.

A profile contains separate materials for `Floor`, `Wall`, `Ceiling` and `FreshCut`. Catalog validation reports duplicate stable ids, missing surface materials and any missing required family. The base catalog material remains a backward-compatible fallback for an unknown or incomplete profile.

The chunk builder assigns a surface role to every exposed face before submesh grouping:

- an exposed logical top face is `Floor`;
- an exposed logical bottom face is `Ceiling`;
- horizontal and depth-facing sides are `Wall`;
- a face bordering an explicit excavation cutaway becomes `FreshCut`.

Natural empty volume is not automatically classified as fresh excavation. This keeps natural cave surfaces and player-cut surfaces separable once provenance and template-cave data are connected.

One cell may therefore contribute triangles to several material submeshes, but it still creates no per-cell GameObject. Missing catalog assets use cached role-aware debug materials so fallback rendering also stays deterministic and allocation bounded.

### Deposit presentation contract

`TerrainDepositPresenter` accepts explicit per-cell facts intended to come from the authoritative deposit model in #91:

- `SpatialCellId`;
- stable deposit id;
- revealed flag;
- remaining and maximum yield;
- source version.

It projects four visual states: `Hidden`, `Revealed`, `Damaged` and `Depleted`. Hidden and depleted entries expose an empty visual id, zero damage band and no connection mask. Their deposit type therefore cannot be selected through a terrain material or deterministic fallback color.

Visible cells receive a six-direction connection mask only for adjacent visible cells with the same stable deposit id. The cells remain independent read models; the mask never creates a shared vein aggregate or changes depletion ownership.

`TerrainDepositDecorationPresenter` consumes that hidden-safe snapshot and publishes only visible decorations. The same cell and deposit id always produce the same four-way variant, quarter-turn rotation and bounded two-axis offset. Damage changes the scale band without changing the stable variant or rotation. Hidden and depleted cells produce no decoration entry.

Each exposed host-rock face keeps its normal terrain material. The chunk mesh adds a separate low-poly deposit overlay consisting of one shape cluster and a bounded number of connector strips selected from the same-id connection mask. Internal faces between solid cells receive no overlay. There is no deposit prefab, collider or per-cell GameObject.

`DigTerrainRenderSnapshotBuilder` copies only decoration entries whose host terrain cell is still solid. Reveal, damage, connection, layout or removal changes invalidate the local terrain chunk. The chunk signature includes variant, rotation, scale and offset bands, so a changed layout cannot reuse stale geometry.

`DigTerrainVisualCatalog` requires five deposit profile families. Each family has `Revealed` and `Damaged` materials and a non-colour silhouette:

- Iron uses a raised `Nodule`;
- Gold uses a flat wide `Plate`;
- Crystal uses a pointed triangular `Crystal`;
- Coal uses a long narrow `Seam`;
- Stone uses a grouped `Pebble` silhouette.

There are deliberately no hidden or depleted materials. Unknown visible deposit ids receive a deterministic fallback silhouette as well as a fallback material, so distinguishability never depends on colour alone. Damage reduces height/scale in addition to changing the material.

The current runtime does not synthesize demo deposits from terrain material ids. Until #91 publishes authoritative deposit instances, the renderer receives no deposit volume and displays only host terrain.

### Template cave provenance and trim contract

`CaveTemplateTrimPresenter` receives only completed `CaveRoomPlan` instances. It does not receive cutaway cells, tunnel strokes or arbitrary empty terrain. Free excavation therefore cannot acquire template arches by accident.

Each completed plan resolves to one of four stable ids:

- `cave.template.small`;
- `cave.template.medium`;
- `cave.template.large`;
- `cave.template.tall`.

The immutable trim instance contains the exact trapezoid rows produced by `CaveRoomPlanner.InterpolateWidth`, explicit internal arch depth slices, a deterministic four-way variant and a back-wall flag. Reordering the same completed plans produces the same instance order, version and variants.

`DigTerrainVisualCatalog` requires a profile for all four template kinds. Every profile has separate `Entrance`, `Arch`, `SideWall` and `BackWall` materials. Missing assets use cached role-aware fallback materials without changing room topology or provenance.

`DigCaveTemplateTrimRenderer` creates one collider-free dynamic mesh object per completed room. At full detail the mesh contains:

- one entrance outline;
- `Depth - 1` internal arch outlines;
- two side-wall quads per trapezoid row;
- one back-wall quad per row when the template enables a back wall.

The deterministic variant changes trim thickness only. It never changes room dimensions, navigation, protected cells or excavation ownership. The renderer creates no per-cell trim object, no primitive collider and no picking target.

Template trim uses the same world scale as terrain: one Unity unit per logical X/Y cell. Its root transform is identity under `DigWorldRenderer`; X comes from cell centers, vertical position is `-CellId.Y`, and Z uses `DigTunnelProjection.DepthOrigin/DepthSpacing`. The room entrance is the provenance pivot; all mesh vertices are emitted in world-root coordinates.

### Accessibility and measured visual detail

`TerrainVisualDetailPolicy` selects one of three presentation-only levels from the measured screen size of one logical cell. The camera probe projects the world-root origin, logical X axis and logical Y axis once per frame; it does not scan cells, chunks or colliders.

The hysteresis thresholds are:

- `Marker -> Reduced` at 12 pixels per cell;
- `Reduced -> Marker` below 8 pixels per cell;
- `Reduced -> Full` at 28 pixels per cell;
- `Full -> Reduced` below 22 pixels per cell.

The levels preserve semantic shape while reducing geometry:

- `Full`: full deposit silhouette, up to two connector strips per exposed face, every template-room internal arch, side walls and back wall;
- `Reduced`: the same deposit family silhouette, at most one connector, sparse internal arches, side walls and back wall;
- `Marker`: a compact deposit family silhouette without connectors and only the template entrance outline.

The immutable terrain/deposit/template snapshots and their versions never include camera distance or detail level. Crossing a threshold invalidates only cached Unity meshes. Chunk and trim signatures include the selected detail level so stale geometry cannot be reused. `TerrainPixelsPerCell`, current detail level, vertex count, triangle count and rebuild count remain available as runtime diagnostics.

### Snapshot contract

`DigTerrainCellKey` and `DigTerrainChunkKey` contain `X`, `Y` and `Z`. Mesh generation and exposure checks evaluate all six neighbours.

Base terrain faces meet at exact half-cell boundaries. Deterministic geometric
roughness is constant for a whole coplanar surface rather than varying per cell,
so chunk meshes do not expose the authoritative gameplay grid as gaps or lighting
seams. Cell coordinates remain available only to simulation, navigation and the
bounded excavation interaction layer.

Front chunks preserve their authoritative `WorldChunkViewModel.Version`. Deep chunks receive deterministic versions from their sorted cell keys, material id and hardness. A changed or removed deep cell therefore invalidates its own chunk and exposed neighbouring faces without rebuilding unrelated chunks.

Dirty chunks are derived from:

- new, removed or version-changed front chunks;
- new, removed or version-changed deep chunks;
- revealed, damaged, reconnected, relaid-out or removed visible deposits;
- changed cutaway cells;
- changed protected cells;
- a changed chunk size or depth.

Every dirty origin also marks its four `X/Y` neighbours and the two adjacent `Z` layers. Chunk signatures provide a second check, so an invalidated neighbour is rebuilt only when its resulting visual state changed.

### Deep terrain projection boundary

The deep projection intentionally receives an explicit material id and hardness from `DigWorldSession`. It never copies those facts from an arbitrary front cell.

For the current demo, `TerrainDepthVolumePresenter` reproduces the previous rock mask:

- `TunnelNavigationVolume.IsOpen` cells are empty;
- explicit depth excavations and completed-room volume cells are empty;
- cells above the demo surface are empty sky;
- the generated natural-cave interior is empty;
- every remaining `Z=1..3` cell is solid rock.

This is a transitional presentation projection. `TunnelNavigationVolume` owns open topology, while the two-dimensional World and Job systems still own front terrain work. Full per-cell deep materials, damage, deposits, Jobs, reservations and save ownership remain part of #88.

## Terrain interaction boundary

The ordinary world surface has no cell picking.

- chunk meshes contain no `MeshCollider` or `BoxCollider`;
- hidden cell proxy colliders are disabled by default;
- `DigWorldInteraction` does not route ordinary clicks to `ContextWorldTargetKind.Ground`;
- entering Tunnel, Delete or Depth excavation mode enables proxy colliders;
- cave-room planning uses the same bounded excavation proxy path;
- leaving excavation mode disables proxies again;
- exact front `CellId` resolution remains limited to tunnel excavation flows.

Objects, residents, jobs, buildings and items continue to use their own interaction components. Layered movement uses dedicated invisible movement surfaces rather than terrain chunks, visible cell floors or hidden front-cell proxies.

Ordinary resident movement uses renderer-free `DigTunnelMovementSurface` colliders. Contiguous horizontal passages and vertical shafts are collapsed into surface runs, so the pointer is not aimed at a visible cell object. The hit point resolves to the nearest open hidden `SpatialCellId` for authoritative pathfinding and retains its clamped within-cell X offset for the final resident presentation position. Entering an excavation mode disables these movement surfaces and enables exact cell proxies; leaving excavation reverses that state.

## Prefab contract

Every production prefab referenced by a visual catalog must have `DigVisualPrefabRoot` on its root object.

- `ModelRoot` identifies the authored visual hierarchy.
- `TintRenderers` optionally lists renderers that accept selection/state tint.
- `SelectionColliders` optionally lists colliders used by pointer routing.
- Empty arrays fall back to child renderer/collider discovery.

`DigVisualTintTarget` applies `_BaseColor` and `_Color` with `MaterialPropertyBlock`, so selection and diagnostics do not instantiate materials.

## Building prefab authoring contract

`DigBuildingVisualCatalog` resolves `BuildingWorldViewModel.DefinitionId` to a typed `DigBuildingVisualProfile`. The first representative profile kinds are `Campfire`, `Furnace` and `Storage`; the kind is an authoring/validation category and never changes the stable gameplay definition id.

Each profile declares:

- expected logical footprint width and height;
- a pivot expressed in North-authored cell coordinates;
- required Presentation anchor kinds;
- separate prefab references for `BuildingBox`, `Assembly`, `Completed`, `Damaged` and `Packing` states.

`BuildingWorldPresenter` publishes the authoritative origin, `BuildingOrientation`, work position, completed/required construction work and packing progress. `BuildingVisualStateResolver` maps those immutable facts to a visual stage. Unity may scale or replace a staged prefab to show progress, but an animation callback never starts, advances, completes or cancels a Domain/Application operation.

Every production building prefab contains both `DigVisualPrefabRoot` and `DigBuildingPrefabAuthoring`. Validation checks:

- positive footprint dimensions matching the profile;
- matching pivot metadata;
- North authoring orientation;
- at least one selection collider under `ModelRoot`;
- non-null colliders and anchors;
- unique, non-empty anchor stable ids;
- every anchor kind required by the profile.

Typed `DigBuildingAnchor` components support `Worker`, `Visitor`, `Input`, `Output`, `Storage` and `Vfx`. These transforms are Presentation metadata only. Authoritative work positions, item locations, storage ownership, job reservations and visitor destinations remain in Domain/Application/read models and are never inferred from prefab transforms.

`DigBuildingRenderer` keeps one stable root per building and one staged prefab instance below it. Origin and orientation are reapplied from the read model on every render. Changing construction/damage/packing stage replaces only the child prefab, so selection remains attached to the stable building root. Changing the visual catalog explicitly invalidates existing child assets.

The placement ghost uses the same `BuildingBox` profile and authoritative preview origin/orientation/footprint. It creates one prefab preview plus one narrow work-position marker, places the hierarchy on Ignore Raycast and disables every preview collider. Validity remains visible through both tint and shape/tilt. Previewing or rebuilding the ghost commits no placement command.

The first-slice budgets are:

- one building root and one active prefab instance per active building;
- no GameObject per footprint cell in the production renderer;
- one placement-preview prefab and one work marker;
- no runtime material allocation per building;
- one bounding-box primitive only as an explicit missing-asset fallback.

## Authoring conventions

- one Unity unit equals one logical X/Y cell;
- authored object forward is Unity +Z and up is +Y;
- resident origin is between the feet;
- building origin is the center of its authoritative anchor cell at floor height;
- building prefabs face North; runtime orientation rotates their stable root;
- item origin is the center of its lowest contact surface;
- apply Blender transforms before export;
- keep stable gameplay ids out of prefab names and use catalog entries for mapping.

Recommended names:

```text
SM_Terrain_Stone_Decor_A
SM_Building_Furnace
SK_Resident_Base
SM_Item_IronOre
PF_Building_Furnace
MAT_Terrain_Stone
AN_Resident_Dig_Horizontal
VFX_Excavation_Stone
```

## Fallback policy

Fallbacks are presentation-only. They must never change gameplay state or infer missing definitions.

- missing catalog: cached role-aware debug materials on terrain and trim submeshes;
- unknown terrain, deposit or cave-template profile id: deterministic shape/material fallback;
- unknown building profile or missing stage prefab: base building-catalog fallback;
- missing building catalog: one footprint bounding-box cube, never one cube per cell;
- invalid prefab root/authoring metadata: validation error and fallback;
- validation errors: logged during renderer startup.

## Current limitations

The unified mesh snapshot covers the current visual `Z=0..3` volume, but only front cells have the full authoritative World state. Deep cells currently carry one explicit rock material/hardness pair and an open/solid projection from tunnel topology and excavation completion.

Terrain, deposits, deterministic decoration geometry, non-colour deposit silhouettes, measured LOD and template-room trim contracts are present. The building pipeline now has read-model orientation/progress, staged profiles, authoring validation, typed anchors and a single-prefab renderer/ghost. Production-authored materials/UVs, authoritative deposit instances from #91 and final building prefabs/animations remain outside the current repository implementation.

## Next steps

- #91: publish authoritative generated deposit instances, reveal and depletion snapshots;
- #88: migrate deep terrain materials, damage, Jobs, reservations and persistence to authoritative spatial ownership;
- #206: connect final authored materials and UV assets when available;
- #207: author representative Campfire, Furnace and Storage assets, then connect function-specific anchors and measured LOD/static batching budgets;
- #208–#210: typed prefab integrations for items, residents and creatures;
- #211: unified overlay styles;
- #212: URP, lighting and pooled VFX.
