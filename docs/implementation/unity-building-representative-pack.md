# Unity representative building visual pack

## Purpose

The representative pack supplies usable low-poly building presentation before final Blender-authored prefabs are available. It is a Presentation asset source only. It does not define building gameplay, placement, jobs, inventory, visitors, storage ownership, damage, construction or packing state.

Runtime lookup order is:

```text
DigBuildingVisualCatalog authored profile
    -> representative Resources pack
    -> configured catalog fallback
    -> one runtime bounding-box fallback
```

A final authored profile therefore replaces a representative without changing `BuildingDefinitionId`, read models, commands or save data.

## Data source

The pack is stored at:

```text
Assets/Dig.Unity/Resources/Dig/VisualCatalogs/RepresentativeBuildings.json
```

It is loaded once through `Resources.Load<TextAsset>`, parsed once and indexed by stable id. Geometry templates are created lazily per representative profile and visual state, then reused by both the world building renderer and the BuildingBox placement ghost.

The current aliases are:

| Representative | Stable ids |
|---|---|
| Campfire | `kitchen.campfire`, `building.campfire` |
| Furnace | `building.furnace`, `building.forge`, `demo.workshop.box` |
| Storage/Arsenal | `building.arsenal`, `building.storage` |

Issue #98 still owns the authoritative kitchen content catalog. Supporting both Campfire aliases is a Presentation compatibility rule and does not introduce a new Domain definition.

## Geometry and stage contract

The pack uses four shared procedural meshes:

- Box — 12 triangles;
- Pyramid — 6 triangles;
- Octahedron — 8 triangles;
- Wedge — 8 triangles.

Meshes and one neutral material are shared by every template. The material enables GPU instancing. Per-instance tint and selection use `MaterialPropertyBlock`; no material is allocated per building.

Each representative supports the same staged contract as an authored profile:

- `BuildingBox` — compact crate silhouette;
- `Assembly` — representative silhouette plus scaffold;
- `Completed` — full representative silhouette;
- `Damaged` — tilted and compressed non-colour damage shape;
- `Packing` — crate silhouette plus packing strap.

Changing stage replaces only the child template. The stable building root, authoritative origin, orientation and selection remain intact. Stage animation reads immutable construction or packing progress and never emits a Domain/Application command.

## Scale, pivot and orientation

- one Unity X/Z unit equals one logical building cell;
- +Y is up;
- representatives are authored facing North;
- runtime orientation rotates the stable building root;
- the root origin is the authoritative building origin at floor height;
- `pivotCell` is expressed in North-authored footprint coordinates;
- selection colliders and Presentation anchors are children of the generated model root.

Current representative footprints and pivots are:

| Representative | Footprint | Pivot |
|---|---:|---:|
| Campfire | 1×1 | 0, 0 |
| Furnace | 1×1 | 0, 0 |
| Storage/Arsenal | 3×2 | 1, 0.5 |

Runtime compares the immutable building footprint with profile metadata and reports a mismatch. The visual never changes the authoritative footprint.

## Presentation anchors

Generated templates use the same `DigBuildingAnchor` metadata as authored prefabs.

- Campfire: Worker, Input, Output, VFX;
- Furnace: Worker, Input, Output, VFX;
- Storage/Arsenal: Worker, Visitor, Input, Output and two Storage anchors.

Anchor transforms are optional Presentation attachment points. Authoritative work positions, item locations, reservations, storage contents and visitor destinations continue to come from Domain/Application/read models. A renderer or animation cannot infer or commit gameplay state from an anchor transform.

## Measured LOD

Buildings reuse `TerrainVisualDetailPolicy` and its pixels-per-cell hysteresis:

- Marker to Reduced at 12 pixels per cell;
- Reduced to Marker below 8 pixels per cell;
- Reduced to Full at 28 pixels per cell;
- Full to Reduced below 22 pixels per cell.

Every representative has at least one Marker-level non-colour silhouette. Reduced and Full parts are child detail groups. Crossing a threshold only toggles those groups; it does not rebuild a prefab, change a read model or create a new material.

Placement ghosts use Reduced detail and disable every collider. Invalid placement remains distinguishable through tint, tilt and vertical scale, not colour alone.

## Budgets and batching policy

Hard CI limits per representative profile are:

- at most 16 authored renderers;
- at most 512 authored triangles;
- at least one Marker-level part;
- unique stable ids and anchor ids;
- required anchor kinds for the representative category.

Current tighter data budgets are:

| Representative | Renderer budget | Triangle budget |
|---|---:|---:|
| Campfire | 8 | 160 |
| Furnace | 8 | 192 |
| Storage/Arsenal | 12 | 256 |

Interactive staged building roots are deliberately not passed to `StaticBatchingUtility`. Their construction scale, packing scale, damage child, selection transform and stage child may change. Shared meshes, one instanced material and bounded child renderers are the supported batching path.

`DigBuildingRenderer` exposes diagnostics for:

- pixels per logical cell;
- current detail level;
- visible renderer count;
- visible triangle count;
- current visual rebuild count.

A detail-band transition must not increment rebuild count.

## Validation and replacement

CI validates JSON aliases, kinds, silhouettes, anchors and budgets independently of Unity scene serialization. Unity source contracts verify shared meshes, template caching, instancing, LOD and the absence of production `GameObject.CreatePrimitive` calls in the representative path.

To replace a representative with final art:

1. author North-facing staged prefabs with `DigVisualPrefabRoot` and `DigBuildingPrefabAuthoring`;
2. validate footprint, pivot, colliders and required anchors;
3. add a `DigBuildingVisualProfile` using the authoritative `BuildingDefinitionId`;
4. assign the profile to `DigBuildingVisualCatalog`;
5. confirm selection, placement ghost and every visual stage in the Unity validation scene.

The authored catalog has higher priority than the representative pack, so no gameplay migration is required.
