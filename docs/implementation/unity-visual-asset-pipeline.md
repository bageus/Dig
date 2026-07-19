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

The first slice integrates `DigTerrainVisualCatalog` with `DigWorldRenderer`. The renderer loads an optional catalog from `Resources/Dig/VisualCatalogs/Terrain`. Existing scenes without a catalog continue to use primitive fallback visuals and the established debug colors.

The transition adapter applies catalog materials only when a cell presentation version changes. This phase does not replace the per-cell prototype renderer. Chunked terrain and direct prefab creation are tracked separately in #205.

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

- missing catalog: existing primitive and debug-color path;
- unknown id: catalog fallback prefab/material/tint;
- invalid prefab root: catalog fallback;
- validation errors: logged during renderer startup.

## Next steps

- #205: chunked low-poly terrain mesh and cell picking;
- #206: production terrain/deposit/template-cave assets;
- #207–#210: typed prefab integrations for buildings, items, residents and creatures;
- #211: unified overlay styles;
- #212: URP, lighting and pooled VFX.
