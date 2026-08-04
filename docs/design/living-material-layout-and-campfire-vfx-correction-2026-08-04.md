# Living-material item layout and campfire VFX correction — 2026-08-04

Status: `APPROVED`.

Tracking issue: [#619](https://github.com/bageus/Dig/issues/619).

Authoritative together with:

- [`building-production-and-internal-supply.md`](building-production-and-internal-supply.md);
- [`hamsters-and-grubs-ecology.md`](hamsters-and-grubs-ecology.md);
- [`presentation-input-ui-and-diagnostics.md`](presentation-input-ui-and-diagnostics.md).

## Confirmed observable behavior

### Living-material co-location

- `creature.hamster`, `creature.grub` and legacy `creature.larva` keep one linked quantity-one Inventory entity and one creature projection.
- Their linked world-item geometry remains hidden while the creature projection is active.
- A hidden living-material Inventory proxy does not consume a world-item co-cell layout slot.
- Entering, leaving or moving through a cell cannot change the transform of an unrelated world item, unfinished production package or closed production package in that cell.
- Package transform remains derived only from its authoritative output cell until authoritative state moves or removes it.
- Hamster/grub interaction colliders remain triggers and raycastable; they do not physically push Rigidbody items.

### Campfire and ambient VFX

- A campfire keeps authored flame geometry and its realtime light.
- A campfire emits no pooled ParticleSystem, including while Cooking is active.
- `ProductionWorkApplied` particles remain available for non-campfire production buildings only.
- Periodic ambient-dust/sky particle spawning is disabled; no decorative fireworks are emitted above the playable area.
- Excavation, deposit, construction, status and combat effects are unchanged.

## Ownership and persistence

This correction changes Presentation only. Inventory, Ecology, Production, package lifecycle, cell occupancy, navigation, jobs, reservations and save/load remain authoritative in their existing owners. Visual offsets, particles and light instances are not saved.

## Acceptance

- one living-material ID helper is shared by item visual and world-item renderer;
- hidden living-material proxies do not increment cell layout slots;
- Play Mode moves hamster/grub into and out of a package cell without changing package transform;
- existing trigger/raycast pass-through regression remains green;
- `CampfireGlow` creates a light and no effect request;
- campfire `ProductionWorkApplied` effects are filtered;
- runtime creates no periodic `AmbientDust` fact;
- repository build, full tests, source contracts, headless smoke and deterministic soaks pass;
- licensed Unity Play Mode remains required for `VERIFIED`.
