# Unity creature visual presentation pipeline

## Ownership

Ecology and Combat own creature identity, species, lifecycle, position, disposition, health, attacks, damage, death, reproduction and drops. The creature presentation layer receives immutable `CreatureVisualSnapshot` values and may only choose a rig, variant, palette, marker shape, pose and LOD.

No animation callback, renderer visibility event, marker object or attachment anchor may spawn a creature, apply damage, finish an attack, tame an individual, change lifecycle state or release a drop.

## Immutable snapshot

The first #210 slice accepts these facts:

- stable creature ID;
- stable species ID;
- lifecycle stage: seed, egg, larva, child or adult;
- neutral, tamed or hostile disposition;
- alive state and cell position;
- move, attack, impact, growth and special-action flags;
- normalized action progress and authoritative version.

The snapshot is engine-independent. Unity objects are not stored in save data and renderer rebuilds do not mutate the snapshot.

## Starter-biome species mapping

`CreatureVisualPresenter` maps the current #149 species catalog:

- hamster and larva;
- poisonous and fire plants;
- Vuker and sulfur Vuker;
- spider and spider egg;
- swallower and lava demons;
- troll and goblin.

Unknown or modded species use `creature.rig.fallback` and set `IsFallback`. Missing art therefore remains visible and diagnosable without changing ecology state.

## Shared family rigs

Species definitions resolve to one of six reusable rig families:

- plant;
- Vuker;
- arachnid;
- biped;
- large demon;
- small creature.

Variants may share one family rig. Poison/fire plants, common/sulfur Vukers and troll/goblin deliberately use common rig IDs while retaining distinct variant IDs. Lifecycle is appended to the variant, so child/adult Vukers and spider eggs can reuse the same species definition without random asset selection.

## Typed Unity profiles

`DigCreatureVisualProfile` is keyed by stable species ID and stores:

- shared rig stable ID;
- expected family;
- optional prefab and material;
- tint and scale;
- renderer budget from 3 through 32.

A prefab requires `DigVisualPrefabRoot`. Missing profiles, missing prefabs and renderer-budget violations fall back to the representative family rig. `DigCreatureVisualCatalog.ValidateCatalog` reports invalid and duplicate species profiles.

## Stable root and rebuild boundary

Each active creature has one stable root owned by `DigCreatureRenderer`:

- one `DigCreatureVisual` component;
- one family-sized `SphereCollider` for future selection routing;
- authoritative projected world position;
- one collider-free child rig.

Changing species family replaces only the child rig. The root ID, collider, interpolation state and selection identity remain stable. Ordinary lifecycle/action/palette updates reuse the existing root and rig.

Removed roots enter a bounded pool. The engine-independent `CreatureRenderReconciliationPlan` separates create, update and remove IDs, rejects duplicates and enforces the caller-provided population cap before Unity changes anything.

## Representative family rigs

Runtime fallback rigs are simple low-poly compositions that define readable silhouettes and anchor locations. They are not final art assets. Generated primitive colliders are removed, all body renderers share one instanced material, and palette/selection changes use `MaterialPropertyBlock`.

The six family shapes cover plants, quadruped Vukers, arachnids, bipeds, large demons and small creatures. An authored prefab may replace any fallback through the same profile contract.

## Disposition and accessibility

Disposition is communicated by geometry as well as color:

- neutral: ring marker;
- tamed: shield/diamond marker;
- hostile: spike marker.

This keeps tamed and hostile creatures distinguishable in grayscale and under color-vision differences. Selection adds a shared gold blend but does not replace the disposition shape.

## Action projection

Presentation action precedence is deterministic:

1. death;
2. hit;
3. attack;
4. special action;
5. growth;
6. move;
7. idle.

Death publishes progress 1.0. Idle and move are looping visual states; all other states consume the authoritative normalized progress. The fallback rig uses bounded transform poses only. Root motion and combat animation events are forbidden.

## Anchors

Every creature rig exposes four presentation anchors:

- Equipment;
- Drop;
- InsideCreature;
- VFX.

The InsideCreature anchor can display the one swallowed item owned by ecology/inventory, while Drop marks a visual release point after authoritative death resolution. Anchors never become item locations themselves.

## LOD and off-screen policy

`CreatureVisualPresenter.PresentLod` defines four tiers:

- Near: update every frame;
- Mid: reduced updates every 4 frames;
- Far: reduced updates every 12 frames and half renderer detail;
- Hidden/off-screen: body disabled and animation frozen.

The renderer uses camera distance and viewport visibility. The root and snapshot remain active even when body rendering is disabled, so visibility cannot affect combat or ecology.

## Bootstrap boundary

`DigUnityBootstrap` composes `DigCreatureRenderer` with an empty snapshot list. This validates resources and ownership without inventing demo ecology state. Issue #149 will later provide authoritative snapshots to the same renderer.

The foundation does not edit `Main.unity`; current scene changes remain owned by `main`, while runtime composition stays code-driven.

## Remaining #210 work

- ecology/combat snapshot adapter from #149 and #12;
- authored starter-biome prefabs and animation controllers;
- selected/hostile overlay integration with #211;
- presentation attachments for equipment, swallowed items and drops;
- measured population gallery and Play Mode pooling tests;
- final off-screen animation scheduler and profiler budgets;
- audio/VFX-only animation events.
