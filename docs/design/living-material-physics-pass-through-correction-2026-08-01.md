# Living-material physics pass-through correction

Status: `APPROVED`; implementation and licensed Unity runtime evidence are tracked separately.

Tracking issues: [#524](https://github.com/bageus/Dig/issues/524), [#433](https://github.com/bageus/Dig/issues/433).

Authoritative parent specifications:

- [`hamsters-and-grubs-ecology.md`](hamsters-and-grubs-ecology.md);
- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`world-item-gravity-selection-and-pickup.md`](world-item-gravity-selection-and-pickup.md).

## Confirmed correction — 2026-08-01

Free hamster and grub are living Inventory materials whose world movement is committed by the ecology system. Their Unity collider is only an interaction/raycast proxy and must not participate in solid physics.

Consequently:

- hamster and grub pass through ordinary world items, raw materials and production packages without pushing, displacing or rotating them;
- an unfinished production package remains at its authoritative Inventory world cell until production lifecycle code moves, removes or replaces it;
- living-material pointer selection and ordinary pickup remain available through the same proxy collider;
- world items and packages do not become non-interactive merely to avoid collisions;
- other creature families keep their existing collider policy unless separately specified;
- Presentation and Unity physics never become an authoritative owner of item or living-material position.

`creature.larva` is a compatibility alias for grub and receives the same pass-through policy if it reaches Presentation before canonicalization.

## Implementation contract

`DigCreatureRenderer.ConfigureCollider` must explicitly reset collider mode on every create/update because visual roots are pooled. Hamster, grub and the larva compatibility alias use `SphereCollider.isTrigger = true`; all other creature species use the existing solid mode.

The trigger must remain enabled on the normal interaction layer so raycasts can still resolve `DigCreatureVisual`. Child rig colliders remain disabled.

The existing world-item contract remains authoritative:

- interactive item roots use a trigger collider for pointer targeting;
- non-interactive unfinished packages disable that interaction collider;
- visual child colliders are disabled.

## Failure, retry and concurrency

- Repeated renderer reconciliation or pooled-root reuse must not leave a stale trigger/solid setting from the previous species.
- Overlap with any item/material/package produces no physics impulse and no transform drift.
- Ecology movement, pickup, storage, production cancel/finalize and save/load behavior are otherwise unchanged.
- A raycast miss after changing the collider to trigger is a regression, not an accepted tradeoff.

## Acceptance

- [ ] source contract requires species-specific trigger configuration and explicit reset for non-living species;
- [ ] checked-in Unity Play Mode renders hamster and grub, verifies their root colliders are enabled triggers and remain raycastable;
- [ ] checked-in Unity Play Mode overlaps each living-material proxy with a movable Rigidbody item/package and verifies no position, rotation or velocity change after fixed simulation;
- [ ] ordinary world-item and unfinished-package interaction contracts remain green;
- [ ] repository Quality/build/.NET tests pass;
- [ ] licensed Unity EditMode/PlayMode executes the new runtime scenario before status may become `VERIFIED`.

## Verification boundary

Repository source contracts and checked-in Play Mode scenarios may establish `IMPLEMENTED`. Only retained evidence from a licensed Unity Test Runner may establish `VERIFIED`.
