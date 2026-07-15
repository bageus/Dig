# Save, load and migrations

## State ownership

Saving coordinates snapshots; it does not become a second owner of gameplay state.

The first vertical slice persists:

- `WorldState` cells, designations, exploration, damage, temperature and chunk versions;
- `InventoryState` stacks, stable item/content ids, locations and quantity reservations;
- `JobSystem` definitions, lifecycle state, assigned workers, retry metadata and the complete reservation ledger;
- slot metadata, world seed, generator version and simulation tick.

Meshes, colliders, navigation results, UI selection, animation state and other rebuildable projections are deliberately absent from the document.

## Format and deterministic serialization

`SaveGameDocument` has an explicit `FormatVersion`. Version 1 uses data-contract DTOs rather than serializing aggregates or private runtime fields.

`SaveGameBuilder` sorts every unordered collection before serialization:

- chunks and cells by logical coordinates;
- item stacks and item reservations by stable entity id;
- jobs by stable job id;
- job reservations by job id and typed reservation key;
- job codec properties and dependencies by ordinal stable id.

The same authoritative snapshot therefore produces the same UTF-8 JSON bytes.

Entity ids and content ids are serialized only through their stable textual forms. Display names are metadata and are never used as identity keys.

## Restore path

Each state owner exposes a restore factory:

- `WorldState.Restore` validates complete chunk/cell coverage, chunk ownership, material ids and versions;
- `InventoryState.Restore` validates item ids, stack limits, locations and quantity reservations;
- `JobState.Restore` validates status, stage, assigned worker, retry and reason invariants;
- `JobSystem.Restore` validates job references and restores every reservation with its original acquired tick.

`SaveGameLoader` applies migrations first, reconstructs the authoritative owners, then validates cross-system references. An Inventory reservation must point to an existing non-terminal Job.

Restore does not publish gameplay events. Loading recreates a confirmed state; it does not replay commands or side effects.

Derived caches are rebuilt outside the save document from the restored authoritative snapshots. Old navigation paths or presentation objects cannot be applied merely because they existed before saving.

## Extensible job definitions

Job definitions use `JobDefinitionSaveRegistry` and stable codec ids. Version 1 includes `job.dig.v1`.

A saved job type without a registered codec returns `save.job_type.unknown`. New job kinds must add their own codec rather than adding type-name reflection or a generic mutable property bag to Jobs.

## Migrations

`SaveMigrationPipeline` applies exactly one ordered step per version. A migration declares a stable id, source version and next version. Missing steps and future versions return `save.version.unsupported`.

The retained `save-v0.json` fixture verifies the v0 to v1 metadata migration and idempotent replay. Future format changes must add another fixture and a sequential migration; existing fixtures remain immutable.

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
- continuing the same active digging job after load;
- manual and autosave slots;
- overwrite and interrupted-replacement recovery;
- corrupted slot diagnostics;
- unknown item and job ids;
- dangling inventory-to-job references;
- future-version rejection;
- migration of the retained v0 fixture.

The normal Quality workflow still runs architecture and file-size checks, C# compatibility, Release build, all tests, headless smoke and both deterministic soak profiles.