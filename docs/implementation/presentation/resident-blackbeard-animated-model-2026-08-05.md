# Animated Blackbeard resident model integration

Status: `IMPLEMENTED` (Play Mode regression added; runtime verification in Unity Editor/CI is still required).

Tracking issue: [#649](https://github.com/bageus/Dig/issues/649)

Authoritative specification: [`../../design/presentation-input-ui-and-diagnostics.md`](../../design/presentation-input-ui-and-diagnostics.md)

## Scope

The user-provided male Generic-rig model at
`unity/Dig.Unity/Assets/Male_blackbeard_Dwarf_Rigged_Animated`
is connected to the Unity runtime resident presentation without changing
simulation or domain ownership.

The runtime copy is stored under
`Assets/Dig.Unity/Resources/Residents/MaleBlackbeardDwarf.glb` so the player
build can resolve the imported prefab and its animation subassets.

## Runtime mapping

- `Idle` -> `Idle`
- `Walk` -> `Walk`
- `Run` -> `Run`
- `Dig` -> `Mine`
- `Carry`, `Pickup`, `Drop` -> `Carry`
- `Build` -> `Build`
- `Sleep` -> `Rest`
- `Eat` -> `Eat`
- `Hit` -> `Hit`
- `Death` -> `Death`
- vertical traversal -> `Climb`

Animation is driven through Unity Playables. Root motion remains disabled;
the existing authoritative movement projection continues to own world
position and facing.

## Prefab and socket policy

The runtime factory accepts a skinned authored model with at least one
renderer, normalizes it to the resident presentation height, preserves
authored materials, and reuses these model nodes:

- `LeftHandTool`
- `RightHandTool`
- `CarryAnchor`
- `BackAttachment`
- `HeadAccessory`

If the resource, required `Idle` clip, or authored rig is unavailable, the
existing procedural low-poly resident remains the fallback.

## Current sex-selection limitation

The current `AgentViewModel` snapshot does not expose an authoritative sex
attribute. Therefore this model is registered as the masculine
`resident.default` presentation and is used by the current all-male runtime
path. A future male/female split must be driven by an authoritative snapshot
field rather than name- or id-based heuristics.

## Regression evidence

`DigResidentAnimatedModelPlayModeTests` covers:

- `Resources` loading of the GLB prefab and animation subassets;
- construction through the real `DigResidentRigFactory`;
- skinned renderer and Animator presence;
- manifest socket reuse;
- `Idle`, `Walk`, `Mine`, `Climb`, and `Death` clip selection;
- selection highlight round-trip without replacing authored materials;
- procedural fallback when the authored model is unavailable.

The test is committed but was not executed in this environment because the
Unity Editor is unavailable.
