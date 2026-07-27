> **Implementation status: DRAFT.** Unit creation, terrain output and Unity demo/bootstrap creation use physical quantity-1 entities, but the repository-wide pickup/drop/hauling/save migration is incomplete. Tracking issue: [#347](https://github.com/bageus/Dig/issues/347). Audit: [`implemented-systems-audit-2026-07-26.md`](implemented-systems-audit-2026-07-26.md).

# Unit item entities

## Invariant

Every physical item in the world or in a resident inventory is represented by one Inventory entity with quantity `1`.

`InventoryState` remains the authoritative owner of identity, quantity, location and reservations. Presentation must not expand an aggregate stack into visual-only items.

## Creation API

`AddUnit` is the canonical single-item creation path.

`AddUnits` accepts caller-provided stable ids and atomically creates one quantity-1 entity per id. It prevalidates the complete id set before mutation, so duplicate or existing ids cannot partially create a batch.

World batches may contain multiple units at one logical cell because each unit remains independently identifiable and reservable. Resident inventory creation is restricted to one unit per operation so slot validation remains authoritative.

## Completed migration slices

- terrain excavation output creates one deterministic Inventory entity per produced unit;
- Unity demo resident slots create tools and inventory extensions through `AddUnit`;
- the packed demo campfire BuildingBox is created as one world `AddUnit` entity;
- source-contract regression rejects a return to legacy `AddStack` in the demo bootstrap path.

## Remaining migration sequence

The legacy quantity-stack API remains available for aggregate storage and compatibility paths. It cannot be removed until the following work is complete without quantity loss:

1. make pickup, drop and hauling move one entity per job;
2. split legacy world and resident quantities during save loading;
3. reject quantity greater than one at World and AgentInventory locations;
4. remove quantity badges from resident and world presentation;
5. audit remaining production `AddStack` callers and retain it only for explicitly aggregate storage/building inventories.

Storage and building inventories may continue to aggregate until their own migration is complete, but they may not be rendered as one physical item in the world or a resident slot.
