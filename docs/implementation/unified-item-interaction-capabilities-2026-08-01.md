# Unified item interaction capabilities — 2026-08-01

Статус: `IMPLEMENTED`; licensed Unity runtime evidence остаётся у [#511](https://github.com/bageus/Dig/issues/511).

Authoritative design: [`../design/item-interaction-capabilities.md`](../design/item-interaction-capabilities.md).

Tracking: [#67](https://github.com/bageus/Dig/issues/67), [#70](https://github.com/bageus/Dig/issues/70), [#387](https://github.com/bageus/Dig/issues/387), [#390](https://github.com/bageus/Dig/issues/390), [#459](https://github.com/bageus/Dig/issues/459).

## Root cause

Item behavior was classified independently in several layers:

- world presenter overrides for individual BuildingBox IDs;
- `ItemId` prefix checks for food/potion/drink;
- separate production-package IDs;
- category/tool checks only in resident inventory;
- different hover and click hit/classification paths;
- live inventory hover facts converted into a compatibility slot before click routing.

This allowed a cursor to advertise one action while the first click reached movement, excavation or a stale compatibility branch. New content sometimes needed another Unity condition even though it was already a valid Inventory item.

## Implementation

- `ItemInteractionProfile` and `ItemFoodUseDefinition` are authoritative `ItemDefinition` data.
- Default profile resolution is category/definition driven: BuildingBox, food, tool/weapon, then generic.
- `InventoryWorldPresenter`, internal-stock projection and resident inventory projection all publish the same profile.
- World hover and click share `TryResolveWorldItemPointerTarget` and one exact stack/action availability decision.
- Generic item routing occurs before building/movement/excavation fallbacks.
- Inventory primary/Alt/C actions read the live layout slot profile directly.
- Food meal nutrition/bites come from `ItemFoodUseDefinition` rather than a grilled-mushroom ID check.
- Production packages use explicit content-owned profiles; unfinished package remains noninteractive.
- `weapon.club` inherits tool behavior without an item-specific input branch.

## Regression coverage

- Domain/Presentation matrix for generic, BuildingBox, food, club and production package;
- router tests for ordinary pickup, BuildingBox Alt gating and direct use;
- source contract forbids `StartsWith("food.")`, per-item interaction override dictionaries and split hover/click item resolvers;
- inventory tests cover LMB placement and exact `C + LMB` quick drop from the live slot;
- checked-in Play Mode coverage exercises generic pickup, food use and inventory placement/drop.

## Verification boundary

Repository Quality, Release build/tests, headless smoke and deterministic soaks can prove compile/contracts and engine-independent behavior. Runtime status is not `VERIFIED` until licensed Unity EditMode/PlayMode executes the checked-in one-click interaction matrix and retains XML/log evidence under #511.
