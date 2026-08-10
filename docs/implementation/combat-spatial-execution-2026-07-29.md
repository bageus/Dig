# Combat spatial execution — 2026-07-29

Status: `IMPLEMENTED` after merge of [PR #513](https://github.com/bageus/Dig/pull/513); licensed Unity Play Mode evidence remains required for `VERIFIED`.

Authoritative specification: [`../design/combat-spatial-execution.md`](../design/combat-spatial-execution.md).
Tracking: [#508](https://github.com/bageus/Dig/issues/508), [PR #513](https://github.com/bageus/Dig/pull/513).

## Correction

The foundation in `combat-factions-strategy.md` owned combat intentions, deterministic attack resolution, cooldowns/statuses and tactical decisions, but it had no authoritative spatial execution between `Approach` and `ResolveCombatAttack`. Unity direct combat therefore could not provide a complete approach, engagement, pursuit, retreat and cancellation workflow without inventing Presentation-owned state.

The Stage 4 implementation adds one `CombatState`-owned execution lifecycle above the existing replay-safe attack resolver. Navigation and Movement remain the only route/transition owners; Agents remain the position/Health owner; equipment and Factions remain separate owners; Unity only routes input and presents typed state/events.

## Main contracts

- `src/Dig.Domain/Combat/CombatSpatialValues.cs`;
- `src/Dig.Domain/Combat/CombatSpatialResolvers.cs`;
- `src/Dig.Domain/Combat/CombatState.Executions.cs`;
- `src/Dig.Application/Combat/CombatSpatialExecutionHandler*.cs`;
- `src/Dig.Application/Saving/CombatSaveData.cs`;
- `src/Dig.Application/Saving/CombatSaveAdapter.cs`;
- `src/Dig.Domain/Combat/CombatState.Restore.cs`;
- `Assets/Dig.Unity/Runtime/DigAgentSession.Combat.cs`;
- `Assets/Dig.Unity/Runtime/DigWorldInteraction.Combat.cs`.

## Execution lifecycle

```text
AcquireTarget
-> SelectEquipment
-> SelectEngagementCell
-> Approach
-> FaceTarget
-> WindUp
-> ResolveAttack
-> Recover
-> Reevaluate
   -> re-engage / retarget / retreat / complete
```

`CombatActionId` continues to own attack idempotency. Resolving one hit no longer completes a long-lived attack intent; terminal target-loss, retreat, cancel, replacement, expiry and retry exhaustion are owned by the spatial execution path.

## Spatial policy

- range and threat distance use `CellId(X,Y,Z)` Manhattan distance;
- melee requires an immediate valid Navigation traversal edge;
- ranged attacks require a deterministic World 3D terrain line-of-sight;
- residents/creatures do not block ranged attacks and friendly fire is absent;
- engagement cells use derived soft-claim counts, never hard `ReservationLedger` ownership;
- target sight loss preserves the last known target cell;
- player target loss/death completes the intent, autonomous/alarm execution may retarget the nearest hostile;
- ally attack publishes an alarm stimulus; allies evaluate and create their own intent;
- retreat maximizes minimum threat distance, then prefers own territory, lower route cost and stable `CellId`.

## Unity input and Presentation

- selected resident + hostile hover uses the sword cursor;
- hover and click call the same `CanIssuePlayerAttackOrder` classification path;
- LMB creates a `PlayerOrder Attack` intent after typed excavation interruption;
- RMB cancels active player combat intents before clearing selection;
- fixed-tick resident execution advances combat before ordinary/manual/work movement;
- Unity code does not call `ResolveCombatAttackCommand` directly from pointer or animation callbacks.

## Save/load

Save format `v10` stores combat intents, execution identity/stage, selected target/equipment/engagement cell, last-known target, retry state, resolved action IDs, cooldowns and statuses. Migration `v9 -> v10` adds an empty combat section. Routes, candidate sets, soft claims, LoS projection, interpolation and animations are derived and rebuilt after load.

## Regression coverage

- `CombatSpatialResolverTests` — XYZ distance, LoS, soft claims and retreat ordering;
- `CombatSpatialExecutionTests` — direct approach-to-attack, target death and terrain-blocked ranged execution;
- `CombatSpatialSaveRoundTripTests` — active execution and resolved-action replay across v10 round-trip;
- `CombatSpatialUnityRuntimeContractTests` — sword cursor, common classifier, interruption, RMB cancel and no Unity damage authority;
- `CombatSpatialExecutionPlayModeTests` — executable approach, wind-up, one damage commit and recovery fixture.

The first local Unity compile of the checked-in fixture exposed `CS7036`: the positional `AgentState` overload requires explicit `skills` and `traits` arguments before `initialPosition`. The PlayMode factory now passes `skills: null`, `traits: null`, then the intended cell. `UnityRuntimeEvidenceGateTests` locks this constructor contract so normal Quality CI detects the same source regression even while licensed Unity execution is unavailable.

## Verification boundary

Quality build/tests, headless smoke and deterministic soaks must pass before merge. The Unity fixture is checked in, but status remains below `VERIFIED` until the licensed Unity Test Runner actually executes EditMode/PlayMode tests and publishes results; an activation-gated skipped step is not runtime evidence.
