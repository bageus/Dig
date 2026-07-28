> **Audit status (2026-07-28): resident runtime and active food meal slice implemented.** Save format v9 now captures current resident needs, last needs tick and active three-bite meal progress. Pickup-then-use intent is persisted in the pickup job codec. The broader save system remains `DRAFT` until the complete Unity save/load composition and Play Mode evidence tracked by #13/#15/#94 are present.

# Save, load and migrations

## State ownership

Saving coordinates snapshots; it does not become a second owner of gameplay state.

The implemented slices persist:

- `WorldState` cells, designations, exploration, damage, temperature and chunk versions;
- `InventoryState` stacks, stable item/content ids, locations and quantity reservations;
- `JobSystem` definitions, lifecycle state, assigned workers, retry metadata and the complete reservation ledger;
- slot metadata, world seed, generator version and simulation tick;
- mining-output exactly-once commit ledger, including empty commits and stable output stack ids;
- resident skill progression and authoritative positions;
- resident current needs, last needs tick and optional active food meal identity/progress;
- production, building supply, mushroom, barrel and packable-building runtime sections.

Meshes, colliders, navigation results, UI selection, cursor state and animation frame state remain rebuildable projections and are deliberately absent from the document.

## Format and deterministic serialization

`SaveGameDocument` has an explicit `FormatVersion`. The current version is 9 and uses data-contract DTOs rather than serializing aggregates or private runtime fields.

Relevant sequential additions:

- v4: separately owned `AgentSkills` section described by [`ADR-0002`](../adr/0002-save-v4-agent-skill-progression.md);
- v5: authoritative XYZ coordinates and legacy 2D-to-`Z=0` migration;
- v6: mushroom runtime state;
- v7: building production/internal supply and barrels;
- v8: excavation progress;
- v9: `AgentRuntime` needs and active food meal progress.

`SaveGameBuilder` sorts every unordered collection before serialization:

- chunks and cells by logical coordinates;
- item stacks and item reservations by stable entity id;
- jobs by stable job id;
- job reservations by job id and typed reservation key;
- job codec properties and dependencies by ordinal stable id;
- residents and their skills/runtime snapshots by stable agent id;
- applied source keys and migration steps by ordinal value.

The same authoritative snapshot therefore produces the same UTF-8 JSON bytes. Entity ids and content ids are serialized only through their stable textual forms. Display names are metadata and are never used as identity keys.

## Restore path

Each state owner exposes a restore factory or validated runtime restore method:

- `WorldState.Restore` validates complete chunk/cell coverage, chunk ownership, material ids and versions;
- `InventoryState.Restore` validates item ids, stack limits, locations and quantity reservations;
- `JobState.Restore` validates status, stage, assigned worker, retry and reason invariants;
- `JobSystem.Restore` validates job references and restores every reservation with its original acquired tick;
- `AgentState.RestoreRuntime` restores needs and active meal counters without replaying need effects.

`SaveGameLoader` applies format and skill-precision migrations first, reconstructs authoritative owners, validates cross-system references and decodes resident runtime snapshots. An Inventory reservation must point to an existing non-terminal Job.

`SaveGameService.Load(..., IAgentRepository)` validates every referenced resident before mutation, then restores skill progression, runtime needs/meal state and authoritative position. Active meal restoration rebuilds the existing `Eat` action and completed-bite counters. It does not consume another Inventory portion or apply already-completed Nutrition again.

Restore does not publish gameplay events. Loading recreates a confirmed state; it does not replay commands or side effects.

`LoadedGameState.MiningOutput` exposes the restored ledger and its integrity report. `LoadedGameState.AgentRuntime` exposes validated runtime snapshots for live-agent restoration.

Derived caches are rebuilt outside the save document from restored authoritative snapshots. Old navigation paths or presentation objects cannot be applied merely because they existed before saving.

## Extensible job definitions

Job definitions use `JobDefinitionSaveRegistry` and stable codec ids. A saved job type without a registered codec returns `save.job_type.unknown`. New job kinds add their own codec rather than type-name reflection or a generic mutable property bag.

`WorldItemPickupJobSaveCodec` stores `completion_action`. Older pickup payloads without that property decode as `None`; `UseConsumable` survives save/load and starts the shared resident consumable action only after successful pickup commit.

## Migrations

`SaveMigrationPipeline` applies exactly one ordered step per version. A migration declares a stable id, source version and next version. Missing steps and future versions return `save.version.unsupported`.

The retained `save-v0.json` fixture verifies the complete sequential migration through v9 and idempotent replay. `save-v3.json` verifies that later sections are added without inventing resident progression or active meals. `save.v8_to_v9.agent_runtime` adds an empty runtime section, so older saves load with no active meal.

A separate precision-v0 fixture verifies integer largest-remainder conversion, capacity scaling and migration diagnostics. Values and capacity use the same rational scale; migration rejects documents whose converted value sum would exceed converted capacity rather than silently inflating capacity.

Future format changes must add another fixture and a sequential migration; existing fixtures remain immutable.

## Slots and atomic writes

`FileSaveSlotStore` accepts only safe stable slot ids. Manual slots and the reserved `autosave` slot use the same document and validation path.

Writing follows this sequence:

1. serialize to a temporary file;
2. flush the temporary bytes to disk;
3. move the previous complete slot to a backup;
4. move the temporary file into the target path;
5. remove the backup only after commit.

If replacement fails, the previous slot is restored. If the process stops between moves, the next load recovers the backup and removes the incomplete temporary file.

Corrupted files remain visible in slot listings through `SaveSlotInfo.IsCorrupted`; loading throws a typed `SaveStorageException` with `Corrupted`, `Missing`, `InvalidSlotId` or `IoFailure`.

## Validation

Automated coverage includes:

- deterministic document bytes after round-trip;
- world, inventory, jobs and reservation equality;
- continuing active jobs after load;
- manual and autosave slots;
- overwrite and interrupted-replacement recovery;
- corrupted slot diagnostics;
- unknown item and job ids;
- dangling inventory-to-job references;
- future-version rejection;
- complete v0/v3-to-v9 migration chains;
- skill capacity, report and source-key round trip;
- deterministic precision migration and exact capacity preservation;
- service-level load into a live agent repository;
- mining-output commit round trip and integrity failures;
- active meal save after a completed bite, exact needs restoration and completion of only remaining bites;
- pickup completion-action round trip and backward-compatible missing-property decode.

The normal Quality workflow runs architecture/source-contract checks, Release build, .NET tests, headless smoke and both deterministic soak profiles. Unity Play Mode remains non-blocking when activation is unavailable; that evidence gap is tracked by #15.
