# Mining output save data contract

`MiningOutputCommitState` remains the authoritative exactly-once ledger and
`InventoryState` remains the authoritative owner of world stacks and reservations.

`MiningOutputCommitsSaveData` is a serialization DTO only. It stores the versioned
ledger snapshot in stable primitive fields suitable for the existing data-contract
JSON codec:

- exact `X`, `Y`, `Z` cell coordinates;
- terrain/deposit source kind;
- item id and quantity;
- stable stack id when a stack exists;
- empty output commits without a stack.

`MiningOutputSaveDataAdapter` converts only between the DTO and the existing
`MiningOutputCommitSaveSnapshot`. Snapshot constructors continue to own invariant
validation, duplicate-cell rejection and deterministic ordering.

The DTO is wired into the normal production orchestration without creating another
state owner:

- `SaveGameContext.MiningOutputCommits` carries the authoritative ledger reference;
- `SaveGameBuilder.Build(context)` validates Inventory/world integrity and captures it;
- `SaveGameService.Save` and `Autosave` use that normal builder path;
- `SaveGameLoader.Load` restores the ledger and exposes it through
  `LoadedGameState.MiningOutput`;
- `DigTerrainWorkSession` accepts the restored commit state during composition instead
  of always creating a fresh empty ledger.

A missing section from an older document restores as an empty ledger. A malformed,
out-of-bounds or Inventory-mismatched section fails with a typed mining-output save
diagnostic rather than replaying output.

## Deposit snapshot v11

Issue #91 moves `TerrainDepositState` under `WorldState` and raises the save document to version 11. `TerrainDepositsSaveData` now stores its own snapshot format, generator version and per-entry definition version in addition to exact XYZ, reveal/depletion, yield and state version. Migration `save.v10_to_v11.terrain_deposit_contract` assigns legacy definition version 1 and preserves coordinates and quantities.

The completion path resolves and validates deposit identity/yield before mutation. `WorldState.Excavate` then opens terrain, depletes the same instance and reveals six-axis neighbours before Inventory/Job commit. Unity no longer performs a second post-commit depletion mutation.
