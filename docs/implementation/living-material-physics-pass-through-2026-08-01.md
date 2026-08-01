# Living-material physics pass-through — 2026-08-01

Status: `IMPLEMENTED` on branch `fix/living-material-pass-through-world-items`; licensed Unity runtime evidence remains pending.

Authoritative decision: [`../design/living-material-physics-pass-through-correction-2026-08-01.md`](../design/living-material-physics-pass-through-correction-2026-08-01.md).

Tracking issues: [#524](https://github.com/bageus/Dig/issues/524), [#433](https://github.com/bageus/Dig/issues/433).

## Reported regression

Free hamster and grub could physically push or displace the temporary production package and other movable item/material visuals while wandering through their world position.

## Root cause

`DigCreatureRenderer` creates one root `SphereCollider` so pointer raycasts can resolve `DigCreatureVisual`. The collider was left as a solid collider for every species. Living-material movement interpolates the root transform from authoritative ecology snapshots, so Unity treated the moving proxy as solid geometry and could apply depenetration impulses to overlapping Rigidbody item/package visuals.

The item and unfinished-package lifecycle was not the position owner causing the move: ordinary world-item targeting already uses a trigger collider, the unfinished package disables interaction, and child art colliders are disabled.

## Correction

- `DigCreatureRenderer.ConfigureCollider` now explicitly enables and configures collider mode on every create/update, including pooled-root reuse.
- `creature.hamster`, `creature.grub` and compatibility alias `creature.larva` use `SphereCollider.isTrigger = true`.
- Other creature species explicitly reset to solid mode and keep their previous sizes.
- Living-material trigger colliders remain on the interaction layer and stay raycastable.
- Domain ecology, Inventory ownership, production lifecycle, pickup/drop/storage and save data are unchanged.

## Regression coverage

- `LivingMaterialPhysicsUnityContractTests` locks species-specific trigger configuration, pooled reset, world-item trigger targeting and unfinished-package non-interactivity.
- `LivingMaterialPhysicsPassThroughPlayModeTests` renders a hamster and grub, verifies enabled trigger colliders and direct collider raycasts, overlaps each proxy with a movable Rigidbody cube, advances fixed simulation twice, and requires unchanged position, rotation, linear velocity and angular velocity.

## Verification boundary

The checked-in source and Play Mode tests are implementation evidence only until executed. Repository Quality/build/.NET tests and licensed Unity EditMode/PlayMode evidence must be reported from actual runs; no runtime `VERIFIED` claim is made by this note.
