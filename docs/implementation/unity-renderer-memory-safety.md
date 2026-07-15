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

## Local recovery after an allocator crash

1. Close Unity and Unity Hub.
2. Delete `unity/Dig.Unity/Library`, `unity/Dig.Unity/Temp` and `unity/Dig.Unity/obj`.
3. Delete generated `bin` and `obj` directories below `src` if they exist.
4. Reopen exactly `unity/Dig.Unity` with the supported Unity 6 LTS editor.
5. Open `Assets/Scenes/Main.unity` and enter Play Mode.
6. If the allocator failure repeats, attach the end of `%LOCALAPPDATA%/Unity/Editor/Editor.log` to issue #85. The lines before `Could not allocate memory` are required to identify the native subsystem.

## Validation

CI validates architecture, file-size and C# compatibility gates, Unity module references, Release build, all engine-independent tests, headless smoke and both deterministic soak profiles. Unity Play Mode remains a local validation step because CI does not launch the editor.
