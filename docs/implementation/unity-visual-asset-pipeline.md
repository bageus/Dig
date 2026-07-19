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

The terrain presentation has three separate responsibilities:

1. `DigTerrainRenderSnapshotBuilder` converts the latest presentation read model and current presentation-only cutaway/protected sets into an immutable snapshot.
2. `DigTerrainChunkRenderer` consumes that snapshot and creates collider-free meshes per dirty world chunk.
3. Existing per-cell primitives stay hidden and are retained only as bounded tunnel-dig interaction proxies.

The renderer no longer walks hidden `DigCellVisual` objects in `LateUpdate`. A world refresh, cutaway change, protected-cell change or catalog change explicitly refreshes the terrain projection.

### Snapshot contract

`DigTerrainCellKey` and `DigTerrainChunkKey` contain `X`, `Y` and `Z`. Mesh generation and exposure checks already evaluate all six neighbours.

The current `WorldViewModel` still exposes only the authoritative front layer, so `DigTerrainRenderSnapshotBuilder` publishes those cells at `Z=0` and reports `Depth=1`. It does not infer deeper materials, hardness, exploration or deposits from the front layer. The depth-ready contract is intended to accept the unified `Z=0..3` read model from #88 without changing the renderer API.

Dirty chunks are derived from:

- new, removed or version-changed source chunks;
- changed cutaway cells;
- changed protected cells;
- a changed chunk layout.

Every dirty origin also marks its four `X/Y` neighbours and the two adjacent `Z` layers. This conservative invalidation keeps exposed faces correct across chunk and layer boundaries. Chunk signatures provide a second check, so an invalidated neighbour is rebuilt only when its resulting visual state actually changed.

## Terrain interaction boundary

The ordinary world surface has no cell picking.

- chunk meshes contain no `MeshCollider` or `BoxCollider`;
- hidden cell proxy colliders are disabled by default;
- `DigWorldInteraction` does not route ordinary clicks to `ContextWorldTargetKind.Ground`;
- entering Tunnel, Delete or Depth excavation mode enables proxy colliders;
- cave-room planning uses the same bounded excavation proxy path;
- leaving excavation mode disables proxies again;
- exact `CellId` resolution remains limited to tunnel excavation flows.

Objects, residents, jobs, buildings and items continue to use their own interaction components. Tunnel navigation continues to use the dedicated layered tunnel and completed-room floor renderers.

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

- missing catalog: cached debug materials on chunk submeshes;
- unknown id: catalog fallback material;
- invalid prefab root: catalog fallback;
- validation errors: logged during renderer startup.

## Current limitations

The immutable snapshot currently contains only authoritative `Z=0` cells. The deeper `Z=1..3` rock volume remains rendered by the existing tunnel/rock presentation until #88 exposes material and state data for every spatial cell.

Texture UV conventions, greedy meshing, production terrain materials, deposits and template cave trim remain part of #206.

## Next steps

- #205: feed authoritative `Z=0..3` terrain chunks into `DigTerrainRenderSnapshot` and retire duplicate deeper rock geometry;
- #206: production terrain/deposit/template-cave assets;
- #207–#210: typed prefab integrations for buildings, items, residents and creatures;
- #211: unified overlay styles;
- #212: URP, lighting and pooled VFX.
