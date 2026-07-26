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
