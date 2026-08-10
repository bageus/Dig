# Unity renderer memory safety

## Incident

A local Unity 6 Editor run terminated with a native allocator error while requesting a DynamicArray allocation. The dialog showed that ALLOC_DEFAULT had already grown to roughly 2.13 GB. The screenshot alone does not contain the native call stack, so it cannot prove a single root cause.

## Code-side pressure removed

The presentation host previously had two avoidable native allocation patterns:

- every world refresh destroyed every cell GameObject and recreated the complete world;
- every simulation tick destroyed and recreated the stockpile root and all primitive children.

`DigWorldRenderer` now keeps one visual per cell and applies new immutable view models to the existing objects. Only cells or chunks absent from a later view are destroyed.

`DigStockpileRenderer` now creates one base, twelve bounded pile blocks and one incoming-reservation plate. Later renders only update transforms and active states.

The bootstrap also clamps each demo-world dimension to 64 cells, preventing accidentally serialized extreme values from creating an unbounded primitive scene.

## Destroyed MeshRenderer hover incident — 2026-08-04

A local runtime screenshot showed `MissingReferenceException` for a destroyed `UnityEngine.MeshRenderer` shortly after the representative scene started. The observable trigger was a building visual-state refresh while the pointer remained over the same building.

Root cause:

- `DigBuildingVisual` correctly replaced its prefab instance when the building visual asset/state changed;
- `DigWorldInteraction` kept hover tint targets captured from the previous child instance because the stable parent `DigBuildingVisual` had not changed;
- `DigVisualTintTarget` also treated every non-empty renderer array as permanently valid;
- the following `LateUpdate` called `SetTint` through a stale target whose cached `MeshRenderer` had already been destroyed by Unity.

Correction:

- hover state now detects destroyed tint targets even when the stable hovered entity is unchanged, restores surviving targets, and recaptures the replacement visual children;
- apply/restore loops skip Unity-destroyed targets and tolerate temporarily mismatched cache arrays;
- `DigVisualTintTarget` validates every cached renderer, reacquires current child renderers after a rebuild, filters destroyed references, and skips a renderer destroyed between validation and application;
- building selection, hover identity and authoritative building state remain unchanged.

Regression coverage includes a Play Mode scenario for replacing cached renderer geometry and another for rebuilding a hovered target without producing a stale tint access. A .NET source contract keeps both cache-recovery paths in the Unity runtime source even when licensed Play Mode is unavailable.

## Local recovery after an allocator crash

1. Close Unity and Unity Hub.
2. Delete `Library`, `Temp` and `obj`.
3. Delete generated `bin` and `obj` directories below `src` if they exist.
4. Reopen exactly `.` with the supported Unity 6 LTS editor.
5. Open `Assets/Scenes/Main.unity` and enter Play Mode.
6. If the allocator failure repeats, attach the end of `%LOCALAPPDATA%/Unity/Editor/Editor.log` to issue #85. The lines before `Could not allocate memory` are required to identify the native subsystem.

## Validation

CI validates architecture, file-size and C# compatibility gates, Unity module/source contracts, Release build, all engine-independent tests, headless smoke and both deterministic soak profiles. The destroyed-renderer workflow requires licensed Unity Play Mode or an equivalent local representative-scene run before it can be marked `VERIFIED`.
