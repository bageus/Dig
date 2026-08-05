# Animated Blackbeard resident model integration

Status: `IMPLEMENTED` (source, build, deterministic and blocked-runtime evidence complete; executed Unity Editor runtime verification remains pending).

Tracking issue: [#649](https://github.com/bageus/Dig/issues/649)

Implementation PR: [#651](https://github.com/bageus/Dig/pull/651)

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

## Importer and package graph

The canonical Unity host uses the pinned `com.unity.cloud.gltfast` `6.19.0`
package graph. The obsolete `org.khronos.unitygltf` git dependency is not part
of the supported package graph.

PR #651 restores the previously validated glTFast manifest and lock state after
the base branch regressed to the forbidden UnityGLTF dependency. This is a
package-baseline correction and does not change gameplay ownership or resident
action semantics.

## Current sex-selection limitation

The current `AgentViewModel` snapshot does not expose an authoritative sex
attribute. Therefore this model is registered as the masculine
`resident.default` presentation and is used by the current all-male runtime
path. A future male/female split must be driven by an authoritative snapshot
field rather than name- or id-based heuristics.

## Regression coverage

`DigResidentAnimatedModelPlayModeTests` covers:

- `Resources` loading of the GLB prefab and animation subassets;
- construction through the real `DigResidentRigFactory`;
- skinned renderer and Animator presence;
- manifest socket reuse;
- `Idle`, `Walk`, `Mine`, `Climb`, and `Death` clip selection;
- selection highlight round-trip without replacing authored materials;
- procedural fallback when the authored model is unavailable.

The authored-model configuration was separated into
`DigAuthoredResidentRigConfigurator` so `DigResidentRigFactory` remains below
the 350-line project limit while keeping renderer discovery, fallback creation
and root lifecycle at the factory boundary.

## CI evidence on PR #651

Exact head `6de5fc6f453538314e9788c0c8513b99b7716fb3`:

- Quality run `31017427868`: success;
- architecture, file-size and C# compatibility checks: success;
- Unity module and source-contract checks: success;
- resident visual contract: success;
- Release restore and build: success;
- full .NET test suite: success;
- headless smoke: success;
- standard deterministic soak: success;
- large-settlement deterministic soak: success;
- Stage 2 v2 export run `31017427813`: success;
- Stage 2 v3 export run `31017427839`: success.

Unity workflow `31017427669` completed successfully in blocked-evidence mode.
The licensed `Run Unity EditMode and PlayMode tests` step was skipped because
executed Unity activation was unavailable, so this workflow is not evidence
that `DigResidentAnimatedModelPlayModeTests` actually ran.

## Verification still required

Before status can become `VERIFIED`, run the Unity test suite on a licensed
Editor/runner and execute the complete resident workflow: spawn, idle, walk,
work, climb, hit, death, selection highlighting, equipment sockets, fallback,
and repeated resident recreation.
