# Campfire BuildingBox content

## Scope

This slice implements issue #329 by adding the campfire as data-driven content on top of the existing authoritative `BuildingsState`, `BuildingBoxPlanState`, `BuildingPackingPlanState`, Inventory and Jobs pipeline.

It deliberately does not introduce another pack/unpack lifecycle owner.

## Stable content ids

- building: `building.campfire`
- packed item: `building_box.campfire`
- item category: `building.box`

The packed item is non-stackable so each box can later preserve building-specific serialized state without ambiguous stack merging.

## Work profile

Assembly and packing each contain three discrete iterations.

- base duration per iteration: 10 in-game minutes;
- Logistics grant per completed iteration: 0.7;
- total Logistics grant for all three iterations: 2.1.

The existing `BuildingDefinition.RequiredWork` and `BuildingBoxPolicy.PackingWork` are both configured to three work units. Later execution work maps one authoritative completed work unit to one iteration and applies worker-specific timing in #334.

## Placement profile

The campfire carries declarative placement metadata:

- 1.5 by 1.5 cell physical footprint;
- flat surface required;
- outdoor-only;
- tunnel placement forbidden.

The current `BuildingDefinition` retains one logical anchor cell because placement validation for the physical footprint belongs to #332. Presentation and input must not treat this profile as already-authoritative placement approval.

## Ownership

- Buildings owns building and packing state.
- Inventory owns the packed box stack and its location.
- Jobs owns worker assignment, execution stages and reservations.
- The content profile is immutable configuration only.
- Unity and UI consume projected state and never mutate these definitions.
