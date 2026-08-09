# Resident V3 dwarf model integration — 2026-08-09

## Status

`IMPLEMENTED`, pending executed Unity Play Mode verification.

Authoritative system: [`../design/presentation-input-ui-and-diagnostics.md`](../design/presentation-input-ui-and-diagnostics.md).
System index owner: `World/agent/building/item visuals` in [`../systems/README.md`](../systems/README.md).

## Request

Use the newly added authored resident model
`unity/Dig.Unity/Assets/DigDwarfEtalonV3/Dwarf_Hi3D_LowPoly_70k_Rigged.glb`
as the default in-game dwarf visual.

## Implementation

- the source GLB remains in `Assets/DigDwarfEtalonV3` as the authored input;
- the identical Git blob is exposed to the runtime at
  `Assets/Dig.Unity/Resources/Residents/Dwarf_Hi3D_LowPoly_70k_Rigged.glb`;
- both Unity assets receive explicit `.meta` GUIDs so imports remain stable;
- `DigResidentAnimatedModel` resolves the V3 resource and stable visual id
  `resident.dwarf.hi3d.lowpoly70k.rigged`;
- the resident visual profile retains its existing maximum-renderer budget for catalog-authored resident visuals;
- the default V3 dwarf is explicitly exempt from renderer-count rejection, so a successfully imported multipart V3 hierarchy is not destroyed and replaced with the procedural model solely because it exceeds that budget;
- V3 fallback is reserved for a genuinely missing/unloadable GameObject resource or an authored hierarchy with no renderer components;
- those hard fallback paths emit explicit Unity Console diagnostics so a failed glTF import is distinguishable from a renderer-budget decision;
- authored bounds are still normalized to the existing 1.5-unit resident presentation height; authoritative resident position and the gameplay interaction capsule are unchanged;
- imported bone lookup accepts common left/right upper-arm, upper-leg and namespaced (`prefix:Bone`) names;
- hand equipment sockets prefer actual imported hand bones when available and otherwise retain the existing upper-arm fallback;
- when the V3 GLB contains animation clips, the existing `DigResidentAnimationPlayer` uses them;
- when the V3 GLB contains no clips, the authored mesh remains active and `DigResidentRig` uses its existing deterministic pose fallback for movement/work/action presentation. No animation from the previous Blackbeard skeleton is applied to the new skeleton implicitly.

## Runtime fallback correction

The initial integration applied `MaximumRenderers` to the default V3 as an authored-model validity rule. If its imported hierarchy contained more renderer components than the configured budget, `DigAuthoredResidentRigConfigurator` returned `false`, the authored instance was destroyed, and `DigResidentRigFactory` created the procedural resident. That made a valid V3 import look like a missing model in-game.

The default V3 now bypasses that rejection. Other catalog-authored resident visuals continue to respect their configured renderer budget, preserving the existing bounded-presentation rule outside this model-specific compatibility path.

## Ownership and invariants

This change is Presentation-only. It does not change resident identity, logical position,
actions, jobs, navigation, inventory, combat state or simulation timing. Animation and mesh
state remain visual projections and cannot complete Domain actions.

## Regression coverage

`DigResidentAnimatedModelPlayModeTests` checks that:

- the V3 resource resolves as the default resident asset;
- the instantiated rig contains the V3 model rather than the procedural representative;
- the authored hierarchy exposes runtime renderer components without imposing a V3 fallback threshold on their count;
- equipment/cargo/head sockets are available;
- Idle/Walk/Dig/Climb/Death visual states can be applied;
- embedded animation clips are used when matching clips exist;
- a clipless authored mesh remains active and uses the rig pose path instead of a stale animation set;
- a genuinely missing authored asset still falls back to the procedural resident.

`DigAuthoredResidentRigConfiguratorTests` additionally creates a default-V3 stand-in whose renderer count exceeds its configured budget and verifies that the authored rig remains active.

## Verification status

Repository/source checks and CI can verify the code contracts, but actual licensed Unity execution is still required before this result can be called `VERIFIED` under issue #511.
The required runtime check is to open the representative scene and confirm the V3 dwarf is visible,
correctly grounded/scaled, faces the expected direction, moves through Walk/Climb, performs Dig,
keeps held tools/cargo attached to the intended hands/sockets, preserves authored materials and
produces no unexpected Console errors. If the V3 asset itself fails to import as a GameObject, the Console now reports the failing `Resources` path explicitly.
