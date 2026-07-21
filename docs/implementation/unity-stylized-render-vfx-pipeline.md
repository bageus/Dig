# Unity stylized rendering and pooled VFX

## Ownership

Domain and Application own excavation, construction, production, combat, statuses, spawning, damage, death and drops. Presentation receives immutable rendering requests and may omit, delay, pool, replace or disable visual effects and realtime lights without changing those results.

No particle callback, light visibility event, animation event or material state may commit a command, apply damage, complete work or spawn an entity.

## Current source-only scope

The first #212 runtime slice provides:

- immutable effect and realtime-light requests;
- deterministic per-frame budgets;
- centralized Lit, Unlit and Overlay material resolution;
- an ambient/key/rim lighting baseline;
- a bounded realtime-light pool;
- a typed VFX catalog and bounded pooled player;
- one migrated production consumer: creature visuals;
- source contracts and deterministic tests.

It does not claim that the project is already assigned to an authored URP pipeline asset. Unity 6000.0.71f1 must create and serialize the Universal Render Pipeline asset, renderer data and Graphics Settings assignment through the Editor. Source-only automation must not fabricate those opaque serialized assets.

## Immutable requests and deterministic budgets

`EffectSpawnRequest` contains stable request/effect IDs, category, priority, world position, duration, particle budget, scale and version. Categories cover excavation, deposits, construction, production, status, combat and ambient effects. Reusing an ID with the same version is idempotent.

`LightRequest` contains stable ID, point/spot kind, priority, world position, range, intensity, RGB color, shadow preference and version. The renderer may downgrade shadows when the shadow budget is exhausted.

`RenderFrameBudget.Default` limits a frame to 64 active effects, 2,048 particles, 8 realtime lights and 2 shadowed lights. `RenderBudgetPlan` selects higher priority first, then shorter focus distance, then ordinal stable ID. Duplicate IDs are rejected before Unity objects change.

## Shared material ownership

`DigRenderMaterialProfile` is keyed by `RenderMaterialSemantic` and `RenderSurfaceKind`. Authored materials must enable GPU instancing. `DigRenderMaterialCatalog` validates duplicates and profiles.

`DigRenderMaterialLibrary` loads `VisualCatalogs/RenderMaterials`, caches each semantic/surface pair, never destroys authored catalog materials and destroys only owned runtime fallbacks. Runtime shader lookup is centralized here:

- Lit: `Universal Render Pipeline/Lit`, then `Standard`;
- Unlit/Overlay: `Universal Render Pipeline/Unlit`, then built-in unlit fallbacks.

This is a compatibility path, not the final production workflow. Creature rendering is the first migrated consumer and no longer owns a private shader/material lifecycle.

## Stylized lighting

`DigStylizedLightingRig` creates a bake-independent baseline with flat ambient light, a warm directional key and a cool directional rim. Baseline directional lights do not cast realtime shadows. Dynamic excavation therefore does not require a baked-lighting rebuild.

`DigRealtimeLightPool` owns reusable Unity Light components. It grows only to the configured maximum, disables unused lights, and enables shadows only for the selected shadow-budget subset. Lava, crystals, campfires and production buildings will publish requests in later slices instead of creating unbounded Light objects.

## VFX catalog and pooling

`DigVfxProfile` stores a stable effect ID, category, optional prefab, maximum simultaneous instances and maximum particles. Authored prefabs require `DigVisualPrefabRoot`. `DigVfxCatalog` reports invalid and duplicate profiles.

`DigPooledVfxPlayer` applies the global budget, deduplicates request/version pairs, enforces per-profile limits, removes colliders, reuses one shared Unlit material and keeps at most 64 inactive roots. Missing or invalid authored effects use a simple bounded ParticleSystem fallback.

Effect loss, delay or disable never changes simulation state.

## Overlay and per-instance readability

Palette, selection and per-instance differences should use `MaterialPropertyBlock` rather than cloned materials. Overlay materials remain Unlit/Overlay semantic content. Non-color shape and pattern cues from #211 remain required so lighting changes cannot hide interaction state.

## URP completion boundary

Completion of URP migration requires Editor-created pipeline and renderer assets, assignment through Graphics Settings, reimport, and representative Play Mode validation. `GraphicsSettings.currentRenderPipeline` is diagnostic until that work is performed.

## Remaining #212 work

- create and assign URP pipeline/renderer assets in Unity 6000.0.71f1;
- author shared Lit, Unlit, Overlay and emissive materials;
- migrate terrain, buildings, items, residents and overlays from local fallback ownership;
- publish effect requests from excavation, deposits, construction, production, status and combat;
- publish light requests for lava, crystals, campfires and active production buildings;
- add terrain vertex-color/AO authoring and profiler diagnostics;
- run Editor reimport and representative Play Mode validation.
