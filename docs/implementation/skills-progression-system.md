# Skills and progression implementation

## Scope

Issues #103–#107 are implemented as one progression path owned by `AgentState`.
Jobs, Production, Construction, Hauling and Combat do not edit skill values. They
preflight a `SkillGrantBundle` before the first mutation. After the authoritative
result succeeds, the mandatory `AgentSkillGrantService` applies the bundle and
publishes `SkillProgressionResultConfirmed` as a fact. The event is not passed
back into the service as a hidden command.

## Domain ownership

- `AgentSkillCatalog` defines exactly seven work and five combat skills.
- Authoritative precision is 100 integer units per displayed point.
- Individual maximum is 10,000 units (100 points).
- `TotalCapacityUnits` starts at 10,000 and can be expanded by University
  progression to 20,000.
- `AgentSkillSet` owns values, applied source keys and the last immutable
  `SkillRedistributionReport`.
- Legacy `general.work`, `mining` and `building` values are mapped to Logistics,
  Stonework and Woodworking by a pure snapshot conversion. A mutating conversion
  occurs only on an explicit progression command.
- `SkillGrantProfileDefinition` carries a stable profile id and immutable grants.
  `SkillProgressionContentCatalog.ValidateAndCreate` rejects missing or duplicate
  definitions before it creates the runtime catalog. The default content defines
  stone extraction, mushroom/wood harvest, cooking, logistics, metallurgy,
  alchemy, services and construction. Job/recipe definitions carry an immutable
  profile and can receive a content-provided override.

## Atomic bundle algorithm

`AgentSkillSet.Grants` aggregates duplicate recipient IDs, clamps every recipient
to its individual maximum, allocates the possible mixed gain proportionally and
excludes every growing recipient from the donor pool. Both recipient gains and
donor losses use integer floor division followed by largest remainder allocation.
Equal remainders use ascending `AgentSkillId`.

The report preserves:

- requested, eligible, applied, free and rejected units per recipient;
- total free-capacity gain and overflow;
- donor value/weight, exact loss, fractional remainder and rounding award;
- values after the transaction;
- source kind, stable source ID, tick, capacity and sums before/after.

An applied key is `{SourceKind}:{SourceId}`. Replaying the bundle returns a
duplicate report without changing values or aggregate version.

## Confirmed result integration

| Result boundary | Source kind | Profile and multiplier |
|---|---|---|
| terrain excavation completed | `JobCompleted` | deposit/content profile; Stonework fallback |
| hauling deposited and completed | `JobCompleted` | Logistics job profile |
| construction committed | `JobCompleted` | Stonework + Woodworking + Logistics bundle |
| building box assembly completed | `JobCompleted` | Stonework + Woodworking + Logistics bundle |
| building box packing completed | `JobCompleted` | Logistics job profile |
| production output committed | `ProductionCommitted` | recipe profile × committed output quantity |
| weapon hit resolved | `CombatHit` | actual `CombatSkillProfile` from weapon |
| shield block resolved | `ShieldDefense` | actual `ShieldSkillProfile` from defender |
| healing job completed | `ServiceCompleted` | Service job profile |

Cancelled, failed, interrupted, missed or uncommitted actions never reach these
boundaries. Weapon and shield results use separate source IDs and can therefore
progress independently in the same encounter. Their diagnostic IDs include the
combat action plus weapon/shield profile. An optional attack intent is validated
before combat mutation and completed after the first successful resolution;
cancelled or expired intents cannot grant progression. Replaying an already
resolved action returns its recorded resolution without revalidating or reapplying
the skill bundle.

Terrain deposit definitions own their extraction profile. The built-in content
maps iron/gold to Metallurgy, crystal/coal to Alchemy and stone to Stonework.
Cells without an active deposit use the Stonework fallback. This mapping is read
when the dig job definition is created rather than inferred in the completion
handler.

`SkillWorkSpeedCurve` converts the configured skill into work efficiency.
Production inputs and outputs remain recipe-owned, so Cooking changes only cycle
duration and cannot change quantity, Nutrition, Mood, quality or bonus results.
`ApplyProductionWorkHandler` derives the curve from the assigned resident; callers
cannot supply a precomputed skill context.

Every expected grant failure is checked before World, Inventory, Jobs, Buildings,
Production or Combat mutates. The current in-memory simulation then executes the
validated coordinator on one thread. A persistent or concurrent adapter still
requires a real unit-of-work boundary across repositories.

## Presentation

`AgentSnapshot.SkillProgression` is the source for both roster top-five and the
expanded inspector. The top-five order is value descending and stable ID
ascending. The Unity inspector renders all 12 values, used/max capacity,
University progress, Stonework thresholds, a dark-blue-to-green value bar and
the last Domain report. Full stable IDs and both displayed points and integer
units are retained in diagnostics, including fractional remainders and rounding
awards. Presentation never recalculates redistribution.
Medium, large and tall cave placements are revalidated against maximum colony
Stonework at 20/40/60 before designation; existing plans are not revoked.

## Save and deterministic replay

Save format v4 adds `AgentSkillsSaveData` with:

- all 12 fixed-point values;
- schema version, precision version and units per point;
- total capacity;
- applied source keys;
- the complete last redistribution report;
- applied migration-step diagnostics.

`SaveGameService.Load` has an agent-repository overload which invokes
`LoadedAgentSkillProgressionRestorer` after document validation. The restorer
preflights every resident before mutating any aggregate. A repeated source
remains a no-op after load. The headless
state hash includes precision, capacity, ordered values and applied source keys.
The v3→v4 migration initializes the new section without inventing progression.
Precision migration scales values and capacity by the same rational factor and
rejects a document whose migrated sum would exceed the migrated capacity.

## Verification coverage

- catalog size and category split;
- free and full capacity;
- proportional donor loss and exact conservation;
- mixed order independence and recipient exclusion;
- deterministic randomized invariant/property cases and a 64-resident grant budget;
- individual clamp, University capacity and documented 100-point example;
- pure legacy snapshots, idempotency after restore and serialized round trip;
- v3 format fixture and precision-v0 largest-remainder migration;
- production quantity multiplier, Cooking speed-only behavior;
- cave Stonework access and assigned-worker Cooking speed;
- excavation, deposit-specific extraction, hauling, construction,
  healing-service and production completion;
- all four offensive combat profiles, weapon switching, repeated hits, shield
  grants, intent interruption and idempotent replay;
- Unity Play Mode test source for all 12 rows, stable IDs, capacity, University,
  thresholds, gradient and deterministic top-five ordering;
- Unity source contract for bootstrap, inspector, gradient and persistence.

The repository quality scripts validate these contracts without Unity. The
Play Mode tests are committed as a Unity Test Framework assembly, but full
`.NET` and Unity runtime execution still requires their respective SDK/editor
environments and is not implied by a static quality pass.
