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

This slice intentionally does not create another save coordinator, Inventory owner,
or mining output restore path. Wiring the DTO into `SaveGameDocument`, migrations,
and aggregate loader/builder orchestration remains the next #94 slice.
