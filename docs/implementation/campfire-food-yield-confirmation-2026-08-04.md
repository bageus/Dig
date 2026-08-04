# Campfire food yield confirmation — 2026-08-04

Status: `IMPLEMENTED`.

Authoritative food specification: [`../design/content/food.md`](../design/content/food.md).  
Related campfire specification: [`../design/campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md).  
Tracking issue: [#459](https://github.com/bageus/Dig/issues/459).

## Confirmed rule

- one grilled-mushroom order consumes exactly `1 material.mushroom_cap` and produces exactly `2 food.grilled_mushroom`;
- one grilled-hamster order consumes exactly `1 creature.hamster` and produces exactly `2 food.roasted_hamster`;
- each order has one Cooking material step for the matching ingredient;
- `skill.cooking` changes processing duration only and cannot change input or output quantity;
- output and progression remain exactly once through the existing production/package lifecycle.

## Current implementation

`CampfireProductionContent.FoodRecipe` already defines one input unit, two output units and one matching Cooking material step. No runtime behavior change was required.

`CampfireFoodYieldContractTests` adds an explicit regression for both recipes so the hamster input quantity cannot drift independently from the existing grilled-mushroom rule.

## Verification boundary

The .NET regression verifies the authoritative content contract. Existing production integration and Play Mode scenarios continue to own physical pickup, workbench staging, package deposit, package close, output materialization, cancellation, retry and save/load.
