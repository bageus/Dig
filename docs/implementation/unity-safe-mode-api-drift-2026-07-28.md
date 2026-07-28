# Unity Safe Mode API-drift correction — 2026-07-28

Status: `IMPLEMENTED` pending actual Unity Editor/Test Runner confirmation.

Affected authoritative systems and tracking issues:

- mushroom growth and direct chopping: [`../design/mushroom-growth-and-chopping.md`](../design/mushroom-growth-and-chopping.md), [#423](https://github.com/bageus/Dig/issues/423);
- destructible barrels: [`../design/destructible-barrels.md`](../design/destructible-barrels.md), [#443](https://github.com/bageus/Dig/issues/443);
- world-item pickup/context input: [`../design/world-item-gravity-selection-and-pickup.md`](../design/world-item-gravity-selection-and-pickup.md), [#390](https://github.com/bageus/Dig/issues/390);
- campfire food use: [`../design/campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md), [#459](https://github.com/bageus/Dig/issues/459).

## Runtime report

Unity Safe Mode exposed ten compile errors that repository Release builds did not catch because Unity runtime partials are compiled by the Unity Editor rather than the ordinary `.NET` solution build.

Root causes:

- barrel navigation referenced `TerrainWorkRoutePlan` without importing its authoritative `Dig.Application.Jobs` namespace;
- mushroom runtime still referenced removed result/error names and the obsolete four-argument completion command;
- direct pickup contained two local `sequence` declarations and used removed `ItemStackSnapshot.Id` members instead of `StackId`;
- the food cursor referenced a removed `NewCursorPixels` helper.

## Correction

- `DigTerrainWorkSession.BarrelNavigation.cs` imports `Dig.Application.Jobs` and keeps the existing route diagnostics owner;
- `DigTerrainWorkSession.Mushrooms.cs` uses `MushroomErrors.NotFound`, `Result<bool>`, `MushroomChopCompletionResult`, and the current three-argument `CompleteMushroomChopCommand`;
- `DigWorldItemPickupSession.cs` allocates one deterministic sequence per accepted command and uses `ItemStackSnapshot.StackId`;
- `DigWorldInteraction.FoodCursorTextures.cs` allocates its buffer from `CommandCursorSize`;
- `UnitySafeModeApiDriftContractTests` binds these Unity source usages to the current Domain/Application API surface.

No approved growth, chopping, drops, barrel movement, pickup quantity, internal-stock protection, cursor behavior, or eating behavior changed.

## Verification boundary

Repository quality/source-contract checks and the full `.NET` suite must pass on the PR head. Actual Unity compilation and Play Mode remain required before any affected system can be raised to `VERIFIED`.
