# Campfire cooking and direct food use implementation

Status: `IMPLEMENTATION IN PROGRESS`

Authoritative design: [`../design/campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md)

Tracking issue: [#459](https://github.com/bageus/Dig/issues/459)

Implementation PR: [#464](https://github.com/bageus/Dig/pull/464)

## Implemented ownership

- `CampfireProductionContent` owns stable recipe/item/category IDs and the `1 cap -> 2 grilled mushrooms` content definition.
- `ProductionStepTiming` remains the only duration calculator. Unity now loads the real `15 * 60` base duration instead of the one-tick demo value.
- `BuildingSupplyState` and `BuildingSupplyPlanner` remain the only automatic material reservation owners.
- Mushroom chop completion now publishes cap/leg drops into the shared production inventory, so ordinary BuildingSupply can reserve those world stacks.
- `ProductionOutputPlacement` resolves deterministic perimeter rings around the workstation in front-first order.
- `WorldItemPickupJobDefinition` remains the approach/pickup job. Direct eating adds only a post-pickup intent keyed by the same job ID.
- `AgentState` owns the active three-bite meal and exposes it through the existing `AgentIntentKind.Eat` action for status and animation.

## Runtime workflow

`SynchronizeBuildingProduction` executes the following sequence:

1. register completed workstations;
2. prepare a production order when internal inputs exist;
3. assign available production jobs;
4. create ordinary protected supply jobs for visible/reachable world material;
5. when a queued grilled-mushroom order still lacks a cap and no eligible cap exists, create one ordinary chop job for a visible/reachable Large mushroom.

The chop job produces ordinary world drops. The next synchronization tick re-enters step 4, delivery moves the cap to the campfire internal stock, and the next preparation pass creates the production job. No code reads another building internal stock or another resident inventory as an automatic source.

The assigned production worker remains the actor through `Finalize`. Completion creates one world stack with quantity two in the first valid front-first surrounding cell. A blocked ring expands deterministically; a fully blocked search returns `production.output_space_unavailable` without a second input commit.

## Direct use

- A grilled mushroom world stack is classified as `ContextWorldTargetKind.FoodItem`.
- Plain hover/LMB uses the existing animated pickup arrow and `PickupWorldItem` command.
- Alt hover uses the animated green mouth and `EatWorldItem` command.
- `EatWorldItem` validates resident capacity and creates the same pickup job with `eatAfterPickup: true`.
- Successful pickup starts `StartResidentFoodMealCommand` for the exact carried stack.
- Meal start consumes one portion, then three ticks apply `500` Nutrition units each.
- A later direct command clears pending pickup/eat intent and interrupts an active meal. Completed bites remain applied; the consumed remainder is not restored.

## Files

Domain/Application:

- `src/Dig.Domain/Content/CampfireProductionContent.cs`;
- `src/Dig.Domain/Production/ProductionOutputPlacement.cs`;
- `src/Dig.Domain/Agents/AgentState.FoodMeals.cs`;
- `src/Dig.Application/Agents/ResidentFoodMealUseCases.cs`;
- `src/Dig.Application/Agents/AgentAutonomySystem.cs`.

Presentation/Unity:

- `src/Dig.Presentation.Abstractions/Input/ContextInputModels.cs`;
- `src/Dig.Presentation.Abstractions/Input/ContextInputRouter.World.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigBuildingProductionFoodDependencies.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigBuildingProductionSynchronization.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.Mushrooms.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.FoodItems.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.FoodCursorTextures.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.WorldFood.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldItemPickupSession.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldItemPickupExecution.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.DirectCommands.cs`.

## Automated evidence

- `CampfireProductionContentTests` verifies recipe quantities, food category and exact 15-minute Cooking percentage timing.
- `ProductionOutputPlacementTests` verifies orientation, front-first lateral order and expansion to the next ring.
- `CampfireFoodInputRouterTests` verifies plain pickup and Alt pickup-then-eat commands.
- `ResidentFoodMealTests` verifies one consumed portion, three exactly-once bites, interruption and unsupported-item rejection.
- `CampfireFoodUnityRuntimeContractTests` guards the runtime composition, cursor branches, shared inventory and post-pickup meal wiring.

## Remaining verification

- GitHub Quality must pass for the final branch head.
- Unity Play Mode must execute the two full scenarios from the authoritative acceptance section, including repeated use, full output ring and cancellation.
- Active meal and post-pickup intent save/load restoration is not yet implemented because the global agent/runtime save owner remains `DRAFT`; #459 stays open and the PR remains draft until that acceptance point is either implemented or explicitly split under #13.

## Unity merge/parser regression (2026-07-28)

A post-merge Unity Editor compile exposed conflict damage that repository source-contract gates did not cover: missing method/loop braces in the direct cursor and pointer-hit partials, aliased food/barrel enum values, and a duplicated `DigWorldInteraction.Initialize` argument sequence that omitted the authoritative barrel renderer dependency from the receiver.

The correction restores compile-safe partial boundaries, preserves the legacy barrel/sword numeric identifiers while assigning distinct food/eat identifiers, wires `DigBarrelRenderer` exactly once through bootstrap and interaction initialization, and reinstates regression checks for balanced braces, exact method boundaries, unique input identities and one composition argument list. No approved gameplay rule changes. Unity Editor/Test Runner execution remains required before `VERIFIED`.
