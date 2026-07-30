# Mining output and terrain profile implementation

Status: `IMPLEMENTED` after merge of the linked PR; licensed Unity execution is still required for `VERIFIED`.

Tracking: #87, #92, #94, #109, #110.

## Authoritative owners

- `MaterialCatalog` and `TerrainOutputProfile` own typed terrain definitions and allowed raw outputs.
- `TerrainOutputResolver` owns deterministic per-entry rolls from world seed, generator version, exact XYZ, profile version and stable ItemId.
- `CompleteTerrainWorkCommandHandler` owns the authoritative excavation/output commit.
- `InventoryState` owns every produced quantity-one world entity.
- `MiningOutputCommitState` owns exactly-once output history by cell.
- World/Deposit/Inventory/Jobs remain the existing state owners; Unity only resolves a plan and sends the command.

## Terrain catalog

`DefaultTerrainMaterials` contains six stable definitions:

- `terrain.sand`;
- `terrain.stone_rock`;
- `terrain.metal_bearing_rock` with display name `Рудная порода`;
- `terrain.crystalline_rock`;
- `terrain.lava_rock`;
- `terrain.unmineable`.

Profiles reference only `material.stone`, `material.coal`, `ore.iron`, `ore.gold` and `ore.crystal`. The numeric probabilities, ranges and hardness values are versioned content fixtures retained from the existing catalog; they do not resolve Q-014 balance.

Independent entry streams allow deterministic multi-output while preserving empty results. `terrain.unmineable` has no output profile and cannot be excavated.

## Atomic completion

The completion command carries the resolved source id/version, optional deposit identity/yield, all output lines and one deterministic base entity id. Preflight derives the complete quantity-one id set and rejects unknown items, duplicate/existing ids, stale deposits and duplicate cell commits before World mutation.

On success the handler:

1. opens the exact XYZ terrain cell and depletes a matching deposit when present;
2. creates each output unit at `ItemLocation.InWorld(targetCell)`;
3. completes the Job;
4. records all output lines and entity ids in the shared ledger;
5. saves/publishes the existing World, Inventory and Job owners.

Deposit output is exclusive and never executes the terrain table. Empty terrain output still records a cell commit, preventing reroll after retry or load. The miner inventory and held item are not changed. Quantity-one output identities are preserved through hauling ingress and storage deposit, so the ledger remains valid after delivery. Legacy aggregate storage paths remain compatibility-only.

## Save format v12

Mining-output section v2 stores:

- exact XYZ and source kind;
- stable terrain-profile/deposit source id and version;
- ordered output lines;
- item id, quantity and stable entity ids for every line;
- empty commits without output lines.

`save.v11_to_v12.terrain_output_contract` upgrades legacy single-output ledger records and maps `material.metal` to `material.iron` across saved Inventory, resident slot claims, job properties, production allocations/stocks, active meals, barrels and mining-output entries. Replay is idempotent.

## Diagnostics and coverage

Coverage includes the six terrain definitions, allowed outputs, sand empty output, stone-only output, deterministic multi-output and exact-Z changes, catalog validation, deposit exclusivity, duplicate/id-conflict rollback, quantity-one world placement, ledger capture/restore, v1-to-v2 section compatibility, save v12 migration and Unity composition/source contracts.

The checked-in Play Mode scenario verifies that the demo composes the common six-type catalog. Actual licensed EditMode/PlayMode execution is still the boundary for `VERIFIED`.

## Hauling boundary

A storage zone is a valid demand only through its explicit `StorageFilter`; the demo stockpile uses an ItemId whitelist. General building-demand, current-fog eligibility, retry and demand reservations remain the single authoritative scope of #110. This implementation does not create a second hauling planner.
