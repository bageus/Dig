# Campfire cooking and direct food use implementation

Status: `IMPLEMENTED`

Authoritative design: [`../design/campfire-cooking-and-food-use.md`](../design/campfire-cooking-and-food-use.md)

Tracking issue: [#459](https://github.com/bageus/Dig/issues/459)

Implementation PRs: [#464](https://github.com/bageus/Dig/pull/464), [#485](https://github.com/bageus/Dig/pull/485)

## Implemented ownership

- `CampfireProductionContent` owns stable recipe/item/category IDs and the `1 cap -> 2 grilled mushrooms` content definition.
- `ProductionStepTiming` remains the only duration calculator. Unity loads the real `15 * 60` base duration instead of the one-tick demo value.
- `BuildingSupplyState` and `BuildingSupplyPlanner` remain the only automatic material reservation owners.
- Mushroom chop completion publishes cap/leg drops into the shared production inventory, so ordinary BuildingSupply can reserve those world stacks.
- `ProductionOutputPlacement.ResolveMany` resolves distinct deterministic cells in the generic right finished-output zone.
- `WorldItemPickupJobDefinition.CompletionAction` owns pickup-only versus pickup-then-use behavior and is persisted by `WorldItemPickupJobSaveCodec`.
- `AgentState` owns the active three-bite meal and exposes it through the existing `AgentIntentKind.Eat` action for status and animation.
- Save format v9 owns resident needs, last needs tick, active meal identity, original start tick and completed bite count.

## Runtime workflow

`SynchronizeBuildingProduction` executes the following sequence:

1. register completed workstations;
2. prepare a production order when internal inputs exist;
3. assign available production jobs;
4. create ordinary protected supply jobs for visible/reachable world material;
5. when a queued grilled-mushroom order still lacks a cap and no eligible cap exists, create one ordinary chop job and one source-unresolved dependent delivery job in the same synchronization pass.

The dependent delivery keeps one stable job id and remains `Created` until the chop completes. After cap drops appear as ordinary world items, the existing BuildingSupply planner binds a revealed/reachable/unreserved cap, reserves incoming capacity and resident slots, then executes the ordinary delivery into campfire internal stock. No code reads another building internal stock or another resident inventory as an automatic source.

The assigned production worker remains the actor through `Finalize`. The product icon owns a no-text full-cell fill overlay only for actual cooking work; it remains full while output placement is pending and clears together with terminal completion/counter decrement. Completion expands the recipe quantity into two distinct quantity-one world entities and atomically resolves two separate cells in the right finished-output zone. If fewer than two cells are available, `production.output_space_unavailable` leaves the order ready without partial output or a second input commit.

## Direct use

- A grilled mushroom world stack is classified as `ContextWorldTargetKind.FoodItem`.
- Plain hover/LMB uses the existing animated pickup arrow and `PickupWorldItem` command.
- Alt hover uses the animated green mouth and `EatWorldItem` command.
- `EatWorldItem` validates resident capacity and creates the same pickup job with `CompletionAction = UseConsumable`.
- Successful pickup starts `StartResidentFoodMealCommand` for the exact carried stack or deterministic split destination stack.
- Meal start consumes one portion, then three ticks apply `500` Nutrition units each.
- A later direct command cancels the pickup job or interrupts an active meal. Completed bites remain applied; the consumed remainder is not restored.

## Save/load

- `completion_action` is encoded in the pickup job payload; legacy payloads default to `None`.
- `AgentRuntimeSaveData` records current needs and optional active food meal.
- `AgentState.RestoreRuntime` rebuilds the Eat action and internal completed-bite counters without applying Nutrition again.
- v8 saves migrate through `save.v8_to_v9.agent_runtime`; older saves receive an empty runtime section.
- `SaveGameService` restores skill progression, runtime meal/needs state and position in a validated order.

## Files

Domain/Application:

- `src/Dig.Domain/Content/CampfireProductionContent.cs`;
- `src/Dig.Domain/Production/ProductionOutputPlacement.cs`;
- `src/Dig.Domain/Jobs/WorldItemPickupJobDefinition.cs`;
- `src/Dig.Domain/Agents/AgentState.FoodMeals.cs`;
- `src/Dig.Domain/Agents/AgentState.RuntimeRestore.cs`;
- `src/Dig.Application/Agents/ResidentFoodMealUseCases.cs`;
- `src/Dig.Application/Inventory/WorldItemPickupContracts.cs`;
- `src/Dig.Application/Inventory/WorldItemPickupHandlers.cs`;
- `src/Dig.Application/Saving/AgentRuntimeSaveData.cs`;
- `src/Dig.Application/Saving/SaveGameBuilder.AgentRuntime.cs`;
- `src/Dig.Application/Saving/SaveGameLoader.AgentRuntime.cs`;
- `src/Dig.Application/Saving/SaveMigrations.AgentRuntime.cs`;
- `src/Dig.Application/Saving/WorldItemPickupJobSaveCodec.cs`.

Presentation/Unity:

- `src/Dig.Presentation.Abstractions/Input/ContextInputRouter.World.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigBuildingProductionFoodDependencies.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigBuildingProductionSynchronization.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.Mushrooms.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.FoodCursorTextures.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldInteraction.WorldFood.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldItemPickupSession.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldItemPickupExecution.cs`;
- `unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.DirectCommands.cs`.

## Automated evidence

- `CampfireProductionContentTests` verifies recipe quantities, food category and exact 15-minute Cooking percentage timing.
- `ProductionOutputPlacementTests` verifies right-only deterministic ordering, multi-cell resolution and atomic failure when the full output quantity cannot fit.
- `CampfireFoodInputRouterTests` verifies plain pickup and Alt pickup-then-eat commands.
- `ResidentFoodMealTests` verifies one consumed portion, three exactly-once bites, interruption and runtime restoration after a completed bite.
- `CampfireFoodSaveTests` verifies save/load of active meal progress and backward-compatible pickup job decoding.
- `SaveMigrationAndCorruptionTests`, `MushroomSaveRoundTripTests` and `BarrelSaveRoundTripTests` verify the complete v9 migration chain.
- `CampfireFoodUnityRuntimeContractTests` guards runtime composition, cursor branches, persisted pickup action, shared meal wiring and the Play Mode harness/Application command signature boundary.
- `CampfireFoodWorkflowPlayModeTests` implements the missing-cap dependency and pickup-to-three-bites runtime scenarios.
- `CampfireFoodCompletionPlayModeTests` implements two-cell quantity-one output, fully blocked right-zone retry without partial commit, repeated production and cancellation of pickup-then-use without losing food or reservations.
- `CampfireFoodProductionPlayModeHarness` composes completed campfire, production, inventory, jobs and resident owners for deterministic runtime tests without duplicating gameplay logic.

## Unity harness compile regression (2026-07-28)

Unity Editor reported `CS1739` in `CampfireFoodProductionPlayModeHarness.cs`: the harness called `ApplyProductionWorkCommand` with the obsolete named argument `elapsedTicks`, while the authoritative Application contract exposes `baseWork`. The correction changes only the test harness call to `baseWork: 1`; production timing, work calculation and gameplay behavior are unchanged. A source-contract regression now requires `int baseWork` in `ProductionContracts.cs`, `baseWork: 1` in the Play Mode harness and rejects `elapsedTicks:` in that harness.

## Verification status

GitHub Quality passes architecture checks, source contracts, build, all .NET tests, headless smoke and standard/large deterministic soak. Both Stage 2 export workflows pass.

The hosted Unity workflow currently skips `Run Play Mode tests` when Unity activation is unavailable. The executable Play Mode regressions are present, but no runtime result XML was produced for this branch; therefore the system remains `IMPLEMENTED`, not `VERIFIED`.