# Combat, factions and strategic AI

## State ownership

The implementation keeps three independent authoritative owners:

- `CombatState` owns resolved attacks, weapon cooldown facts and active combat statuses;
- `FactionState` owns memberships, diplomacy scores and territory claims;
- `StrategicAiState` owns each AI faction's current strategic goal and next planning tick.

Agents continue to own Health and life/death. Jobs owns healing work and all worker/position reservations. Presentation may animate typed events, but an animation callback cannot resolve an attack or apply damage.

## Factions and territory

Faction content is data-driven through `FactionCatalog` and `FactionDiplomacyPolicy`. Stable faction ids are separate from display names.

Relations use a canonical unordered faction pair and a bounded score. Policy thresholds derive Hostile, Neutral, Friendly and Allied states. Territory is owned per logical cell. Entering another faction's territory publishes `TerritoryViolated` and changes diplomacy through `FactionState`; allied entry is allowed without a penalty.

Membership, relations and territory are available through immutable snapshots in stable order.

## Weapon and combat profiles

`WeaponProfile` defines:

- minimum and maximum range;
- accuracy;
- base damage;
- armor penetration;
- cooldown in simulation ticks;
- optional status-on-hit definition.

`CombatantSnapshot` supplies faction, logical position, alive state, Health and typed attack/defense modifiers. No Unity type or animation state appears in these contracts.

## Deterministic attack resolution

Every attack has a stable `CombatActionId`. Random rolls are derived only from:

```text
WorldSeed + "combat.attack." + CombatActionId
```

Resolution validates living combatants, faction hostility, range and cooldown before rolling. The result records hit chance, rolls, outcome, damage and applied status.

`CombatState` stores the first resolution by action id. Replaying the same request returns the cached result with `WasAlreadyProcessed = true`, publishes no second event and cannot create another status. This prevents visual events, retries or replay from applying damage twice.

## Damage and statuses

`ResolveCombatAttackHandler` reads authoritative Agent and Faction snapshots, resolves the attack, and applies one external Health delta only for a new resolution. The target Agent publishes `AgentExternalEffectApplied` and the existing `AgentDied` fact if Health reaches zero.

Statuses are advanced by simulation tick. `AdvanceCombatStatusesHandler` groups all due status damage by target and applies one Health transaction per target. Repeating the same status tick produces no additional damage.

## Threats and tactical decisions

`CombatThreatDetector` filters living hostile entities within a logical sight range and sorts them by distance, strength and stable id.

`CombatTacticalEvaluator` returns Defend, Approach, Attack or Retreat with a typed reason code. Retreat is selected from data-driven Health and threat-ratio policy rather than animation or frame timing.

## Healing

Healing is not an instantaneous combat side effect. `HealingJobDefinition` is a typed Job with travel, work and finalize stages. It reserves the patient destination and work position through the common Job ledger.

`CompleteHealingJobHandler` restores Health and completes the Job in one validated path. Job completion releases reservations, and a repeated completion command cannot heal the patient again.

## Strategic AI

`StrategicAiState` evaluates candidates for:

- resource development;
- housing development;
- territory expansion;
- defense;
- attack;
- retreat.

Candidates include stable scores and reason codes. The highest score wins with `StrategicGoalKind` as the deterministic tie-break. Evaluation before `NextPlanningTick` returns a skipped report without recalculating the plan or changing state, so strategic AI does not run an expensive full pass every frame.

## Headless validation

The normal headless smoke now executes a hostile deterministic encounter, verifies that replay does not apply damage twice, and confirms that Strategic AI retreats from an overwhelming threat.

Automated tests additionally cover faction territory violations, range/cooldown/hostility rejection, status expiry, tactical threat selection, application damage idempotency, healing reservations and sparse strategic planning.

## Follow-up integration

Content-specific weapon and shield catalogs, combat skill grants, sentry/arsenal buildings, player attack orders and Unity combat visuals build on these contracts. They must consume `CombatAttackResolved` and other typed facts rather than creating a parallel combat state.
