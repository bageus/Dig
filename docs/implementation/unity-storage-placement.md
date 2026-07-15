# Unity stockpile placement

The Unity vertical slice treats the stockpile cell as part of the authoritative
`StorageZoneDefinition`.

## Ownership

- `StorageState` owns the zone definition, cell, capacity, priority, filter and
  incoming reservations.
- `InventoryState` owns stored item quantities.
- `WorldState` determines whether a requested destination cell is open.
- Unity owns only selection, input and rebuildable stockpile visuals.

## Command path

Select an open cell and press `5`.

`MoveStorageZoneHandler` validates the world cell, stored quantity and incoming
reservations before calling `StorageState.MoveZone`. A rejected command changes
none of the three owners.

Relocation is limited to an empty zone without incoming reservations. This
avoids teleporting stored items or invalidating an active hauling destination.

## Controls

- `Space`: pause or resume;
- `.`: one simulation tick while paused;
- `-` / `+`: slower or faster;
- HUD buttons: pause, step, `1x`, `2x`, `4x`;
- `3`: jobs and reservations;
- `4`: navigation routes;
- `5`: place the empty stockpile on the selected open cell.
