# Resident basket surface pickup and Cargo fallback — 2026-07-29

Status: `IMPLEMENTED` in repository code and checked-in regressions. Actual Unity Play Mode execution remains owned by [#511](https://github.com/bageus/Dig/issues/511).

Authoritative specification: [`../design/resident-inventory-expansion.md`](../design/resident-inventory-expansion.md).

Tracking: [#67](https://github.com/bageus/Dig/issues/67), [#68](https://github.com/bageus/Dig/issues/68), [#69](https://github.com/bageus/Dig/issues/69), [#70](https://github.com/bageus/Dig/issues/70).

## Reported workflow

- ordinary basket is a pickable surface item;
- after pickup it occupies one Main slot and exposes four Cargo UI slots;
- ordinary items use Cargo only after Main capacity is exhausted;
- non-empty Cargo shows a basket-backpack behind the resident and applies the configured movement multiplier;
- dropping the active basket spills Cargo, removes the extension, hides the attachment and restores movement speed;
- a distinct large basket is also present on the surface and exposes six Cargo slots when picked up.

## Existing foundation reused

No new inventory owner was introduced.

- `InventoryState` remains authoritative for stacks, locations, reservations, active expansion selection, speed and spill;
- `ResidentInventoryLayoutPresenter` projects the existing 6 Main plus active Cargo/Weapon slots;
- `ResidentInventoryAttachmentPresenter` already suppresses Cargo attachments while Cargo is empty;
- `DropResidentStackWithSpill` already performs quantity-safe transactional spill;
- world pickup continues through `CreateWorldItemPickupHandler` and `CompleteWorldItemPickupHandler`.

## Root causes corrected

### Destination priority

`ResidentInventorySlotClaims.BuildClaimCapacities` previously ranked every compatible merge before empty slots and ranked Cargo/Weapon empty slots before Main. A partial Cargo stack could therefore receive ordinary pickup or hauling while a free Main slot still existed.

The shared resolver now uses:

```text
ordinary item: Main merge -> Main empty -> Cargo merge -> Cargo empty
weapon item:   Weapon merge -> Weapon empty -> Main merge -> Main empty
expansion:     Main only
```

The same capacity resolver is used by direct world pickup, hauling, building supply and restored slot claims.

### Demo composition

The demo previously inserted both basket tiers directly into resident Main slots. It now keeps only weapon expansions in the starting inventory and creates:

- `inventory.basket` at surface `residentStartCell.X + 1`;
- `inventory.large_basket` at surface `residentStartCell.X + 2`.

Both are ordinary authoritative world item entities and use the existing pickup workflow.

### Basket presentation

Basket IDs previously fell through to the generic magenta cube when no authored item catalog prefab was assigned. `DigBasketVisualPolicy` now provides a stable basket-shaped procedural fallback for both world items and resident attachments, with distinct normal/large scales, Cargo socket policy and interactive world collider policy.

Presentation visibility still comes only from `ResidentInventoryAttachmentPresenter`: no Cargo stack means no attachment model; the first Cargo stack creates one, and spill/drop removes it on the next refresh.

## Regression coverage

- Main-first and filled-Main-to-Cargo slot claims;
- actual world pickup of basket into Main;
- four projected Cargo slots after pickup;
- ordinary world pickup falling back to Cargo only after all Main slots are filled;
- occupied Cargo movement multiplier and attachment model;
- active basket drop/spill removing capacity, attachment and speed penalty without quantity loss;
- both basket tiers remaining separate surface entities;
- source contracts for demo spawn and procedural basket integration;
- checked-in Unity Play Mode scenario for rear attachment creation, basket hierarchy, disabled child colliders and hide refresh.

## Verification boundary

Repository quality/source-contract checks can validate code shape and .NET behavior. The checked-in `BasketInventoryLifecyclePlayModeTests` must execute in a licensed Unity Test Runner before the runtime visual workflow can be called `VERIFIED`.
