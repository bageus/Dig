# Animated Blackbeard resident model integration

Status: `IMPLEMENTED` (source, build, deterministic and blocked-runtime evidence complete; executed Unity Editor runtime verification remains pending).

Tracking issue: [#649](https://github.com/bageus/Dig/issues/649)

Regression issue: [#657](https://github.com/bageus/Dig/issues/657)

Implementation PR: [#651](https://github.com/bageus/Dig/pull/651)

Authoritative specification: [`../../design/presentation-input-ui-and-diagnostics.md`](../../design/presentation-input-ui-and-diagnostics.md)

## Scope

The user-provided male Generic-rig model at
`Assets/Male_blackbeard_Dwarf_Rigged_Animated`
is the confirmed authored runtime resident model. `DigDwarfEtalonV3` is not the
selected model for this integration.

The runtime copy is stored under
`Assets/Dig.Unity/Resources/Residents/MaleBlackbeardDwarf.glb` so the player
build can resolve the imported prefab and its animation subassets.

The integration changes Presentation only. It does not change simulation,
domain ownership, resident identity, movement, work or combat rules.

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

The runtime factory accepts the Blackbeard authored model when it contains a
usable renderer set within the configured budget. It normalizes the model to
the resident presentation height, preserves authored materials and reuses
these model nodes:

- `LeftHandTool`
- `RightHandTool`
- `CarryAnchor`
- `BackAttachment`
- `HeadAccessory`

A missing resource or an unusable renderer contract may use the existing
procedural low-poly fallback. Animation setup is not allowed to discard an
otherwise valid Blackbeard mesh. When animation subassets cannot be configured,
the authored model remains visible without animation and Presentation emits a
runtime warning. This separates model visibility from animation diagnostics and
prevents the old procedural resident from silently replacing Blackbeard.

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

- `Resources` loading of the Blackbeard GLB prefab and animation subassets;
- construction through the real `DigResidentRigFactory`;
- a required `SkinnedMeshRenderer`, imported model root and Animator;
- manifest socket reuse;
- `Idle`, `Walk`, `Mine`, `Climb`, and `Death` clip selection;
- selection highlight round-trip without replacing authored materials;
- retention of a valid authored mesh when animation setup is unavailable;
- procedural fallback only when the authored model itself is unavailable.

The authored-model configuration remains in
`DigAuthoredResidentRigConfigurator` so `DigResidentRigFactory` stays below the
350-line project limit while renderer discovery, fallback creation and root
lifecycle remain at the factory boundary.

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

## Regression status on #657

The source fix keeps the confirmed Blackbeard model active when animation
configuration fails and adds a Play Mode regression for this policy. No claim
of executed Unity runtime verification is made until a licensed Editor/runner
executes the updated test suite.

## Verification still required

Before status can become `VERIFIED`, run the Unity test suite on a licensed
Editor/runner and execute the complete resident workflow: spawn, idle, walk,
work, climb, hit, death, selection highlighting, equipment sockets, animation
failure diagnostics, procedural fallback for a missing model and repeated
resident recreation.
