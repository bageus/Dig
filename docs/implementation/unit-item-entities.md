# Unit item entities

## Invariant

Every physical item in the world or in a resident inventory is represented by one Inventory entity with quantity `1`.

`InventoryState` remains the authoritative owner of identity, quantity, location and reservations. Presentation must not expand an aggregate stack into visual-only items.

## Creation API

`AddUnit` is the canonical single-item creation path.

`AddUnits` accepts caller-provided stable ids and atomically creates one quantity-1 entity per id. It prevalidates the complete id set before mutation, so duplicate or existing ids cannot partially create a batch.

World batches may contain multiple units at one logical cell because each unit remains independently identifiable and reservable. Resident inventory creation is restricted to one unit per operation so slot validation remains authoritative.

## Migration sequence

This slice intentionally does not remove the legacy quantity-stack API yet. Existing production and save/load code still creates aggregate stacks and must be migrated without quantity loss.

The remaining sequence is:

1. replace resource and demo creation with deterministic `AddUnits` calls;
2. make pickup, drop and hauling move one entity per job;
3. split legacy world and resident quantities during save loading;
4. reject quantity greater than one at World and AgentInventory locations;
5. remove quantity badges from resident and world presentation.

Storage and building inventories may continue to aggregate until their own migration is complete, but they may not be rendered as one physical item in the world or a resident slot.
