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

Each exposed host-rock face keeps its normal terrain material. The chunk mesh adds a separate low-poly deposit overlay consisting of one faceted cluster and at most two connector strips selected from the same-id connection mask. Internal faces between solid cells receive no overlay. There is no deposit prefab, collider or per-cell GameObject.

`DigTerrainRenderSnapshotBuilder` copies only decoration entries whose host terrain cell is still solid. Reveal, damage, connection, layout or removal changes invalidate the local terrain chunk. The chunk signature includes variant, rotation, scale and offset bands, so a changed layout cannot reuse stale geometry.

`DigTerrainVisualCatalog` requires five deposit profile families: Iron, Gold, Crystal, Coal and Stone. Each profile has `Revealed` and `Damaged` materials. There are deliberately no hidden or depleted materials.

The current runtime does not synthesize demo deposits from terrain material ids. Until #91 publishes authoritative deposit instances, the renderer receives no deposit volume and displays only host terrain.

### Snapshot contract

`DigTerrainCellKey` and `DigTerrainChunkKey` contain `X`, `Y` and `Z`. Mesh generation and exposure checks evaluate all six neighbours.

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

Objects, residents, jobs, buildings and items continue to use their own interaction components. Layered movement uses dedicated tunnel and completed-room floor visuals rather than terrain chunks or hidden front-cell proxies.

## Prefab contract

Every production prefab referenced by a visual catalog must have `DigVisualPrefabRoot` on its root object.

- `ModelRoot` identifies the authored visual hierarchy.
- `TintRenderers` optionally lists renderers that accept selection/state tint.
- `SelectionColliders` optionally lists colliders used by pointer routing.
- Empty arrays fall back to child renderer/collider discovery.

`DigVisualTintTarget` applies `_BaseColor` and `_Color` with `MaterialPropertyBlock`, so future selection and diagnostics do not instantiate materials.

## Authoring conventions

- one Unity unit equals one logical X/Y cell;
- authored object forward is Unity +Z and up is +Y;
- resident origin is between the feet;
- building origin is the center of its anchor cell at floor height;
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

- missing catalog: cached role-aware debug materials on chunk submeshes;
- unknown terrain or visible deposit profile id: base catalog fallback material;
- invalid prefab root: catalog fallback;
- validation errors: logged during renderer startup.

## Current limitations

The unified mesh snapshot covers the current visual `Z=0..3` volume, but only front cells have the full authoritative World state. Deep cells currently carry one explicit rock material/hardness pair and an open/solid projection from tunnel topology and excavation completion.

Terrain and deposit authoring plus deterministic decoration geometry are present, but production materials, authoritative deposit instances from #91, UV conventions and template cave trim remain later #206 slices.

## Next steps

- #91: publish authoritative generated deposit instances, reveal and depletion snapshots;
- #88: migrate deep terrain materials, damage, Jobs, reservations and persistence to authoritative spatial ownership;
- #206: connect production materials and template-cave arches, entrances, side walls and back-wall trim;
- #207–#210: typed prefab integrations for buildings, items, residents and creatures;
- #211: unified overlay styles;
- #212: URP, lighting and pooled VFX.
