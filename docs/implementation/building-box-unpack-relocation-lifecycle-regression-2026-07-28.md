# BuildingBox unpack, relocation and roster lifecycle regression

Status: implementation in [PR #495](https://github.com/bageus/Dig/pull/495), pending final CI and licensed Unity Play Mode execution.

Authoritative design: [`../design/building-box-placement-and-packing.md`](../design/building-box-placement-and-packing.md).

Tracking: [#118](https://github.com/bageus/Dig/issues/118), [#390](https://github.com/bageus/Dig/issues/390), [#398](https://github.com/bageus/Dig/issues/398).

## Reported runtime failures

- a confirmed Z1–Z3 building ghost remained at the target while the assigned resident reached the area but never committed the box to the site or started unpacking;
- a direct move order for the resident carrying the reserved box did not remove the confirmed plan ghost;
- the Buildings roster showed the physical source box and a separate pending building as two entities;
- a confirmed Z0 relocation ghost remained after the resident reached an adjacent position and no authoritative deposit occurred.

## Root causes

1. Unity placement supplied every open World cell as `reachable`. The Domain validator therefore selected work offsets that were open but unsupported or not in the source/holder Navigation region.
2. A held-box assembly job was left `Available` and depended on automatic-planning eligibility even though only the current holder can execute it.
3. Normal cell-click movement bypassed `PrepareResidentsForDirectCommand`; only tunnel movement used the BuildingBox cancellation pipeline.
4. Relocation planned ghosts were refreshed only around placement commands, not after authoritative completion/cancel transitions.
5. Relocation movement selected adjacent cells without requiring a fully supported standing surface.
6. The building presenter exposed a pending internal `BuildingId` as a normal building while HUD code independently listed the same source `StackId` as a BuildingBox.

## Corrected workflow

- placement projects supported Navigation cells and restricts them to the exact holder/source reachable region;
- held-box assembly is immediately claimed by the holder at confirmation;
- demo BuildingBox content exposes supported side work-position alternatives;
- normal direct movement runs the shared direct-command cancellation before movement and refreshes building, item, job and relocation-plan projections;
- authoritative relocation plans refresh while no interactive placement preview owns the ghost renderer;
- relocation work targets require a walkable cell with full actor support; completion still accepts the approved same-Z orthogonal-adjacent deposit position;
- `BuildingWorldViewModel` carries source-stack, job and commit-state identity;
- Buildings roster and management hide the internal pending building row and project one source-stack transformation: physical box/reserved plan, then `AtSite` unpack progress, then completed building.

## Regression coverage

Domain/Application and source-contract coverage verifies:

- held confirmation immediately claims the source owner;
- holder cancellation preserves the same quantity-one stack and releases reservations;
- placement reads Navigation walkable cells and full standing support;
- normal movement prepares direct-command cancellation before moving;
- relocation work-target resolution rejects unsupported adjacent cells;
- roster and management filter pending internal building rows and use source-stack transformation identity.

`BuildingBoxRuntimeLifecyclePlayModeTests` exercises the checked-in runtime composition for:

- held box confirmation, arrival, `AtSite`, `0/3 -> 1/3 -> 2/3 -> 3/3`, final completion and exactly-once box consumption;
- held Z0 relocation deposited from an adjacent supported work cell while preserving the same stack id;
- direct-command cancellation before `AtSite`, removal of the pending plan and preservation of the unreserved box in the holder inventory.

A successful source-contract or .NET run does not by itself mark this runtime workflow `VERIFIED`; the licensed Unity Test Runner must execute these scenarios and publish result artifacts.
