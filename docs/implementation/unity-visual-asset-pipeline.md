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

## Current integration

`DigTerrainVisualCatalog` is loaded from `Resources/Dig/VisualCatalogs/Terrain` when it is not assigned explicitly.

The terrain presentation now has two separate layers:

1. `DigTerrainChunkRenderer` creates visible collider-free meshes per existing world chunk.
2. Existing per-cell primitives are hidden and retained only as bounded tunnel-dig interaction proxies.

Chunk signatures include cell visual state, cutaway state, protected state and neighbouring solid exposure. A changed boundary therefore rebuilds the affected adjacent chunk on the next presentation pass. The mesh builder emits only exposed faces and uses deterministic face offsets without `UnityEngine.Random`.

## Terrain interaction boundary

The ordinary world surface has no cell picking.

- chunk meshes contain no `MeshCollider` or `BoxCollider`;
- hidden cell proxy colliders are disabled by default;
- `DigWorldInteraction` does not route ordinary clicks to `ContextWorldTargetKind.Ground`;
- entering Tunnel, Delete or Depth excavation mode enables proxy colliders;
- leaving excavation mode disables them again;
- exact `CellId` resolution remains limited to the tunnel excavation flow.

Objects, residents, jobs, buildings and items continue to use their own interaction components. Tunnel depth navigation continues to use the dedicated layered tunnel renderer.

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

This slice converts the current Z0 world projection to chunk meshes. The deeper authoritative `Z=0..3` rock volume remains rendered by the existing tunnel/rock presentation until the unified 3D read model and dirty-chunk feed are connected.

Texture UV conventions, greedy meshing, production terrain materials, deposits and template cave trim remain part of #206.

## Next steps

- #205: connect the unified 3D terrain snapshot and dirty-chunk diagnostics;
- #206: production terrain/deposit/template-cave assets;
- #207–#210: typed prefab integrations for buildings, items, residents and creatures;
- #211: unified overlay styles;
- #212: URP, lighting and pooled VFX.
